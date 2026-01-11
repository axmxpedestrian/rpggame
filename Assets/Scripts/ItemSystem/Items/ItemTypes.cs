using System;
using UnityEngine;

namespace ItemSystem.Consumables
{
    using Core;

    /// <summary>
    /// 消耗品基类
    /// </summary>
    public abstract class ConsumableBase : Item, IUsable, IStackable
    {
        [Header("消耗品属性")]
        [SerializeField] protected ConsumableSubType consumableSubType;
        [SerializeField] protected float cooldown = 0f;

        public ConsumableSubType ConsumableSubType => consumableSubType;
        public float Cooldown => cooldown;

        // IStackable 实现
        public int StackCount { get; set; } = 1;

        public bool CanStackWith(Item other)
        {
            return other is ConsumableBase cb &&
                   cb.ItemId == ItemId &&
                   IsStackable;
        }

        public abstract bool CanUse(ICharacter user, ICharacter target = null);
        public abstract void Use(ICharacter user, ICharacter target = null);
    }

    /// <summary>
    /// 回复类消耗品
    /// </summary>
    [CreateAssetMenu(fileName = "NewHealingItem", menuName = "ItemSystem/Consumable/HealingItem")]
    public class HealingItem : ConsumableBase, ICombatUsable
    {
        [Header("回复效果")]
        [SerializeField] private HealingType healingType;
        [SerializeField] private int healAmount;
        [SerializeField] private float healPercent;  // 百分比回复
        [SerializeField] private bool healOverTime;
        [SerializeField] private float duration;
        [SerializeField] private int tickCount;

        [Header("额外效果")]
        [SerializeField] private int stressReduction;
        [SerializeField] private int fatigueReduction;
        [SerializeField] private bool removeDebuffs;
        [SerializeField] private StatusEffectType[] debuffsToRemove;

        [Header("战斗设置")]
        [SerializeField] private int atbCost = 50;
        [SerializeField] private TargetType targetType = TargetType.SingleAlly;

        // ICombatUsable 实现
        public int ATBCost => atbCost;
        public TargetType TargetType => targetType;

        public override bool CanUse(ICharacter user, ICharacter target = null)
        {
            if (target == null) target = user;

            // 检查目标是否需要治疗
            switch (healingType)
            {
                case HealingType.Health:
                    return target.CurrentHealth < target.MaxHealth;
                case HealingType.SkillPoints:
                    return target.CurrentSkillPoints < target.MaxSkillPoints;
                case HealingType.Stress:
                    return target.CurrentStress > 0;
                case HealingType.Fatigue:
                    return target.CurrentFatigue > 0;
                case HealingType.Revive:
                    return target.IsDowned;
                default:
                    return true;
            }
        }

        public bool CanUseInCombat(CombatContext context)
        {
            return HasFlag(ItemFlags.UsableInCombat) &&
                   context.CurrentCharacter.CurrentATB >= atbCost;
        }

        public override void Use(ICharacter user, ICharacter target = null)
        {
            if (target == null) target = user;

            int actualHeal = CalculateHealAmount(target);

            if (healOverTime)
            {
                ApplyHealOverTime(target, actualHeal);
            }
            else
            {
                ApplyInstantHeal(target, actualHeal);
            }

            // 处理压力/疲劳
            if (stressReduction > 0)
                target.ReduceStress(stressReduction);
            if (fatigueReduction > 0)
                target.ReduceFatigue(fatigueReduction);

            // 移除负面效果
            if (removeDebuffs && debuffsToRemove != null)
            {
                foreach (var debuff in debuffsToRemove)
                {
                    target.StatusEffectManager.RemoveEffect(debuff);
                }
            }
        }

        private int CalculateHealAmount(ICharacter target)
        {
            int amount = healAmount;

            if (healPercent > 0)
            {
                int percentHeal = Mathf.RoundToInt(target.MaxHealth * healPercent);
                amount = Mathf.Max(amount, percentHeal);
            }

            return amount;
        }

