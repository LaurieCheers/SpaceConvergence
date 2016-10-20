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

        public void DrawResources(SpriteBatch spriteBatch, ConvergeManaAmount spent, Vector2 drawPos)
        {
            for (int Idx = 0; Idx < amounts.Length; ++Idx)
            {
                int maxAmount = amounts[Idx];
                int currentAmount = maxAmount - spent.amounts[Idx];
                if (maxAmount > 0)
                {
                    spriteBatch.Draw(Game1.resourceTextures[Idx], drawPos, Color.White);
                    spriteBatch.DrawString(Game1.font, "" + currentAmount, drawPos + new Vector2(20, 0), currentAmount > 0 ? Color.Black : Color.DarkGreen);
                    drawPos.Y += 20;
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

        public bool TrySpend(ConvergeManaAmount resources, ConvergeManaAmount cost)
        {
            if (cost == null)
                return true;

            if (!CanSpend(resources, cost))
                return false;

            for (int Idx = 0; Idx < amounts.Length; ++Idx)
            {
                amounts[Idx] += cost.amounts[Idx];
            }

            return true;
        }

        public bool CanSpend(ConvergeManaAmount resources, ConvergeManaAmount cost)
        {
            if (cost == null)
                return true;

            for (int Idx = 0; Idx < amounts.Length; ++Idx)
            {
                if (resources.amounts[Idx] - amounts[Idx] < cost.amounts[Idx])
                    return false;
            }

            return true;
        }
    }
}
