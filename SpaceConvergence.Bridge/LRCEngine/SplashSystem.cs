using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LRCEngine
{
    public class Splash
    {
        string text;
        TextAlignment alignment;
        SpriteFont font;
        Texture2D icon;
        Vector2 pos;
        Vector2 velocity;
        Color color;
        int lifetime;
        float drag;
        float gravity;
        public bool alive { get; private set; }

        public Splash(string text, TextAlignment alignment, SpriteFont font, Color color, Vector2 pos, Vector2 velocity, float drag, float gravity, float lifeSeconds)
        {
            this.text = text;
            this.alignment = alignment;
            this.font = font;
            this.color = color;
            this.pos = pos;
            this.velocity = velocity;
            this.drag = drag;
            this.gravity = gravity;
            this.lifetime = (int)(lifeSeconds * 30);
            this.alive = true;
        }

        public void Update()
        {
            lifetime--;
            if (lifetime <= 0)
                alive = false;
            else
            {
                velocity.Y += gravity;
                velocity *= drag;
                pos += velocity;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (text != null)
                spriteBatch.DrawString(font, text, pos, alignment, color);

            if (icon != null)
                spriteBatch.Draw(icon, new Rectangle((int)pos.X, (int)pos.Y, icon.Width, icon.Height), color);
        }
    }

    public class SplashManager
    {
        List<Splash> splashes = new List<Splash>();

        public void Add(Splash s)
        {
            splashes.Add(s);
        }

        public void Update()
        {
            int numDead = 0;
            foreach (Splash s in splashes)
            {
                if (s.alive)
                    s.Update();
                else
                    numDead++;
            }

            const int GARBAGE_COLLECT_THRESHOLD = 3;
            if (numDead == splashes.Count)
            {
                splashes.Clear();
            }
            else if(numDead > GARBAGE_COLLECT_THRESHOLD)
            {
                List<Splash> newList = new List<Splash>();
                foreach(Splash s in splashes)
                {
                    newList.Add(s);
                }
                splashes = newList;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (Splash s in splashes)
            {
                if (s.alive)
                    s.Draw(spriteBatch);
            }
        }

    }
}
