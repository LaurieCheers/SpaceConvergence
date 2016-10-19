using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LRCEngine
{
    public enum Rotation90
    {
        None,
        Rot90,
        Rot180,
        Rot270
    }

    public enum TextAlignment
    {
        LEFT,
        CENTER,
        RIGHT,
    }

    public static class LRCEngineExtensions
    {
        public static float DotProduct(this Vector2 a, Vector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        // Convert a vector to an angle. Due right (Vector2(1,0)) is at 0.0f, and the angles continue clockwise.
        public static float ToAngle(this Vector2 a)
        {
            float len = a.Length();

            if (len < 0.001f)
            {
                return 0;
            }
            else
            {
                Vector2 dir = a / len;

                float result = (float)Math.Asin(dir.Y);
                if (a.X < 0)
                    result = (float)(Math.PI-result);
                return result;
            }
        }

        public static bool Contains(this Rectangle rect, Vector2 pos)
        {
            return rect.Contains(new Point((int)pos.X, (int)pos.Y));
        }

        public static Vector2 XY(this Rectangle rect)
        {
            return new Vector2(rect.X, rect.Y);
        }

        public static void Draw(this SpriteBatch spriteBatch, RichImage image, Rectangle rect, Color col)
        {
            image.Draw(spriteBatch, rect, col);
        }

        public static Color Multiply(this Color col1, Color col2)
        {
            return new Color(col1.R * col2.R * (1 / 65536.0f), col1.G * col2.G * (1 / 65536.0f), col1.B * col2.B * (1 / 65536.0f), col1.A * col2.A * (1 / 65536.0f));
        }

        public static Vector2 Size(this Texture2D texture)
        {
            return new Vector2(texture.Width, texture.Height);
        }

        public static int hexToInt(this String str)
        {
            int result = 0;
            foreach (char c in str)
            {
                if (c >= 'a' && c <= 'f')
                {
                    result = (c - 'a') + 10 + result * 16;
                }
                else if (c >= 'A' && c <= 'F')
                {
                    result = (c - 'A') + 10 + result * 16;
                }
                else if (c >= '0' && c <= '9')
                {
                    result = (c - '0') + result * 16;
                }
                else
                {
                    return 0;
                }
            }
            return result;
        }

        public static Color toColor(this String str)
        {
            if (str.Length == 6)
            {
                return new Color(str.Substring(0, 2).hexToInt(), str.Substring(2, 2).hexToInt(), str.Substring(4, 2).hexToInt());
            }
            else if (str.Length == 8)
            {
                return new Color(str.Substring(0, 2).hexToInt(), str.Substring(2, 2).hexToInt(), str.Substring(4, 2).hexToInt(), str.Substring(6, 2).hexToInt());
            }
            return Color.White;
        }

        public static int toInt(this Rotation90 rot)
        {
            switch (rot)
            {
                case Rotation90.Rot90: return 90;
                case Rotation90.Rot180: return 180;
                case Rotation90.Rot270: return 270;
                default: return 0;
            }
        }

        public static Rotation90 getRotation(this JSONTable table, string name, Rotation90 defaultValue)
        {
            int angle = table.getInt(name, defaultValue.toInt());
            return (Rotation90)(angle / 90);
        }

        public static Rotation90 rotateBy(this Rotation90 rotation, Rotation90 other)
        {
            int newRotation = (rotation.toInt() + other.toInt()) % 360;
            return (Rotation90)(newRotation / 90);
        }

        public static Rotation90 invert(this Rotation90 rotation)
        {
            int newRotation = 360 - rotation.toInt();
            return (Rotation90)(newRotation / 90);
        }

        public static void DrawString(this SpriteBatch spriteBatch, SpriteFont font, string text, Vector2 position, TextAlignment alignment, Color color)
        {
            switch (alignment)
            {
                case TextAlignment.LEFT:
                    spriteBatch.DrawString(font, text, position, color);
                    break;
                case TextAlignment.RIGHT:
                    {
                        Vector2 size = font.MeasureString(text);
                        spriteBatch.DrawString(font, text, new Vector2((int)(position.X - size.X), position.Y), color);
                    }
                    break;
                case TextAlignment.CENTER:
                    {
                        Vector2 size = font.MeasureString(text);
                        spriteBatch.DrawString(font, text, new Vector2((int)(position.X - size.X / 2), position.Y), color);
                    }
                    break;
            }
        }

        public static Rectangle GetStringBounds(this SpriteFont font, string text, Vector2 position, TextAlignment alignment)
        {
            Vector2 size = font.MeasureString(text);
            switch (alignment)
            {
                case TextAlignment.LEFT:
                default:
                    return new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y);
                case TextAlignment.RIGHT:
                    return new Rectangle((int)(position.X - size.X), (int)position.Y, (int)size.X, (int)size.Y);
                case TextAlignment.CENTER:
                    return new Rectangle((int)(position.X - size.X*0.5f), (int)position.Y, (int)size.X, (int)size.Y);
                    
            }
        }

        public static Rectangle Bloat(this Rectangle rect, int amount)
        {
            return new Rectangle(rect.X - amount, rect.Y - amount, rect.Width + amount * 2, rect.Height + amount * 2);
        }

        public static Rectangle FixNegatives(this Rectangle rect)
        {
            return new Rectangle(
                Math.Min(rect.X, rect.X + rect.Width),
                Math.Min(rect.Y, rect.Y + rect.Height),
                Math.Abs(rect.Width),
                Math.Abs(rect.Height)
            );
        }
    }
}
