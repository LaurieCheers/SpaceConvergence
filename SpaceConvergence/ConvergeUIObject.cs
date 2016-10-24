using LRCEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceConvergence
{
    public class ConvergeUIObject : UIElement
    {
        public readonly ConvergeObject represented;
        public Rectangle gfxFrame;
        bool isMouseOver;
        bool isMousePressing;
        bool isDragging;
        bool isBadDrag;
        Vector2 mousePressedPos;
        bool isVisible;
        List<ConvergeUIAbility> abilityUIs = new List<ConvergeUIAbility>();

        public const int CardTooltipWidth = 225;
        public const int CardTooltipHeight = 75;

        public ConvergeUIObject(ConvergeObject represented)
        {
            this.represented = represented;
            represented.ui = this;
            this.gfxFrame = new Rectangle(represented.nominalPosition.ToPoint(), new Point(50, 60));

            UpdateAbilityUIs();
        }

        void UpdateAbilityUIs()
        {
            abilityUIs.Clear();
            Vector2 offset = new Vector2(this.gfxFrame.Width / 2, 12);
            foreach (ConvergeActivatedAbility ability in represented.activatedAbilities)
            {
                abilityUIs.Add(new ConvergeUIAbility(ability, offset, this));
                offset.X += 32.0f;
            }
        }

        public override void Update(InputState inputState, Vector2 origin)
        {
            if (abilityUIs.Count != represented.activatedAbilities.Count)
                UpdateAbilityUIs();

            isBadDrag = false;
            isVisible = represented.zone.zoneId == ConvergeZoneId.Hand ? (represented.zone.owner.isActivePlayer) : true;

            if (!isVisible)
            {
                return;
            }

            isMouseOver = (inputState.hoveringElement == this);
            if (isMouseOver && inputState.mouseLeft.justPressed && represented.controller.isActivePlayer)
            {
                isMousePressing = true;
                mousePressedPos = inputState.MousePos;
            }

            if (isMousePressing && isMouseOver && inputState.mouseLeft.justReleased)
                represented.Play(Game1.activePlayer); // clicked

            if (!inputState.mouseLeft.isDown)
                isMousePressing = false;

            float MINDRAG = 5;
            float MINDRAGSQR = MINDRAG * MINDRAG;
            if (isDragging)
            {
                Game1.ticking = false;
                if (isMousePressing)
                {
                    this.gfxFrame = new Rectangle((int)inputState.MousePos.X - 25, (int)inputState.MousePos.Y - 30, 50, 60);

                    if (inputState.hoveringElement != null && inputState.hoveringElement is ConvergeUIObject)
                    {
                        isBadDrag = !represented.CanTarget(((ConvergeUIObject)inputState.hoveringElement).represented, represented.controller);
                    }
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
                        ConvergeZone attackZone = represented.controller.attack;
                        ConvergeZone defenseZone = represented.controller.defense;
                        if (currentZone.zoneId == ConvergeZoneId.Defense && attackZone.bounds.Contains(inputState.MousePos))
                        {
                            represented.EnterAttack();
                        }
                        else if (currentZone.zoneId == ConvergeZoneId.Attack && defenseZone.bounds.Contains(inputState.MousePos))
                        {
                            represented.WithdrawAttack();
                        }
                        else if (currentZone.zoneId == ConvergeZoneId.Hand && defenseZone.bounds.Contains(inputState.MousePos))
                        {
                            represented.Play(represented.controller);
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
                    this.gfxFrame = new Rectangle(targetPos.ToPoint(), new Point(50, 60));
                }
                else
                {
                    moveOffset.Normalize();
                    this.gfxFrame = new Rectangle((this.gfxFrame.XY() + moveOffset * MOVESPEED).ToPoint(), new Point(50, 60));
                }
            }

            Vector2 offset = new Vector2(this.gfxFrame.Width / 2, 12);

            foreach(ConvergeUIAbility abilityUI in abilityUIs)
            {
                if (abilityUI.isActive)
                {
                    abilityUI.offset = offset;
                    abilityUI.Update(inputState);
                    offset.X += 32.0f;
                }
            }
        }

        public override UIMouseResponder GetMouseHover(Vector2 pos)
        {
            if (isDragging || !isVisible)
                return null;

            if (abilityUIs != null)
            {
                foreach (ConvergeUIAbility abilityUI in abilityUIs)
                {
                    UIMouseResponder result = abilityUI.GetMouseHover(pos);
                    if (result != null)
                        return result;
                }
            }

            if (gfxFrame.Contains(pos))
                return this;

            return null;
        }

        static Color TappedTint = new Color(128, 128, 255);

        public override void Draw(SpriteBatch spriteBatch, Vector2 pos)
        {
            if (!isVisible)
                return;

            ConvergePlayer controller = represented.controller;

            // attack beam
            if(represented.zone.zoneId == ConvergeZoneId.Attack &&
                !represented.tapped &&
                represented.effectivePower > 0 &&
                !represented.dying &&
                ((represented.controller == Game1.activePlayer) == (Game1.countdown > 0)))
            {
                Rectangle targetFrame = represented.controller.opponent.homeBase.ui.gfxFrame;
                int thickness = Game1.countdown == 0? 16: 16 + (120 - Game1.countdown)/6;
                spriteBatch.DrawBeam(Game1.attackBeam, new Vector2(gfxFrame.Center.X, gfxFrame.Bottom), new Vector2(targetFrame.Center.X, targetFrame.Bottom-5), thickness, Color.White);
            }

            Texture2D art = represented.art;
            Color artTint = Color.White;
            if (represented.zone.zoneId == ConvergeZoneId.DiscardPile)
                artTint = Color.Black;
            else if (represented.tapped)
                artTint = TappedTint;

            spriteBatch.Draw(
                represented.art,
                new Rectangle(gfxFrame.Center.X - art.Width / 2, gfxFrame.Bottom - art.Height, art.Width, art.Height),
                null,
                artTint,
                0.0f,
                Vector2.Zero,
                controller.faceLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                0.0f
            );

            if (represented.tapped)
            {
                spriteBatch.Draw(Game1.tappedicon, new Vector2(gfxFrame.Right - 16, gfxFrame.Top), Color.White);
            }

            if (represented.zone.zoneId == ConvergeZoneId.DiscardPile)
            {
                return;
            }

            if (represented.cardType.HasFlag(ConvergeCardType.Unit))
            {
                if (represented.power > 0)
                {
                    spriteBatch.Draw(Game1.powerbg, new Rectangle(gfxFrame.Left + 8, gfxFrame.Bottom, 16, 16), Color.White);
                    spriteBatch.DrawString(Game1.font, "" + represented.effectivePower, new Vector2(gfxFrame.Left + 16, gfxFrame.Bottom), TextAlignment.CENTER, represented.powerUsed > 0 ? Color.Red : Color.Yellow);
                }

                Rectangle toughnessRect = new Rectangle(gfxFrame.Right - 24, gfxFrame.Bottom, 16, 16);
                Vector2 toughnessTextPos = new Vector2(gfxFrame.Right - 16, gfxFrame.Bottom);
                if (represented.destroyed)
                {
                    spriteBatch.Draw(Game1.woundbg, toughnessRect, Color.White);
                    spriteBatch.DrawString(Game1.font, "X", toughnessTextPos, TextAlignment.CENTER, Color.Red);
                }
                else if (represented.effectiveToughness <= 0)
                {
                    spriteBatch.Draw(Game1.woundbg, toughnessRect, Color.White);
                    spriteBatch.DrawString(Game1.font, ""+ represented.effectiveToughness, toughnessTextPos, TextAlignment.CENTER, Color.Red);
                }
                else
                {
                    spriteBatch.Draw(Game1.shieldbg, new Rectangle(gfxFrame.Right - 24, gfxFrame.Bottom, 16, 16), Color.White);
                    spriteBatch.DrawString(Game1.font, "" + represented.effectiveToughness, toughnessTextPos, TextAlignment.CENTER, represented.damage > 0 ? Color.Red : Color.White);
                }
            }

            if (represented.cardType.HasFlag(ConvergeCardType.Home))
            {
                spriteBatch.DrawString(Game1.font, "" + controller.life, new Vector2(gfxFrame.Center.X, gfxFrame.Top + 20), TextAlignment.CENTER, Color.Black);

                Vector2 resourcePos;
                if (controller.faceLeft)
                    resourcePos = new Vector2(gfxFrame.Left, gfxFrame.Bottom);
                else
                    resourcePos = new Vector2(gfxFrame.Center.X, gfxFrame.Bottom);
                controller.resources.DrawResources(spriteBatch, controller.showResources, resourcePos);
            }

            bool drawHighlight = isMouseOver;
            Color highlightColor = Color.White;
            if (represented.zone.zoneId == ConvergeZoneId.Hand)
            {
                if (represented.CanBePlayed(Game1.activePlayer))
                {
                    drawHighlight = true;
                    if (isBadDrag)
                        highlightColor = Color.Red;
                    else if (isMouseOver)
                        highlightColor = Color.Yellow;
                    else
                        highlightColor = Color.Orange;
                }

/*                if (isMouseOver && represented.cost != null)
                {
                    represented.cost.DrawCost(spriteBatch, new Vector2(gfxFrame.Left, gfxFrame.Top));
                }*/
            }
            else if (drawHighlight && !controller.isActivePlayer)
            {
                highlightColor = Color.Red;
            }

            if (drawHighlight)
            {
                spriteBatch.Draw(Game1.mouseOverGlow, gfxFrame, highlightColor);
            }

            foreach (ConvergeUIAbility abilityUI in abilityUIs)
            {
                abilityUI.Draw(spriteBatch);
            }
        }

        public void DrawTooltip(SpriteBatch spriteBatch)
        {
            int effectiveHeight = CardTooltipHeight + represented.textHeight;
            Vector2 pos;
            if (represented.zone.zoneId == ConvergeZoneId.Hand)
            {
                pos = new Vector2(gfxFrame.Center.X - CardTooltipWidth / 2, gfxFrame.Top -10 - effectiveHeight);
            }
            else
            {
                pos = new Vector2(gfxFrame.Right + 10, gfxFrame.Center.Y - effectiveHeight / 2);
            }

            if (pos.Y < 10)
                pos.Y = 10;
            if (pos.X > 500)
                pos.X = gfxFrame.Left - 10 - CardTooltipWidth;

            RichImage chosenFrame = Game1.cardFrame;
            if (represented.cost != null)
            {
                switch (represented.cost.GetColor())
                {
                    case ConvergeColor.Colorless:
                        chosenFrame = Game1.cardFrame;
                        break;
                    case ConvergeColor.White:
                        chosenFrame = Game1.whiteFrame;
                        break;
                    case ConvergeColor.Blue:
                        chosenFrame = Game1.blueFrame;
                        break;
                    case ConvergeColor.Black:
                        chosenFrame = Game1.blackFrame;
                        break;
                    case ConvergeColor.Red:
                        chosenFrame = Game1.redFrame;
                        break;
                    case ConvergeColor.Green:
                        chosenFrame = Game1.greenFrame;
                        break;
                    default:
                        chosenFrame = Game1.goldFrame;
                        break;
                }
            }

            spriteBatch.Draw(chosenFrame, new Rectangle((int)pos.X, (int)pos.Y, CardTooltipWidth, effectiveHeight), Color.White);
            spriteBatch.DrawString(Game1.font, represented.name, pos + new Vector2(10, 10), Color.Black);

            if (represented.cost != null)
            {
                represented.cost.DrawResources(spriteBatch, null, pos + new Vector2(CardTooltipWidth-40, 10));
            }

            if (represented.keywordsText != "")
            {
                spriteBatch.DrawString(Game1.font, represented.cardType + " - " + represented.keywordsText, pos + new Vector2(10, 35), Color.Black);
            }
            else
            {
                spriteBatch.DrawString(Game1.font, ""+ represented.cardType, pos + new Vector2(10, 35), Color.Black);
            }
            if(represented.produces != null)
                represented.produces.DrawCost(spriteBatch, pos + new Vector2(90, 35));
            spriteBatch.DrawString(Game1.font, represented.text, pos + new Vector2(10, 60), Color.Black); 
        }
    }

    public class ConvergeUIAbility: UIMouseResponder
    {
        ConvergeActivatedAbility ability;
        ConvergeUIObject parent;
        Rectangle frame;
        public Vector2 offset;
        Vector2 draggedTo;

        bool beamVisible;
        bool beamBad;
        Rectangle beamRect;
        Rectangle beamArrowRect;
        float beamRotation;
        
        bool isMouseOver;
        bool isMousePressing;
        bool isDragging;
        public bool isActive { get { return ability.isActive; } }

        public const int AbilityTooltipWidth = 200;
        public const int AbilityTooltipHeight = 45;

        public ConvergeUIAbility(ConvergeActivatedAbility ability, Vector2 offset, ConvergeUIObject parent)
        {
            this.ability = ability;
            this.offset = offset;
            this.parent = parent;
        }

        public void Update(InputState inputState)
        {
            frame = new Rectangle(parent.gfxFrame.Left+(int)offset.X - 16, parent.gfxFrame.Top+(int)offset.Y - 16, 32, 32);

            isMouseOver = (inputState.hoveringElement == this);

            if (isMouseOver &&
                inputState.mouseLeft.justPressed &&
                ability.controller.isActivePlayer &&
                ability.CanActivate(Game1.activePlayer))
            {
                isMousePressing = true;
            }

            if(isMousePressing && ability.hasTarget)
            {
                beamVisible = true;

                Vector2 offset = inputState.MousePos - frame.Center.ToVector2();
                beamRotation = offset.ToAngle();
                beamRect = new Rectangle(frame.Center.X, frame.Center.Y, (int)offset.Length(), 16);
                beamArrowRect = new Rectangle((int)inputState.MousePos.X, (int)inputState.MousePos.Y, 16, 16);

                if (inputState.hoveringElement != null && inputState.hoveringElement is ConvergeUIObject)
                {
                    ConvergeObject targetedObject = ((ConvergeUIObject)inputState.hoveringElement).represented;
                    beamBad = !ability.CanTarget(targetedObject, Game1.activePlayer);
                }
                else
                {
                    beamBad = false;
                }
            }
            else
            {
                beamVisible = false;
            }


            if (isMousePressing && !inputState.mouseLeft.isDown)
            {
                isMousePressing = false;

                if (ability.hasTarget)
                {
                    if (inputState.hoveringElement != null)
                    {
                        // used on target
                        if(inputState.hoveringElement is ConvergeUIObject)
                        {
                            ability.ActivateOn(((ConvergeUIObject)inputState.hoveringElement).represented, Game1.activePlayer);
                        }
                    }
                }
                else
                {
                    ability.Activate(Game1.activePlayer);
                }
            }
        }

        public UIMouseResponder GetMouseHover(Vector2 pos)
        {
            if (!ability.isActive)
                return null;

            if( frame.Contains(pos) )
                return this;

            return null;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!ability.isActive)
                return;

            if(beamVisible)
            {
                if (beamBad)
                {
                    spriteBatch.Draw(Game1.badTargetBeam, beamRect, null, Color.White, beamRotation, new Vector2(0, 8), SpriteEffects.None, 0.0f);
                    spriteBatch.Draw(Game1.badTargetArrow, beamArrowRect, null, Color.White, beamRotation, new Vector2(8, 8), SpriteEffects.None, 0.0f);
                }
                else
                {
                    spriteBatch.Draw(Game1.targetBeam, beamRect, null, Color.White, beamRotation, new Vector2(0, 8), SpriteEffects.None, 0.0f);
                    spriteBatch.Draw(Game1.targetArrow, beamArrowRect, null, Color.White, beamRotation, new Vector2(8, 8), SpriteEffects.None, 0.0f);
                }
            }

            ability.Draw(spriteBatch, isMouseOver, frame);
        }

        public void DrawTooltip(SpriteBatch spriteBatch)
        {
            Vector2 pos = new Vector2(frame.Right + 10, frame.Center.Y - (AbilityTooltipHeight+ability.textHeight) / 2);

            if (pos.Y < 10)
                pos.Y = 10;
            if (pos.X > 500)
                pos.X = frame.Left - 10 - AbilityTooltipWidth;
            spriteBatch.Draw(Game1.cardFrame, new Rectangle((int)pos.X, (int)pos.Y, AbilityTooltipWidth, AbilityTooltipHeight+ability.textHeight), Color.White);

            ability.manacost.DrawCost(spriteBatch, pos + new Vector2(10, 10));
            spriteBatch.DrawString(Game1.font, ability.text, pos + new Vector2(10, 35), Color.Black);
        }
    }
}
