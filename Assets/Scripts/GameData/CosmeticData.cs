// ============================================
// CosmeticData.cs - 服装/时装数据 ScriptableObject
// ============================================
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 服装部位
/// </summary>
public enum CosmeticSlot
{
    FullBody,       // 全身
    Head,           // 头部
    Face,           // 面部
    Back,           // 背部
    Weapon,         // 武器皮肤
    Pet,            // 宠物
    Effect          // 特效
}

/// <summary>
/// 服装数据定义
/// </summary>
[CreateAssetMenu(fileName = "New Cosmetic", menuName = "Game Data/Items/Cosmetic")]
public class CosmeticData : ItemData
{
    [Header("服装信息")]
    [Tooltip("服装部位")]
    public CosmeticSlot cosmeticSlot;

    [Header("外观")]
    [Tooltip("模型/皮肤资源路径")]
    public string modelPath;

    [Tooltip("预览图")]
    public Sprite previewImage;

    [Tooltip("颜色可自定义")]
    public bool colorCustomizable = false;

    [Tooltip("默认颜色")]
    public Color defaultColor = Color.white;

    [Header("属性加成（可选）")]
    [Tooltip("是否带有属性")]
    public bool hasStats = false;

    [Tooltip("属性加成列表")]
    public List<StatBonus> statBonuses = new List<StatBonus>();

    [Header("特效")]
    [Tooltip("特效资源路径")]
    public string effectPath;

    [Tooltip("光环效果")]
    public string auraEffect;

    [Header("限制")]
    [Tooltip("适用的角色ID列表（空为所有角色）")]
    public List<string> applicableCharacters = new List<string>();

    [Tooltip("是否限时")]
    public bool isTimeLimited = false;

    [Tooltip("有效期（天，0为永久）")]
    public int validDays = 0;

    /// <summary>
    /// 检查是否适用于指定角色
    /// </summary>
    public bool IsApplicableTo(string characterID)
    {
        if (applicableCharacters.Count == 0) return true;
        return applicableCharacters.Contains(characterID);
    }

    public override string GenerateTooltip()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(base.GenerateTooltip());

        sb.AppendLine();
        sb.AppendLine($"<b>部位:</b> {GetSlotName()}");

        if (hasStats && statBonuses.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<b>属性加成:</b>");
            foreach (var bonus in statBonuses)
            {
                sb.AppendLine($"  {bonus.GetDescription()}");
            }
        }

        if (applicableCharacters.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<color=yellow>限定角色使用</color>");
        }

        if (isTimeLimited)
        {
            sb.AppendLine();
            sb.AppendLine($"<color=red>限时: {validDays}天</color>");
        }

        return sb.ToString();
    }

    private string GetSlotName()
    {
        return cosmeticSlot switch
        {
            CosmeticSlot.FullBody => "全身",
            CosmeticSlot.Head => "头部",
            CosmeticSlot.Face => "面部",
            CosmeticSlot.Back => "背部",
            CosmeticSlot.Weapon => "武器皮肤",
            CosmeticSlot.Pet => "宠物",
            CosmeticSlot.Effect => "特效",
            _ => cosmeticSlot.ToString()
        };
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        itemType = ItemType.Cosmetic;
        isStackable = false;
        maxStackSize = 1;
    }
#endif
}