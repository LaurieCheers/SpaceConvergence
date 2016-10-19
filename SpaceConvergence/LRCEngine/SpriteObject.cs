using LRCEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LRCEngine
{
    public class SpriteObject
    {
        public Vector2 pos;
        public Vector2 size
        {
            get { return _size; }
            set { _size = value; _scale = new Vector2(value.X / textureRegion.Width, value.Y / textureRegion.Height); }
        }
        Vector2 _size;
        Vector2 _scale;
        public Texture2D texture;
        public Rectangle textureRegion;
        Color color = Color.White;
        public float layerDepth;
        public SpriteEffects spriteEffects = SpriteEffects.None;

        public SpriteObject(Texture2D texture, Vector2 pos): this(texture, pos, texture.Size())
        {
        }

        public SpriteObject(Texture2D texture, Vector2 pos, Vector2 size)
        {
            this.texture = texture;
            this.pos = pos;
            this.textureRegion = new Rectangle(0, 0, texture.Width, texture.Height);
            this.size = size;
        }

        public SpriteObject(Texture2D texture, Vector2 pos, Vector2 size, Color color): this(texture, pos, size)
        {
            this.color = color;
        }

        public SpriteObject(Texture2D texture, Vector2 pos, Vector2 size, Color color, Rectangle textureRegion)
        {
            this.texture = texture;
            this.pos = pos;
            this.textureRegion = textureRegion;
            this.size = size;
            this.color = color;
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, new Vector2((int)pos.X, (int)pos.Y), textureRegion, color, 0, Vector2.Zero, _scale, spriteEffects, layerDepth);
        }

        public Vectangle bounds { get { return new Vectangle(pos, size); } }
    }
}
