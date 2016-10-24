using LRCEngine;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceConvergence
{
    public class ConvergeEffectContext
    {
        public ConvergeObject source;
        public ConvergeObject target;
        public ConvergeObject subject;
        public ConvergePlayer you;
        public TriggerData trigger;

        public ConvergeEffectContext(ConvergeObject source, ConvergePlayer you)
        {
            this.source = source;
            this.you = you;
        }
    }

    public abstract class ConvergeCommand
    {
        public abstract void Run(ConvergeEffectContext context);

        public static ConvergeCommand New(JSONArray template, ContentManager Content)
        {
            switch(template.getString(0))
            {
                case "damage":
                    return new ConvergeCommand_Damage(template);
                case "fight":
                    return new ConvergeCommand_Fight(template);
                case "heal":
                    return new ConvergeCommand_Heal(template);
                case "gainLife":
                    return new ConvergeCommand_GainLife(template);
                case "takeControl":
                    return new ConvergeCommand_TakeControl(template);
                case "tap":
                    return new ConvergeCommand_Tap(template);
                case "untap":
                    return new ConvergeCommand_Untap(template);
                case "retreat":
                    return new ConvergeCommand_Retreat(template);
                case "upgrade":
                    return new ConvergeCommand_Upgrade(template, Content);
                case "destroy":
                    return new ConvergeCommand_Destroy(template);
                case "produceMana":
                    return new ConvergeCommand_ProduceMana(template);
                case "grantActivated":
                    return new ConvergeCommand_GrantActivated(template, Content);
                case "grantTriggered":
                    return new ConvergeCommand_GrantTriggered(template, Content);
                case "spawn":
                    return new ConvergeCommand_Spawn(template, Content);
                case "sequence":
                    return new ConvergeCommand_Sequence(template, Content);
                default:
                    throw new ArgumentException();
            }
        }
    }

    public class ConvergeCommand_Damage : ConvergeCommand
    {
        ConvergeSelector sources;
        ConvergeSelector victims;
        ConvergeCalculation amount;

        public ConvergeCommand_Damage(JSONArray template)
        {
            sources = ConvergeSelector.New(template.getProperty(1));
            amount = ConvergeCalculation.New(template.getProperty(2));
            victims = ConvergeSelector.New(template.getProperty(3));
        }

        public override void Run(ConvergeEffectContext context)
        {
            List<ConvergeObject> sourcesList = sources.GetList(context);
            int amountValue = amount.GetValue(context);

            foreach (ConvergeObject victim in victims.GetList(context))
            {
                foreach (ConvergeObject source in sourcesList)
                {
                    source.DealDamage(victim, amountValue, false);
                }
            }
        }
    }

    public class ConvergeCommand_Fight : ConvergeCommand
    {
        ConvergeSelector sources;
        ConvergeSelector victims;

        public ConvergeCommand_Fight(JSONArray template)
        {
            sources = ConvergeSelector.New(template.getProperty(1));
            victims = ConvergeSelector.New(template.getProperty(2));
        }

        public override void Run(ConvergeEffectContext context)
        {
            List<ConvergeObject> sourcesList = sources.GetList(context);

            foreach (ConvergeObject victim in victims.GetList(context))
            {
                foreach (ConvergeObject source in sourcesList)
                {
                    source.Fight(victim, false);
                }
            }
        }
    }

    public class ConvergeCommand_Heal : ConvergeCommand
    {
        ConvergeSelector patients;
        ConvergeCalculation amount;

        public ConvergeCommand_Heal(JSONArray template)
        {
            amount = ConvergeCalculation.New(template.getProperty(1));
            patients = ConvergeSelector.New(template.getProperty(2));
        }

        public override void Run(ConvergeEffectContext context)
        {
            int amountValue = amount.GetValue(context);

            foreach (ConvergeObject patient in patients.GetList(context))
            {
                patient.Heal(amountValue);
            }
        }
    }

    public class ConvergeCommand_GainLife : ConvergeCommand
    {
        ConvergeSelector subjects;
        ConvergeCalculation amount;

        public ConvergeCommand_GainLife(JSONArray template)
        {
            subjects = ConvergeSelector.New(template.getProperty(1));
            amount = ConvergeCalculation.New(template.getProperty(2));
        }

        public override void Run(ConvergeEffectContext context)
        {
            int amountValue = amount.GetValue(context);

            foreach (ConvergeObject subject in subjects.GetList(context))
            {
                subject.controller.GainLife(amountValue);
            }
        }
    }

    public class ConvergeCommand_TakeControl : ConvergeCommand
    {
        ConvergeSelector newControllerSelector;
        ConvergeSelector victims;
        ConvergeDuration duration;

        public ConvergeCommand_TakeControl(JSONArray template)
        {
            newControllerSelector = ConvergeSelector.New(template.getProperty(1));
            victims = ConvergeSelector.New(template.getProperty(2));

            if (template.Length == 4)
                duration = (ConvergeDuration)Enum.Parse(typeof(ConvergeDuration), template.getString(3));
            else
                duration = ConvergeDuration.Permanent;
        }

        public override void Run(ConvergeEffectContext context)
        {
            List<ConvergeObject> newControllerList = newControllerSelector.GetList(context);
            if (newControllerList.Count == 0)
                return;
            ConvergePlayer newController = newControllerList[0].controller;

            foreach (ConvergeObject victim in victims.GetList(context))
            {
                victim.AddEffect(new ConvergeEffect_Control(newController, context.source, duration));
            }
        }
    }

    public class ConvergeCommand_Untap : ConvergeCommand
    {
        ConvergeSelector patients;

        public ConvergeCommand_Untap(JSONArray template)
        {
            patients = ConvergeSelector.New(template.getProperty(1));
        }

        public override void Run(ConvergeEffectContext context)
        {
            foreach (ConvergeObject patient in patients.GetList(context))
            {
                patient.tapped = false;
            }
        }
    }

    public class ConvergeCommand_Tap : ConvergeCommand
    {
        ConvergeSelector subjects;

        public ConvergeCommand_Tap(JSONArray template)
        {
            subjects = ConvergeSelector.New(template.getProperty(1));
        }

        public override void Run(ConvergeEffectContext context)
        {
            foreach (ConvergeObject subject in subjects.GetList(context))
            {
                subject.tapped = true;
            }
        }
    }

    public class ConvergeCommand_Retreat : ConvergeCommand
    {
        ConvergeSelector subjects;

        public ConvergeCommand_Retreat(JSONArray template)
        {
            subjects = ConvergeSelector.New(template.getProperty(1));
        }

        public override void Run(ConvergeEffectContext context)
        {
            foreach (ConvergeObject subject in subjects.GetList(context))
            {
                subject.WithdrawAttack();
            }
        }
    }

    public class ConvergeCommand_ProduceMana : ConvergeCommand
    {
        ConvergeManaAmount produced;

        public ConvergeCommand_ProduceMana(JSONArray template)
        {
            produced = new ConvergeManaAmount(template.getString(1));
        }

        public override void Run(ConvergeEffectContext context)
        {
            context.you.resources.Add(produced);
        }
    }

    public class ConvergeCommand_Upgrade : ConvergeCommand
    {
        ConvergeSelector patients;
        ConvergeCalculation powerAmount;
        ConvergeCalculation toughnessAmount;
        ConvergeKeyword keywords;
        Texture2D new_art;
        ConvergeDuration duration;

        public ConvergeCommand_Upgrade(JSONArray template, ContentManager Content)
        {
            patients = ConvergeSelector.New(template.getProperty(1));
            powerAmount = ConvergeCalculation.New(template.getProperty(2));
            toughnessAmount = ConvergeCalculation.New(template.getProperty(3));
            if (template.Length >= 5)
            {
                keywords = template.getArray(4).ToKeywords();
            }

            if (template.Length >= 6)
                duration = (ConvergeDuration)Enum.Parse(typeof(ConvergeDuration), template.getString(5));
            else
                duration = ConvergeDuration.Permanent;

            if (template.Length >= 7)
                new_art = Content.Load<Texture2D>(template.getString(6));
        }

        public override void Run(ConvergeEffectContext context)
        {
            int power = powerAmount.GetValue(context);
            int toughness = toughnessAmount.GetValue(context);

            foreach (ConvergeObject patient in patients.GetList(context))
            {
                patient.AddEffect(new ConvergeEffect_Upgrade(power, toughness, keywords, context.source, new_art, duration));
            }
        }
    }

    public class ConvergeCommand_Destroy : ConvergeCommand
    {
        ConvergeSelector victims;

        public ConvergeCommand_Destroy(JSONArray template)
        {
            victims = ConvergeSelector.New(template.getProperty(1));
        }

        public override void Run(ConvergeEffectContext context)
        {
            foreach(ConvergeObject victim in victims.GetList(context))
            {
                victim.destroyed = true;
            }
        }
    }

    public class ConvergeCommand_GrantActivated : ConvergeCommand
    {
        ConvergeSelector subjects;
        ConvergeDuration duration;
        ConvergeActivatedAbilitySpec abilitySpec;

        public ConvergeCommand_GrantActivated(JSONArray template, ContentManager Content)
        {
            subjects = ConvergeSelector.New(template.getProperty(1));
            duration = (ConvergeDuration)Enum.Parse(typeof(ConvergeDuration), template.getString(2));
            abilitySpec = new ConvergeActivatedAbilitySpec(template.getJSON(3), Content);
        }

        public override void Run(ConvergeEffectContext context)
        {
            foreach (ConvergeObject subject in subjects.GetList(context))
            {
                subject.AddEffect(new ConvergeEffect_GainActivated(abilitySpec, subject, context.source, duration));
            }
        }
    }

    public class ConvergeCommand_GrantTriggered : ConvergeCommand
    {
        ConvergeSelector subjects;
        ConvergeDuration duration;
        ConvergeTriggeredAbilitySpec abilitySpec;

        public ConvergeCommand_GrantTriggered(JSONArray template, ContentManager Content)
        {
            subjects = ConvergeSelector.New(template.getProperty(1));
            duration = (ConvergeDuration)Enum.Parse(typeof(ConvergeDuration), template.getString(2));
            abilitySpec = new ConvergeTriggeredAbilitySpec(template.getJSON(3), Content);
        }

        public override void Run(ConvergeEffectContext context)
        {
            foreach (ConvergeObject subject in subjects.GetList(context))
            {
                subject.AddEffect(new ConvergeEffect_GainTriggered(abilitySpec, subject, context.source, duration));
            }
        }
    }

    public class ConvergeCommand_Spawn : ConvergeCommand
    {
        ConvergeSelector players;
        ConvergeCardSpec cardSpec;
        ConvergeZoneId zoneId;

        public ConvergeCommand_Spawn(JSONArray template, ContentManager Content)
        {
            players = ConvergeSelector.New(template.getProperty(1));
            cardSpec = ConvergeCardSpec.allCards[template.getString(2)];
            zoneId = (ConvergeZoneId)Enum.Parse(typeof(ConvergeZoneId), template.getString(3, "Defense"));
        }

        public override void Run(ConvergeEffectContext context)
        {
            foreach (ConvergeObject player in players.GetList(context))
            {
                ConvergeZone spawnZone = player.controller.GetZone(zoneId);
                ConvergeObject newSpawned = new ConvergeObject(cardSpec, spawnZone);
                if(spawnZone.inPlay && newSpawned.cardType.HasFlag(ConvergeCardType.Unit))
                {
                    newSpawned.tapped = true;
                }
            }
        }
    }

    public class ConvergeCommand_Sequence : ConvergeCommand
    {
        List<ConvergeCommand> commands;

        public ConvergeCommand_Sequence(JSONArray template, ContentManager Content)
        {
            commands = new List<ConvergeCommand>();
            for (int Idx = 1; Idx < template.Length; ++Idx)
            {
                commands.Add( ConvergeCommand.New(template.getArray(Idx), Content) );
            }
        }

        public override void Run(ConvergeEffectContext context)
        {
            foreach (ConvergeCommand command in commands)
            {
                command.Run(context);
            }
        }
    }



    public abstract class ConvergeSelector
    {
        public virtual List<ConvergeObject> GetList(ConvergeEffectContext context)
        {
            throw new NotImplementedException();
        }
        public abstract bool Test(ConvergeObject subject, ConvergeEffectContext context);

        public virtual void Filter(List<ConvergeObject> list, ConvergeEffectContext context)
        {
            for(int Idx = 0; Idx < list.Count();)
            {
                if (!Test(list[Idx], context))
                    list.RemoveAt(Idx);
                else
                    ++Idx;
            }
        }

        public static ConvergeSelector New(object template)
        {
            if (template == null)
                return ConvergeSelector_DontCare.instance;

            if (template is string)
            {
                switch ((string)template)
                {
                    case "source":
                        return new ConvergeSelector_Source();
                    case "you":
                        return new ConvergeSelector_You();
                    case "opponent":
                        return new ConvergeSelector_Opponent();
                    case "target":
                        return new ConvergeSelector_Target();
                    case "subject":
                        return new ConvergeSelector_Subject();
                    case "upgraded":
                        return new ConvergeSelector_Upgraded();
                    case "bloodthirst":
                        return new ConvergeSelector_Bloodthirst();
                    default:
                        throw new ArgumentException();
                }
            }
            else if (template is System.Object[])
            {
                JSONArray arrayTemplate = new JSONArray((System.Object[])template);
                switch (arrayTemplate.getString(0))
                {
                    case "allOf":
                        return new ConvergeSelector_AllOf(arrayTemplate);
                    case "type":
                        return new ConvergeSelector_Type(arrayTemplate);
                    case "control":
                        return new ConvergeSelector_Control(arrayTemplate);
                    case "battlefield":
                        return new ConvergeSelector_Battlefield(arrayTemplate);
                    case "zone":
                        return new ConvergeSelector_Zone(arrayTemplate);
                    case "equal":
                    case "notEqual":
                    case "less":
                    case "greater":
                    case "lessOrEqual":
                    case "greaterOrEqual":
                        return new ConvergeSelector_Compare(arrayTemplate);
                    default:
                        throw new ArgumentException();
                }
            }
            else
            {
                throw new ArgumentException();
            }
        }
    }

    public class ConvergeSelector_DontCare: ConvergeSelector
    {
        public override bool Test(ConvergeObject subject, ConvergeEffectContext context)
        {
            return true;
        }
        public static ConvergeSelector_DontCare instance = new ConvergeSelector_DontCare();
    }

    public class ConvergeSelector_Source : ConvergeSelector
    {
        public override List<ConvergeObject> GetList(ConvergeEffectContext context)
        {
            return new List<ConvergeObject> { context.source };
        }
        public override bool Test(ConvergeObject subject, ConvergeEffectContext context)
        {
            return context.source == subject;
        }
    }

    public class ConvergeSelector_You : ConvergeSelector
    {
        public override List<ConvergeObject> GetList(ConvergeEffectContext context)
        {
            return new List<ConvergeObject> { context.you.homeBase };
        }
        public override bool Test(ConvergeObject subject, ConvergeEffectContext context)
        {
            return context.you.homeBase == subject;
        }
    }

    public class ConvergeSelector_Opponent: ConvergeSelector
    {
        public override List<ConvergeObject> GetList(ConvergeEffectContext context)
        {
            return new List<ConvergeObject> { context.you.opponent.homeBase };
        }
        public override bool Test(ConvergeObject subject, ConvergeEffectContext context)
        {
            return context.you.opponent.homeBase == subject;
        }
    }

    public class ConvergeSelector_Target : ConvergeSelector
    {
        public override List<ConvergeObject> GetList(ConvergeEffectContext context)
        {
            return new List<ConvergeObject> { context.target };
        }
        public override bool Test(ConvergeObject subject, ConvergeEffectContext context)
        {
            return context.target == subject;
        }
    }

    public class ConvergeSelector_Subject : ConvergeSelector
    {
        public override List<ConvergeObject> GetList(ConvergeEffectContext context)
        {
            return new List<ConvergeObject> { context.subject };
        }
        public override bool Test(ConvergeObject subject, ConvergeEffectContext context)
        {
            return context.subject == subject;
        }
    }

    public class ConvergeSelector_Upgraded : ConvergeSelector
    {
        public override bool Test(ConvergeObject subject, ConvergeEffectContext context)
        {
            return subject.hasUpgrades;
        }
    }

    public class ConvergeSelector_Bloodthirst : ConvergeSelector
    {
        public override bool Test(ConvergeObject subject, ConvergeEffectContext context)
        {
            return context.you.opponent.damagedThisTurn;
        }
    }

    public class ConvergeSelector_AllOf: ConvergeSelector
    {
        List<ConvergeSelector> cases;
        public ConvergeSelector_AllOf(JSONArray template)
        {
            for(int Idx = 1; Idx < template.Length; ++Idx)
            {
                cases.Add(ConvergeSelector.New(template.getProperty(Idx)));
            }
        }

        public override bool Test(ConvergeObject subject, ConvergeEffectContext context)
        {
            foreach (ConvergeSelector select in cases)
            {
                if (!select.Test(subject, context))
                    return false;
            }
            return true;
        }

        public override List<ConvergeObject> GetList(ConvergeEffectContext context)
        {
            List<ConvergeObject> result = null;
            foreach(ConvergeSelector select in cases)
            {
                if (result == null)
                {
                    result = select.GetList(context);
                }
                else
                {
                    for(int Idx = 0; Idx < result.Count; ++Idx)
                    {
                        if (!select.Test(result[Idx], context))
                            result.RemoveAt(Idx);
                    }
                }
            }
            return result;
        }
    }

    public class ConvergeSelector_Type : ConvergeSelector
    {
        ConvergeCardType type;

        public ConvergeSelector_Type(JSONArray template)
        {
            type = 0;
            for (int Idx = 1; Idx < template.Length; ++Idx)
            {
                type |= (ConvergeCardType)Enum.Parse(typeof(ConvergeCardType), template.getString(Idx));
            }
        }
        public override bool Test(ConvergeObject subject, ConvergeEffectContext context)
        {
            // TBD: "all" or "any"?
            return (subject.cardType & type) != 0;
        }
    }

    public enum ConvergeComparison
    {
        equal,
        notEqual,
        less,
        greater,
        lessOrEqual,
        greaterOrEqual,
    }

    public class ConvergeSelector_Compare : ConvergeSelector
    {
        ConvergeCalculation a;
        ConvergeComparison comparison;
        ConvergeCalculation b;

        public ConvergeSelector_Compare(JSONArray template)
        {
            comparison = (ConvergeComparison)Enum.Parse(typeof(ConvergeComparison), template.getString(0));
            a = ConvergeCalculation.New(template.getProperty(1));
            b = ConvergeCalculation.New(template.getProperty(2));
        }
        public override bool Test(ConvergeObject subject, ConvergeEffectContext context)
        {
            context.subject = subject;
            int aValue = a.GetValue(context);
            int bValue = b.GetValue(context);
            switch(comparison)
            {
                case ConvergeComparison.equal: return aValue == bValue;
                case ConvergeComparison.notEqual: return aValue != bValue;
                case ConvergeComparison.greater: return aValue > bValue;
                case ConvergeComparison.less: return aValue < bValue;
                case ConvergeComparison.greaterOrEqual: return aValue >= bValue;
                case ConvergeComparison.lessOrEqual: return aValue <= bValue;
                default: throw new NotImplementedException();
            }
        }
    }

    public class ConvergeSelector_Control : ConvergeSelector
    {
        ConvergeSelector controller;

        public ConvergeSelector_Control(JSONArray template)
        {
            controller = ConvergeSelector.New(template.getProperty(1));
        }
        public override bool Test(ConvergeObject subject, ConvergeEffectContext context)
        {
            context.subject = subject;
            foreach(ConvergeObject potentialController in controller.GetList(context))
            {
                if (potentialController.controller == subject.controller)
                    return true;
            }

            return false;
        }
    }

    public class ConvergeSelector_Battlefield : ConvergeSelector
    {
        List<ConvergeSelector> filters;

        public ConvergeSelector_Battlefield(JSONArray template)
        {
            filters = new List<ConvergeSelector>();
            for(int Idx = 1; Idx < template.Length; ++Idx)
            {
                filters.Add(ConvergeSelector.New(template.getProperty(Idx)));
            }
        }
        public override List<ConvergeObject> GetList(ConvergeEffectContext context)
        {
            List<ConvergeObject> result = Game1.inPlayList.ToList();
            Filter(result, context);
            return result;
        }
        public override bool Test(ConvergeObject subject, ConvergeEffectContext context)
        {
            if ((subject.zone.zoneId & ConvergeZoneId.Play) == 0)
                return false;

            foreach (ConvergeSelector select in filters)
                if (!select.Test(subject, context))
                    return false;

            return true;
        }
    }

    public class ConvergeSelector_Zone : ConvergeSelector
    {
        ConvergeSelector whose;
        ConvergeZoneId zoneId;
        List<ConvergeSelector> filters;

        public ConvergeSelector_Zone(JSONArray template)
        {
            whose = ConvergeSelector.New(template.getProperty(1));
            zoneId = (ConvergeZoneId)Enum.Parse(typeof(ConvergeZoneId), template.getString(2));
            filters = new List<ConvergeSelector>();
            for (int Idx = 3; Idx < template.Length; ++Idx)
            {
                filters.Add(ConvergeSelector.New(template.getProperty(Idx)));
            }
        }
        public override List<ConvergeObject> GetList(ConvergeEffectContext context)
        {
            List<ConvergeObject> players = whose.GetList(context);
            List<ConvergeObject> result = new List<ConvergeObject>();
            foreach(ConvergeObject player in players)
            {
                result.AddRange(player.controller.GetZone(zoneId).contents);
            }

            Filter(result, context);
            return result;
        }
        public override bool Test(ConvergeObject subject, ConvergeEffectContext context)
        {
            // TO DO: at some point we're going to be sad this isn't checking the zone correctly
            if (subject.zone.zoneId != zoneId)// || !whose.Test(subject.zone.owner.homeBase, context))
                return false;

            foreach (ConvergeSelector select in filters)
                if (!select.Test(subject, context))
                    return false;

            return true;
        }
    }



    public abstract class ConvergeCalculation
    {
        public abstract int GetValue(ConvergeEffectContext context);

        public static ConvergeCalculation New(object template)
        {
            if (template is double)
            {
                return new ConvergeCalculation_Constant((int)(double)template);
            }
            else if (template is string)
            {
                switch((string)template)
                {
                    case "triggerAmount":
                        return new ConvergeCalculation_TriggerAmount();
                    default:
                        throw new ArgumentException();
                }
            }
            else if (template is object[])
            {
                JSONArray array = new JSONArray((object[])template);
                switch(array.getString(0))
                {
                    case "powerOf":
                        return new ConvergeCalculation_Power(array);
                    case "toughnessOf":
                        return new ConvergeCalculation_Toughness(array);
                    default:
                        throw new ArgumentException();
                }
            }
            else
            {
                throw new ArgumentException();
            }
        }
    }

    public class ConvergeCalculation_Constant: ConvergeCalculation
    {
        int constant;

        public ConvergeCalculation_Constant(int constant)
        {
            this.constant = constant;
        }

        public override int GetValue(ConvergeEffectContext context)
        {
            return constant;
        }
    }

    public class ConvergeCalculation_Power : ConvergeCalculation
    {
        ConvergeSelector select;

        public ConvergeCalculation_Power(JSONArray template)
        {
            select = ConvergeSelector.New(template.getProperty(1));
        }

        public override int GetValue(ConvergeEffectContext context)
        {
            int total = 0;
            foreach(ConvergeObject obj in select.GetList(context))
            {
                total += obj.power;
            }
            return total;
        }
    }

    public class ConvergeCalculation_Toughness : ConvergeCalculation
    {
        ConvergeSelector select;

        public ConvergeCalculation_Toughness(JSONArray template)
        {
            select = ConvergeSelector.New(template.getProperty(1));
        }

        public override int GetValue(ConvergeEffectContext context)
        {
            int total = 0;
            foreach (ConvergeObject obj in select.GetList(context))
            {
                total += obj.toughness;
            }
            return total;
        }
    }

    public class ConvergeCalculation_TriggerAmount : ConvergeCalculation
    {
        public override int GetValue(ConvergeEffectContext context)
        {
            return context.trigger.amount;
        }
    }
}
