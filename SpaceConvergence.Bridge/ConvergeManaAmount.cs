using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceConvergence
{
    public class ConvergeManaAmount
    {
        int[] amounts;

        public ConvergeManaAmount()
        {
            amounts = new int[6];
        }

        public ConvergeManaAmount(string template)
        {
            amounts = new int[6];
            foreach (char c in template)
            {
                switch (c)
                {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        amounts[0] += (c - '0');
                        break;
                    case 'C':
                        amounts[0]++;
                        break;
                    case 'W':
                        amounts[1]++;
                        break;
                    case 'U':
                        amounts[2]++;
                        break;
                    case 'B':
                        amounts[3]++;
                        break;
                    case 'R':
                        amounts[4]++;
                        break;
                    case 'G':
                        amounts[5]++;
                        break;
                    default: throw new ArgumentException();
                }
            }
        }

        public void Clear()
        {
            for (int Idx = 0; Idx < amounts.Length; ++Idx)
                amounts[Idx] = 0;
        }

        public void Add(ConvergeManaAmount other)
        {
            for (int Idx = 0; Idx < amounts.Length; ++Idx)
                amounts[Idx] += other.amounts[Idx];
        }

        public void DrawResources(SpriteBatch spriteBatch, bool[] showResources, Vector2 drawPos)
        {
            for (int Idx = 0; Idx < amounts.Length; ++Idx)
            {
                int currentAmount = amounts[Idx];
                if (currentAmount > 0 || (showResources != null && showResources[Idx]))
                {
                    spriteBatch.Draw(Game1.resourceTextures[Idx], drawPos, Color.White);
                    spriteBatch.DrawString(Game1.font, "" + currentAmount, drawPos + new Vector2(20, 0), currentAmount > 0 ? Color.Black : Color.DarkGreen);
                    drawPos.Y += 20;
                    if(showResources != null)
                        showResources[Idx] = true;
                }
            }
        }

        public void DrawCost(SpriteBatch spriteBatch, Vector2 drawPos)
        {
            for (int Idx = 0; Idx < amounts.Length; ++Idx)
            {
                for (int amount = amounts[Idx]; amount > 0; --amount)
                {
                    spriteBatch.Draw(Game1.resourceTextures[Idx], drawPos, Color.White);
                    drawPos.X += 18;
                }
            }
        }

        public bool TrySpend(ConvergeManaAmount cost)
        {
            if (cost == null)
                return true;

            if (!CanSpend(cost))
                return false;

            for (int Idx = 0; Idx < amounts.Length; ++Idx)
            {
                amounts[Idx] -= cost.amounts[Idx];
            }

            return true;
        }

        public bool CanSpend(ConvergeManaAmount cost)
        {
            if (cost == null)
                return true;

            for (int Idx = 0; Idx < amounts.Length; ++Idx)
            {
                if (amounts[Idx] < cost.amounts[Idx])
                    return false;
            }

            return true;
        }

        public ConvergeColor GetColor()
        {
            ConvergeColor color = 0;
            for (int Idx = 1; Idx < amounts.Length; ++Idx)
            {
                if(amounts[Idx] > 0)
                {
                    color |= (ConvergeColor)(1 << (Idx-1));
                }
            }
            return color;
        }
    }
}