        private void ApplyInstantHeal(ICharacter target, int amount)
        {
            switch (healingType)
            {
                case HealingType.Health:
                    target.Heal(amount);
                    break;
                case HealingType.SkillPoints:
                    target.RestoreSkillPoints(amount);
                    break;
                case HealingType.Revive:
                    target.Revive(amount);
                    break;
            }
        }

        private void ApplyHealOverTime(ICharacter target, int totalAmount)
        {
            int amountPerTick = totalAmount / tickCount;
            float interval = duration / tickCount;

            // 创建持续回复效果
            var hotEffect = new HealOverTimeEffect(healingType, amountPerTick, interval, tickCount);
            target.StatusEffectManager.AddEffect(hotEffect);
        }
    }

    // 注意: HealingType 已移至 ItemSystem.Core.ItemEnums.cs

    /// <summary>
    /// 属性提升类消耗品
    /// </summary>
    [CreateAssetMenu(fileName = "NewBuffItem", menuName = "ItemSystem/Consumable/BuffItem")]
    public class BuffItem : ConsumableBase, ICombatUsable
    {
        [Header("增益效果")]
        [SerializeField] private BuffEffect[] buffEffects;
        [SerializeField] private float duration;
        [SerializeField] private bool isPermanent;  // 战斗内永久

        [Header("战斗设置")]
        [SerializeField] private int atbCost = 50;
        [SerializeField] private TargetType targetType = TargetType.SingleAlly;

        public BuffEffect[] BuffEffects => buffEffects;
        public float Duration => duration;

        // ICombatUsable 实现
        public int ATBCost => atbCost;
        public TargetType TargetType => targetType;

        public override bool CanUse(ICharacter user, ICharacter target = null)
        {
            // 检查是否已有相同buff（可选：允许刷新）
            return true;
        }

        public bool CanUseInCombat(CombatContext context)
        {
            return HasFlag(ItemFlags.UsableInCombat) &&
                   context.CurrentCharacter.CurrentATB >= atbCost;
        }

        public override void Use(ICharacter user, ICharacter target = null)
        {
            if (target == null) target = user;

            foreach (var buff in buffEffects)
            {
                var statusEffect = new BuffStatusEffect(buff, duration, isPermanent);
                target.StatusEffectManager.AddEffect(statusEffect);
            }
        }
    }

    // 注意: BuffEffect 已移至 ItemSystem.Core.ItemEnums.cs
    // 注意: BuffType 已移至 ItemSystem.Core.ItemEnums.cs
    // 注意: HealOverTimeEffect 和 BuffStatusEffect 已定义在 ItemSystem.Core.Database.cs
}

namespace ItemSystem.Tools
{
    using Core;

    /// <summary>
    /// 工具类物品
    /// </summary>
    [CreateAssetMenu(fileName = "NewTool", menuName = "ItemSystem/Tool")]
    public class Tool : Item, IUsable
    {
        [Header("工具属性")]
        [SerializeField] private ToolType toolType;
        [SerializeField] private int useCount = 1;  // 可使用次数，-1为无限
        [SerializeField] private float cooldown = 0f;

        [Header("效果")]
        [SerializeField] private ToolEffect effect;

        public ToolType ToolType => toolType;
        public int UseCount => useCount;
        public float Cooldown => cooldown;

        public bool CanUse(ICharacter user, ICharacter target = null)
        {
            return effect?.CanActivate(user, this) ?? false;
        }

        public void Use(ICharacter user, ICharacter target = null)
        {
            effect?.Activate(user, target, this);
        }
    }

    public enum ToolType
    {
        Key,            // 钥匙
        Bomb,           // 炸弹
        Torch,          // 火把
        Rope,           // 绳索
        Pickaxe,        // 镐
        Shovel,         // 铲子
        FishingRod,     // 钓竿
        Compass,        // 指南针
        Map,            // 地图
        Teleporter      // 传送道具
    }

