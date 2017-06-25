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
    public interface UIMouseResponder
    {
        UIMouseResponder GetMouseHover(Vector2 localMousePos);
    }

    public abstract class UIElement: UIMouseResponder
    {
        public UIContainer parent;
        public abstract UIMouseResponder GetMouseHover(Vector2 localMousePos);
        public abstract void Update(InputState inputState, Vector2 origin);
        public void Update(InputState inputState) { Update(inputState, Vector2.Zero);  }
        public abstract void Draw(SpriteBatch spriteBatch, Vector2 origin);
        public void Draw(SpriteBatch spriteBatch) { Draw(spriteBatch, Vector2.Zero); }
    }

    public class UIContainer :UIElement
    {
        public Vector2 origin;
        public List<UIElement> elements = new List<UIElement>();

        public UIContainer()
        {

        }

        public UIContainer(Vector2 origin)
        {
            this.origin = origin;
        }

        public override UIMouseResponder GetMouseHover(Vector2 localMousePos)
        {
            Vector2 childMousePos = localMousePos - origin;
            for(int Idx = elements.Count-1; Idx >= 0; --Idx)
            {
                UIMouseResponder selected = elements[Idx].GetMouseHover(childMousePos);
                if (selected != null)
                    return selected;
            }

            return null;
        }

        public override void Update(InputState inputState, Vector2 origin)
        {
            Vector2 newOrigin = origin + this.origin;
            foreach (UIElement element in elements)
                element.Update(inputState, newOrigin);
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 origin)
        {
            Vector2 newOrigin = origin + this.origin;
            foreach (UIElement element in elements)
                element.Draw(spriteBatch, newOrigin);
        }

        public void Add(UIElement element)
        {
            elements.Add(element);
            element.parent = this;
        }

        public void Remove(UIElement element)
        {
            elements.Remove(element);
            element.parent = null;
        }

        public void Clear()
        {
            foreach(UIElement element in elements)
            {
                element.parent = null;
            }
            elements.Clear();
        }
    }
}
