// ============================================
// MaterialData.cs - 材料数据 ScriptableObject
// ============================================
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 材料数据定义
/// </summary>
[CreateAssetMenu(fileName = "New Material", menuName = "Game Data/Items/Material")]
public class MaterialData : ItemData
{
    [Header("材料信息")]
    [Tooltip("材料类型")]
    public MaterialType materialType;

    [Header("获取方式")]
    [Tooltip("获取来源描述")]
    public string obtainSource;

    [Tooltip("关联的专业技能（采集用）")]
    public ProfessionType gatherProfession;

    [Tooltip("采集需要的技能等级")]
    public int gatherRequiredLevel = 0;

    [Header("合成用途")]
    [Tooltip("可用于合成的配方ID列表")]
    public List<string> usedInRecipes = new List<string>();

    [Header("炼金属性")]
    [Tooltip("炼金价值")]
    public int alchemyValue = 0;

    [Tooltip("元素属性")]
    public ElementType elementProperty = ElementType.None;

    /// <summary>
    /// 创建运行时材料实例
    /// </summary>
    public MaterialItem CreateInstance(int count = 1)
    {
        return new MaterialItem(this, count);
    }

    public override string GenerateTooltip()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(base.GenerateTooltip());

        sb.AppendLine();
        sb.AppendLine($"<b>类型:</b> {GetTypeName()}");

        if (!string.IsNullOrEmpty(obtainSource))
        {
            sb.AppendLine($"<b>获取:</b> {obtainSource}");
        }

        if (gatherRequiredLevel > 0)
        {
            sb.AppendLine($"<color=yellow>采集需要{GetProfessionName()}等级: {gatherRequiredLevel}</color>");
        }

        if (elementProperty != ElementType.None)
        {
            sb.AppendLine($"<b>元素属性:</b> {GetElementName()}");
        }

        if (usedInRecipes.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"<color=cyan>可用于{usedInRecipes.Count}个配方</color>");
        }

        return sb.ToString();
    }

    private string GetTypeName()
    {
        return materialType switch
        {
            MaterialType.Ore => "矿石",
            MaterialType.Wood => "木材",
            MaterialType.Herb => "草药",
            MaterialType.Cloth => "布料",
            MaterialType.Leather => "皮革",
            MaterialType.Gem => "宝石原石",
            MaterialType.MonsterDrop => "怪物掉落",
            MaterialType.Essence => "精华",
            MaterialType.Ingredient => "烹饪原料",
            _ => materialType.ToString()
        };
    }

    private string GetProfessionName()
    {
        return gatherProfession switch
        {
            ProfessionType.Mining => "采矿",
            ProfessionType.Logging => "伐木",
            ProfessionType.Planting => "种植",
            _ => gatherProfession.ToString()
        };
    }

    private string GetElementName()
    {
        return elementProperty switch
        {
            ElementType.Fire => "火焰",
            ElementType.Ice => "冰霜",
            ElementType.Lightning => "雷电",
            ElementType.Poison => "毒素",
            ElementType.Holy => "神圣",
            ElementType.Dark => "黑暗",
            _ => "无"
        };
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        itemType = ItemType.Material;
        isStackable = true;
        if (maxStackSize < 1) maxStackSize = 999;
    }
#endif
}