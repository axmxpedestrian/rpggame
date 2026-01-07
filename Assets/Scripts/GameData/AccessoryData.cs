// ============================================
// AccessoryData.cs - 饰品数据 ScriptableObject
// ============================================
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 饰品数据定义
/// </summary>
[CreateAssetMenu(fileName = "New Accessory", menuName = "Game Data/Equipment/Accessory")]
public class AccessoryData : EquipmentData
{
    [Header("饰品信息")]
    [Tooltip("饰品类型")]
    public AccessoryType accessoryType;

    [Tooltip("饰品槽位索引（0-3对应4个饰品槽）")]
    [Range(0, 3)]
    public int slotIndex;

    [Header("特殊效果")]
    [Tooltip("特殊被动效果列表")]
    public List<AccessoryPassive> passiveEffects = new List<AccessoryPassive>();

    [Header("套装")]
    [Tooltip("所属套装ID（可选）")]
    public string setID;

    /// <summary>
    /// 创建运行时饰品实例
    /// </summary>
    public Accessory CreateInstance()
    {
        return new Accessory(this);
    }

    public override string GenerateTooltip()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(base.GenerateTooltip());

        sb.AppendLine();
        sb.AppendLine($"<b>类型:</b> {GetTypeName()}");

        if (passiveEffects.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<b>被动效果:</b>");
            foreach (var effect in passiveEffects)
            {
                sb.AppendLine($"  • {effect.description}");
            }
        }

        if (!string.IsNullOrEmpty(setID))
        {
            sb.AppendLine();
            sb.AppendLine($"<color=green>套装: {setID}</color>");
        }

        return sb.ToString();
    }

    private string GetTypeName()
    {
        return accessoryType switch
        {
            AccessoryType.Ring => "戒指",
            AccessoryType.Necklace => "项链",
            AccessoryType.Earring => "耳环",
            AccessoryType.Bracelet => "手镯",
            _ => accessoryType.ToString()
        };
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        itemType = ItemType.Accessory;
        isStackable = false;
        maxStackSize = 1;
    }
#endif
}

/// <summary>
/// 饰品被动效果
/// </summary>
[System.Serializable]
public class AccessoryPassive
{
    [Tooltip("效果ID")]
    public string effectID;

    [Tooltip("效果描述")]
    public string description;

    [Tooltip("触发条件")]
    public PassiveTrigger trigger;

    [Tooltip("效果类型")]
    public PassiveEffectType effectType;

    [Tooltip("效果数值")]
    public float value;

    [Tooltip("持续时间（0为永久）")]
    public float duration;

    [Tooltip("冷却时间")]
    public float cooldown;
}

/// <summary>
/// 被动触发条件
/// </summary>
public enum PassiveTrigger
{
    Always,             // 始终生效
    OnBattleStart,      // 战斗开始时
    OnTurnStart,        // 回合开始时
    OnTurnEnd,          // 回合结束时
    OnAttack,           // 攻击时
    OnHit,              // 命中时
    OnCrit,             // 暴击时
    OnKill,             // 击杀时
    OnTakeDamage,       // 受到伤害时
    OnLowHealth,        // 低生命时
    OnAllyDeath,        // 队友死亡时
    OnDebuffReceived,   // 受到负面效果时
    OnHeal              // 被治疗时
}

/// <summary>
/// 被动效果类型
/// </summary>
public enum PassiveEffectType
{
    StatBoost,          // 属性提升
    DamageBoost,        // 伤害提升
    DamageReduction,    // 伤害减免
    Heal,               // 治疗
    Shield,             // 护盾
    Reflect,            // 反伤
    LifeSteal,          // 生命偷取
    ExtraAction,        // 额外行动
    DebuffImmunity,     // 免疫负面效果
    ElementBoost,       // 元素伤害提升
    CritBoost,          // 暴击提升
    SkillPointRegen,    // 技能点回复
    ExpBoost,           // 经验加成
    GoldBoost           // 金币加成
}