// ============================================
// ItemData.cs - 物品基类 ScriptableObject
// ============================================
using UnityEngine;

/// <summary>
/// 物品数据基类
/// 所有物品类型的公共属性
/// </summary>
public abstract class ItemData : ScriptableObject
{
    [Header("基本信息")]
    [Tooltip("物品唯一ID")]
    public string itemID;

    [Tooltip("显示名称")]
    public string displayName;

    [Tooltip("物品描述")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("物品图标")]
    public Sprite icon;

    [Header("分类")]
    [Tooltip("物品类型")]
    public ItemType itemType;

    [Tooltip("物品稀有度")]
    public ItemRarity rarity;

    [Header("堆叠")]
    [Tooltip("是否可堆叠")]
    public bool isStackable = false;

    [Tooltip("最大堆叠数量")]
    public int maxStackSize = 1;

    [Header("经济")]
    [Tooltip("购买价格")]
    public int buyPrice;

    [Tooltip("出售价格")]
    public int sellPrice;

    [Header("限制")]
    [Tooltip("等级需求")]
    public int levelRequirement;

    [Tooltip("是否可丢弃")]
    public bool isDroppable = true;

    [Tooltip("是否可交易")]
    public bool isTradable = true;

    [Tooltip("是否可在战斗中使用")]
    public bool usableInBattle = false;

    /// <summary>
    /// 获取稀有度颜色
    /// </summary>
    public virtual Color GetRarityColor()
    {
        return rarity switch
        {
            ItemRarity.Common => Color.white,
            ItemRarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),
            ItemRarity.Rare => new Color(0.3f, 0.5f, 1f),
            ItemRarity.Epic => new Color(0.7f, 0.3f, 0.9f),
            ItemRarity.Legendary => new Color(1f, 0.6f, 0.1f),
            ItemRarity.Mythic => new Color(1f, 0.2f, 0.2f),
            _ => Color.gray
        };
    }

    /// <summary>
    /// 生成物品描述
    /// </summary>
    public virtual string GenerateTooltip()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGB(GetRarityColor())}>{displayName}</color>");
        sb.AppendLine($"<size=80%>{itemType} · {rarity}</size>");

        if (!string.IsNullOrEmpty(description))
        {
            sb.AppendLine();
            sb.AppendLine($"<i>{description}</i>");
        }

        if (levelRequirement > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"<color=yellow>需要等级: {levelRequirement}</color>");
        }

        return sb.ToString();
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(itemID))
        {
            itemID = name.ToLower().Replace(" ", "_");
        }

        if (sellPrice == 0 && buyPrice > 0)
        {
            sellPrice = Mathf.RoundToInt(buyPrice * 0.4f);
        }

        if (maxStackSize < 1) maxStackSize = 1;
    }
#endif
}