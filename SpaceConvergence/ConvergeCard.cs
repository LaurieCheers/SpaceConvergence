using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LRCEngine;

namespace SpaceConvergence
{
    public enum ConvergeColor
    {
        Colorless = 0,
        White = 1,
        Blue = 2,
        Black = 4,
        Red = 8,
        Green = 16,
    }

    [Flags]
    public enum ConvergeCardType
    {
        Unit = 1, // creature
        Support = 2, // land
        Device = 4, // artifact
        Tech = 8, // enchantment
        Augment = 16, // Aura
        Planet = 32, // planeswalker
        Home = 64, // player
    };

    public enum ConvergeZoneId
    {
        Hand,
        Home, // battlefield (noncreatures)
        Defense, // battlefield (nonattacking creatures)
        Attack, // battlefield (attacking creatures)
        DiscardPile, // graveyard
        Space, // exile
    };

    public static class ConvergeExtensions
    {
        public static int ToIndex(this ConvergeColor self)
        {
            switch(self)
            {
                case ConvergeColor.Colorless: return 0;
                case ConvergeColor.White: return 1;
                case ConvergeColor.Blue: return 2;
                case ConvergeColor.Black: return 3;
                case ConvergeColor.Red: return 4;
                case ConvergeColor.Green: return 5;
                default: throw new ArgumentException();
            }
        }
    }
    
    public class ConvergeZone
    {
        public ConvergePlayer owner;
        public ConvergeZoneId zoneId;
        public List<ConvergeObject> contents = new List<ConvergeObject>();
        Vector2 basePos;
        Vector2 slotOffset;
        Rectangle region;

        public ConvergeZone(JSONTable template, ConvergePlayer owner, ConvergeZoneId zoneId)
        {
            this.owner = owner;
            this.zoneId = zoneId;
            this.basePos = template.getVector2("basePos");
            this.slotOffset = template.getVector2("slotOffset");
            Vector2 topLeft = template.getVector2("topLeft");
            Vector2 bottomRight = template.getVector2("bottomRight");
            region = new Rectangle(topLeft.ToPoint(), (bottomRight - topLeft).ToPoint());
        }

        public void Add(ConvergeObject newObj)
        {
            if (newObj.zone != null)
                newObj.zone.Remove(newObj);

            newObj.slot = contents.Count;
            newObj.zone = this;
            contents.Add(newObj);
        }

        public void Remove(ConvergeObject oldObj)
        {
            contents.Remove(oldObj);
            oldObj.zone = null;
            oldObj.slot = 0;
        }

        public Vector2 GetNominalPos(int slot)
        {
            return basePos + slotOffset * slot;// + (slot%2==0?new Vector2(10,0):new Vector2(-10,0));
        }
    }

    public class ConvergePlayer
    {
        public ConvergeObject homeBase;
        public ConvergeZone home;
        public ConvergeZone attack;
        public ConvergeZone defense;
        public ConvergeZone hand;
        public int life;
        public int[] resources = new int[6];
        public int[] resourcesSpent = new int[6];

        public ConvergePlayer(JSONTable template, ContentManager Content)
        {
            this.home = new ConvergeZone(template.getJSON("home"), this, ConvergeZoneId.Home);
            this.attack = new ConvergeZone(template.getJSON("attack"), this, ConvergeZoneId.Attack);
            this.defense = new ConvergeZone(template.getJSON("defense"), this, ConvergeZoneId.Defense);
            this.hand = new ConvergeZone(template.getJSON("hand"), this, ConvergeZoneId.Hand);
            this.homeBase = new ConvergeObject(new ConvergeCardSpec(template.getJSON("homebase"), Content), home);
            this.life = template.getInt("startingLife");
        }

        public void UpdateState()
        {
            for (int Idx = 0; Idx < resources.Length; ++Idx)
                resources[Idx] = 0;

            UpdateZone(home);
            UpdateZone(defense);
            UpdateZone(attack);
        }

