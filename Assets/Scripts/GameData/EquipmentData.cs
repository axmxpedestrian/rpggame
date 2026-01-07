// ============================================
// EquipmentData.cs - 装备基类
// ============================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 装备属性加成
/// </summary>
[Serializable]
public class StatBonus
{
    public CombatStatType statType;
    public float value;
    public bool isPercentage;  // true: 百分比加成, false: 固定值加成

    public StatBonus() { }

    public StatBonus(CombatStatType type, float val, bool isPercent = false)
    {
        statType = type;
        value = val;
        isPercentage = isPercent;
    }

    public string GetDescription()
    {
        string sign = value >= 0 ? "+" : "";
        string suffix = isPercentage ? "%" : "";
        return $"{GetStatName()}{sign}{value}{suffix}";
    }

    private string GetStatName()
    {
        return statType switch
        {
            CombatStatType.MaxHealth => "生命值",
            CombatStatType.PhysicalAttack => "物理攻击",
            CombatStatType.MagicAttack => "魔法攻击",
            CombatStatType.PhysicalDefense => "物理防御",
            CombatStatType.MagicDefense => "魔法防御",
            CombatStatType.Resistance => "抗性",
            CombatStatType.CritRate => "暴击率",
            CombatStatType.CritDamage => "暴击伤害",
            CombatStatType.Speed => "速度",
            CombatStatType.HitRate => "命中率",
            CombatStatType.DodgeRate => "闪避率",
            CombatStatType.PhysicalBlock => "物理格挡",
            CombatStatType.MagicBlock => "魔法格挡",
            CombatStatType.PhysicalSkillCap => "物理技能点",
            CombatStatType.MagicSkillCap => "魔法技能点",
            _ => statType.ToString()
        };
    }
}

/// <summary>
/// 装备数据基类
/// </summary>
public abstract class EquipmentData : ItemData
{
    [Header("装备属性")]
    [Tooltip("属性加成列表")]
    public List<StatBonus> statBonuses = new List<StatBonus>();

    [Header("配件槽位")]
    [Tooltip("配件槽位数量（根据品质自动计算）")]
    public int attachmentSlotCount;

    /// <summary>
    /// 根据品质获取配件槽位数量
    /// </summary>
    public virtual int GetSlotCountByRarity()
    {
        return rarity switch
        {
            ItemRarity.Common => 0,
            ItemRarity.Uncommon => 1,
            ItemRarity.Rare => 2,
            ItemRarity.Epic => 3,
            ItemRarity.Legendary => 4,
            ItemRarity.Mythic => 5,
            _ => 0
        };
    }

    /// <summary>
    /// 获取指定属性的加成值
    /// </summary>
    public float GetStatBonus(CombatStatType statType, bool percentageOnly = false)
    {
        float total = 0;
        foreach (var bonus in statBonuses)
        {
            if (bonus.statType == statType)
            {
                if (!percentageOnly || bonus.isPercentage)
                {
                    total += bonus.value;
                }
            }
        }
        return total;
    }

    public override string GenerateTooltip()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(base.GenerateTooltip());

        if (statBonuses.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<b>属性加成:</b>");
            foreach (var bonus in statBonuses)
            {
                sb.AppendLine($"  {bonus.GetDescription()}");
            }
        }

        if (attachmentSlotCount > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"<color=cyan>配件槽位: {attachmentSlotCount}</color>");
        }

        return sb.ToString();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        attachmentSlotCount = GetSlotCountByRarity();
    }
#endif
}