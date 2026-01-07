// ============================================
// QuestItemData.cs - 剧情道具数据 ScriptableObject
// ============================================
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 剧情道具数据定义
/// </summary>
[CreateAssetMenu(fileName = "New QuestItem", menuName = "Game Data/Items/QuestItem")]
public class QuestItemData : ItemData
{
    [Header("剧情信息")]
    [Tooltip("关联的任务ID")]
    public string questID;

    [Tooltip("是否为关键道具")]
    public bool isKeyItem = true;

    [Header("获取/使用")]
    [Tooltip("获取时触发的事件ID")]
    public string onObtainEventID;

    [Tooltip("使用时触发的事件ID")]
    public string onUseEventID;

    [Tooltip("是否可使用")]
    public bool isUsable = false;

    [Tooltip("使用后是否消耗")]
    public bool consumeOnUse = false;

    [Header("任务完成后")]
    [Tooltip("任务完成后是否移除")]
    public bool removeOnQuestComplete = true;

    [Tooltip("任务完成后转化为的物品ID（可选）")]
    public string transformToItemID;

    [Header("提示")]
    [Tooltip("收集提示文本")]
    public string hintText;

    [Tooltip("使用目标描述")]
    public string useTargetDescription;

    public override string GenerateTooltip()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(base.GenerateTooltip());

        if (isKeyItem)
        {
            sb.AppendLine();
            sb.AppendLine("<color=yellow>【关键道具】</color>");
        }

        if (!string.IsNullOrEmpty(hintText))
        {
            sb.AppendLine();
            sb.AppendLine($"<i>{hintText}</i>");
        }

        if (isUsable && !string.IsNullOrEmpty(useTargetDescription))
        {
            sb.AppendLine();
            sb.AppendLine($"<color=cyan>使用: {useTargetDescription}</color>");
        }

        sb.AppendLine();
        sb.AppendLine("<color=red>不可丢弃</color>");

        return sb.ToString();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        itemType = ItemType.QuestItem;
        isStackable = false;
        maxStackSize = 1;
        isDroppable = false;  // 剧情道具不可丢弃
        isTradable = false;   // 剧情道具不可交易
    }
#endif
}