        void UpdateZone(ConvergeZone zone)
        {
            foreach (ConvergeObject obj in zone.contents)
            {
                if (obj.produces != null)
                {
                    for (int Idx = 0; Idx < resources.Length; ++Idx)
                    {
                        resources[Idx] += obj.produces[Idx];
                    }
                }
            }
        }
    }

    public class ConvergeCardSpec
    {
        public readonly Texture2D art;
        public readonly ConvergeCardType cardType;
        public readonly int power;
        public readonly int maxShields;
        public readonly int[] produces;

        public ConvergeCardSpec(JSONTable template, ContentManager Content)
        {
            art = Content.Load<Texture2D>(template.getString("art"));
            cardType = 0;
            foreach (string name in template.getArray("cardType").asStrings())
            {
                cardType |= (ConvergeCardType)Enum.Parse(typeof(ConvergeCardType), name);
            }
            power = template.getInt("power", 0);
            maxShields = template.getInt("shields", 0);
            JSONArray producesTemplate = template.getArray("produces", null);

            if (producesTemplate != null)
            {
                produces = new int[6];
                foreach (string text in template.getArray("produces", JSONArray.empty).asStrings())
                {
                    ConvergeColor color = ((ConvergeColor)Enum.Parse(typeof(ConvergeColor), text));
                    produces[0]++;
                    produces[color.ToIndex()]++;
                }
            }
        }
    }

    public class ConvergeObject
    {
        ConvergeCardSpec original;
        public Texture2D art { get { return original.art; } }
        public ConvergeCardType cardType { get { return original.cardType; } }
        public int power { get { return original.power; } }
        public int maxShields { get { return original.maxShields; } }
        public int[] produces { get { return original.produces; } }
        public ConvergeZone zone;
        public int slot;

        public int shields;
        public int wounds;

        public ConvergeObject(ConvergeCardSpec original, ConvergeZone zone)
        {
            this.original = original;
            this.shields = maxShields;
            this.wounds = 0;
            zone.Add(this);
        }

        public void UseOn(ConvergeObject target)
        {
            int incomingDamage = target.power;
            int dealtDamage = power;
            TakeDamage(incomingDamage);
            target.TakeDamage(dealtDamage);
        }

        public void Use()
        {
            if(zone.zoneId == ConvergeZoneId.Hand)
            {
                if (this.cardType.HasFlag(ConvergeCardType.Unit))
                    zone.owner.defense.Add(this);
                else
                    zone.owner.home.Add(this);

                zone.owner.UpdateState();
            }
        }

        void TakeDamage(int amount)
        {
            if(shields <= amount)
            {
                wounds += (amount - shields);
                shields = 0;
            }
            else
            {
                shields -= amount;
            }
        }

        public Vector2 nominalPosition
        {
            get { return zone.GetNominalPos(slot); }
        }
    }

    public class ConvergeUIObject : UIElement
    {
        ConvergeObject represented;
        Rectangle gfxFrame;
        bool isMouseOver;
        bool isMousePressing;
        bool isDragging;
        Vector2 mousePressedPos;

        public ConvergeUIObject(ConvergeObject represented)
        {
            this.represented = represented;
            this.gfxFrame = new Rectangle(represented.nominalPosition.ToPoint(), new Point(50,60));
        }

