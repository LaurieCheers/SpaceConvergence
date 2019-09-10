using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace LRCEngine
{
    public enum MouseButton
    {
        LEFT,
        MIDDLE,
        RIGHT
    }

    public class MouseButtonState
    {
        public MouseButton button;
        public bool isDown;
        public bool dragged;
        public int durationFrames;
        public Vector2 initialMousePos;

        public static float DRAG_THRESHOLD = 3.0f;
        public static float FRAMERATE = 1 / 30.0f;

        public MouseButtonState(MouseButton button, MouseState initialState)
        {
            this.button = button;
            isDown = IsButtonPressed(initialState);
            durationFrames = 100;
            initialMousePos = new Vector2(initialState.X, initialState.Y);
        }

        public void Update(MouseState state)
        {
            bool newPressed = IsButtonPressed(state);
            if( isDown != newPressed )
            {
                isDown = newPressed;
                durationFrames = 0;
                dragged = false;
                initialMousePos = new Vector2(state.X, state.Y);
            }
            else
            {
                durationFrames++;

                if (isDown && !dragged && (initialMousePos - new Vector2(state.X, state.Y)).LengthSquared() > DRAG_THRESHOLD * DRAG_THRESHOLD)
                {
                    dragged = true;
                }
            }
        }

        bool IsButtonPressed(MouseState state)
        {
            switch(button)
            {
                case MouseButton.LEFT:
                    return state.LeftButton == ButtonState.Pressed;
                case MouseButton.MIDDLE:
                    return state.MiddleButton == ButtonState.Pressed;
                case MouseButton.RIGHT:
                    return state.RightButton == ButtonState.Pressed;
            }
            return false;
        }

        public float duration {
            get { return durationFrames * FRAMERATE; }
        }

        public bool justPressed {
            get { return isDown && durationFrames == 0; }
        }

        public bool justReleased {
            get { return !isDown && durationFrames == 0; }
        }
    }

    public class InputState
    {
        MouseState oldMouse;
        public MouseState mouse { get; internal set; }
        KeyboardState oldKeyboard;
        public KeyboardState keyboard { get; internal set; }
        public bool pauseMouse { get; private set; }
        bool preFirstUpdate = true;
        bool firstUpdate = true;
        public UIMouseResponder hoveringElement;
        public UIMouseResponder hoveringElementMouseDown;

        public MouseButtonState mouseLeft;
        public MouseButtonState mouseMiddle;
        public MouseButtonState mouseRight;

        public void Update()
        {
            if (firstUpdate && !preFirstUpdate)
                firstUpdate = false;
            preFirstUpdate = false;
            oldKeyboard = keyboard;
            keyboard = Keyboard.GetState();
            /*if (WasKeyJustPressed(Keys.Space))
            {
                pauseMouse = !pauseMouse;
            }
            else if (IsKeyDown(Keys.Space) && pauseMouse && (WasMouseLeftJustPressed() || WasMouseRightJustPressed()))
            {
                // force an update if the user clicks
                mouse = Mouse.GetState();
            }*/

            if (pauseMouse)
            {
                mouse = oldMouse;
            }
            else
            {
                oldMouse = mouse;
                mouse = Mouse.GetState();
            }

            if (mouseLeft != null)
            {
                mouseLeft.Update(mouse);
                mouseMiddle.Update(mouse);
                mouseRight.Update(mouse);
            }
            else
            {
                mouseLeft = new MouseButtonState(MouseButton.LEFT, mouse);
                mouseMiddle = new MouseButtonState(MouseButton.MIDDLE, mouse);
                mouseRight = new MouseButtonState(MouseButton.RIGHT, mouse);
            }

            if (mouseLeft.justPressed)
            {
                hoveringElementMouseDown = hoveringElement;
            }

            hoveringElement = null;
        }

        public Vector2 MousePos { get { return new Vector2(mouse.X, mouse.Y); } }
        public Vector2 OldMousePos { get { return new Vector2(oldMouse.X, oldMouse.Y); } }
        public Vector2 MouseDelta { get { return new Vector2(mouse.X-oldMouse.X, mouse.Y-oldMouse.Y); } }

        public void UpdateMouseHover(IReadOnlyList<UIMouseResponder> list)
        {
            if (hoveringElement != null)
                return;

            for (int Idx = list.Count - 1; Idx >= 0; --Idx)
            {
                hoveringElement = list[Idx].GetMouseHover(MousePos);
                if (hoveringElement != null)
                    break;
            }
        }

        public bool WasMouseLeftJustPressed()
        {
            return mouseLeft.isDown && mouseLeft.duration == 0;
        }

        public bool WasMouseLeftJustReleased()
        {
            return !mouseLeft.isDown && mouseLeft.duration == 0;
        }

        public bool WasMouseRightJustPressed()
        {
            return mouseRight.isDown && mouseRight.duration == 0;
        }

        public bool WasMouseRightJustReleased()
        {
            return !mouseRight.isDown && mouseRight.duration == 0;
        }

        public bool WasKeyJustPressed(Keys key)
        {
            return keyboard.IsKeyDown(key) && !oldKeyboard.IsKeyDown(key);
        }

        public bool WasKeyJustReleased(Keys key)
        {
            return !keyboard.IsKeyDown(key) && oldKeyboard.IsKeyDown(key);
        }

        public bool IsKeyDown(Keys key)
        {
            return keyboard.IsKeyDown(key);
        }

        public bool IsKeyUp(Keys key)
        {
            return keyboard.IsKeyUp(key);
        }

        public Vector2 GetPseudoJoystick(Keys up, Keys down, Keys left, Keys right)
        {
            float upDown = 0.0f;
            if( keyboard.IsKeyDown(up) )
            {
                upDown = -1.0f;
            }
            else if( keyboard.IsKeyDown(down) )
            {
                upDown = 1.0f;
            }

            float leftRight = 0.0f;
            if (keyboard.IsKeyDown(left))
            {
                leftRight = -1.0f;
            }
            else if (keyboard.IsKeyDown(right))
            {
                leftRight = 1.0f;
            }

            return new Vector2(leftRight, upDown);
        }
    }
}
