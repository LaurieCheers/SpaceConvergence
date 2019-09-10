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
        public ConvergeZone resourceZone;
        public ConvergeZone attack;
        public ConvergeZone defense;
        public ConvergeZone hand;
        public ConvergeZone laboratory;
        public ConvergeZone discardPile;
        _Dictionary<ConvergeZoneId, ConvergeZone> zones;
        public ConvergeZone GetZone(ConvergeZoneId zoneId) { return zones[zoneId]; }
        public int life;
        public int numLandsPlayed;
        public int numLandsPlayable = 1;
        public ConvergeManaAmount resources = new ConvergeManaAmount();
        public bool[] showResources = new bool[6];
        public ConvergePlayer opponent;
        public bool faceLeft;
        public bool damagedThisTurn { get; private set; }
        public bool isActivePlayer { get { return this == Game1.activePlayer; } }
        public bool skipMana;

        public ConvergePlayer(JSONTable template, ContentManager Content)
        {
            this.home = new ConvergeZone(template.getJSON("home"), this, ConvergeZoneId.Home);
            this.resourceZone = new ConvergeZone(template.getJSON("resources"), this, ConvergeZoneId.Resources);
            this.attack = new ConvergeZone(template.getJSON("attack"), this, ConvergeZoneId.Attack);
            this.defense = new ConvergeZone(template.getJSON("defense"), this, ConvergeZoneId.Defense);
            this.hand = new ConvergeZone(template.getJSON("hand"), this, ConvergeZoneId.Hand);
            this.homeBase = new ConvergeObject(new ConvergeCardSpec(template.getJSON("homebase"), Content), home);
            this.discardPile = new ConvergeZone(template.getJSON("discardPile"), this, ConvergeZoneId.DiscardPile);
            this.laboratory = new ConvergeZone(template.getJSON("laboratory"), this, ConvergeZoneId.Laboratory);

            zones = new _Dictionary<ConvergeZoneId, ConvergeZone>()
            {
                {ConvergeZoneId.Home, home},
                {ConvergeZoneId.Resources, resourceZone},
                {ConvergeZoneId.Attack, attack},
                {ConvergeZoneId.Defense, defense},
                {ConvergeZoneId.Hand, hand},
                {ConvergeZoneId.DiscardPile, discardPile},
                {ConvergeZoneId.Laboratory, laboratory}
            };

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

        public void SkipMana()
        {
            skipMana = true;
        }

        public void TakeDamage(int amount)
        {
            life -= amount;
            damagedThisTurn = true;
        }

        public void GainLife(int amount)
        {
            life += amount;

            if (TriggerSystem.HasTriggers(ConvergeTriggerType.GainLife))
            {
                TriggerSystem.CheckTriggers(ConvergeTriggerType.GainLife, new TriggerData(this, null, null, amount));
            }
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
            damagedThisTurn = false;

            if (skipMana)
            {
                skipMana = false;
            }
            else
            {
                resources.Clear();
                foreach (ConvergeObject obj in resourceZone.contents)
                {
                    if (obj.produces != null)
                        resources.Add(obj.produces);
                }
            }

            numLandsPlayed = 0;
            attack.BeginMyTurn();
            defense.BeginMyTurn();
            home.BeginMyTurn();
            resourceZone.BeginMyTurn();
            DrawCards(1);
        }

        public void EndMyTurn()
        {
            damagedThisTurn = false;
        }

        public void SufferWounds()
        {
            attack.SufferWounds();
            defense.SufferWounds();
            home.SufferWounds();
            resourceZone.SufferWounds();
        }
    }
}