        public override void Update(InputState inputState, Vector2 origin)
        {
            isMouseOver = (inputState.hoveringElement == this);
            if (isMouseOver && inputState.mouseLeft.justPressed)
            {
                isMousePressing = true;
                mousePressedPos = inputState.MousePos;
            }

            if (isMousePressing && isMouseOver && inputState.mouseLeft.justReleased)
                represented.Use(); // clicked

            if (!inputState.mouseLeft.isDown)
                isMousePressing = false;

            float MINDRAG = 5;
            float MINDRAGSQR = MINDRAG * MINDRAG;
            if (isDragging)
            {
                if (isMousePressing)
                {
                    this.gfxFrame = new Rectangle((int)inputState.MousePos.X - 25, (int)inputState.MousePos.Y - 30, 50, 60);
                }
                else
                {
                    //dragged onto = inputState.hoveringElement
                    if (inputState.hoveringElement != null)
                    {
                        if (inputState.hoveringElement is ConvergeUIObject)
                        {
                            represented.UseOn(((ConvergeUIObject)inputState.hoveringElement).represented);
                        }
                    }

                    isDragging = false;
                }
            }
            else
            {
                if (isMousePressing && (inputState.MousePos - mousePressedPos).LengthSquared() > MINDRAGSQR)
                {
                    isDragging = true;
                }

                const float MOVESPEED = 15.0f;
                Vector2 targetPos = represented.nominalPosition;
                Vector2 moveOffset = targetPos - this.gfxFrame.XY();
                if (moveOffset.LengthSquared() < MOVESPEED * MOVESPEED)
                {
                    this.gfxFrame = new Rectangle(targetPos.ToPoint(), new Point(50,60));
                }
                else
                {
                    moveOffset.Normalize();
                    this.gfxFrame = new Rectangle((this.gfxFrame.XY() + moveOffset*MOVESPEED).ToPoint(), new Point(50, 60));
                }
            }
        }

        public override UIMouseResponder GetMouseHover(Vector2 pos)
        {
            if (isDragging)
                return null;

            if (gfxFrame.Contains(pos))
                return this;

            return null;
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 pos)
        {
            Texture2D art = represented.art;
            spriteBatch.Draw(represented.art, new Rectangle(gfxFrame.Center.X - art.Width/2, gfxFrame.Bottom-art.Height, art.Width, art.Height), Color.White);

            if (represented.cardType.HasFlag(ConvergeCardType.Unit))
            {
                if (represented.wounds > 0)
                {
                    spriteBatch.Draw(Game1.woundbg, new Rectangle(gfxFrame.Center.X - 11, gfxFrame.Center.Y, 22, 16), Color.White);
                    spriteBatch.DrawString(Game1.font, "-" + represented.wounds, gfxFrame.Center.ToVector2(), TextAlignment.CENTER, Color.Red);
                }
                else
                {
                    if (represented.power > 0)
                    {
                        spriteBatch.Draw(Game1.powerbg, new Rectangle(gfxFrame.Left + 8, gfxFrame.Bottom, 16, 16), Color.White);
                        spriteBatch.DrawString(Game1.font, "" + represented.power, new Vector2(gfxFrame.Left + 16, gfxFrame.Bottom), TextAlignment.CENTER, Color.Yellow);
                    }
                    if (represented.shields > 0)
                    {
                        spriteBatch.Draw(Game1.shieldbg, new Rectangle(gfxFrame.Right - 24, gfxFrame.Bottom, 16, 16), Color.White);
                        spriteBatch.DrawString(Game1.font, "" + represented.shields, new Vector2(gfxFrame.Right - 16, gfxFrame.Bottom), TextAlignment.CENTER, represented.shields < represented.maxShields ? Color.Red : Color.White);
                    }
                }
            }
            
            if(represented.cardType.HasFlag(ConvergeCardType.Home))
            {
                spriteBatch.DrawString(Game1.font, "" + represented.zone.owner.life, new Vector2(gfxFrame.Center.X, gfxFrame.Top + 20), TextAlignment.CENTER, Color.Black);

                Vector2 resourceDrawPos = new Vector2(gfxFrame.Center.X, gfxFrame.Bottom);
                for (int Idx = 0; Idx < 6; ++Idx)
                {
                    int maxAmount = represented.zone.owner.resources[Idx];
                    int currentAmount = maxAmount - represented.zone.owner.resourcesSpent[Idx];
                    if(currentAmount > 0)
                    {
                        spriteBatch.Draw(Game1.resourceTextures[Idx], resourceDrawPos, Color.White);
                        spriteBatch.DrawString(Game1.font, "" + currentAmount, resourceDrawPos + new Vector2(20,0), Color.Black);
                        resourceDrawPos.Y += 20;
                    }
                }
            }

            if (isMouseOver)
            {
                spriteBatch.Draw(Game1.mouseOverGlow, gfxFrame, Color.White);
            }
        }
    }
}
