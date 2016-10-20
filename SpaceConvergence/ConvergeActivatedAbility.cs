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
    public class ConvergeActivatedAbility
    {
        Texture2D frame;
        Texture2D icon;
        ConvergeManaAmount cost;
        ConvergeCommand effect;

        public ConvergeActivatedAbility(JSONTable template, ContentManager Content)
        {
            frame = Content.Load<Texture2D>(template.getString("frame", "abilityFrame"));
            icon = Content.Load<Texture2D>(template.getString("icon"));
            effect = ConvergeCommand.New(template.getArray("effect"));
            cost = new ConvergeManaAmount(template.getString("cost", ""));
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 pos)
        {
            Rectangle frameRect = new Rectangle((int)pos.X - 16, (int)pos.Y - 16, 32, 32);
            spriteBatch.Draw(frame, frameRect, Color.Red);
            spriteBatch.Draw(icon, frameRect, Color.White);
        }
    }
}
