using LRCEngine;
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
        public ConvergePlayer you;

        public ConvergeEffectContext(ConvergeObject source, ConvergePlayer you)
        {
            this.source = source;
            this.you = you;
        }
    }

    public abstract class ConvergeCommand
    {
        public abstract void Run(ConvergeEffectContext context);

        public static ConvergeCommand New(JSONArray template)
        {
            switch(template.getString(0))
            {
                case "damage":
                    return new ConvergeCommand_Damage(template);
                case "heal":
                    return new ConvergeCommand_Heal(template);
                case "takecontrol":
                    return new ConvergeCommand_TakeControl(template);
                case "untap":
                    return new ConvergeCommand_Untap(template);
                case "sequence":
                    return new ConvergeCommand_Sequence(template);
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

    public class ConvergeCommand_Sequence : ConvergeCommand
    {
        List<ConvergeCommand> commands;

        public ConvergeCommand_Sequence(JSONArray template)
        {
            commands = new List<ConvergeCommand>();
            for (int Idx = 1; Idx < template.Length; ++Idx)
            {
                commands.Add( ConvergeCommand.New(template.getArray(Idx)) );
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
        public abstract List<ConvergeObject> GetList(ConvergeEffectContext context);
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
        public override List<ConvergeObject> GetList(ConvergeEffectContext context)
        {
            return null;
        }
        public override bool Test(ConvergeObject subject, ConvergeEffectContext context)
        {
            // TBD: "all" or "any"?
            return (subject.cardType & type) != 0;
        }
    }

    public class ConvergeSelector_Control : ConvergeSelector
    {
        ConvergeSelector controller;

        public ConvergeSelector_Control(JSONArray template)
        {
            controller = ConvergeSelector.New(template.getProperty(1));
        }
        public override List<ConvergeObject> GetList(ConvergeEffectContext context)
        {
            return null;
        }
        public override bool Test(ConvergeObject subject, ConvergeEffectContext context)
        {
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



    public abstract class ConvergeCalculation
    {
        public abstract int GetValue(ConvergeEffectContext context);

        public static ConvergeCalculation New(object template)
        {
            if (template is int)
            {
                return new ConvergeCalculation_Constant((int)template);
            }
            else if (template is double)
            {
                return new ConvergeCalculation_Constant((int)(double)template);
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
}
