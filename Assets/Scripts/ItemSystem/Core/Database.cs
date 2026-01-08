using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItemSystem.Core
{
    /// <summary>
    /// 物品数据库 - 管理所有物品模板
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "ItemSystem/Databases/ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        private static ItemDatabase _instance;
        public static ItemDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ItemDatabase>("Databases/ItemDatabase");
                    if (_instance != null)
                        _instance.Initialize();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 手动设置实例（用于非Resources加载方式）
        /// </summary>
        public static void SetInstance(ItemDatabase database)
        {
            _instance = database;
            _instance?.Initialize();
        }

        [SerializeField] private Item[] allItems;

        private Dictionary<int, Item> _itemLookup;
        private Dictionary<ItemType, List<Item>> _typeIndex;
        private bool _initialized;

        private void OnEnable()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_initialized) return;
            BuildLookups();
            _initialized = true;
        }

        private void BuildLookups()
        {
            _itemLookup = new Dictionary<int, Item>();
            _typeIndex = new Dictionary<ItemType, List<Item>>();

            if (allItems == null) return;

            foreach (var item in allItems)
            {
                if (item == null) continue;

                _itemLookup[item.ItemId] = item;

                if (!_typeIndex.ContainsKey(item.ItemType))
                    _typeIndex[item.ItemType] = new List<Item>();
                _typeIndex[item.ItemType].Add(item);
            }
        }

        public Item GetItem(int itemId)
        {
            if (_itemLookup == null) BuildLookups();
            return _itemLookup.TryGetValue(itemId, out var item) ? item : null;
        }

        public List<Item> GetItemsByType(ItemType type)
        {
            if (_typeIndex == null) BuildLookups();
            return _typeIndex.TryGetValue(type, out var items) ? new List<Item>(items) : new List<Item>();
        }

        public T GetItem<T>(int itemId) where T : Item
        {
            return GetItem(itemId) as T;
        }

        public List<Item> GetAllItems()
        {
            return allItems != null ? new List<Item>(allItems) : new List<Item>();
        }

        /// <summary>
        /// 编辑器用：设置物品数组
        /// </summary>
        public void SetItems(Item[] items)
        {
            allItems = items;
            _initialized = false;
            BuildLookups();
        }
    }

    /// <summary>
    /// 属性修改器
    /// </summary>
    [Serializable]
    public class StatModifier
    {
        public StatType StatType;
        public ModifierType ModifierType;
        public float Value;
        [NonSerialized] public object Source;  // 修改器来源（用于追踪）

        public StatModifier() { }

        public StatModifier(StatType stat, ModifierType type, float value, object source = null)
        {
            StatType = stat;
            ModifierType = type;
            Value = value;
            Source = source;
        }
    }

    public enum StatType
    {
        // 基础属性
        Constitution,   // 体质
        Strength,       // 力量
        Perception,     // 感知
        Reaction,       // 反应
        Wisdom,         // 智慧
        Luck,           // 幸运

        // 战斗属性
        MaxHealth,
        PhysicalAttack,
        MagicAttack,
        PhysicalDefense,
        MagicDefense,
        Resistance,
        CriticalRate,
        Speed,
        Accuracy,
        Evasion,
        PhysicalBlockRate,
        MagicBlockRate,
        MaxPhysicalSkillPoints,
        MaxMagicSkillPoints
    }

    public enum ModifierType
    {
        Flat,           // 固定值
        PercentAdd,     // 百分比加成（加法）
        PercentMult     // 百分比乘数（乘法）
    }

    /// <summary>
    /// 有耐久度接口
    /// </summary>
    public interface IDurable
    {
        int CurrentDurability { get; set; }
        int MaxDurability { get; }
        void ReduceDurability(int amount);
        void Repair(int amount);
        bool IsBroken { get; }
    }

    /// <summary>
    /// 镶嵌宝石
    /// </summary>
    [CreateAssetMenu(fileName = "NewSocketGem", menuName = "ItemSystem/SocketGem")]
    public class SocketGem : Item
    {
        [Header("宝石属性")]
        [SerializeField] private GemType gemType;
        [SerializeField] private int gemTier;
        [SerializeField] private StatModifier[] bonuses;
        [SerializeField] private GemCompatibility compatibility;

        public GemType GemType => gemType;
        public int GemTier => gemTier;
        public StatModifier[] Bonuses => bonuses;

        /// <summary>
        /// 检查是否可镶嵌到指定装备
        /// </summary>
        public bool CanInsertInto(Item equipment)
        {
            if (equipment == null) return false;

            return compatibility switch
            {
                GemCompatibility.All => true,
                GemCompatibility.WeaponOnly => equipment.ItemType == ItemType.Equipment,
                GemCompatibility.ArmorOnly => equipment.ItemType == ItemType.Equipment,
                GemCompatibility.AccessoryOnly => equipment.ItemType == ItemType.Equipment,
                _ => true
            };
        }
    }

    public enum GemType
    {
        Ruby,           // 红宝石 - 攻击
        Sapphire,       // 蓝宝石 - 魔法
        Emerald,        // 绿宝石 - 生命
        Diamond,        // 钻石 - 全属性
        Amethyst,       // 紫水晶 - 技能
        Topaz,          // 黄宝石 - 速度
        Onyx            // 黑曜石 - 暴击
    }

    public enum GemCompatibility
    {
        All,
        WeaponOnly,
        ArmorOnly,
        AccessoryOnly
    }

    /// <summary>
    /// 状态效果基类
    /// </summary>
    public abstract class StatusEffect : IStatusEffect
    {
        public virtual void OnApply(ICharacter target) { }
        public virtual void OnRemove(ICharacter target) { }
        public virtual void OnTick(ICharacter target, float deltaTime) { }
        public abstract bool IsExpired { get; }
    }

    public enum StatusEffectType
    {
        // 负面效果
        Bleeding,       // 流血
        Poisoned,       // 中毒
        Burning,        // 燃烧
        Frozen,         // 冰冻
        Stunned,        // 眩晕
        Vulnerable,     // 脆弱
        Weakened,       // 虚弱
        Blinded,        // 致盲
        Silenced,       // 沉默

        // 正面效果
        Regeneration,   // 再生
        Shield,         // 护盾
        Haste,          // 加速
        Strength,       // 强化
        Protection,     // 防护
        Focus           // 专注
    }

    /// <summary>
    /// 技能数据基类（技能系统扩展此类）
    /// </summary>
    public class SkillData : ScriptableObject
    {
        [SerializeField] protected int skillId;
        [SerializeField] protected string skillName;

        public int SkillId => skillId;
        public string SkillName => skillName;
    }

    /// <summary>
    /// 被动技能数据基类
    /// </summary>
    public class PassiveSkillData : ScriptableObject
    {
        [SerializeField] protected int passiveId;
        [SerializeField] protected string passiveName;

        public int PassiveId => passiveId;
        public string PassiveName => passiveName;
    }
}

