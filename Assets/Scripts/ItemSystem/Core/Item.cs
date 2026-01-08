using System;
using UnityEngine;

namespace ItemSystem.Core
{
    /// <summary>
    /// 物品基类 - ScriptableObject用于静态模板定义
    /// 运行时通过ItemInstance包装实现动态属性
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "ItemSystem/Item")]
    public class Item : ScriptableObject
    {
        [Header("基础信息")]
        [SerializeField] private int itemId;
        [SerializeField] private string itemName;
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemType itemType;
        [SerializeField] private ItemRarity rarity = ItemRarity.Common;

        [Header("功能标记")]
        [SerializeField] private ItemFlags flags;

        [Header("经济属性")]
        [SerializeField] private int buyPrice;
        [SerializeField] private int sellPrice;

        [Header("堆叠设置")]
        [SerializeField] private int maxStackSize = 1;

        // 公开属性
        public int ItemId => itemId;
        public string ItemName => itemName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemType ItemType => itemType;
        public ItemRarity Rarity => rarity;
        public ItemFlags Flags => flags;
        public int BuyPrice => buyPrice;
        public int SellPrice => sellPrice;
        public int MaxStackSize => maxStackSize;

        // 标记检查便捷方法
        public bool HasFlag(ItemFlags flag) => (flags & flag) != 0;
        public bool IsStackable => HasFlag(ItemFlags.Stackable);
        public bool IsCraftable => HasFlag(ItemFlags.Craftable);
        public bool IsReforgeable => HasFlag(ItemFlags.Reforgeable);
        public bool IsSellable => HasFlag(ItemFlags.Sellable);
        public bool IsDroppable => HasFlag(ItemFlags.Droppable);
        public bool CanCarryToBattle => HasFlag(ItemFlags.CanCarryToBattle);
        public bool IsUsableInCombat => HasFlag(ItemFlags.UsableInCombat);

        /// <summary>
        /// 创建物品实例（运行时动态数据）
        /// </summary>
        public virtual ItemInstance CreateInstance(int stackCount = 1)
        {
            return new ItemInstance(this, stackCount);
        }

        /// <summary>
        /// 获取配件槽数量（基于品质）
        /// </summary>
        public int GetSocketCount()
        {
            return rarity switch
            {
                ItemRarity.Common => 0,
                ItemRarity.Uncommon => 1,
                ItemRarity.Rare => 2,
                ItemRarity.Epic => 3,
                ItemRarity.Legendary => 4,
                _ => 0
            };
        }
    }

    /// <summary>
    /// 物品实例 - 运行时动态数据（支持序列化存档）
    /// 遵循 Terraria 设计: Item = { type, prefix, stack }
    /// </summary>
    [Serializable]
    public class ItemInstance
    {
        [SerializeField] private int itemId;           // 物品模板ID
        [SerializeField] private int prefixId;         // 修饰语ID（0=无）
        [SerializeField] private int stackCount;       // 堆叠数量
        [SerializeField] private int currentDurability;// 当前耐久
        [SerializeField] private int[] socketGemIds;   // 镶嵌宝石ID数组

        // 缓存引用（运行时，不序列化）
        [NonSerialized] private Item _template;

        public int ItemId => itemId;
        public int PrefixId => prefixId;
        public int StackCount
        {
            get => stackCount;
            set => stackCount = Mathf.Clamp(value, 0, Template?.MaxStackSize ?? 999);
        }
        public int CurrentDurability
        {
            get => currentDurability;
            set => currentDurability = Mathf.Max(0, value);
        }
        public int[] SocketGemIds => socketGemIds;

        public Item Template
        {
            get
            {
                if (_template == null && ItemDatabase.Instance != null)
                    _template = ItemDatabase.Instance.GetItem(itemId);
                return _template;
            }
        }

        /// <summary>
        /// 获取前缀显示名称（延迟查询，避免循环依赖）
        /// </summary>
        public string GetPrefixDisplayName()
        {
            if (prefixId <= 0) return null;
            // 通过 PrefixRegistry 获取
            return PrefixRegistry.GetPrefixName(prefixId);
        }

        public ItemInstance(Item template, int stack = 1)
        {
            _template = template;
            itemId = template.ItemId;
            prefixId = 0;
            stackCount = stack;
            currentDurability = template is IDurable durable ? durable.MaxDurability : 0;
            socketGemIds = new int[template.GetSocketCount()];
        }

        // 用于反序列化
        public ItemInstance(int itemId, int prefixId, int stack)
        {
            this.itemId = itemId;
            this.prefixId = prefixId;
            this.stackCount = stack;
            this.socketGemIds = Array.Empty<int>();
        }

        /// <summary>
        /// 应用修饰语（重铸）
        /// </summary>
        public void SetPrefix(int newPrefixId)
        {
            prefixId = newPrefixId;
        }

        /// <summary>
        /// 获取显示名称（包含前缀）
        /// </summary>
        public string GetDisplayName()
        {
            string prefixName = GetPrefixDisplayName();
            if (!string.IsNullOrEmpty(prefixName))
                return $"{prefixName} {Template?.ItemName ?? "Unknown"}";
            return Template?.ItemName ?? "Unknown";
        }

        /// <summary>
        /// 判断是否可与另一个实例堆叠
        /// </summary>
        public bool CanStackWith(ItemInstance other)
        {
            if (other == null) return false;
            if (Template == null || !Template.IsStackable) return false;

            // 只有相同模板且无特殊属性的物品可堆叠
            return itemId == other.itemId &&
                   prefixId == other.prefixId &&
                   stackCount + other.stackCount <= Template.MaxStackSize;
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        public ItemInstance Clone()
        {
            var clone = new ItemInstance(itemId, prefixId, stackCount);
            clone.currentDurability = currentDurability;
            clone.socketGemIds = (int[])socketGemIds?.Clone() ?? Array.Empty<int>();
            return clone;
        }

        /// <summary>
        /// 初始化模板缓存（从存档加载后调用）
        /// </summary>
        public void InitializeFromSave()
        {
            _template = null; // 清除缓存，下次访问时会重新加载
            if (socketGemIds == null)
                socketGemIds = Array.Empty<int>();
        }
    }

    /// <summary>
    /// 前缀注册表 - 提供简单的前缀名称查询，避免循环依赖
    /// </summary>
    public static class PrefixRegistry
    {
        private static Func<int, string> _getPrefixNameFunc;

        /// <summary>
        /// 注册前缀名称查询函数（由 PrefixDatabase 初始化时调用）
        /// </summary>
        public static void RegisterPrefixNameProvider(Func<int, string> func)
        {
            _getPrefixNameFunc = func;
        }

        /// <summary>
        /// 获取前缀显示名称
        /// </summary>
        public static string GetPrefixName(int prefixId)
        {
            return _getPrefixNameFunc?.Invoke(prefixId);
        }
    }
}