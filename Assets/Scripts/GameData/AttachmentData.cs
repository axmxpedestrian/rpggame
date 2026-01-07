// ============================================
// AttachmentData.cs - 配件数据 ScriptableObject
// ============================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 配件效果
/// </summary>
[Serializable]
public class AttachmentEffect
{
    public AttachmentEffectType effectType;
    public float value;
    public float duration;

    [Range(0f, 1f)]
    public float triggerChance = 1f;

    public string description;

    public AttachmentEffect() { }

    public AttachmentEffect(AttachmentEffectType type, float val, float dur = 0, float chance = 1f)
    {
        effectType = type;
        value = val;
        duration = dur;
        triggerChance = chance;
        description = GenerateDescription();
    }

    public bool IsDebuff()
    {
        return effectType >= AttachmentEffectType.Bleeding &&
               effectType <= AttachmentEffectType.Marked;
    }

    public bool IsStatBonus()
    {
        return effectType >= AttachmentEffectType.PhysicalDamageBonus &&
               effectType <= AttachmentEffectType.MagicPenetration;
    }

    public string GenerateDescription()
    {
        string chanceText = triggerChance < 1f ? $"{triggerChance * 100:F0}%几率" : "";
        string durationText = duration > 0 ? $"，持续{duration}秒" : "";

        return effectType switch
        {
            AttachmentEffectType.PhysicalDamageBonus => $"物理伤害+{value:F0}",
            AttachmentEffectType.MagicDamageBonus => $"魔法伤害+{value:F0}",
            AttachmentEffectType.CritRateBonus => $"暴击率+{value * 100:F1}%",
            AttachmentEffectType.CritDamageBonus => $"暴击伤害+{value * 100:F1}%",
            AttachmentEffectType.Bleeding => $"{chanceText}使目标流血{durationText}",
            AttachmentEffectType.Burning => $"{chanceText}使目标燃烧{durationText}",
            AttachmentEffectType.Poisoned => $"{chanceText}使目标中毒{durationText}",
            AttachmentEffectType.Stunned => $"{chanceText}眩晕目标{durationText}",
            _ => $"{effectType}: {value}"
        };
    }
}

/// <summary>
/// 配件数据定义
/// </summary>
[CreateAssetMenu(fileName = "New Attachment", menuName = "Game Data/Equipment/Attachment")]
public class AttachmentData : ItemData
{
    [Header("配件信息")]
    [Tooltip("配件类型")]
    public AttachmentType attachmentType;

    [Tooltip("配件稀有度")]
    public AttachmentRarity attachmentRarity;

    [Tooltip("适用的武器类型")]
    public WeaponTypeFlag compatibleWeapons = WeaponTypeFlag.All;

    [Header("效果列表")]
    [Tooltip("配件提供的所有效果")]
    public List<AttachmentEffect> effects = new List<AttachmentEffect>();

    [Header("镶嵌")]
    [Tooltip("镶嵌所需金币")]
    public int socketCost;

    [Tooltip("是否可拆卸")]
    public bool isRemovable = true;

    [Tooltip("拆卸所需金币")]
    public int removeCost;

    /// <summary>
    /// 检查是否兼容指定武器类型
    /// </summary>
    public bool IsCompatibleWith(WeaponTypeFlag weaponType)
    {
        return (compatibleWeapons & weaponType) != 0;
    }

    /// <summary>
    /// 获取所有属性加成效果
    /// </summary>
    public List<AttachmentEffect> GetStatBonuses()
    {
        return effects.FindAll(e => e.IsStatBonus());
    }

    /// <summary>
    /// 获取所有负面效果
    /// </summary>
    public List<AttachmentEffect> GetDebuffs()
    {
        return effects.FindAll(e => e.IsDebuff());
    }

    public Color GetAttachmentRarityColor()
    {
        return attachmentRarity switch
        {
            AttachmentRarity.Common => Color.white,
            AttachmentRarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),
            AttachmentRarity.Rare => new Color(0.3f, 0.5f, 1f),
            AttachmentRarity.Epic => new Color(0.7f, 0.3f, 0.9f),
            AttachmentRarity.Legendary => new Color(1f, 0.6f, 0.1f),
            AttachmentRarity.Mythic => new Color(1f, 0.2f, 0.2f),
            _ => Color.gray
        };
    }

    public override string GenerateTooltip()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGB(GetAttachmentRarityColor())}>{displayName}</color>");
        sb.AppendLine($"<size=80%>{attachmentType} · {attachmentRarity}</size>");

        if (!string.IsNullOrEmpty(description))
        {
            sb.AppendLine();
            sb.AppendLine($"<i>{description}</i>");
        }

        if (effects.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<b>效果：</b>");
            foreach (var effect in effects)
            {
                string desc = string.IsNullOrEmpty(effect.description)
                    ? effect.GenerateDescription()
                    : effect.description;
                sb.AppendLine($"  • {desc}");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"<color=cyan>镶嵌费用: {socketCost}金币</color>");

        if (isRemovable)
        {
            sb.AppendLine($"<color=cyan>拆卸费用: {removeCost}金币</color>");
        }
        else
        {
            sb.AppendLine("<color=red>不可拆卸</color>");
        }

        return sb.ToString();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        itemType = ItemType.Attachment;
    }
#endif
}