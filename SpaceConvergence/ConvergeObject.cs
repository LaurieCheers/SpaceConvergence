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
        Green = 0x10,
        _Multicolored = 0x20,
    }

    [Flags]
    public enum ConvergeCardType
    {
        Unit = 1, // creature
        Resource = 2, // land
        Device = 4, // artifact
        Tech = 8, // enchantment
        Augment = 0x10, // Aura
        Planet = 0x20, // planeswalker
        Home = 0x40, // player
        Action = 0x80, // instant/sorcery
    };

    [Flags]
    public enum ConvergeZoneId
    {
        Hand = 1,
        Home = 2, // battlefield (noncreatures)
        Resources = 4, // battlefield (lands)
        Defense = 8, // battlefield (nonattacking creatures)
        Attack = 0x10, // battlefield (attacking creatures)
        DiscardPile = 0x20, // graveyard
        Laboratory = 0x40, // library
        Space = 0x80, // exile
        Play = Home|Resources|Defense|Attack,
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
        Hunt = 0x400,

        _Last = Hunt,
    };

    [Flags]
    public enum ConvergeAltCost
    {
        None = 0,
        Tap = 1,
        Sacrifice = 2,
        Discard = 4,
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
            switch (self)
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

        public static ConvergeKeyword ToKeywords(this JSONArray template)
        {
            ConvergeKeyword result = 0;
            foreach (string name in template.asStrings())
            {
                result |= (ConvergeKeyword)Enum.Parse(typeof(ConvergeKeyword), name);
            }
            return result;
        }

        public static bool TickTurn<T> (this List<T> effectsList) where T : ConvergeEffect
        {
            bool anyExpired = false;
            for (int Idx = 0; Idx < effectsList.Count;)
            {
                ConvergeEffect effect = effectsList[Idx];
                effect.EndOfTurn();
                if (effect.expired)
                {
                    effectsList.RemoveAt(Idx);
                    anyExpired = true;
                }
                else
                    ++Idx;
            }
            return anyExpired;
        }
    }

    public class ConvergeCardSpec
    {
        public string name { get; private set; }
        public string text { get; private set; }
        public int textHeight { get; private set; }
        public Texture2D art { get; private set; }
        public ConvergeCardType cardType { get; private set; }
        public int power { get; private set; }
        public int toughness { get; private set; }
        public ConvergeManaAmount produces { get; private set; }
        public ConvergeManaAmount cost { get; private set; }
        public ConvergeKeyword keywords { get; private set; }
        public List<ConvergeActivatedAbilitySpec> activatedAbilities { get; private set; }
        public List<ConvergeTriggeredAbilitySpec> triggeredAbilities { get; private set; }

        public ConvergeSelector actionTarget { get; private set; }
        public ConvergeCommand actionEffect { get; private set; }

        public static readonly Dictionary<string, ConvergeCardSpec> allCards = new Dictionary<string, ConvergeCardSpec>();

        public ConvergeCardSpec()
        {
        }

        public ConvergeCardSpec(JSONTable template, ContentManager Content)
        {
            Init(template, Content);
        }

        public void Init(JSONTable template, ContentManager Content)
        {
            string artName = template.getString("art");
            art = Content.Load<Texture2D>(artName);
            name = template.getString("name", artName); // for now
            text = template.getString("text", "").InsertLineBreaks(Game1.font, ConvergeUIObject.CardTooltipWidth-15);
            textHeight = (int)Game1.font.MeasureString(text).Y;
            cardType = 0;
            foreach (string name in template.getArray("cardType").asStrings())
            {
                cardType |= (ConvergeCardType)Enum.Parse(typeof(ConvergeCardType), name);
            }
            power = template.getInt("power", 0);
            toughness = template.getInt("toughness", 0);
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

            triggeredAbilities = new List<ConvergeTriggeredAbilitySpec>();
            foreach (JSONTable abilityTemplate in template.getArray("triggered", JSONArray.empty).asJSONTables())
            {
                triggeredAbilities.Add(new ConvergeTriggeredAbilitySpec(abilityTemplate, Content));
            }

            if (template.hasKey("effect"))
            {
                actionEffect = ConvergeCommand.New(template.getArray("effect"), Content);
            }
            if (template.hasKey("target"))
            {
                actionTarget = ConvergeSelector.New(template.getProperty("target"));
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

    public class ConvergeEffect_Upgrade : ConvergeEffect
    {
        public readonly int power;
        public readonly int toughness;
        public readonly ConvergeKeyword keywords;
        public readonly Texture2D new_art;

        public ConvergeEffect_Upgrade(int power, int toughness, ConvergeKeyword keywords, ConvergeObject source, Texture2D new_art, ConvergeDuration duration) : base(source, duration)
        {
            this.power = power;
            this.toughness = toughness;
            this.keywords = keywords;
            this.new_art = new_art;
        }
    }

    public class ConvergeEffect_GainActivated : ConvergeEffect
    {
        public readonly ConvergeActivatedAbility ability;

        public ConvergeEffect_GainActivated(ConvergeActivatedAbilitySpec abilitySpec, ConvergeObject subject, ConvergeObject source, ConvergeDuration duration) : base(source, duration)
        {
            this.ability = new ConvergeActivatedAbility(abilitySpec, subject);
        }
    }

    public class ConvergeEffect_GainTriggered : ConvergeEffect
    {
        public readonly ConvergeTriggeredAbility ability;

        public ConvergeEffect_GainTriggered(ConvergeTriggeredAbilitySpec abilitySpec, ConvergeObject subject, ConvergeObject source, ConvergeDuration duration) : base(source, duration)
        {
            this.ability = new ConvergeTriggeredAbility(abilitySpec, subject);
        }
    }

    public class ConvergeObject
    {
        ConvergeCardSpec original;
        public Texture2D art;
        public ConvergeCardType cardType { get { return original.cardType; } }
        public int power;
        public int effectivePower { get { return power - powerUsed; } }
        public int toughness;
        public int effectiveToughness { get { return toughness - damage; } }
        public ConvergeManaAmount produces { get { return original.produces; } }
        public ConvergeManaAmount cost { get { return original.cost; } }
        public ConvergePlayer owner;
        public ConvergeZone zone;
        public ConvergeKeyword keywords;
        public int slot;
        public ConvergeUIObject ui;
        public string name { get { return original.name; } }
        public string text { get { return original.text; } }
        public int textHeight { get { return original.textHeight; } }
        public delegate void DealsDamage(ConvergeObject source, ConvergeObject target, int damageDealt, bool isCombatDamage);
        public event DealsDamage OnDealsDamage;

        public ConvergePlayer controller { get; private set; }
        List<ConvergeEffect_Control> controlEffects = new List<ConvergeEffect_Control>();
        List<ConvergeEffect_Upgrade> upgradeEffects = new List<ConvergeEffect_Upgrade>();
        List<ConvergeEffect_GainActivated> extraActivatedEffects = new List<ConvergeEffect_GainActivated>();
        List<ConvergeEffect_GainTriggered> extraTriggeredEffects = new List<ConvergeEffect_GainTriggered>();

        public bool hasUpgrades { get { return upgradeEffects.Count > 0; } }

        public int powerUsed;
        public int damage;
        public bool destroyed;
        public bool tapped;
        public List<ConvergeActivatedAbility> activatedAbilities;
        public List<ConvergeTriggeredAbility> triggeredAbilities;
        List<ConvergeActivatedAbility> originalActivatedAbilities;
        List<ConvergeTriggeredAbility> originalTriggeredAbilities;

        public string keywordsText { get; private set; }

        public bool dying { get { return cardType.HasFlag(ConvergeCardType.Unit) && (damage >= toughness || destroyed); } }

        public Vector2 nominalPosition
        {
            get { return zone != null? zone.GetNominalPos(slot): Vector2.Zero; }
        }

        public ConvergeObject(ConvergeCardSpec original, ConvergeZone zone)
        {
            this.original = original;
            this.art = original.art;
            this.power = original.power;
            this.toughness = original.toughness;
            this.keywords = original.keywords;
            GenerateKeywordsText();
            this.owner = zone.owner;
            this.controller = owner;
            this.originalActivatedAbilities = new List<ConvergeActivatedAbility>();
            foreach(ConvergeActivatedAbilitySpec spec in original.activatedAbilities)
            {
                originalActivatedAbilities.Add(new ConvergeActivatedAbility(spec, this));
            }
            activatedAbilities = originalActivatedAbilities;

            this.originalTriggeredAbilities = new List<ConvergeTriggeredAbility>();
            foreach (ConvergeTriggeredAbilitySpec spec in original.triggeredAbilities)
            {
                originalTriggeredAbilities.Add(new ConvergeTriggeredAbility(spec, this));
            }
            this.triggeredAbilities = originalTriggeredAbilities;
            MoveZone(zone);
        }

        public void AddEffect(ConvergeEffect_Control controlEffect)
        {
            controlEffects.Add(controlEffect);
            UpdateController();
        }
        public void AddEffect(ConvergeEffect_Upgrade upgradeEffect)
        {
            upgradeEffects.Add(upgradeEffect);
            UpdateUpgrades();
        }
        public void AddEffect(ConvergeEffect_GainActivated gainActivatedEffect)
        {
            extraActivatedEffects.Add(gainActivatedEffect);
            UpdateActivated();
        }
        public void AddEffect(ConvergeEffect_GainTriggered gainTriggeredEffect)
        {
            extraTriggeredEffects.Add(gainTriggeredEffect);
            UpdateTriggered();
        }

        public void UseOn(ConvergeObject target)
        {
            if (this.cardType.HasFlag(ConvergeCardType.Unit) && target.cardType.HasFlag(ConvergeCardType.Unit) &&
                this.controller != target.controller
                )
            {
                // see if we can fight this creature
                if (target.zone.zoneId != ConvergeZoneId.Attack)
                    return;

                if (this.tapped || this.keywords.HasFlag(ConvergeKeyword.CantBlock))
                    return;

                if (this.zone.zoneId != ConvergeZoneId.Defense && this.zone.zoneId != ConvergeZoneId.Attack && !this.keywords.HasFlag(ConvergeKeyword.Vigilance))
                    return;

                if (target.keywords.HasFlag(ConvergeKeyword.Flying) &&
                    !this.keywords.HasFlag(ConvergeKeyword.Flying) &&
                    !this.keywords.HasFlag(ConvergeKeyword.Reach))
                {
                    return;
                }

                tapped = true;

                if (!target.keywords.HasFlag(ConvergeKeyword.Trample))
                    target.tapped = true;

                Fight(target, true);
            }
            else if(original.actionTarget != null && zone.zoneId == ConvergeZoneId.Hand)
            {
                // playing an action/augment from my hand
                PlayOn(target, Game1.activePlayer);
            }
        }

        public void Fight(ConvergeObject target, bool isCombatDamage)
        {
            if (dying || target.dying)
                return;

            int selfPowerUsed = GetPowerToUse(target.effectiveToughness);
            powerUsed += selfPowerUsed;
            
            int targetPowerUsed = target.GetPowerToUse(effectiveToughness);
            target.powerUsed += targetPowerUsed;

            int incomingFirstDamage;
            int incomingNormalDamage;
            int dealtFirstDamage;
            int dealtNormalDamage;

            if (target.keywords.HasFlag(ConvergeKeyword.DoubleStrike))
            {
                incomingFirstDamage = targetPowerUsed;
                incomingNormalDamage = targetPowerUsed;
            }
            else if (target.keywords.HasFlag(ConvergeKeyword.FirstStrike))
            {
                incomingFirstDamage = targetPowerUsed;
                incomingNormalDamage = 0;
            }
            else
            {
                incomingFirstDamage = 0;
                incomingNormalDamage = targetPowerUsed;
            }

            if (keywords.HasFlag(ConvergeKeyword.DoubleStrike))
            {
                dealtFirstDamage = selfPowerUsed;
                dealtNormalDamage = selfPowerUsed;
            }
            else if (keywords.HasFlag(ConvergeKeyword.FirstStrike))
            {
                dealtFirstDamage = selfPowerUsed;
                dealtNormalDamage = 0;
            }
            else
            {
                dealtFirstDamage = 0;
                dealtNormalDamage = selfPowerUsed;
            }

            DealDamage(target, dealtFirstDamage, isCombatDamage);
            target.DealDamage(this, incomingFirstDamage, isCombatDamage);

            if (dying || target.dying)
                return;

            DealDamage(target, dealtNormalDamage, isCombatDamage);
            target.DealDamage(this, incomingNormalDamage, isCombatDamage);
        }

        int GetPowerToUse(int targetEffectiveToughness)
        {
            if (keywords.HasFlag(ConvergeKeyword.Deathtouch) && effectivePower >= 1)
                return 1;
            else if (targetEffectiveToughness < effectivePower)
                return targetEffectiveToughness;
            else
                return effectivePower;
        }

        public void Play(ConvergePlayer you)
        {
            if(zone.zoneId == ConvergeZoneId.Hand)
            {
                if(this.cardType.HasFlag(ConvergeCardType.Resource))
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
                else if(original.actionTarget != null)
                {
                    // this spell needs a target
                    return;
                }

                if (TriggerSystem.HasTriggers(ConvergeTriggerType.PlayCard))
                {
                    TriggerSystem.CheckTriggers(ConvergeTriggerType.PlayCard, new TriggerData(you, this, null, 0));
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
                    else if (this.cardType.HasFlag(ConvergeCardType.Action))
                    {
                        ConvergeEffectContext context = new ConvergeEffectContext(this, you);
                        original.actionEffect.Run(context);
                        MoveZone(owner.discardPile);
                    }
                    else if (this.cardType.HasFlag(ConvergeCardType.Resource) && this.activatedAbilities.Count == 0)
                    {
                        MoveZone(you.resourceZone);
                    }
                    else
                    {
                        MoveZone(you.home);
                    }
                }
            }
        }

        public bool CanTarget(ConvergeObject target, ConvergePlayer you)
        {
            if (original.actionTarget == null)
                return false;

            ConvergeEffectContext context = new ConvergeEffectContext(this, you);
            return original.actionTarget.Test(target, context);
        }

        public void PlayOn(ConvergeObject target, ConvergePlayer you)
        {
            if (zone.zoneId == ConvergeZoneId.Hand &&
                (cardType.HasFlag(ConvergeCardType.Action) || cardType.HasFlag(ConvergeCardType.Augment)) &&
                CanTarget(target, you)
                )
            {
                if (you.TryPayCost(cost))
                {
                    if (TriggerSystem.HasTriggers(ConvergeTriggerType.PlayCard))
                    {
                        TriggerSystem.CheckTriggers(ConvergeTriggerType.PlayCard, new TriggerData(you, this, target, 0));
                    }

                    ConvergeEffectContext context = new ConvergeEffectContext(this, you);
                    context.target = target;
                    original.actionEffect.Run(context);
                    MoveZone(owner.discardPile);
                }
            }
        }

        public void DealDamage(ConvergeObject victim, int amount, bool isCombatDamage)
        {
            if (TriggerSystem.HasTriggers(ConvergeTriggerType.DealDamage))
            {
                TriggerSystem.CheckTriggers(ConvergeTriggerType.DealDamage, new TriggerData(controller, this, victim, amount));
            }

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
                damage += amount;
                if (source.keywords.HasFlag(ConvergeKeyword.Deathtouch))
                {
                    destroyed = true;
                }
            }
        }

        public void Heal(int amount)
        {
            if (damage > amount)
                damage -= amount;
            else
                damage = 0;
        }

        public void MoveZone(ConvergeZone newZone)
        {
            Game1.zoneChanges.Add(new KeyValuePair<ConvergeObject, ConvergeZone>(this, newZone));
        }

        public bool CanBePlayed(ConvergePlayer you)
        {
            if (cost != null && !you.CanPayCost(cost))
                return false;

            if (cardType.HasFlag(ConvergeCardType.Resource) && you.numLandsPlayed >= you.numLandsPlayable)
                return false;

            return true;
        }

        public bool CanPayAltCost(ConvergeAltCost altCost)
        {
            if (tapped && altCost.HasFlag(ConvergeAltCost.Tap))
                return false;

            if (altCost.HasFlag(ConvergeAltCost.Sacrifice | ConvergeAltCost.Tap) && (zone.zoneId & ConvergeZoneId.Play) == 0)
                return false;

            if (altCost.HasFlag(ConvergeAltCost.Discard) && zone.zoneId != ConvergeZoneId.Hand)
                return false;

            return true;
        }

        public bool TryPayAltCost(ConvergeAltCost altCost)
        {
            if (!CanPayAltCost(altCost))
                return false;

            if (altCost.HasFlag(ConvergeAltCost.Tap))
                tapped = true;

            if ((altCost & (ConvergeAltCost.Sacrifice | ConvergeAltCost.Discard)) != 0)
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
                tapped = true;
            }
        }

        public void BeginMyTurn()
        {
            if (zone.zoneId == ConvergeZoneId.Attack && !tapped && !dying)
            {
                DealDamage(controller.opponent.homeBase, effectivePower, true);

                if(keywords.HasFlag(ConvergeKeyword.DoubleStrike))
                {
                    DealDamage(controller.opponent.homeBase, effectivePower, true);
                }
            }
            
            if (keywords.HasFlag(ConvergeKeyword.Vigilance))
            {
                WithdrawAttack();
                tapped = false;
            }
            tapped = false;
            damage = 0;
            powerUsed = 0;
        }

        public void SufferWounds()
        {
            if (dying)
            {
                MoveZone(controller.discardPile);
            }
        }

        public void BeginAnyTurn(ConvergePlayer activePlayer)
        {
            if(controlEffects.TickTurn())
                UpdateController();

            if (upgradeEffects.TickTurn())
                UpdateUpgrades();

            if (extraActivatedEffects.TickTurn())
                UpdateActivated();

            if (extraTriggeredEffects.TickTurn())
                UpdateTriggered();
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
                if (zone.zoneId == ConvergeZoneId.Attack)
                    MoveZone(controller.defense);
                else
                    MoveZone(controller.GetZone(zone.zoneId));
            }
        }

        void UpdateUpgrades()
        {
            power = original.power;
            toughness = original.toughness;
            keywords = original.keywords;
            art = original.art;
            foreach (ConvergeEffect_Upgrade upgradeEffect in upgradeEffects)
            {
                power += upgradeEffect.power;
                toughness += upgradeEffect.toughness;
                keywords |= upgradeEffect.keywords;
                if(upgradeEffect.new_art != null)
                    art = upgradeEffect.new_art;
            }
            GenerateKeywordsText();
        }

        void GenerateKeywordsText()
        {
            keywordsText = "";
            for(int KeywordIdx = 1; KeywordIdx <= (int)ConvergeKeyword._Last; KeywordIdx <<= 1)
            {
                if (keywords.HasFlag((ConvergeKeyword)KeywordIdx))
                {
                    if (keywordsText != "")
                        keywordsText += ", ";

                    ConvergeKeyword keyword = (ConvergeKeyword)KeywordIdx;
                    switch(keyword)
                    {
                        case ConvergeKeyword.CantBlock:
                            keywordsText += "Can't Block";
                            break;
                        case ConvergeKeyword.DoubleStrike:
                            keywordsText += "Double Strike";
                            break;
                        case ConvergeKeyword.FirstStrike:
                            keywordsText += "First Strike";
                            break;
                        default:
                            keywordsText += (ConvergeKeyword)KeywordIdx;
                            break;
                    }
                }
            }
        }

        void UpdateActivated()
        {
            activatedAbilities = originalActivatedAbilities.ToList();
            foreach(ConvergeEffect_GainActivated effect in extraActivatedEffects)
            {
                activatedAbilities.Add(effect.ability);
            }
        }

        void UpdateTriggered()
        {
            foreach (ConvergeTriggeredAbility triggeredAbility in triggeredAbilities)
            {
                triggeredAbility.OnLeavingPlay();
            }

            triggeredAbilities = originalTriggeredAbilities.ToList();
            foreach (ConvergeEffect_GainTriggered effect in extraTriggeredEffects)
            {
                triggeredAbilities.Add(effect.ability);
            }

            foreach (ConvergeTriggeredAbility triggeredAbility in triggeredAbilities)
            {
                triggeredAbility.OnEnteringPlay();
            }
        }

        public void OnEnteringPlay()
        {
            Game1.inPlayList.Add(this);
            foreach(ConvergeActivatedAbility activatedAbility in activatedAbilities)
            {
                activatedAbility.OnEnteringPlay();
            }
            foreach (ConvergeTriggeredAbility triggeredAbility in triggeredAbilities)
            {
                triggeredAbility.OnEnteringPlay();
            }

            if (produces != null)
            {
                controller.resources.Add(produces);
            }

            if (TriggerSystem.HasTriggers(ConvergeTriggerType.EnterPlay))
            {
                TriggerSystem.CheckTriggers(ConvergeTriggerType.EnterPlay, new TriggerData(controller, this, null, 0));
            }
        }

        // this is called after the object has actually been added to its new zone
        public void OnLeavingPlay()
        {
            foreach (ConvergeTriggeredAbility triggeredAbility in triggeredAbilities)
            {
                triggeredAbility.OnLeavingPlay();
            }

            damage = 0;
            destroyed = false;
            tapped = false;
            Game1.inPlayList.Remove(this);
            controlEffects.Clear();
            UpdateController();
        }
    }
}
