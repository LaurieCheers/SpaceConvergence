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
        ConvergeObject represented;
        Rectangle gfxFrame;
        bool isMouseOver;
        bool isMousePressing;
        bool isDragging;
        Vector2 mousePressedPos;
        bool isVisible;

        public ConvergeUIObject(ConvergeObject represented)
        {
            this.represented = represented;
            represented.ui = this;
            this.gfxFrame = new Rectangle(represented.nominalPosition.ToPoint(), new Point(50, 60));
        }

        public override void Update(InputState inputState, Vector2 origin)
        {
            isVisible = represented.zone.zoneId == ConvergeZoneId.Hand ? (represented.zone.owner.isActivePlayer) : true;

            if (!isVisible)
            {
                return;
            }

            isMouseOver = (inputState.hoveringElement == this);
            if (isMouseOver && inputState.mouseLeft.justPressed && represented.zone.owner.isActivePlayer)
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
                        if (currentZone.zoneId == ConvergeZoneId.Defense && attackZone.bounds.Contains(inputState.MousePos))
                        {
                            represented.EnterAttack();
                        }
                        else if (currentZone.zoneId == ConvergeZoneId.Attack && defenseZone.bounds.Contains(inputState.MousePos))
                        {
                            represented.WithdrawAttack();
                        }
                        else if(currentZone.zoneId == ConvergeZoneId.Hand && defenseZone.bounds.Contains(inputState.MousePos))
                        {
                            represented.Play();
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
        }

        public override UIMouseResponder GetMouseHover(Vector2 pos)
        {
            if (isDragging || !isVisible)
                return null;

            if (gfxFrame.Contains(pos))
                return this;

            return null;
        }

        static Color TappedTint = new Color(128, 128, 255);

        public override void Draw(SpriteBatch spriteBatch, Vector2 pos)
        {
            if (!isVisible)
                return;

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
                represented.zone.owner.faceLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
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

            if (represented.cardType.HasFlag(ConvergeCardType.Home))
            {
                spriteBatch.DrawString(Game1.font, "" + represented.zone.owner.life, new Vector2(gfxFrame.Center.X, gfxFrame.Top + 20), TextAlignment.CENTER, Color.Black);

                Vector2 resourcePos;
                if (represented.zone.owner.faceLeft)
                    resourcePos = new Vector2(gfxFrame.Left, gfxFrame.Bottom);
                else
                    resourcePos = new Vector2(gfxFrame.Center.X, gfxFrame.Bottom);
                represented.zone.owner.resources.DrawResources(spriteBatch, represented.zone.owner.resourcesSpent, resourcePos);
            }

            bool drawHighlight = isMouseOver;
            Color highlightColor = Color.White;
            if (represented.zone.zoneId == ConvergeZoneId.Hand)
            {
                if (represented.CanBePlayed())
                {
                    drawHighlight = true;
                    if (isMouseOver)
                        highlightColor = Color.Yellow;
                    else
                        highlightColor = Color.Orange;
                }

                if (isMouseOver && represented.cost != null)
                {
                    represented.cost.DrawCost(spriteBatch, new Vector2(gfxFrame.Left, gfxFrame.Top));
                }
            }
            else if (drawHighlight && !represented.zone.owner.isActivePlayer)
            {
                highlightColor = Color.Red;
            }

            if (drawHighlight)
            {
                spriteBatch.Draw(Game1.mouseOverGlow, gfxFrame, highlightColor);
            }
        }
    }
}
