using LRCEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceConvergence
{
    public abstract class ConvergeCommand
    {
        public abstract void Run(ConvergeEffectContext context);

        public static ConvergeCommand New(JSONArray template)
        {
            switch(template.getString(0))
            {
                case "damage":
                    return new ConvergeCommand_Damage(template);
            }

            return null;
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

            foreach (ConvergeObject victim in victims.GetList(context))
            {
                foreach (ConvergeObject source in sourcesList)
                {
                    source.DealDamage(victim, amount.GetValue(context), false);
                }
            }
        }
    }

    public class ConvergeEffectContext
    {
        public ConvergeObject source;
        public ConvergePlayer you;

        public ConvergeEffectContext(ConvergeObject source, ConvergePlayer you)
        {
            this.source = source;
            this.you = you;
        }
    }

    public abstract class ConvergeSelector
    {
        public abstract List<ConvergeObject> GetList(ConvergeEffectContext context);

        public static ConvergeSelector New(object template)
        {
            if(template is string)
            {
                switch((string)template)
                {
                    case "source":
                        return new ConvergeSelector_Source();
                    case "opponent":
                        return new ConvergeSelector_Opponent();
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
    }

    public class ConvergeSelector_Opponent: ConvergeSelector
    {
        public override List<ConvergeObject> GetList(ConvergeEffectContext context)
        {
            return new List<ConvergeObject> { context.source.zone.owner.opponent.homeBase };
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