    /// <summary>
    /// 工具效果基类
    /// </summary>
    [Serializable]
    public abstract class ToolEffect
    {
        public abstract bool CanActivate(ICharacter user, Tool tool);
        public abstract void Activate(ICharacter user, ICharacter target, Tool tool);
    }
}

namespace ItemSystem.Materials
{
    using Core;

    /// <summary>
    /// 材料类物品
    /// </summary>
    [CreateAssetMenu(fileName = "NewMaterial", menuName = "ItemSystem/Material")]
    public class Material : Item, IStackable
    {
        [Header("材料属性")]
        [SerializeField] private MaterialCategory category;
        [SerializeField] private int tier;  // 材料等级

        public MaterialCategory Category => category;
        public int Tier => tier;

        // IStackable 实现
        public int StackCount { get; set; } = 1;

        public bool CanStackWith(Item other)
        {
            return other is Material m && m.ItemId == ItemId && IsStackable;
        }
    }

    public enum MaterialCategory
    {
        Ore,            // 矿石
        Wood,           // 木材
        Herb,           // 草药
        Gem,            // 宝石
        Fabric,         // 布料
        Leather,        // 皮革
        Bone,           // 骨头
        Monster,        // 怪物素材
        Essence,        // 精华
        Other
    }
}

namespace ItemSystem.Cosmetics
{
    using Core;

    /// <summary>
    /// 服装/时装类物品
    /// </summary>
    [CreateAssetMenu(fileName = "NewCosmetic", menuName = "ItemSystem/Cosmetic")]
    public class Cosmetic : Item, IEquippable
    {
        [Header("外观设置")]
        [SerializeField] private CosmeticSlot slot;
        [SerializeField] private Sprite[] visualSprites;  // 不同角度/动画帧
        [SerializeField] private Color tintColor = Color.white;

        [Header("可选属性加成")]
        [SerializeField] private bool hasStats;
        [SerializeField] private StatModifier[] statModifiers;

        public CosmeticSlot Slot => slot;
        public EquipmentSubType EquipSubType => EquipmentSubType.Accessory;

        public void OnEquip(ICharacter character)
        {
            // 更新视觉外观
            character.VisualManager.SetCosmetic(slot, this);

            // 应用属性（如果有）
            if (hasStats && statModifiers != null)
            {
                foreach (var mod in statModifiers)
                {
                    character.Stats.AddModifier(mod);
                }
            }
        }

        public void OnUnequip(ICharacter character)
        {
            character.VisualManager.RemoveCosmetic(slot);

            if (hasStats && statModifiers != null)
            {
                foreach (var mod in statModifiers)
                {
                    character.Stats.RemoveModifier(mod);
                }
            }
        }

        public StatModifier[] GetStatModifiers()
        {
            return hasStats ? statModifiers : Array.Empty<StatModifier>();
        }
    }

    public enum CosmeticSlot
    {
        Hat,
        Hair,
        Face,
        Back,       // 披风等
        Outfit,
        Pet
    }
}

namespace ItemSystem.Quest
{
    using Core;

    /// <summary>
    /// 剧情道具类
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuestItem", menuName = "ItemSystem/QuestItem")]
    public class QuestItem : Item
    {
        [Header("任务关联")]
        [SerializeField] private string questId;
        [SerializeField] private QuestItemType questItemType;
        [SerializeField] private bool autoRemoveOnQuestComplete = true;

        [Header("剧情文本")]
        [SerializeField][TextArea] private string loreText;

        public string QuestId => questId;
        public QuestItemType QuestItemType => questItemType;
        public bool AutoRemoveOnQuestComplete => autoRemoveOnQuestComplete;
        public string LoreText => loreText;

        // 剧情道具默认不可丢弃、不可出售
        private void OnValidate()
        {
            // 在编辑器中自动设置标记
        }
    }

    public enum QuestItemType
    {
        KeyItem,        // 关键道具
        Clue,           // 线索
        Letter,         // 信件
        Photo,          // 照片
        Artifact,       // 古物
        Memory          // 记忆碎片
    }
}