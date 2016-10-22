using LRCEngine;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceConvergence
{
    public class ConvergePlayer
    {
        public ConvergeObject homeBase;
        public ConvergeZone home;
        public ConvergeZone attack;
        public ConvergeZone defense;
        public ConvergeZone hand;
        public ConvergeZone laboratory;
        public ConvergeZone discardPile;
        public int life;
        public int numLandsPlayed;
        public int numLandsPlayable = 1;
        public ConvergeManaAmount resources = new ConvergeManaAmount();
        public bool[] showResources = new bool[6];
        public ConvergePlayer opponent;
        public bool faceLeft;
        public bool isActivePlayer { get { return this == Game1.activePlayer; } }

        public ConvergePlayer(JSONTable template, ContentManager Content)
        {
            this.home = new ConvergeZone(template.getJSON("home"), this, ConvergeZoneId.Home);
            this.attack = new ConvergeZone(template.getJSON("attack"), this, ConvergeZoneId.Attack);
            this.defense = new ConvergeZone(template.getJSON("defense"), this, ConvergeZoneId.Defense);
            this.hand = new ConvergeZone(template.getJSON("hand"), this, ConvergeZoneId.Hand);
            this.homeBase = new ConvergeObject(new ConvergeCardSpec(template.getJSON("homebase"), Content), home);
            this.discardPile = new ConvergeZone(template.getJSON("discardPile"), this, ConvergeZoneId.DiscardPile);
            this.laboratory = new ConvergeZone(template.getJSON("laboratory"), this, ConvergeZoneId.Laboratory);
            this.life = template.getInt("startingLife");
            this.faceLeft = template.getBool("faceLeft", false);
        }

        public bool CanPayCost(ConvergeManaAmount cost)
        {
            return resources.CanSpend(cost);
        }

        public bool TryPayCost(ConvergeManaAmount cost)
        {
            return resources.TrySpend(cost);
        }

        public void TakeDamage(int amount)
        {
            life -= amount;
        }

        public void GainLife(int amount)
        {
            life += amount;
        }

        public void DrawCards(int n)
        {
            if (laboratory.contents.Count < n)
                n = laboratory.contents.Count;

            for (int Idx = 0; Idx < n; ++Idx)
            {
                ConvergeObject drawn = laboratory.contents[Idx];
                drawn.MoveZone(hand);
            }
        }

        public void BeginGame()
        {
            laboratory.Shuffle();
            DrawCards(7);
        }

        public void BeginMyTurn()
        {
            resources.Clear();
            foreach(ConvergeObject obj in home.contents)
            {
                if(obj.produces != null)
                    resources.Add(obj.produces);
            }

            numLandsPlayed = 0;
            attack.BeginMyTurn();
            defense.BeginMyTurn();
            home.BeginMyTurn();
            DrawCards(1);
        }

        public void EndMyTurn()
        {
            attack.EndMyTurn();
            defense.EndMyTurn();
            home.EndMyTurn();
        }
    }
}
