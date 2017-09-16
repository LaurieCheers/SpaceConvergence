using LRCEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceConvergence
{
    public class ConvergeActivatedAbilitySpec
    {
        public readonly Texture2D frame;
        public readonly Texture2D icon;
        public readonly string text;
        public readonly int textHeight;
        public readonly Color frameColor;
        public readonly ConvergeManaAmount manacost;
        public readonly ConvergeAltCost altCost;
        public readonly ConvergeCommand effect;
        public readonly ConvergeCommand attackEffect;
        public readonly ConvergeSelector target;
        public readonly ConvergeZoneId activeZones;
        public readonly int uses;

        public ConvergeActivatedAbilitySpec(JSONTable template, ContentManager Content)
        {
            frame = Content.Load<Texture2D>(template.getString("frame", "abilityFrame"));
            icon = Content.Load<Texture2D>(template.getString("icon"));
            text = template.getString("text", "").InsertLineBreaks(Game1.font, ConvergeUIAbility.AbilityTooltipWidth - 15);
            textHeight = (int)Game1.font.MeasureString(text).Y;
            effect = ConvergeCommand.New(template.getArray("effect"), Content);
            frameColor = template.getString("frameColor", "FFFFFF").toColor();

            if (template.hasKey("attackEffect"))
                attackEffect = ConvergeCommand.New(template.getArray("attackEffect"), Content);

            if (template.hasKey("target"))
                target = ConvergeSelector.New(template.getProperty("target"));

            manacost = new ConvergeManaAmount(template.getString("manaCost", ""));
            uses = template.getInt("uses", 0);

            altCost = 0;
            foreach(string altcostString in template.getArray("altCost", JSONArray.empty).asStrings())
            {
                altCost |= (ConvergeAltCost)Enum.Parse(typeof(ConvergeAltCost), altcostString);
            }

            JSONArray zoneTemplate = template.getArray("activeZones", null);
            if (zoneTemplate == null)
            {
                activeZones = ConvergeZoneId.Attack | ConvergeZoneId.Defense | ConvergeZoneId.Home;
            }
            else
            {
                activeZones = 0;
                foreach(string zoneName in zoneTemplate.asStrings())
                {
                    activeZones |= (ConvergeZoneId)Enum.Parse(typeof(ConvergeZoneId), zoneName);
                }
            }
        }
    }

    public class ConvergeActivatedAbility
    {
        ConvergeActivatedAbilitySpec spec;
        ConvergeObject source;
        int timesUsed = 0;

        Texture2D frame { get { return spec.frame; } }
        Texture2D icon { get { return spec.icon; } }
        public string text { get { return spec.text; } }
        public int textHeight { get { return spec.textHeight; } }
        public ConvergeManaAmount manacost { get { return spec.manacost; } }
        ConvergeCommand effect { get { return spec.effect; } }
        ConvergeCommand attackEffect { get { return spec.attackEffect; } }
        ConvergeZoneId activeZones { get { return spec.activeZones; } }
        ConvergeAltCost altCost { get { return spec.altCost; } }
        public bool hasTarget { get { return spec.target != null; } }
        public bool isActive { get
            {
                return (source.zone.zoneId & activeZones) != 0 && (spec.uses == 0 || timesUsed < spec.uses);
            }
        }

        public ConvergePlayer controller { get { return source.controller; } }

        public ConvergeActivatedAbility(ConvergeActivatedAbilitySpec spec, ConvergeObject source)
        {
            this.spec = spec;
            this.source = source;
        }

        public void Draw(SpriteBatch spriteBatch, bool isMouseOver, Rectangle frameRect)
        {
            Rectangle iconRect = new Rectangle(frameRect.Center.X - icon.Width / 2, frameRect.Center.Y - icon.Height / 2, icon.Width, icon.Height);
            if (CanActivate(Game1.activePlayer))
            {
                spriteBatch.Draw(frame, frameRect, spec.frameColor);
                spriteBatch.Draw(icon, iconRect, Color.White);
            }
            else
            {
                spriteBatch.Draw(frame, frameRect, spec.frameColor.Multiply(Color.Gray));
                spriteBatch.Draw(icon, iconRect, Color.Gray);
            }

/*            if (isMouseOver)
            {
                spriteBatch.Draw(Game1.abilityHighlight, frameRect, Color.White);

                manacost.DrawCost(spriteBatch, new Vector2(frameRect.Left, frameRect.Top - 20));
            }*/
        }

        public bool CanTarget(ConvergeObject target, ConvergePlayer you)
        {
            ConvergeEffectContext context = new ConvergeEffectContext(source, you, this);
            return spec.target.Test(target, context);
        }

        public bool CanActivate(ConvergePlayer you)
        {
            if (!isActive || you != source.controller)
                return false;

            if (!you.CanPayCost(manacost) || !source.CanPayAltCost(spec.altCost))
                return false;

            return true;
        }

        public void Activate(ConvergePlayer you)
        {
            if (hasTarget)
                return;

            if (you.TryPayCost(manacost) && source.TryPayAltCost(altCost))
            {
                timesUsed++;
                ConvergeEffectContext context = new ConvergeEffectContext(source, you, this);
                effect.Run(context);
            }
        }

        public void ActivateOn(ConvergeObject target, ConvergePlayer you)
        {
            if (!hasTarget)
                return;

            if (!CanTarget(target, you))
                return;

            if (you.TryPayCost(manacost) && source.TryPayAltCost(altCost))
            {
                timesUsed++;
                ConvergeEffectContext context = new ConvergeEffectContext(source, you, this);
                context.target = target;
                effect.Run(context);
            }
        }

        public void OnEnteringPlay()
        {
            timesUsed = 0;
        }

        public void DoAttackEffect(ConvergeObject target, ConvergePlayer you)
        {
            if (!CanTarget(target, you))
                return;

            ConvergeEffectContext context = new ConvergeEffectContext(source, you, this);
            context.target = target;
            attackEffect.Run(context);
        }
    }
}
