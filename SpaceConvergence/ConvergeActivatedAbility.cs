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
        public readonly ConvergeManaAmount cost;
        public readonly ConvergeCommand effect;
        public readonly ConvergeZoneId activeZones;

        public ConvergeActivatedAbilitySpec(JSONTable template, ContentManager Content)
        {
            frame = Content.Load<Texture2D>(template.getString("frame", "abilityFrame"));
            icon = Content.Load<Texture2D>(template.getString("icon"));
            effect = ConvergeCommand.New(template.getArray("effect"));
            cost = new ConvergeManaAmount(template.getString("cost", ""));
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

        Texture2D frame { get { return spec.frame; } }
        Texture2D icon { get { return spec.icon; } }
        ConvergeManaAmount cost { get { return spec.cost; } }
        ConvergeCommand effect { get { return spec.effect; } }
        ConvergeZoneId activeZones { get { return spec.activeZones; } }
        public readonly bool hasTarget = false;
        public bool isActive { get { return (source.zone.zoneId & activeZones) != 0; } }

        public ConvergePlayer controller { get { return source.zone.owner; } }

        public ConvergeActivatedAbility(ConvergeActivatedAbilitySpec spec, ConvergeObject source)
        {
            this.spec = spec;
            this.source = source;
        }

        public void Draw(SpriteBatch spriteBatch, bool isMouseOver, Rectangle frameRect)
        {
            if (CanActivate(Game1.activePlayer))
            {
                spriteBatch.Draw(frame, frameRect, Color.Red);
                spriteBatch.Draw(icon, frameRect, Color.White);
            }
            else
            {
                spriteBatch.Draw(frame, frameRect, Color.DarkRed);
                spriteBatch.Draw(icon, frameRect, Color.Gray);
            }

            if (isMouseOver)
            {
                spriteBatch.Draw(Game1.abilityHighlight, frameRect, Color.White);

                cost.DrawCost(spriteBatch, new Vector2(frameRect.Left, frameRect.Top - 20));
            }
        }

        public bool CanActivate(ConvergePlayer you)
        {
            if (you != source.zone.owner)
                return false;

            if (!you.CanPayCost(cost))
                return false;

            return true;
        }

        public void Activate(ConvergePlayer you)
        {
            if (you.TryPayCost(cost))
            {
                ConvergeEffectContext context = new ConvergeEffectContext(source, you);
                effect.Run(context);
            }
        }

        public void ActivateOn(ConvergeObject target)
        {

        }
    }
}
