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
    [Flags]
    public enum ConvergeCardType
    {
        Unit, // creature
        Power, // land
        Device, // artifact
        Tech, // enchantment
        Augment, // Aura
        Planet, // planeswalker
        Home, // player
    };

    public enum ConvergeZoneId
    {
        Hand,
        Attack, // battlefield
        Home, // battlefield
        DiscardPile, // graveyard
        Space, // exile
    };
    
    
    public class ConvergeZone
    {
        public ConvergePlayer owner;
        public ConvergeZoneId zoneId;
        public List<ConvergeObject> contents = new List<ConvergeObject>();
        Vector2 basePos;
        Vector2 slotOffset;

        public ConvergeZone(JSONTable template, ConvergePlayer owner, ConvergeZoneId zoneId)
        {
            this.owner = owner;
            this.zoneId = zoneId;
            this.basePos = template.getVector2("basePos");
            this.slotOffset = template.getVector2("slotOffset");
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
            return basePos + slotOffset * slot;
        }
    }

    public class ConvergePlayer
    {
        public ConvergeObject homeBase;
        public ConvergeZone home;
        public ConvergeZone attack;
        public ConvergeZone hand;
        public int life;

        public ConvergePlayer(JSONTable template, ContentManager Content)
        {
            this.home = new ConvergeZone(template.getJSON("home"), this, ConvergeZoneId.Home);
            this.attack = new ConvergeZone(template.getJSON("attack"), this, ConvergeZoneId.Attack);
            this.hand = new ConvergeZone(template.getJSON("hand"), this, ConvergeZoneId.Hand);
            this.homeBase = new ConvergeObject(new ConvergeCardSpec(template.getJSON("homebase"), Content), home);
            this.life = template.getInt("startingLife");
        }
    }

    public class ConvergeCardSpec
    {
        public readonly Texture2D art;
        public readonly ConvergeCardType cardType;
        public readonly int power;
        public readonly int maxShields;

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
        }
    }

    public class ConvergeObject
    {
        ConvergeCardSpec original;
        public Texture2D art { get { return original.art; } }
        public ConvergeCardType cardType { get { return original.cardType; } }
        public int power { get { return original.power; } }
        public int maxShields { get { return original.maxShields; } }
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

        public Rectangle nominalPosition
        {
            get { Vector2 pos = zone.GetNominalPos(slot); return new Rectangle((int)pos.X, (int)pos.Y, 50, 60); }
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
            this.gfxFrame = represented.nominalPosition;
        }

        public override void Update(InputState inputState, Vector2 origin)
        {
            isMouseOver = (inputState.hoveringElement == this);
            if (isMouseOver && inputState.mouseLeft.justPressed)
            {
                isMousePressing = true;
                mousePressedPos = inputState.MousePos;
            }

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
                    if(inputState.hoveringElement != null)
                    {
                        if(inputState.hoveringElement is ConvergeUIObject)
                        {
                            represented.UseOn( ((ConvergeUIObject)inputState.hoveringElement).represented );
                        }
                    }

                    this.gfxFrame = represented.nominalPosition;
                    isDragging = false;
                }
            }
            else if(isMousePressing && (inputState.MousePos - mousePressedPos).LengthSquared() > MINDRAGSQR)
            {
                isDragging = true;
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
                        spriteBatch.Draw(Game1.powerbg, new Rectangle(gfxFrame.Left - 8, gfxFrame.Bottom - 10, 16, 16), Color.White);
                        spriteBatch.DrawString(Game1.font, "" + represented.power, new Vector2(gfxFrame.Left, gfxFrame.Bottom - 10), TextAlignment.CENTER, Color.Yellow);
                    }
                    if (represented.shields > 0)
                    {
                        spriteBatch.Draw(Game1.shieldbg, new Rectangle(gfxFrame.Right - 8, gfxFrame.Bottom - 10, 16, 16), Color.White);
                        spriteBatch.DrawString(Game1.font, "" + represented.shields, new Vector2(gfxFrame.Right, gfxFrame.Bottom - 10), TextAlignment.CENTER, represented.shields < represented.maxShields ? Color.Red : Color.White);
                    }
                }
            }
            
            if(represented.cardType.HasFlag(ConvergeCardType.Home))
            {
                spriteBatch.DrawString(Game1.font, "" + represented.zone.owner.life, new Vector2(gfxFrame.Center.X, gfxFrame.Top + 20), TextAlignment.CENTER, Color.Black);
            }

            if (isMouseOver)
            {
                spriteBatch.Draw(Game1.mouseOverGlow, gfxFrame, Color.White);
            }
        }
    }
}
