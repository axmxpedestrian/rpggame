using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ItemSystem.Inventory
{
    using Core;
    
    /// <summary>
    /// 库存接口
    /// </summary>
    public interface IInventory
    {
        int Capacity { get; }
        int UsedSlots { get; }
        
        bool AddItem(ItemInstance item);
        bool RemoveItem(int itemId, int count = 1);
        bool HasItem(int itemId, int count = 1);
        int GetItemCount(int itemId);
        ItemInstance GetItem(int itemId);
        List<ItemInstance> GetAllItems();
        void Clear();
    }
    
    /// <summary>
    /// 主库存 - 存储所有物品
    /// </summary>
    [Serializable]
    public class MainInventory : IInventory
    {
        [SerializeField] private int capacity = 100;
        [SerializeField] private List<ItemInstance> items = new();
        
        public int Capacity => capacity;
        public int UsedSlots => items.Count;
        
        public event Action<ItemInstance> OnItemAdded;
        public event Action<ItemInstance, int> OnItemRemoved;
        public event Action OnInventoryChanged;
        
        public bool AddItem(ItemInstance item)
        {
            if (item == null) return false;
            
            // 尝试堆叠
            if (item.Template.IsStackable)
            {
                var existing = items.FirstOrDefault(i => i.CanStackWith(item));
                if (existing != null)
                {
                    existing.StackCount += item.StackCount;
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
            
            // 新增槽位
            if (UsedSlots >= capacity)
                return false;
            
            items.Add(item);
            OnItemAdded?.Invoke(item);
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        public bool RemoveItem(int itemId, int count = 1)
        {
            var item = items.FirstOrDefault(i => i.ItemId == itemId);
            if (item == null) return false;
            
            if (item.StackCount <= count)
            {
                items.Remove(item);
            }
            else
            {
                item.StackCount -= count;
            }
            
            OnItemRemoved?.Invoke(item, count);
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        public bool HasItem(int itemId, int count = 1)
        {
            return GetItemCount(itemId) >= count;
        }
        
        public int GetItemCount(int itemId)
        {
            return items
                .Where(i => i.ItemId == itemId)
                .Sum(i => i.StackCount);
        }
        
        public ItemInstance GetItem(int itemId)
        {
            return items.FirstOrDefault(i => i.ItemId == itemId);
        }
        
        public List<ItemInstance> GetAllItems() => new(items);
        
        public void Clear()
        {
            items.Clear();
            OnInventoryChanged?.Invoke();
        }
        
        /// <summary>
        /// 按类型筛选物品
        /// </summary>
        public List<ItemInstance> GetItemsByType(ItemType type)
        {
            return items.Where(i => i.Template.ItemType == type).ToList();
        }
        
        /// <summary>
        /// 获取可携带进入战斗的物品
        /// </summary>
        public List<ItemInstance> GetBattleCarriableItems()
        {
            return items.Where(i => i.Template.CanCarryToBattle).ToList();
        }
    }
    
    /// <summary>
    /// 战斗背包 - 战斗中临时使用
    /// </summary>
    [Serializable]
    public class CombatInventory : IInventory
    {
        [SerializeField] private int capacity = 10;
        [SerializeField] private List<ItemInstance> items = new();
        [SerializeField] private List<ItemInstance> loot = new();  // 战利品
        
        public int Capacity => capacity;
        public int UsedSlots => items.Count;
        public List<ItemInstance> Loot => loot;
        
        public event Action<ItemInstance> OnItemUsed;
        public event Action<ItemInstance> OnLootAcquired;
        
        public bool AddItem(ItemInstance item)
        {
            if (item == null || UsedSlots >= capacity) return false;
            
            // 尝试堆叠
            if (item.Template.IsStackable)
            {
                var existing = items.FirstOrDefault(i => i.CanStackWith(item));
                if (existing != null)
                {
                    existing.StackCount += item.StackCount;
                    return true;
                }
            }
            
            items.Add(item);
            return true;
        }
        
        public bool RemoveItem(int itemId, int count = 1)
        {
            var item = items.FirstOrDefault(i => i.ItemId == itemId);
            if (item == null) return false;
            
            if (item.StackCount <= count)
            {
                items.Remove(item);
            }
            else
            {
                item.StackCount -= count;
            }
            
            OnItemUsed?.Invoke(item);
            return true;
        }
        
        public bool HasItem(int itemId, int count = 1) => GetItemCount(itemId) >= count;
        
        public int GetItemCount(int itemId)
        {
            return items.Where(i => i.ItemId == itemId).Sum(i => i.StackCount);
        }
        
        public ItemInstance GetItem(int itemId)
        {
            return items.FirstOrDefault(i => i.ItemId == itemId);
        }
        
        public List<ItemInstance> GetAllItems() => new(items);
        
        public void Clear()
        {
            items.Clear();
            loot.Clear();
        }
        
        /// <summary>
        /// 添加战利品
        /// </summary>
        public void AddLoot(ItemInstance item)
        {
            if (item == null) return;
            
            // 尝试堆叠
            if (item.Template.IsStackable)
            {
                var existing = loot.FirstOrDefault(i => i.CanStackWith(item));
                if (existing != null)
                {
                    existing.StackCount += item.StackCount;
                    OnLootAcquired?.Invoke(item);
                    return;
                }
            }
            
            loot.Add(item);
            OnLootAcquired?.Invoke(item);
        }
        
        /// <summary>
        /// 获取战斗中可使用的物品
        /// </summary>
        public List<ItemInstance> GetUsableItems()
        {
            return items.Where(i => i.Template.IsUsableInCombat).ToList();
        }
    }
    
    /// <summary>
    /// 携带规则配置
    /// </summary>
    [CreateAssetMenu(fileName = "NewCarryRule", menuName = "ItemSystem/CarryRule")]
    public class CarryRuleConfig : ScriptableObject
    {
        [Header("基础限制")]
        public int maxTotalItems = 10;
        public int maxConsumables = 5;
        
        [Header("类型限制")]
        public ItemType[] allowedTypes = { ItemType.Consumable };
        public ItemType[] forbiddenTypes = { ItemType.Material, ItemType.QuestItem };
        
        [Header("特殊规则")]
        public bool allowEquipmentSwap = false;
        public int maxHealingItems = 3;
        public int maxBuffItems = 3;
        
        /// <summary>
        /// 检查物品是否可携带
        /// </summary>
        public bool CanCarry(ItemInstance item)
        {
            if (item == null) return false;
            
            var template = item.Template;
            
            // 检查基础标记
            if (!template.CanCarryToBattle) return false;
            
            // 检查禁止类型
            if (forbiddenTypes.Contains(template.ItemType)) return false;
            
            // 检查允许类型
            if (allowedTypes.Length > 0 && !allowedTypes.Contains(template.ItemType))
                return false;
            
            return true;
        }
    }
    
    /// <summary>
    /// 库存管理器 - 协调主库存和战斗背包
    /// </summary>
    public class InventoryManager
    {
        public MainInventory MainInventory { get; private set; }
        public CombatInventory CombatInventory { get; private set; }
        
        private CarryRuleConfig _currentCarryRule;
        
        public event Action OnPrepareBattle;
        public event Action OnBattleEnd;
        
        public InventoryManager()
        {
            MainInventory = new MainInventory();
            CombatInventory = new CombatInventory();
        }
        
        /// <summary>
        /// 获取战斗背包引用
        /// </summary>
        public CombatInventory GetCombatInventory()
        {
            return CombatInventory;
        }
        
        /// <summary>
        /// 设置携带规则
        /// </summary>
        public void SetCarryRule(CarryRuleConfig rule)
        {
            _currentCarryRule = rule;
        }
        
        /// <summary>
        /// 准备进入战斗 - 选择携带物品
        /// </summary>
        public BattlePreparationResult PrepareBattle(List<int> selectedItemIds)
        {
            var result = new BattlePreparationResult();
            CombatInventory.Clear();
            
            foreach (var itemId in selectedItemIds)
            {
                var item = MainInventory.GetItem(itemId);
                if (item == null)
                {
                    result.FailedItems.Add(itemId);
                    continue;
                }
                
                // 验证携带规则
                if (_currentCarryRule != null && !_currentCarryRule.CanCarry(item))
                {
                    result.FailedItems.Add(itemId);
                    result.FailureReasons[itemId] = "不符合携带规则";
                    continue;
                }
                
                // 检查数量限制
                if (!CheckQuantityLimits(item, result))
                {
                    result.FailedItems.Add(itemId);
                    continue;
                }
                
                // 创建副本放入战斗背包（保持主库存不变）
                var combatCopy = item.Clone();
                combatCopy.StackCount = 1;  // 每次只携带1个
                
                if (CombatInventory.AddItem(combatCopy))
                {
                    result.CarriedItems.Add(combatCopy);
                }
            }
            
            result.Success = result.FailedItems.Count == 0;
            OnPrepareBattle?.Invoke();
            return result;
        }
        
        private bool CheckQuantityLimits(ItemInstance item, BattlePreparationResult result)
        {
            if (_currentCarryRule == null) return true;
            
            // 检查总数限制
            if (CombatInventory.UsedSlots >= _currentCarryRule.maxTotalItems)
            {
                result.FailureReasons[item.ItemId] = "已达到携带上限";
                return false;
            }
            
            // 检查消耗品数量
            if (item.Template.ItemType == ItemType.Consumable)
            {
                int currentConsumables = CombatInventory.GetAllItems()
                    .Count(i => i.Template.ItemType == ItemType.Consumable);
                if (currentConsumables >= _currentCarryRule.maxConsumables)
                {
                    result.FailureReasons[item.ItemId] = "消耗品已达上限";
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 战斗中使用物品
        /// </summary>
        public bool UseItemInCombat(int itemId, ICharacter user, ICharacter target = null)
        {
            var item = CombatInventory.GetItem(itemId);
            if (item == null) return false;
            
            var template = item.Template;
            if (template is not ICombatUsable usable) return false;
            
            // 使用物品
            usable.Use(user, target);
            
            // 消耗物品
            CombatInventory.RemoveItem(itemId, 1);
            
            // 同步消耗主库存
            MainInventory.RemoveItem(itemId, 1);
            
            return true;
        }
        
        /// <summary>
        /// 战斗结束 - 合并战利品
        /// </summary>
        public BattleEndResult EndBattle(bool victory)
        {
            var result = new BattleEndResult { Victory = victory };
            
            if (victory)
            {
                // 将战利品转移到主库存
                foreach (var lootItem in CombatInventory.Loot)
                {
                    if (MainInventory.AddItem(lootItem))
                    {
                        result.AcquiredLoot.Add(lootItem);
                    }
                    else
                    {
                        result.OverflowLoot.Add(lootItem);
                    }
                }
            }
            
            // 清空战斗背包
            CombatInventory.Clear();
            
            OnBattleEnd?.Invoke();
            return result;
        }
        
        /// <summary>
        /// 获取可携带进入战斗的物品列表
        /// </summary>
        public List<ItemInstance> GetCarriableItems()
        {
            if (_currentCarryRule == null)
                return MainInventory.GetBattleCarriableItems();
            
            return MainInventory.GetAllItems()
                .Where(i => _currentCarryRule.CanCarry(i))
                .ToList();
        }
    }
    
    public class BattlePreparationResult
    {
        public bool Success;
        public List<ItemInstance> CarriedItems = new();
        public List<int> FailedItems = new();
        public Dictionary<int, string> FailureReasons = new();
    }
    
    public class BattleEndResult
    {
        public bool Victory;
        public List<ItemInstance> AcquiredLoot = new();
        public List<ItemInstance> OverflowLoot = new();  // 库存满溢出的物品
    }
}
