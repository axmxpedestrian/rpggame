using System;
using UnityEngine;

namespace CombatSystem
{
    using ItemSystem.Core;
    
    /// <summary>
    /// 状态效果数据 - ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewStatusEffect", menuName = "CombatSystem/StatusEffect")]
    public class StatusEffectData : ScriptableObject
    {
        [Header("基础信息")]
        [SerializeField] private string effectId;
        [SerializeField] private string effectName;
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private StatusEffectType effectType;
        [SerializeField] private StatusEffectCategory category;
        
        [Header("效果配置")]
        [SerializeField] private bool isStackable;          // 是否可叠加
        [SerializeField] private int maxStacks = 1;         // 最大层数
        [SerializeField] private bool refreshOnReapply;     // 重复施加时刷新持续时间
        
        [Header("DOT/HOT配置")]
        [SerializeField] private float tickInterval = 1f;   // 触发间隔
        [SerializeField] private int valuePerTick;          // 每次触发的数值
        [SerializeField] private bool isPercentage;         // 是否按最大值百分比计算
        [SerializeField] private DamageCategory damageCategory;  // DOT伤害类型
        [SerializeField] private ElementType element;       // DOT元素
        
        [Header("属性修改")]
        [SerializeField] private StatModifierConfig[] statModifiers;
        
        [Header("特殊效果")]
        [SerializeField] private bool preventsAction;       // 阻止行动（眩晕）
        [SerializeField] private bool preventsMovement;     // 阻止移动
        [SerializeField] private bool preventsCasting;      // 阻止施法（沉默）
        [SerializeField] private float incomingDamageMultiplier = 1f;  // 受到伤害倍率
        [SerializeField] private float outgoingDamageMultiplier = 1f;  // 造成伤害倍率
        
        // 属性访问
        public string EffectId => effectId;
        public string EffectName => effectName;
        public string Description => description;
        public Sprite Icon => icon;
        public StatusEffectType EffectType => effectType;
        public StatusEffectCategory Category => category;
        public bool IsStackable => isStackable;
        public int MaxStacks => maxStacks;
        public bool RefreshOnReapply => refreshOnReapply;
        public float TickInterval => tickInterval;
        public int ValuePerTick => valuePerTick;
        public bool IsPercentage => isPercentage;
        public DamageCategory DamageCategory => damageCategory;
        public ElementType Element => element;
        public StatModifierConfig[] StatModifiers => statModifiers;
        public bool PreventsAction => preventsAction;
        public bool PreventsMovement => preventsMovement;
        public bool PreventsCasting => preventsCasting;
        public float IncomingDamageMultiplier => incomingDamageMultiplier;
        public float OutgoingDamageMultiplier => outgoingDamageMultiplier;
    }
    
    /// <summary>
    /// 状态效果类别
    /// </summary>
    public enum StatusEffectCategory
    {
        Debuff,         // 负面效果
        Buff,           // 正面效果
        Neutral         // 中性效果
    }
    
    /// <summary>
    /// 属性修改配置
    /// </summary>
    [Serializable]
    public class StatModifierConfig
    {
        public StatType StatType;
        public ModifierType ModifierType;
        public float Value;
    }
    
    /// <summary>
    /// 状态效果实例 - 运行时
    /// </summary>
    public class StatusEffectInstance
    {
        public StatusEffectData EffectData { get; private set; }
        public Character Target { get; private set; }
        public Character Source { get; private set; }
        
        public float Duration { get; private set; }
        public float RemainingTime { get; private set; }
        public int CurrentStacks { get; private set; }
        public bool IsExpired => RemainingTime <= 0;
        
        private float _tickTimer;
        private readonly StatModifier[] _appliedModifiers;
        
        public StatusEffectInstance(StatusEffectData data, Character target, Character source, float duration)
        {
            EffectData = data;
            Target = target;
            Source = source;
            Duration = duration;
            RemainingTime = duration;
            CurrentStacks = 1;
            _tickTimer = 0f;
            
            // 创建属性修改器
            if (data.StatModifiers != null)
            {
                _appliedModifiers = new StatModifier[data.StatModifiers.Length];
                for (int i = 0; i < data.StatModifiers.Length; i++)
                {
                    var config = data.StatModifiers[i];
                    _appliedModifiers[i] = new StatModifier(
                        config.StatType,
                        config.ModifierType,
                        config.Value,
                        this
                    );
                }
            }
            else
            {
                _appliedModifiers = Array.Empty<StatModifier>();
            }
        }
        
