using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LRCEngine;
using System.Diagnostics;

namespace SpaceConvergence
{
    public enum ConvergeColor
    {
        Colorless = 0,
        White = 1,
        Blue = 2,
        Black = 4,
        Red = 8,
        Green = 16,
    }

    [Flags]
    public enum ConvergeCardType
    {
        Unit = 1, // creature
        Support = 2, // land
        Device = 4, // artifact
        Tech = 8, // enchantment
        Augment = 16, // Aura
        Planet = 32, // planeswalker
        Home = 64, // player
    };

    [Flags]
    public enum ConvergeZoneId
    {
        Hand = 1,
        Home = 2, // battlefield (noncreatures)
        Defense = 4, // battlefield (nonattacking creatures)
        Attack = 8, // battlefield (attacking creatures)
        DiscardPile = 0x10, // graveyard
        Laboratory = 0x20, // library
        Space = 0x40, // exile
        Play = Home|Defense|Attack,
    };

    [Flags]
    public enum ConvergeKeyword
    {
        None = 0,
        Flying = 1,
        Deathtouch = 2,
        FirstStrike = 4,
        DoubleStrike = 8,
        Lifelink = 0x10,
        Vigilance = 0x20,
        Haste = 0x40,
        Reach = 0x80,
        Trample = 0x100,
        CantBlock = 0x200,
    };

    [Flags]
    public enum ConvergeAltCost
    {
        None = 0,
        Tap = 1,
        Sacrifice = 2,
    };

    public enum ConvergeDuration
    {
        Permanent,
        ThisTurn,
        YourNextTurn,
        SourceLeavesPlay,
    }

    public static class ConvergeExtensions
    {
        public static int ToIndex(this ConvergeColor self)
        {
            switch(self)
            {
                case ConvergeColor.Colorless: return 0;
                case ConvergeColor.White: return 1;
                case ConvergeColor.Blue: return 2;
                case ConvergeColor.Black: return 3;
                case ConvergeColor.Red: return 4;
                case ConvergeColor.Green: return 5;
                default: throw new ArgumentException();
            }
        }
    }

    public class ConvergeCardSpec
    {
        public readonly Texture2D art;
        public readonly ConvergeCardType cardType;
        public readonly int power;
        public readonly int maxShields;
        public readonly ConvergeManaAmount produces;
        public readonly ConvergeManaAmount cost;
        public readonly ConvergeKeyword keywords;
        public readonly List<ConvergeActivatedAbilitySpec> activatedAbilities;

        public ConvergeCardSpec(JSONTable template, ContentManager Content)
        {
            art = Content.Load<Texture2D>(template.getString("art"));
            cardType = 0;
            foreach (string name in template.getArray("cardType").asStrings())
            {
                cardType |= (ConvergeCardType)Enum.Parse(typeof(ConvergeCardType), name);
            }
            power = template.getInt("power", 0);
            maxShields = template.getInt("shields", 0);
            string producesTemplate = template.getString("produces", "");
            if (producesTemplate != "")
            {
                produces = new ConvergeManaAmount(template.getString("produces"));
            }

            string costTemplate = template.getString("cost", "");
            if (costTemplate != "")
            {
                cost = new ConvergeManaAmount(costTemplate);
            }

            keywords = 0;
            foreach (string name in template.getArray("keywords", JSONArray.empty).asStrings())
            {
                keywords |= (ConvergeKeyword)Enum.Parse(typeof(ConvergeKeyword), name);
            }

            activatedAbilities = new List<ConvergeActivatedAbilitySpec>();
            foreach(JSONTable abilityTemplate in template.getArray("activated", JSONArray.empty).asJSONTables())
            {
                activatedAbilities.Add(new ConvergeActivatedAbilitySpec(abilityTemplate, Content));
            }
        }
    }

    public abstract class ConvergeEffect
    {
        public bool expired;
        int turns;
        public readonly ConvergeObject source;
        public readonly ConvergeDuration duration;

        public ConvergeEffect(ConvergeObject source, ConvergeDuration duration)
        {
            this.source = source;
            this.duration = duration;
            this.expired = false;
        }

        public void EndOfTurn()
        {
            turns++;
            switch(duration)
            {
                case ConvergeDuration.ThisTurn:
                    expired = true;
                    break;
                case ConvergeDuration.YourNextTurn:
                    if (turns > 1)
                        expired = true;
                    break;
                case ConvergeDuration.Permanent:
                    break;
                case ConvergeDuration.SourceLeavesPlay:
                    if (!source.zone.inPlay)
                        expired = true;
                    break;
            }
        }
    }

