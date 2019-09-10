using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LRCEngine
{
    public class UIButtonStyle
    {
        public readonly UIButtonAppearance normal;
        public readonly UIButtonAppearance hover;
        public readonly UIButtonAppearance pressed;
        public readonly UIButtonAppearance disabled;

        public UIButtonStyle(UIButtonAppearance normal, UIButtonAppearance hover, UIButtonAppearance pressed, UIButtonAppearance disabled)
        {
            this.normal = normal;
            this.hover = hover;
            this.pressed = pressed;
            this.disabled = disabled;
        }
    }

    public class UIButtonAppearance
    {
        public readonly SpriteFont font;
        public readonly Color textColor;
        public readonly RichImage image;
        public readonly Vector2 textOffset;
        public readonly Color fillColor;

        public UIButtonAppearance(SpriteFont font, Color textColor, RichImage image, Color fillColor)
        {
            this.font = font;
            this.textColor = textColor;
            this.image = image;
            this.fillColor = fillColor;
        }

        public UIButtonAppearance(SpriteFont font, Color textColor, RichImage image, Color fillColor, Vector2 textOffset)
        {
            this.font = font;
            this.textColor = textColor;
            this.image = image;
            this.fillColor = fillColor;
            this.textOffset = textOffset;
        }

        public void Draw(SpriteBatch spriteBatch, string label, Texture2D icon, Rectangle frame)
        {
            image.Draw(spriteBatch, frame, fillColor);
            //            MagicUI.Draw9Grid(spriteBatch, texture, frame, fillColor);
            //            spriteBatch.Draw(texture, frame, fillColor);

            if (icon != null)
            {
                if (font != null)
                {
                    // icon and text
                    Vector2 labelSize = font.MeasureString(label);
                    float iconSpacing = 2;
                    Vector2 iconOrigin = frame.Center.ToVector2() + textOffset - new Vector2(labelSize.X + icon.Width + iconSpacing, icon.Height) / 2;
                    Vector2 textOrigin = new Vector2((int)(iconOrigin.X + icon.Width + iconSpacing), (int)(frame.Center.Y + textOffset.Y - labelSize.Y/2));
                    spriteBatch.Draw(icon, iconOrigin, Color.White);
                    spriteBatch.DrawString(font, label, textOrigin, textColor);
                }
                else
                {
                    // icon only
                    spriteBatch.Draw(icon, frame.Center.ToVector2() + textOffset - icon.Size() / 2, Color.White);
                }
            }
            else if (font != null)
            {
                // text only
                Vector2 labelSize = font.MeasureString(label);
                spriteBatch.DrawString(font, label, new Vector2((float)Math.Floor(frame.Center.X + textOffset.X - labelSize.X / 2), (float)Math.Floor(frame.Center.Y + textOffset.Y - labelSize.Y / 2)), textColor);
            }
        }
    }

    public class UIButton : UIElement
    {
        public string label;
        public Texture2D icon;
        public Rectangle frame;
        public readonly UIButtonStyle styles;
        public OnPressDelegate onPress
        {
            get; protected set;
        }
        public bool mouseInside;
        public bool pressedInside;
        public bool enabled = true;
        public bool visible = true;

        public delegate void OnPressDelegate();

        public static UIButtonStyle GetDefaultStyle(ContentManager Content)
        {
            SpriteFont font = Content.Load<SpriteFont>("Arial");
            RichImage normalImage = new RichImage(new RichImageLayer_Texture(Content.Load<Texture2D>("button3d"), Color.White, "stretched9grid", 0, Rotation90.None));
            RichImage hoverImage = new RichImage(new RichImageLayer_Texture(Content.Load<Texture2D>("button3d_hover"), Color.White, "stretched9grid", 0, Rotation90.None));
            RichImage pressedImage = new RichImage(new RichImageLayer_Texture(Content.Load<Texture2D>("button3d_pressed"), Color.White, "stretched9grid", 0, Rotation90.None));

            return new UIButtonStyle(
                new UIButtonAppearance(font, Color.Black, normalImage, Color.White),
                new UIButtonAppearance(font, Color.Black, hoverImage, Color.White),
                new UIButtonAppearance(font, Color.Black, pressedImage, Color.White, new Vector2(0,1)),
                new UIButtonAppearance(font, Color.Black, normalImage, Color.Gray)
            );
        }

        public UIButton(string label, Rectangle frame, UIButtonStyle styles, OnPressDelegate onPress)
        {
            this.label = label;
            this.frame = frame;
            this.styles = styles;
            this.onPress = onPress;
        }

        public UIButton(string label, Texture2D icon, Rectangle frame, UIButtonStyle styles, OnPressDelegate onPress)
        {
            this.label = label;
            this.icon = icon;
            this.frame = frame;
            this.styles = styles;
            this.onPress = onPress;
        }

        public override UIMouseResponder GetMouseHover(Vector2 localMousePos)
        {
            return frame.Contains(localMousePos) ? this : null;
        }

        public override void Update(InputState inputState, Vector2 origin)
        {
            if (!enabled || !visible)
            {
                mouseInside = false;
                pressedInside = false;
                return;
            }

            mouseInside = inputState.hoveringElement == this;// frame.Contains(inputState.MousePos - origin);
            if (mouseInside && inputState.WasMouseLeftJustPressed())
            {
                pressedInside = true;
            }

            if (!inputState.mouseLeft.isDown)
            {
                if (mouseInside && pressedInside)
                {
                    Pressed();
                }
                pressedInside = false;
            }
        }

        protected virtual void Pressed()
        {
            if(onPress != null)
                onPress();
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 origin)
        {
            if (!visible)
                return;

            UIButtonAppearance currentStyle;
            if (!enabled)
            {
                currentStyle = styles.disabled;
            }
            else if (mouseInside)
            {
                if (pressedInside)
                    currentStyle = styles.pressed;
                else
                    currentStyle = styles.hover;
            }
            else
            {
                currentStyle = styles.normal;
            }

            currentStyle.Draw(spriteBatch, label, icon, new Rectangle(frame.X + (int)origin.X, frame.Y + (int)origin.Y, frame.Width, frame.Height));
        }

        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
        }

        public void SetVisible(bool visible)
        {
            this.visible = visible;
        }
    }

    public class UIRadioButtonGroup<T>
    {
        public UIRadioButton<T> selectedButton;
        public T selectedValue { get { return selectedButton.value; } }
    }

    public class UIRadioButton<T>: UIButton
    {
        public readonly UIRadioButtonGroup<T> group;
        public readonly T value;
        UIButtonAppearance activeAppearance;
        public readonly OnRadioPressDelegate onRadioPress;

        public delegate void OnRadioPressDelegate(T value);

        public UIRadioButton(string label, T value, UIRadioButtonGroup<T> group, Rectangle frame, UIButtonStyle styles, UIButtonAppearance activeAppearance, OnPressDelegate onPress) :
            base(label, frame, styles, onPress)
        {
            this.group = group;
            this.value = value;
            this.activeAppearance = activeAppearance;
        }

        public UIRadioButton(string label, T value, UIRadioButtonGroup<T> group, Rectangle frame, UIButtonStyle styles, UIButtonAppearance activeAppearance, OnRadioPressDelegate onRadioPress) :
            base(label, frame, styles, null)
        {
            this.group = group;
            this.value = value;
            this.activeAppearance = activeAppearance;
            this.onRadioPress = onRadioPress;
        }

        protected override void Pressed()
        {
            group.selectedButton = this;

            if(onRadioPress != null)
                onRadioPress(value);

            base.Pressed();
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 origin)
        {
            if (group.selectedButton == this)
            {
                activeAppearance.Draw(spriteBatch, label, icon, new Rectangle(frame.X + (int)origin.X, frame.Y + (int)origin.Y, frame.Width, frame.Height));
            }
            else
            {
                base.Draw(spriteBatch, origin);
            }
        }
    }
}