        /// <summary>
        /// 效果应用时
        /// </summary>
        public void OnApply()
        {
            // 应用属性修改器
            foreach (var mod in _appliedModifiers)
            {
                Target.Stats.AddModifier(mod);
            }
            
            Target.CombatCache.MarkAllDirty();
        }
        
        /// <summary>
        /// 效果移除时
        /// </summary>
        public void OnRemove()
        {
            // 移除属性修改器
            foreach (var mod in _appliedModifiers)
            {
                Target.Stats.RemoveModifier(mod);
            }
            
            Target.CombatCache.MarkAllDirty();
        }
        
        /// <summary>
        /// 更新效果
        /// </summary>
        public void Update(float deltaTime)
        {
            RemainingTime -= deltaTime;
            
            // DOT/HOT触发
            if (EffectData.TickInterval > 0)
            {
                _tickTimer += deltaTime;
                
                while (_tickTimer >= EffectData.TickInterval)
                {
                    _tickTimer -= EffectData.TickInterval;
                    OnTick();
                }
            }
        }
        
        /// <summary>
        /// 周期性触发
        /// </summary>
        private void OnTick()
        {
            int value = EffectData.ValuePerTick * CurrentStacks;
            
            // 百分比计算
            if (EffectData.IsPercentage)
            {
                value = Mathf.RoundToInt(Target.Stats.MaxHealth * EffectData.ValuePerTick * 0.01f * CurrentStacks);
            }
            
            switch (EffectData.Category)
            {
                case StatusEffectCategory.Debuff:
                    // DOT伤害
                    if (value > 0)
                    {
                        var damageResult = DamageCalculator.CalculateFixedDamage(
                            Source ?? Target,
                            Target,
                            value,
                            EffectData.DamageCategory,
                            EffectData.Element,
                            ignoreDef: false
                        );
                        Target.TakeDamage(damageResult.ToDamageInfo());
                    }
                    break;
                    
                case StatusEffectCategory.Buff:
                    // HOT治疗
                    if (value > 0)
                    {
                        Target.Heal(value);
                    }
                    break;
            }
        }
        
        /// <summary>
        /// 刷新持续时间
        /// </summary>
        public void RefreshDuration(float newDuration)
        {
            if (EffectData.RefreshOnReapply)
            {
                RemainingTime = Mathf.Max(RemainingTime, newDuration);
            }
            
            // 叠加层数
            if (EffectData.IsStackable && CurrentStacks < EffectData.MaxStacks)
            {
                CurrentStacks++;
                
                // 更新属性修改器的值
                foreach (var mod in _appliedModifiers)
                {
                    Target.Stats.RemoveModifier(mod);
                }
                
                for (int i = 0; i < _appliedModifiers.Length; i++)
                {
                    var original = EffectData.StatModifiers[i];
                    _appliedModifiers[i] = new StatModifier(
                        original.StatType,
                        original.ModifierType,
                        original.Value * CurrentStacks,
                        this
                    );
                    Target.Stats.AddModifier(_appliedModifiers[i]);
                }
                
                Target.CombatCache.MarkAllDirty();
            }
        }
        
        /// <summary>
        /// 检查是否阻止行动
        /// </summary>
        public bool CanAct() => !EffectData.PreventsAction;
        public bool CanMove() => !EffectData.PreventsMovement;
        public bool CanCast() => !EffectData.PreventsCasting;
    }
    
    /// <summary>
    /// Buff类型（用于消耗品）
    /// </summary>
    public enum BuffType
    {
        Attack,
        Defense,
        MagicAttack,
        MagicDefense,
        Speed,
        CriticalChance,
        CriticalDamage,
        Accuracy,
        Evasion,
        AllStats
    }
    
    /// <summary>
    /// Buff效果数据
    /// </summary>
    [Serializable]
    public class BuffEffect
    {
        public BuffType buffType;
        public float value;
        public bool isPercentage;
        
        public void Apply(Character target)
        {
            target.CombatCache.AddTemporaryBuff(buffType, isPercentage ? value : value / 100f);
        }
        
        public void Remove(Character target)
        {
            target.CombatCache.RemoveTemporaryBuff(buffType, isPercentage ? value : value / 100f);
        }
    }
}
