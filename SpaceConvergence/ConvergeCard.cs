﻿using Microsoft.Xna.Framework;
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
        public Rectangle bounds;

        public ConvergeZone(JSONTable template, ConvergePlayer owner, ConvergeZoneId zoneId)
        {
            this.owner = owner;
            this.zoneId = zoneId;
            this.basePos = template.getVector2("basePos");
            this.slotOffset = template.getVector2("slotOffset");
            Vector2 topLeft = template.getVector2("topLeft");
            Vector2 bottomRight = template.getVector2("bottomRight");
            bounds = new Rectangle(topLeft.ToPoint(), (bottomRight - topLeft).ToPoint());
        }

        public void Add(ConvergeObject newObj)
        {
            if (newObj.zone != null)
                newObj.zone.Remove(newObj);

            newObj.slot = contents.Count;
            newObj.zone = this;
            contents.Add(newObj);
            RenumberAll();
        }

        public void Remove(ConvergeObject oldObj)
        {
            contents.Remove(oldObj);
            oldObj.zone = null;
            oldObj.slot = 0;
            RenumberAll();
        }

        void RenumberAll()
        {
            for(int Idx = 0; Idx < contents.Count; ++Idx )
            {
                contents[Idx].slot = Idx;
            }
        }

        public Vector2 GetNominalPos(int slot)
        {
            return basePos + slotOffset * slot;// + (slot%2==0?new Vector2(10,0):new Vector2(-10,0));
        }

        public void BeginTurn()
        {
            foreach(ConvergeObject obj in contents)
            {
                obj.BeginTurn();
            }
        }

        public void EndTurn()
        {
            foreach (ConvergeObject obj in contents)
            {
                obj.EndTurn();
            }
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
        public int numLandsPlayed;
        public int numLandsPlayable = 1;
        public ConvergeManaAmount resources = new ConvergeManaAmount();
        public ConvergeManaAmount resourcesSpent = new ConvergeManaAmount();
        public ConvergePlayer opponent;

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
            resources.Clear();
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
                    resources.Add(obj.produces);
                }
            }
        }

        public bool CanPayCost(ConvergeManaAmount cost)
        {
            return resourcesSpent.CanSpend(resources, cost);
        }

        public bool TryPayCost(ConvergeManaAmount cost)
        {
            return resourcesSpent.TrySpend(resources, cost);
        }

        public void TakeDamage(int amount)
        {
            life -= amount;
        }

        public void BeginTurn()
        {
            resourcesSpent.Clear();
            numLandsPlayed = 0;
            attack.BeginTurn();
            defense.BeginTurn();
            home.BeginTurn();
        }

        public void EndTurn()
        {
            attack.EndTurn();
            defense.EndTurn();
            home.EndTurn();
        }
    }

    public class ConvergeManaAmount
    {
        int[] amounts;

        public ConvergeManaAmount()
        {
            amounts = new int[6];
        }

        public ConvergeManaAmount(string template)
        {
            amounts = new int[6];
            foreach(char c in template)
            {
                switch (c)
                {
                    case '0': case '1': case '2': case '3': case '4': case '5': case '6': case '7': case '8': case '9':
                        amounts[0] += (c-'0');
                        break;
                    case 'C': amounts[0]++;
                        break;
                    case 'W': amounts[1]++;
                        break;
                    case 'U': amounts[2]++;
                        break;
                    case 'B': amounts[3]++;
                        break;
                    case 'R': amounts[4]++;
                        break;
                    case 'G': amounts[5]++;
                        break;
                    default: throw new ArgumentException();
                }
            }
        }

        public void Clear()
        {
            for (int Idx = 0; Idx < amounts.Length; ++Idx)
                amounts[Idx] = 0;
        }

        public void Add(ConvergeManaAmount other)
        {
            for (int Idx = 0; Idx < amounts.Length; ++Idx)
                amounts[Idx] += other.amounts[Idx];
        }

        public void DrawResources(SpriteBatch spriteBatch, ConvergeManaAmount spent, Vector2 drawPos)
        {
            for (int Idx = 0; Idx < amounts.Length; ++Idx)
            {
                int maxAmount = amounts[Idx];
                int currentAmount = maxAmount - spent.amounts[Idx];
                if (maxAmount > 0)
                {
                    spriteBatch.Draw(Game1.resourceTextures[Idx], drawPos, Color.White);
                    spriteBatch.DrawString(Game1.font, "" + currentAmount, drawPos + new Vector2(20, 0), currentAmount>0? Color.Black: Color.DarkGreen);
                    drawPos.Y += 20;
                }
            }
        }

        public void DrawCost(SpriteBatch spriteBatch, Vector2 drawPos)
        {
            for (int Idx = 0; Idx < amounts.Length; ++Idx)
            {
                for(int amount = amounts[Idx]; amount > 0; --amount)
                {
                    spriteBatch.Draw(Game1.resourceTextures[Idx], drawPos, Color.White);
                    drawPos.X += 18;
                }
            }
        }

        public bool TrySpend(ConvergeManaAmount resources, ConvergeManaAmount cost)
        {
            if (cost == null)
                return true;

            if (!CanSpend(resources, cost))
                return false;

            for (int Idx = 0; Idx < amounts.Length; ++Idx)
            {
                amounts[Idx] += cost.amounts[Idx];
            }

            return true;
        }

        public bool CanSpend(ConvergeManaAmount resources, ConvergeManaAmount cost)
        {
            if (cost == null)
                return true;

            for (int Idx = 0; Idx < amounts.Length; ++Idx)
            {
                if (resources.amounts[Idx] - amounts[Idx] < cost.amounts[Idx])
                    return false;
            }

            return true;
        }
    }

    public class ConvergeCardSpec
    {
        public readonly Texture2D art;
        public readonly ConvergeCardType cardType;
        public readonly int power;
        public readonly int maxShields;
        public readonly ConvergeManaAmount produces;
        public readonly ConvergeManaAmount cost;

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
            string producesTemplate = template.getString("produces", "");
            if (producesTemplate != "")
            {
                produces = new ConvergeManaAmount(template.getString("produces"));
            }

            string costTemplate = template.getString("cost", "");
            if (costTemplate != "")
            {
                cost = new ConvergeManaAmount(costTemplate);
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
        public ConvergeManaAmount produces { get { return original.produces; } }
        public ConvergeManaAmount cost { get { return original.cost; } }
        public ConvergeZone zone;
        public int slot;

        public int shields;
        public int wounds;
        public bool tapped;

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

        public void Play()
        {
            if(zone.zoneId == ConvergeZoneId.Hand)
            {
                if(this.cardType.HasFlag(ConvergeCardType.Support))
                {
                    if(zone.owner.numLandsPlayed < zone.owner.numLandsPlayable)
                    {
                        zone.owner.numLandsPlayed++;
                    }
                    else
                    {
                        return;
                    }
                }

                if (zone.owner.TryPayCost(cost))
                {
                    if (this.cardType.HasFlag(ConvergeCardType.Unit))
                    {
                        zone.owner.defense.Add(this);
                        tapped = true;
                    }
                    else
                    {
                        zone.owner.home.Add(this);
                    }

                    zone.owner.UpdateState();
                }
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

        public bool CanBePlayed()
        {
            if (cost != null && !zone.owner.CanPayCost(cost))
                return false;

            if (cardType.HasFlag(ConvergeCardType.Support) && zone.owner.numLandsPlayed >= zone.owner.numLandsPlayable)
                return false;

            return true;
        }

        public void EnterAttack()
        {
            if(!tapped && cardType.HasFlag(ConvergeCardType.Unit))
                zone.owner.attack.Add(this);
        }

        public void WithdrawAttack()
        {
            zone.owner.defense.Add(this);
            tapped = true;
        }

        public void BeginTurn()
        {
            if(zone.zoneId == ConvergeZoneId.Attack)
                zone.owner.opponent.TakeDamage(power);
            tapped = false;
            shields = maxShields;
        }

        public void EndTurn()
        {
            if(wounds > 0)
            {
                //zone.owner.discardPile.Add(this);
            }
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
                represented.Play(); // clicked

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
                    else
                    {
                        ConvergeZone currentZone = represented.zone;
                        ConvergeZone attackZone = represented.zone.owner.attack;
                        ConvergeZone defenseZone = represented.zone.owner.defense;
                        if (currentZone != attackZone && attackZone.bounds.Contains(inputState.MousePos))
                        {
                            represented.EnterAttack();
                        }
                        else if (currentZone != defenseZone && defenseZone.bounds.Contains(inputState.MousePos))
                        {
                            represented.WithdrawAttack();
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
            spriteBatch.Draw(
                represented.art,
                new Rectangle(gfxFrame.Center.X - art.Width / 2, gfxFrame.Bottom - art.Height, art.Width, art.Height),
                represented.tapped ? new Color(128,128,255) : Color.White
            );

            if(represented.tapped)
            {
                spriteBatch.Draw(Game1.tappedicon, new Vector2(gfxFrame.Right-16, gfxFrame.Top), Color.White);
            }

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

                represented.zone.owner.resources.DrawResources(spriteBatch, represented.zone.owner.resourcesSpent, new Vector2(gfxFrame.Center.X, gfxFrame.Bottom));
            }

            bool drawHighlight = isMouseOver;
            Color highlightColor = Color.White;
            if (represented.zone.zoneId == ConvergeZoneId.Hand)
            {
                if(represented.CanBePlayed())
                {
                    drawHighlight = true;
                    highlightColor = Color.Orange;
                }

                if (isMouseOver && represented.cost != null)
                {
                    represented.cost.DrawCost(spriteBatch, new Vector2(gfxFrame.Left, gfxFrame.Top));
                }
            }

            if (drawHighlight)
            {
                spriteBatch.Draw(Game1.mouseOverGlow, gfxFrame, highlightColor);
            }
        }
    }
}
