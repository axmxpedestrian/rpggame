// ============================================
// ToolData.cs - 工具数据 ScriptableObject
// ============================================
using UnityEngine;

/// <summary>
/// 工具数据定义
/// </summary>
[CreateAssetMenu(fileName = "New Tool", menuName = "Game Data/Items/Tool")]
public class ToolData : ItemData
{
    [Header("工具信息")]
    [Tooltip("工具类型")]
    public ToolType toolType;

    [Header("使用属性")]
    [Tooltip("使用次数（0为无限）")]
    public int maxUses = 0;

    [Tooltip("使用后是否消耗")]
    public bool consumeOnUse = false;

    [Header("效率属性")]
    [Tooltip("工作效率加成")]
    public float efficiencyBonus = 1.0f;

    [Tooltip("关联的专业技能")]
    public ProfessionType relatedProfession;

    [Tooltip("使用需要的技能等级")]
    public int requiredProfessionLevel = 0;

    [Header("特殊属性")]
    [Tooltip("可开启的门/宝箱ID列表（钥匙用）")]
    public string[] unlockableIDs;

    [Tooltip("伤害值（炸弹等）")]
    public int damage;

    [Tooltip("影响范围")]
    public float effectRadius;

    /// <summary>
    /// 创建运行时工具实例
    /// </summary>
    public ToolItem CreateInstance(int count = 1)
    {
        return new ToolItem(this, count);
    }

    public override string GenerateTooltip()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(base.GenerateTooltip());

        sb.AppendLine();
        sb.AppendLine($"<b>类型:</b> {GetTypeName()}");

        if (maxUses > 0)
        {
            sb.AppendLine($"<b>使用次数:</b> {maxUses}");
        }

        if (efficiencyBonus != 1.0f)
        {
            sb.AppendLine($"<b>效率加成:</b> {efficiencyBonus * 100:F0}%");
        }

        if (requiredProfessionLevel > 0)
        {
            sb.AppendLine($"<color=yellow>需要{GetProfessionName()}等级: {requiredProfessionLevel}</color>");
        }

        if (damage > 0)
        {
            sb.AppendLine($"<b>伤害:</b> {damage}");
        }

        return sb.ToString();
    }

    private string GetTypeName()
    {
        return toolType switch
        {
            ToolType.Key => "钥匙",
            ToolType.Bomb => "炸弹",
            ToolType.Rope => "绳索",
            ToolType.Torch => "火把",
            ToolType.Map => "地图",
            ToolType.Compass => "指南针",
            ToolType.Pickaxe => "镐",
            ToolType.Axe => "斧头",
            ToolType.FishingRod => "钓竿",
            _ => toolType.ToString()
        };
    }

    private string GetProfessionName()
    {
        return relatedProfession switch
        {
            ProfessionType.Mining => "采矿",
            ProfessionType.Logging => "伐木",
            ProfessionType.Planting => "种植",
            ProfessionType.Cooking => "烹饪",
            ProfessionType.Building => "建造",
            ProfessionType.Archaeology => "考古",
            _ => relatedProfession.ToString()
        };
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        itemType = ItemType.Tool;
        isStackable = consumeOnUse;
        if (isStackable && maxStackSize < 1) maxStackSize = 99;
    }
#endif
}