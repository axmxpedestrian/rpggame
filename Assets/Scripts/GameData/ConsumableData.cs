// ============================================
// ConsumableData.cs - 消耗品数据 ScriptableObject
// ============================================
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 消耗品数据定义
/// </summary>
[CreateAssetMenu(fileName = "New Consumable", menuName = "Game Data/Items/Consumable")]
public class ConsumableData : ItemData
{
    [Header("消耗品信息")]
    [Tooltip("消耗品类型")]
    public ConsumableType consumableType;

    [Header("使用效果")]
    [Tooltip("效果列表")]
    public List<ConsumableEffect> effects = new List<ConsumableEffect>();

    [Header("使用限制")]
    [Tooltip("使用目标")]
    public ConsumableTarget targetType = ConsumableTarget.Self;

    [Tooltip("冷却时间（秒）")]
    public float cooldown = 0f;

    [Tooltip("每场战斗最大使用次数（0为无限）")]
    public int maxUsesPerBattle = 0;

    [Header("动画/音效")]
    [Tooltip("使用动画ID")]
    public string useAnimationID;

    [Tooltip("使用音效ID")]
    public string useSoundID;

    /// <summary>
    /// 创建运行时消耗品实例
    /// </summary>
    public ConsumableItem CreateInstance(int count = 1)
    {
        return new ConsumableItem(this, count);
    }

    public override string GenerateTooltip()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(base.GenerateTooltip());

        sb.AppendLine();
        sb.AppendLine($"<b>类型:</b> {GetTypeName()}");
        sb.AppendLine($"<b>目标:</b> {GetTargetName()}");

        if (effects.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<b>效果:</b>");
            foreach (var effect in effects)
            {
                sb.AppendLine($"  • {effect.GetDescription()}");
            }
        }

        if (cooldown > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"<color=yellow>冷却时间: {cooldown}秒</color>");
        }

        if (maxUsesPerBattle > 0)
        {
            sb.AppendLine($"<color=yellow>战斗中最多使用: {maxUsesPerBattle}次</color>");
        }

        if (usableInBattle)
        {
            sb.AppendLine();
            sb.AppendLine("<color=green>可在战斗中使用</color>");
        }

        return sb.ToString();
    }

    private string GetTypeName()
    {
        return consumableType switch
        {
            ConsumableType.Healing => "回复类",
            ConsumableType.Buff => "增益类",
            ConsumableType.Antidote => "解毒类",
            ConsumableType.Revival => "复活类",
            ConsumableType.SkillPoint => "技能点回复",
            _ => consumableType.ToString()
        };
    }

    private string GetTargetName()
    {
        return targetType switch
        {
            ConsumableTarget.Self => "自身",
            ConsumableTarget.SingleAlly => "单个队友",
            ConsumableTarget.AllAllies => "全体队友",
            ConsumableTarget.SingleEnemy => "单个敌人",
            ConsumableTarget.AllEnemies => "全体敌人",
            ConsumableTarget.DeadAlly => "已倒下队友",
            _ => targetType.ToString()
        };
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        itemType = ItemType.Consumable;
        isStackable = true;
        if (maxStackSize < 1) maxStackSize = 99;

        // 回复类和增益类可在战斗中使用
        usableInBattle = consumableType == ConsumableType.Healing ||
                         consumableType == ConsumableType.Buff ||
                         consumableType == ConsumableType.Antidote ||
                         consumableType == ConsumableType.Revival ||
                         consumableType == ConsumableType.SkillPoint;
    }
#endif
}

/// <summary>
/// 消耗品目标类型
/// </summary>
public enum ConsumableTarget
{
    Self,           // 自身
    SingleAlly,     // 单个队友
    AllAllies,      // 全体队友
    SingleEnemy,    // 单个敌人
    AllEnemies,     // 全体敌人
    DeadAlly        // 已倒下队友
}

/// <summary>
/// 消耗品效果
/// </summary>
[System.Serializable]
public class ConsumableEffect
{
    public ConsumableEffectType effectType;
    public float value;
    public float duration;
    public bool isPercentage;

    public string GetDescription()
    {
        string valueStr = isPercentage ? $"{value * 100:F0}%" : $"{value:F0}";
        string durationStr = duration > 0 ? $"，持续{duration}秒" : "";

        return effectType switch
        {
            ConsumableEffectType.RestoreHealth => $"恢复{valueStr}生命值{durationStr}",
            ConsumableEffectType.RestoreHealthPercent => $"恢复{valueStr}生命值{durationStr}",
            ConsumableEffectType.RestoreSkillPoint => $"恢复{valueStr}技能点",
            ConsumableEffectType.RemoveDebuff => "解除负面效果",
            ConsumableEffectType.ReviveAlly => $"复活并恢复{valueStr}生命值",
            ConsumableEffectType.AttackBuff => $"攻击力+{valueStr}{durationStr}",
            ConsumableEffectType.DefenseBuff => $"防御力+{valueStr}{durationStr}",
            ConsumableEffectType.SpeedBuff => $"速度+{valueStr}{durationStr}",
            ConsumableEffectType.CritBuff => $"暴击率+{valueStr}{durationStr}",
            ConsumableEffectType.DamageReduction => $"减少{valueStr}伤害{durationStr}",
            ConsumableEffectType.Immunity => $"免疫负面效果{durationStr}",
            _ => $"{effectType}: {valueStr}"
        };
    }
}

/// <summary>
/// 消耗品效果类型
/// </summary>
public enum ConsumableEffectType
{
    // 回复类
    RestoreHealth,          // 恢复固定生命值
    RestoreHealthPercent,   // 恢复百分比生命值
    RestoreSkillPoint,      // 恢复技能点

    // 解除类
    RemoveDebuff,           // 解除负面效果
    RemoveAllDebuffs,       // 解除所有负面效果

    // 复活类
    ReviveAlly,             // 复活队友

    // 增益类
    AttackBuff,             // 攻击提升
    DefenseBuff,            // 防御提升
    SpeedBuff,              // 速度提升
    CritBuff,               // 暴击提升
    DamageReduction,        // 减伤
    Immunity,               // 免疫
    Regeneration            // 持续回复
}