namespace ItemSystem
{
    using Core;
    using Consumables;

    /// <summary>
    /// 战斗上下文
    /// </summary>
    public class CombatContext
    {
        public ICharacter CurrentCharacter;
        public System.Collections.Generic.List<ICharacter> Allies;
        public System.Collections.Generic.List<ICharacter> Enemies;
    }

    // ============================================================
    // 注意：Character 类定义在 CombatSystem 命名空间中
    // 物品系统通过 ICharacter 接口与角色交互
    // ============================================================

    /// <summary>
    /// Buff效果（与 ICharacter 配合使用）
    /// </summary>
    [System.Serializable]
    public class BuffEffect
    {
        public BuffType buffType;
        public float value;
        public bool isPercentage;

        public void Apply(ICharacter target)
        {
            target.CombatStats?.AddTemporaryBonus(buffType, isPercentage ? value : value / 100f);
        }

        public void Remove(ICharacter target)
        {
            target.CombatStats?.RemoveTemporaryBonus(buffType, isPercentage ? value : value / 100f);
        }
    }

    /// <summary>
    /// 持续回复效果
    /// </summary>
    public class HealOverTimeEffect : StatusEffect
    {
        private readonly HealingType _healingType;
        private readonly int _amountPerTick;
        private readonly float _tickInterval;
        private readonly int _totalTicks;
        private int _currentTick;
        private float _timer;

        public HealOverTimeEffect(HealingType healingType, int amountPerTick,
            float tickInterval, int totalTicks)
        {
            _healingType = healingType;
            _amountPerTick = amountPerTick;
            _tickInterval = tickInterval;
            _totalTicks = totalTicks;
        }

        public override void OnTick(ICharacter target, float deltaTime)
        {
            _timer += deltaTime;

            if (_timer >= _tickInterval && _currentTick < _totalTicks)
            {
                _timer = 0f;
                _currentTick++;

                switch (_healingType)
                {
                    case HealingType.Health:
                        target.Heal(_amountPerTick);
                        break;
                    case HealingType.SkillPoints:
                        target.RestoreSkillPoints(_amountPerTick);
                        break;
                }
            }
        }

        public override bool IsExpired => _currentTick >= _totalTicks;
    }

    /// <summary>
    /// Buff状态效果
    /// </summary>
    public class BuffStatusEffect : StatusEffect
    {
        private readonly BuffEffect _buff;
        private readonly float _duration;
        private readonly bool _isPermanent;
        private float _remainingTime;

        public BuffStatusEffect(BuffEffect buff, float duration, bool isPermanent)
        {
            _buff = buff;
            _duration = duration;
            _isPermanent = isPermanent;
            _remainingTime = duration;
        }

        public override void OnApply(ICharacter target)
        {
            _buff.Apply(target);
        }

        public override void OnRemove(ICharacter target)
        {
            _buff.Remove(target);
        }

        public override void OnTick(ICharacter target, float deltaTime)
        {
            if (!_isPermanent)
            {
                _remainingTime -= deltaTime;
            }
        }

        public override bool IsExpired => !_isPermanent && _remainingTime <= 0;
    }
}
