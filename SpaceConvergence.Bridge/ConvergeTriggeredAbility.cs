using LRCEngine;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceConvergence
{
    public enum ConvergeTriggerType
    {
        PlayCard,
        DealDamage,
        EnterPlay,
        Discarded,
        GainLife,

        _Count,
    };

    public class TriggerData
    {
        public readonly ConvergePlayer player;
        public readonly ConvergeObject subject;
        public readonly ConvergeObject target;
        public readonly int amount;

        public TriggerData(ConvergePlayer player, ConvergeObject subject, ConvergeObject target, int amount)
        {
            this.player = player;
            this.subject = subject;
            this.target = target;
            this.amount = amount;
        }
    }

    public class ConvergeTriggeredAbilitySpec
    {
        public readonly ConvergeTriggerType triggerType;
        public readonly ConvergeSelector triggerPlayer;
        public readonly ConvergeSelector triggerSubject;
        public readonly ConvergeSelector triggerTarget;
        public readonly ConvergeSelector condition;
        public readonly ConvergeCommand effect;

        public ConvergeTriggeredAbilitySpec(JSONTable template, ContentManager Content)
        {
            triggerType = (ConvergeTriggerType)Enum.Parse(typeof(ConvergeTriggerType), template.getString("trigger"));
            triggerPlayer = ConvergeSelector.New(template.getProperty("triggerPlayer", null));
            triggerSubject = ConvergeSelector.New(template.getProperty("triggerSubject", null));
            triggerTarget = ConvergeSelector.New(template.getProperty("triggerTarget", null));
            condition = ConvergeSelector.New(template.getProperty("condition", null));
            effect = ConvergeCommand.New(template.getArray("effect"), Content);
        }
    }

    public class ConvergeTriggeredAbility
    {
        ConvergeTriggeredAbilitySpec spec;
        public ConvergeTriggerType triggerType { get { return spec.triggerType; } }
        ConvergeObject source;

        public ConvergeTriggeredAbility(ConvergeTriggeredAbilitySpec spec, ConvergeObject source)
        {
            this.spec = spec;
            this.source = source;
        }

        public void OnEnteringPlay()
        {
            TriggerSystem.Add(this);
        }

        public void OnLeavingPlay()
        {
            TriggerSystem.Remove(this);
        }

        public void CheckTrigger(TriggerData triggerData)
        {
            ConvergeEffectContext context = new ConvergeEffectContext(source, source.controller);
            context.trigger = triggerData;
            if (spec.triggerPlayer.Test(triggerData.player.homeBase, context) &&
                spec.triggerSubject.Test(triggerData.subject, context) &&
                spec.triggerTarget.Test(triggerData.target, context) &&
                spec.condition.Test(null, context))
            {
                spec.effect.Run(context);
            }
        }
    }

    public class TriggerSystem
    {
        static List<ConvergeTriggeredAbility>[] activeAbilities = new List<ConvergeTriggeredAbility>[(int)ConvergeTriggerType._Count];

        public static void Add(ConvergeTriggeredAbility ability)
        {
            List<ConvergeTriggeredAbility> abilityList = activeAbilities[(int)ability.triggerType];
            
            if(abilityList == null)
            {
                abilityList = new List<ConvergeTriggeredAbility>();
                activeAbilities[(int)ability.triggerType] = abilityList;
            }

            abilityList.Add(ability);
        }

        public static void Remove(ConvergeTriggeredAbility ability)
        {
            List<ConvergeTriggeredAbility> abilityList = activeAbilities[(int)ability.triggerType];

            abilityList.Remove(ability);
        }

        public static bool HasTriggers(ConvergeTriggerType type)
        {
            List<ConvergeTriggeredAbility> list = activeAbilities[(int)type];
            return list != null && list.Count > 0;
        }

        public static void CheckTriggers(ConvergeTriggerType type, TriggerData triggerData)
        {
            foreach(ConvergeTriggeredAbility ability in activeAbilities[(int)type])
            {
                ability.CheckTrigger(triggerData);
            }
        }
    }
}