    public class ConvergeEffect_Control: ConvergeEffect
    {
        public readonly ConvergePlayer controller;

        public ConvergeEffect_Control(ConvergePlayer controller, ConvergeObject source, ConvergeDuration duration): base(source, duration)
        {
            this.controller = controller;
        }
    }

    public class ConvergeObject
    {
        ConvergeCardSpec original;
        public Texture2D art { get { return original.art; } }
        public ConvergeCardType cardType { get { return original.cardType; } }
        public int power { get { return original.power; } }
        public int maxShields { get { return original.maxShields; } }
        public ConvergeManaAmount produces { get { return original.produces; } }
        public ConvergeManaAmount cost { get { return original.cost; } }
        public ConvergePlayer owner;
        public ConvergeZone zone;
        public ConvergeKeyword keywords { get { return original.keywords; } }
        public int slot;
        public ConvergeUIObject ui;
        public delegate void DealsDamage(ConvergeObject source, ConvergeObject target, int damageDealt, bool isCombatDamage);
        public event DealsDamage OnDealsDamage;

        public ConvergePlayer controller { get; private set; }
        List<ConvergeEffect_Control> controlEffects = new List<ConvergeEffect_Control>();

        public int shields;
        public int wounds;
        public bool tapped;
        public bool dead;
        public List<ConvergeActivatedAbility> activatedAbilities;

        public Vector2 nominalPosition
        {
            get { return zone != null? zone.GetNominalPos(slot): Vector2.Zero; }
        }

        public ConvergeObject(ConvergeCardSpec original, ConvergeZone zone)
        {
            this.original = original;
            this.shields = maxShields;
            this.wounds = 0;
            this.owner = zone.owner;
            this.controller = owner;
            activatedAbilities = new List<ConvergeActivatedAbility>();
            foreach(ConvergeActivatedAbilitySpec spec in original.activatedAbilities)
            {
                activatedAbilities.Add(new ConvergeActivatedAbility(spec, this));
            }
            MoveZone(zone);
        }

        public void AddEffect(ConvergeEffect_Control controlEffect)
        {
            controlEffects.Add(controlEffect);
            UpdateController();
        }

        public void UseOn(ConvergeObject target)
        {
            if (this.cardType.HasFlag(ConvergeCardType.Unit) && target.cardType.HasFlag(ConvergeCardType.Unit) &&
                target.zone.zoneId == ConvergeZoneId.Attack &&
                this.controller != target.controller
                )
            {
                // see if we can block this creature
                if (this.tapped)
                    return;

                if (this.zone.zoneId != ConvergeZoneId.Defense && this.zone.zoneId != ConvergeZoneId.Attack && !this.keywords.HasFlag(ConvergeKeyword.Vigilance))
                    return;

                if (target.keywords.HasFlag(ConvergeKeyword.Flying) &&
                    !this.keywords.HasFlag(ConvergeKeyword.Flying) &&
                    !this.keywords.HasFlag(ConvergeKeyword.Reach))
                {
                    return;
                }

                int incomingDamage = target.power;
                int dealtDamage = power;
                tapped = true;

                if(!target.keywords.HasFlag(ConvergeKeyword.Trample))
                    target.tapped = true;

                DealDamage(target, dealtDamage, true);
                target.DealDamage(this, incomingDamage, true);
            }
        }

        public void Play(ConvergePlayer you)
        {
            if(zone.zoneId == ConvergeZoneId.Hand)
            {
                if(this.cardType.HasFlag(ConvergeCardType.Support))
                {
                    if(you.numLandsPlayed < you.numLandsPlayable)
                    {
                        you.numLandsPlayed++;
                    }
                    else
                    {
                        return;
                    }
                }

                if (you.TryPayCost(cost))
                {
                    if (this.cardType.HasFlag(ConvergeCardType.Unit))
                    {
                        MoveZone(you.defense);
                        if (!this.keywords.HasFlag(ConvergeKeyword.Haste))
                        {
                            tapped = true;
                        }
                    }
                    else
                    {
                        MoveZone(you.home);
                    }
                }
            }
        }

