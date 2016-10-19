using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LRCEngine
{
    public struct Vectangle
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;

        public Vector2 Origin { get { return new Vector2(X, Y); } set { X = value.X; Y = value.Y; } }
        public Vector2 Size { get { return new Vector2(Width, Height); } set { Width = value.X; Height = value.Y; } }

        public static Vectangle BoundingBox(Vector2 a, Vector2 b)
        {
            Vector2 origin = new Vector2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
            Vector2 botRight = new Vector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
            return new Vectangle(origin, botRight - origin);
        }

        public bool Contains(Vector2 point)
        {
            return X <= point.X && Y <= point.Y && X + Width > point.X && Y + Height > point.Y;
        }

        public bool Contains(Vectangle other)
        {
            return X <= other.X
                && Y <= other.Y
                && X + Width >= other.X + other.Width
                && Y + Height >= other.Y + other.Height;
        }

        public bool Intersects(Vectangle other)
        {
            return X <= other.X + other.Width && Y <= other.Y + other.Height &&
                X + Width >= other.X && Y + Height >= other.Y;
        }

        public Vectangle Bloat(float amount)
        {
            return new Vectangle(X - amount, Y - amount, Width + amount * 2, Height + amount * 2);
        }

        public Vectangle Bloat(float bX, float bY)
        {
            return new Vectangle(X - bX, Y - bY, Width + bX * 2, Height + bY * 2);
        }

        public Vectangle(float aX, float aY, float aWidth, float aHeight)
        {
            X = aX; Y = aY; Width = aWidth; Height = aHeight;
        }

        public Vectangle(Vector2 origin, Vector2 size)
        {
            X = origin.X;
            Y = origin.Y;
            Width = size.X;
            Height = size.Y;
        }

        public float MaxX { get { return X + Width; } }
        public float MaxY { get { return Y + Height; } }

        public Vector2 XY { get { return new Vector2(X, Y); } }
        public Vectangle LeftSide { get { return new Vectangle(X, Y, 0, Height); } }
        public Vectangle RightSide { get { return new Vectangle(X+Width, Y, 0, Height); } }
        public Vectangle TopSide { get { return new Vectangle(X, Y, Width, 0); } }
        public Vectangle BottomSide { get { return new Vectangle(X, Y+Height, Width, 0); } }
        public float Top { get { return Y; } }
        public float Bottom { get { return Y + Height; } }
        public float Left { get { return X; } }
        public float Right { get { return X + Width; } }
        public float CenterX { get { return X + Width / 2; } }
        public float CenterY { get { return Y + Height / 2; } }
        public Vector2 TopLeft { get { return new Vector2(Left, Top); } }
        public Vector2 TopCenter { get { return new Vector2(CenterX, Top); } }
        public Vector2 TopRight { get { return new Vector2(Right, Top); } }
        public Vector2 CenterLeft { get { return new Vector2(Left, CenterY); } }
        public Vector2 Center { get { return new Vector2(CenterX, CenterY); } }
        public Vector2 CenterRight { get { return new Vector2(Right, CenterY); } }
        public Vector2 BottomLeft { get { return new Vector2(Left, Bottom); } }
        public Vector2 BottomCenter { get { return new Vector2(CenterX, Bottom); } }
        public Vector2 BottomRight { get { return new Vector2(Right, Bottom); } }
    }

    public static class Extensions
    {
        public static void Draw(this SpriteBatch spriteBatch, Texture2D texture, Vectangle vect, Color color)
        {
            spriteBatch.Draw(texture, vect.Origin, null, color, 0, Vector2.Zero, new Vector2(vect.Size.X / texture.Width, vect.Size.Y / texture.Height), SpriteEffects.None, 0);
        }

        public static bool Contains(this Rectangle rect, Vector2 point)
        {
            return rect.X <= point.X && rect.Y <= point.Y &&
                rect.X + rect.Width > point.X && rect.Y + rect.Height > point.Y;
        }

        public static bool Intersects(this Rectangle rect, Vectangle other)
        {
            return rect.X <= other.X + other.Width && rect.Y <= other.Y + other.Height &&
                rect.X + rect.Width >= other.X && rect.Y + rect.Height >= other.Y;
        }

        public static Vectangle Vectangle(this Rectangle rect)
        {
            return new Vectangle(rect.X, rect.Y, rect.Width, rect.Height);
        }
    }
}