using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LRCEngine;

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

    public enum ConvergeZoneId
    {
        Hand,
        Home, // battlefield (noncreatures)
        Defense, // battlefield (nonattacking creatures)
        Attack, // battlefield (attacking creatures)
        DiscardPile, // graveyard
        Laboratory,
        Space, // exile
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
        public readonly List<ConvergeActivatedAbility> activatedAbilities;

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

            activatedAbilities = new List<ConvergeActivatedAbility>();
            foreach(JSONTable abilityTemplate in template.getArray("activated", JSONArray.empty).asJSONTables())
            {
                activatedAbilities.Add(new ConvergeActivatedAbility(abilityTemplate, Content));
            }
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
        public ConvergeZone zone;
        public ConvergeKeyword keywords { get { return original.keywords; } }
        public List<ConvergeActivatedAbility> activatedAbilities { get { return original.activatedAbilities; } }
        public int slot;
        public ConvergeUIObject ui;
        public delegate void DealsDamage(ConvergeObject source, ConvergeObject target, int damageDealt, bool isCombatDamage);
        public event DealsDamage OnDealsDamage;

        public int shields;
        public int wounds;
        public bool tapped;
        public bool dead;

        public Vector2 nominalPosition
        {
            get { return zone != null? zone.GetNominalPos(slot): Vector2.Zero; }
        }

        public ConvergeObject(ConvergeCardSpec original, ConvergeZone zone)
        {
            this.original = original;
            this.shields = maxShields;
            this.wounds = 0;
            MoveZone(zone);
        }

        public void UseOn(ConvergeObject target)
        {
            if (this.cardType.HasFlag(ConvergeCardType.Unit) && target.cardType.HasFlag(ConvergeCardType.Unit) &&
                target.zone.zoneId == ConvergeZoneId.Attack &&
                this.zone.owner != target.zone.owner
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

        public void Play()
        {
            if(zone.zoneId == ConvergeZoneId.Hand)
            {
                if(this.cardType.HasFlag(ConvergeCardType.Support))
                {
                    if(zone.owner.numLandsPlayed < zone.owner.numLandsPlayable)
                    {
                        zone.owner.numLandsPlayed++;
                    }
                    else
                    {
                        return;
                    }
                }

                if (zone.owner.TryPayCost(cost))
                {
                    if (this.cardType.HasFlag(ConvergeCardType.Unit))
                    {
                        MoveZone(zone.owner.defense);
                        if (!this.keywords.HasFlag(ConvergeKeyword.Haste))
                        {
                            tapped = true;
                        }
                    }
                    else
                    {
                        MoveZone(zone.owner.home);
                    }
                }
            }
        }

        public void ClearOnLeavingPlay()
        {
            wounds = 0;
            tapped = false;
            shields = maxShields;
            dead = false;
        }

        public void DealDamage(ConvergeObject victim, int amount, bool isCombatDamage)
        {
            victim.TakeDamage(this, amount);

            if (keywords.HasFlag(ConvergeKeyword.Lifelink))
                zone.owner.GainLife(amount);
        }

        void TakeDamage(ConvergeObject source, int amount)
        {
            if (cardType.HasFlag(ConvergeCardType.Home))
            {
                zone.owner.TakeDamage(amount);
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

        public void MoveZone(ConvergeZone newZone)
        {
            Game1.zoneChanges.Add(new KeyValuePair<ConvergeObject, ConvergeZone>(this, newZone));
        }

        public bool CanBePlayed()
        {
            if (cost != null && !zone.owner.CanPayCost(cost))
                return false;

            if (cardType.HasFlag(ConvergeCardType.Support) && zone.owner.numLandsPlayed >= zone.owner.numLandsPlayable)
                return false;

            return true;
        }

        public void EnterAttack()
        {
            if(!tapped && zone.zoneId == ConvergeZoneId.Defense && cardType.HasFlag(ConvergeCardType.Unit))
                MoveZone(zone.owner.attack);
        }

        public void WithdrawAttack()
        {
            if (zone.zoneId == ConvergeZoneId.Attack && cardType.HasFlag(ConvergeCardType.Unit))
            {
                MoveZone(zone.owner.defense);
                if(!keywords.HasFlag(ConvergeKeyword.Vigilance))
                    tapped = true;
            }
        }

        public void BeginTurn()
        {
            if(zone.zoneId == ConvergeZoneId.Attack && !tapped)
                DealDamage(zone.owner.opponent.homeBase, power, true);
            tapped = false;
            shields = maxShields;
        }

        public void EndTurn()
        {
            if(wounds > 0)
            {
                dead = true;
                //zone.owner.discardPile.Add(this);
            }
        }
    }
}