        public void DealDamage(ConvergeObject victim, int amount, bool isCombatDamage)
        {
            victim.TakeDamage(this, amount);

            if (keywords.HasFlag(ConvergeKeyword.Lifelink))
                controller.GainLife(amount);
        }

        void TakeDamage(ConvergeObject source, int amount)
        {
            if (cardType.HasFlag(ConvergeCardType.Home))
            {
                controller.TakeDamage(amount);
            }
            else
            {
                if (shields <= amount)
                {
                    wounds += 1 + (amount - shields);
                    shields = 0;
                }
                else
                {
                    shields -= amount;
                    if (source.keywords.HasFlag(ConvergeKeyword.Deathtouch))
                    {
                        wounds += 1;
                        shields = 0;
                    }
                }
            }
        }

        public void Heal(int amount)
        {
            if (wounds > amount)
                wounds -= amount;
            else
                wounds = 0;
        }

        public void MoveZone(ConvergeZone newZone)
        {
            Game1.zoneChanges.Add(new KeyValuePair<ConvergeObject, ConvergeZone>(this, newZone));
        }

        public bool CanBePlayed(ConvergePlayer you)
        {
            if (cost != null && !you.CanPayCost(cost))
                return false;

            if (cardType.HasFlag(ConvergeCardType.Support) && you.numLandsPlayed >= you.numLandsPlayable)
                return false;

            return true;
        }

        public bool CanPayAltCost(ConvergeAltCost altCost)
        {
            if (tapped && altCost.HasFlag(ConvergeAltCost.Tap))
                return false;

            return true;
        }

        public bool TryPayAltCost(ConvergeAltCost altCost)
        {
            if (!CanPayAltCost(altCost))
                return false;

            if (altCost.HasFlag(ConvergeAltCost.Tap))
                tapped = true;

            if (altCost.HasFlag(ConvergeAltCost.Sacrifice))
                MoveZone(owner.discardPile);

            return true;
        }

        public void EnterAttack()
        {
            if(!tapped && zone.zoneId == ConvergeZoneId.Defense && cardType.HasFlag(ConvergeCardType.Unit))
                MoveZone(controller.attack);
        }

        public void WithdrawAttack()
        {
            if (zone.zoneId == ConvergeZoneId.Attack && cardType.HasFlag(ConvergeCardType.Unit))
            {
                MoveZone(controller.defense);
                if(!keywords.HasFlag(ConvergeKeyword.Vigilance))
                    tapped = true;
            }
        }

        public void BeginMyTurn()
        {
            if(zone.zoneId == ConvergeZoneId.Attack && !tapped)
                DealDamage(controller.opponent.homeBase, power, true);
            tapped = false;
            shields = maxShields;
        }

        public void EndMyTurn()
        {
            if (wounds > 0)
            {
                dead = true;
                //MoveZone(controller.discardPile);
            }
        }

        public void BeginAnyTurn(ConvergePlayer activePlayer)
        {
            bool anyExpired = false;
            for(int Idx = 0; Idx < controlEffects.Count; )
            {
                ConvergeEffect_Control effect = controlEffects[Idx];
                effect.EndOfTurn();
                if (effect.expired)
                {
                    controlEffects.RemoveAt(Idx);
                    anyExpired = true;
                }
                else
                    ++Idx;
            }

            if(anyExpired)
                UpdateController();
        }

        void UpdateController()
        {
            ConvergePlayer oldController = controller;
            if (controlEffects.Count == 0)
                controller = owner;
            else
                controller = controlEffects.Last().controller;

            if(controller != oldController)
            {
                if (zone.zoneId == ConvergeZoneId.Home)
                    MoveZone(controller.home);
                else if (zone.zoneId == ConvergeZoneId.Attack || zone.zoneId == ConvergeZoneId.Defense)
                    MoveZone(controller.defense);
                else
                    Debug.Assert(false);
            }
        }

        public void OnEnteringPlay()
        {
            Game1.inPlayList.Add(this);
            foreach(ConvergeActivatedAbility activatedAbility in activatedAbilities)
            {
                activatedAbility.OnEnteringPlay();
            }
        }

        // this is called after the object has actually been added to its new zone
        public void OnLeavingPlay()
        {
            wounds = 0;
            tapped = false;
            shields = maxShields;
            dead = false;
            Game1.inPlayList.Remove(this);
            controlEffects.Clear();
            UpdateController();
        }
    }
}
