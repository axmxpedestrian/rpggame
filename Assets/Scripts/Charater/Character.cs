using System;
using System.Collections.Generic;
using UnityEngine;

namespace CombatSystem
{
    using ItemSystem;
    using ItemSystem.Core;
    using ItemSystem.Equipment;
    using ItemSystem.Inventory;
    using ItemSystem.Modifiers;

    /// <summary>
    /// 角色类 - 整合属性、装备、状态效果
    /// 实现 ICharacter 接口，与物品系统兼容
    /// </summary>
    public class Character : MonoBehaviour, ICharacter
    {
        #region 基础信息

        [Header("基础信息")]
        [SerializeField] private string characterId;
        [SerializeField] private string characterName;
        [SerializeField] private int level = 1;
        [SerializeField] private CharacterStatsConfig statsConfig;
        [SerializeField] private CharacterType characterType = CharacterType.Player;

        public string CharacterId => characterId;
        public string CharacterName => characterName;
        public int Level => level;
        public CharacterType CharacterType => characterType;

        #endregion

        #region 属性系统

        [Header("属性")]
        [SerializeField] private CharacterStats stats = new();
        public CharacterStats Stats => stats;

        // ICharacter 接口实现
        ICharacterStats ICharacter.Stats => _statsAdapter ??= new CharacterStatsAdapter(stats);
        private CharacterStatsAdapter _statsAdapter;

        // 战斗属性缓存
        private CombatAttributeCache _combatCache;
        public CombatAttributeCache CombatCache => _combatCache ??= new CombatAttributeCache(this);

        // ICharacter 接口实现
        ICombatStats ICharacter.CombatStats => _combatStatsAdapter ??= new CombatStatsAdapter(this);
        private CombatStatsAdapter _combatStatsAdapter;

        #endregion

        #region 运行时状态

        [Header("当前状态")]
        [SerializeField] private int currentHealth;
        [SerializeField] private int currentPhysicalSP;
        [SerializeField] private int currentMagicSP;
        [SerializeField] private int currentStress;
        [SerializeField] private int currentFatigue;
        [SerializeField] private float currentATB;

        public int CurrentHealth
        {
            get => currentHealth;
            set => currentHealth = Mathf.Clamp(value, 0, Stats.MaxHealth);
        }

        public int CurrentPhysicalSP
        {
            get => currentPhysicalSP;
            set => currentPhysicalSP = Mathf.Clamp(value, 0, Stats.MaxPhysicalSP);
        }

        public int CurrentMagicSP
        {
            get => currentMagicSP;
            set => currentMagicSP = Mathf.Clamp(value, 0, Stats.MaxMagicSP);
        }

        public int CurrentStress
        {
            get => currentStress;
            set => currentStress = Mathf.Clamp(value, 0, MaxStress);
        }

        public int CurrentFatigue
        {
            get => currentFatigue;
            set => currentFatigue = Mathf.Clamp(value, 0, MaxFatigue);
        }

        public float CurrentATB
        {
            get => currentATB;
            set => currentATB = Mathf.Clamp(value, 0, MaxATB);
        }

        public const int MaxStress = 100;
        public const int MaxFatigue = 100;
        public const float MaxATB = 100f;

        public bool IsDowned => currentHealth <= 0;
        public bool IsStressed => currentStress >= 80;
        public bool IsFatigued => currentFatigue >= 80;
        public bool CanAct => !IsDowned && currentATB >= MaxATB;

        // ICharacter 接口实现
        public int MaxHealth => Stats.MaxHealth;

        // 技能点（ICharacter使用单一值，这里合并物理和魔法）
        int ICharacter.CurrentSkillPoints
        {
            get => currentPhysicalSP + currentMagicSP;
            set
            {
                // 平均分配
                currentPhysicalSP = value / 2;
                currentMagicSP = value - currentPhysicalSP;
            }
        }
        int ICharacter.MaxSkillPoints => Stats.MaxPhysicalSP + Stats.MaxMagicSP;

        #endregion

        #region 装备系统

        [Header("装备槽")]
        private ItemInstance _equippedWeapon;
        private ItemInstance _equippedHead;
        private ItemInstance _equippedBody;
        private ItemInstance _equippedLegs;
        private readonly ItemInstance[] _equippedAccessories = new ItemInstance[3];

        public ItemInstance EquippedWeapon => _equippedWeapon;
        public ItemInstance EquippedHead => _equippedHead;
        public ItemInstance EquippedBody => _equippedBody;
        public ItemInstance EquippedLegs => _equippedLegs;
        public ItemInstance[] EquippedAccessories => _equippedAccessories;

        #endregion

        #region 状态效果

        private readonly List<StatusEffectInstance> _statusEffects = new();
        public IReadOnlyList<StatusEffectInstance> StatusEffects => _statusEffects;

        #endregion

        #region 元素抗性

        private readonly Dictionary<ElementType, float> _elementResistances = new();

        public float GetElementResistance(ElementType element)
        {
            return _elementResistances.TryGetValue(element, out float value) ? value : 0f;
        }

        public void SetElementResistance(ElementType element, float value)
        {
            _elementResistances[element] = Mathf.Clamp(value, -1f, 1f);
        }

        #endregion

        #region 事件

        public event Action<Character, int, DamageInfo> OnDamageTaken;
        public event Action<Character, int> OnHealed;
        public event Action<Character> OnDowned;
        public event Action<Character> OnRevived;
        public event Action<Character, StatusEffectInstance> OnStatusEffectAdded;
        public event Action<Character, StatusEffectInstance> OnStatusEffectRemoved;
        public event Action<Character> OnATBFull;
        public event Action<Character, ItemInstance> OnEquipmentChanged;

        #endregion

        #region 生命周期

        private void Awake()
        {
            if (statsConfig != null)
            {
                stats.Initialize(statsConfig);
            }
        }

        private void Start()
        {
            InitializeRuntimeStats();
        }

        /// <summary>
        /// 初始化运行时状态（满状态）
        /// </summary>
        public void InitializeRuntimeStats()
        {
            currentHealth = Stats.MaxHealth;
            currentPhysicalSP = Stats.MaxPhysicalSP;
            currentMagicSP = Stats.MaxMagicSP;
            currentStress = 0;
            currentFatigue = 0;
            currentATB = 0;

            CombatCache.MarkAllDirty();
        }

        #endregion

        #region 装备管理

        /// <summary>
        /// 装备物品
        /// </summary>
        public bool Equip(ItemInstance item)
        {
            if (item?.Template == null) return false;

            var template = item.Template;

            // 检查等级要求
            if (template is EquipmentBase equipment && equipment.LevelRequirement > level)
                return false;

            ItemInstance oldItem = null;

            switch (template)
            {
                case Weapon:
                    oldItem = _equippedWeapon;
                    UnequipInternal(oldItem);
                    _equippedWeapon = item;
                    EquipInternal(item);
                    break;

                case Armor armor:
                    switch (armor.ArmorSlot)
                    {
                        case ArmorSlot.Head:
                            oldItem = _equippedHead;
                            UnequipInternal(oldItem);
                            _equippedHead = item;
                            break;
                        case ArmorSlot.Body:
                            oldItem = _equippedBody;
                            UnequipInternal(oldItem);
                            _equippedBody = item;
                            break;
                        case ArmorSlot.Legs:
                            oldItem = _equippedLegs;
                            UnequipInternal(oldItem);
                            _equippedLegs = item;
                            break;
                    }
                    EquipInternal(item);
                    break;

                case Accessory:
                    // 找空槽位
                    int slot = Array.FindIndex(_equippedAccessories, a => a == null);
                    if (slot < 0) return false;
                    _equippedAccessories[slot] = item;
                    EquipInternal(item);
                    break;

                default:
                    return false;
            }

            CombatCache.MarkAllDirty();
            OnEquipmentChanged?.Invoke(this, item);
            return true;
        }

        /// <summary>
        /// 卸下装备
        /// </summary>
        public ItemInstance Unequip(EquipmentSlot slot, int accessoryIndex = 0)
        {
            ItemInstance item = null;

            switch (slot)
            {
                case EquipmentSlot.Weapon:
                    item = _equippedWeapon;
                    _equippedWeapon = null;
                    break;
                case EquipmentSlot.Head:
                    item = _equippedHead;
                    _equippedHead = null;
                    break;
                case EquipmentSlot.Body:
                    item = _equippedBody;
                    _equippedBody = null;
                    break;
                case EquipmentSlot.Legs:
                    item = _equippedLegs;
                    _equippedLegs = null;
                    break;
                case EquipmentSlot.Accessory:
                    if (accessoryIndex >= 0 && accessoryIndex < _equippedAccessories.Length)
                    {
                        item = _equippedAccessories[accessoryIndex];
                        _equippedAccessories[accessoryIndex] = null;
                    }
                    break;
            }

            if (item != null)
            {
                UnequipInternal(item);
                CombatCache.MarkAllDirty();
                OnEquipmentChanged?.Invoke(this, null);
            }

            return item;
        }

        private void EquipInternal(ItemInstance item)
        {
            if (item?.Template is IEquippable equippable)
            {
                // 应用基础属性修饰器
                foreach (var mod in equippable.GetStatModifiers())
                {
                    mod.Source = item;
                    stats.AddModifier(mod);
                }

                equippable.OnEquip(this);
            }

            // 应用武器前缀
            if (item?.Template is Weapon && item.PrefixId > 0)
            {
                ApplyWeaponPrefix(item);
            }
        }

        private void UnequipInternal(ItemInstance item)
        {
            if (item == null) return;

            stats.RemoveModifiersFromSource(item);

            if (item.Template is IEquippable equippable)
            {
                equippable.OnUnequip(this);
            }
        }

        private void ApplyWeaponPrefix(ItemInstance weaponInstance)
        {
            var prefix = PrefixDatabase.Instance?.GetPrefix(weaponInstance.PrefixId);
            if (prefix == null) return;

            // 前缀伤害加成作为百分比乘数
            if (prefix.DamageModifier != 0)
            {
                stats.AddModifier(new StatModifier(
                    StatType.PhysicalAttack,
                    ModifierType.PercentMult,
                    prefix.DamageModifier,
                    weaponInstance
                ));
            }

            // 暴击加成
            if (prefix.CriticalModifier != 0)
            {
                stats.AddModifier(new StatModifier(
                    StatType.CriticalRate,
                    ModifierType.Flat,
                    prefix.CriticalModifier,
                    weaponInstance
                ));
            }

            // 速度加成
            if (prefix.SpeedModifier != 0)
            {
                stats.AddModifier(new StatModifier(
                    StatType.Speed,
                    ModifierType.PercentMult,
                    prefix.SpeedModifier,
                    weaponInstance
                ));
            }
        }

        #endregion

        #region 伤害与治疗

        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(DamageInfo damageInfo)
        {
            if (IsDowned) return;

            int finalDamage = damageInfo.FinalDamage;
            currentHealth -= finalDamage;

            // 增加压力
            currentStress += Mathf.RoundToInt(finalDamage * 0.1f);

            OnDamageTaken?.Invoke(this, finalDamage, damageInfo);

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                OnDowned?.Invoke(this);
            }
        }

        /// <summary>
        /// 治疗
        /// </summary>
        public void Heal(int amount)
        {
            if (IsDowned) return;

            int oldHealth = currentHealth;
            currentHealth = Mathf.Min(currentHealth + amount, Stats.MaxHealth);
            int actualHeal = currentHealth - oldHealth;

            if (actualHeal > 0)
            {
                OnHealed?.Invoke(this, actualHeal);
            }
        }

        /// <summary>
        /// 复活
        /// </summary>
        public void Revive(int healthAmount, float healthPercent = 0f)
        {
            if (!IsDowned) return;

            int healAmount = healthAmount + Mathf.RoundToInt(Stats.MaxHealth * healthPercent);
            currentHealth = Mathf.Max(1, healAmount);

            OnRevived?.Invoke(this);
        }

        /// <summary>
        /// 恢复技能点
        /// </summary>
        public void RestoreSkillPoints(int physical, int magic)
        {
            currentPhysicalSP = Mathf.Min(currentPhysicalSP + physical, Stats.MaxPhysicalSP);
            currentMagicSP = Mathf.Min(currentMagicSP + magic, Stats.MaxMagicSP);
        }

        /// <summary>
        /// 恢复技能点（ICharacter接口，平均分配）
        /// </summary>
        void ICharacter.RestoreSkillPoints(int amount)
        {
            RestoreSkillPoints(amount / 2, amount - amount / 2);
        }

        /// <summary>
        /// 减少压力
        /// </summary>
        public void ReduceStress(int amount)
        {
            currentStress = Mathf.Max(0, currentStress - amount);
        }

        /// <summary>
        /// 减少疲劳
        /// </summary>
        public void ReduceFatigue(int amount)
        {
            currentFatigue = Mathf.Max(0, currentFatigue - amount);
        }

        #endregion

        #region 状态效果

        /// <summary>
        /// 添加状态效果
        /// </summary>
        public void AddStatusEffect(StatusEffectData effectData, Character source, float duration)
        {
            // 检查是否已存在相同效果
            var existing = _statusEffects.Find(e => e.EffectData == effectData);

            if (existing != null)
            {
                // 刷新持续时间
                existing.RefreshDuration(duration);
                return;
            }

            var instance = new StatusEffectInstance(effectData, this, source, duration);
            _statusEffects.Add(instance);
            instance.OnApply();

            OnStatusEffectAdded?.Invoke(this, instance);
        }

        /// <summary>
        /// 移除状态效果
        /// </summary>
        public void RemoveStatusEffect(StatusEffectType effectType)
        {
            var toRemove = _statusEffects.FindAll(e => e.EffectData.EffectType == effectType);

            foreach (var effect in toRemove)
            {
                effect.OnRemove();
                _statusEffects.Remove(effect);
                OnStatusEffectRemoved?.Invoke(this, effect);
            }
        }

        /// <summary>
        /// 更新状态效果
        /// </summary>
        public void UpdateStatusEffects(float deltaTime)
        {
            for (int i = _statusEffects.Count - 1; i >= 0; i--)
            {
                var effect = _statusEffects[i];
                effect.Update(deltaTime);

                if (effect.IsExpired)
                {
                    effect.OnRemove();
                    _statusEffects.RemoveAt(i);
                    OnStatusEffectRemoved?.Invoke(this, effect);
                }
            }
        }

        /// <summary>
        /// 检查是否有指定状态
        /// </summary>
        public bool HasStatusEffect(StatusEffectType effectType)
        {
            return _statusEffects.Exists(e => e.EffectData.EffectType == effectType);
        }

        #endregion

        #region ATB系统

        /// <summary>
        /// 更新ATB条
        /// </summary>
        public void UpdateATB(float deltaTime)
        {
            if (IsDowned) return;

            // ATB增长速度基于角色速度
            float atbGain = Stats.Speed * deltaTime * 0.5f;

            // 压力和疲劳影响ATB增长
            if (IsStressed)
                atbGain *= 0.8f;
            if (IsFatigued)
                atbGain *= 0.9f;

            currentATB += atbGain;

            if (currentATB >= MaxATB)
            {
                currentATB = MaxATB;
                OnATBFull?.Invoke(this);
            }
        }

        /// <summary>
        /// 消耗ATB
        /// </summary>
        public bool ConsumeATB(float amount)
        {
            if (currentATB < amount) return false;

            currentATB -= amount;
            return true;
        }

        /// <summary>
        /// 重置ATB（行动后）
        /// </summary>
        public void ResetATB()
        {
            currentATB = 0;

            // 行动后增加疲劳
            currentFatigue += 2;
        }

        #endregion

        #region 物品系统桥接 (ICharacter 接口实现)

        // IPassiveManager 和 IVisualManager 适配器
        private PassiveManagerAdapter _passiveAdapter;
        private VisualManagerAdapter _visualAdapter;

        public PassiveManager PassiveManager { get; } = new();
        public VisualManager VisualManager { get; } = new();

        IPassiveManager ICharacter.PassiveManager => _passiveAdapter ??= new PassiveManagerAdapter(PassiveManager);
        IVisualManager ICharacter.VisualManager => _visualAdapter ??= new VisualManagerAdapter(VisualManager);

        // ICharacter.AddStatusEffect 实现
        void ICharacter.AddStatusEffect(IStatusEffect effect)
        {
            if (effect is StatusEffectInstance instance)
            {
                // 如果是我们自己的类型，直接添加
                _statusEffects.Add(instance);
                instance.OnApply();
                OnStatusEffectAdded?.Invoke(this, instance);
            }
            // 其他 IStatusEffect 实现可以通过包装器处理
        }

        #endregion
    }

    #region 接口适配器

    /// <summary>
    /// CharacterStats 到 ICharacterStats 的适配器
    /// </summary>
    public class CharacterStatsAdapter : ICharacterStats
    {
        private readonly CharacterStats _stats;

        public CharacterStatsAdapter(CharacterStats stats)
        {
            _stats = stats;
        }

        public void AddModifier(StatModifier modifier) => _stats.AddModifier(modifier);
        public void RemoveModifier(StatModifier modifier) => _stats.RemoveModifier(modifier);
        public void RemoveModifiersFromSource(object source) => _stats.RemoveModifiersFromSource(source);
    }

    /// <summary>
    /// CombatAttributeCache 到 ICombatStats 的适配器
    /// </summary>
    public class CombatStatsAdapter : ICombatStats
    {
        private readonly Character _character;

        public CombatStatsAdapter(Character character)
        {
            _character = character;
        }

        public void AddDamageMultiplier(float value) => _character.CombatCache.AddDamageMultiplier(value);
        public void AddElementalDamageBonus(ElementType element, float value) => _character.CombatCache.AddElementalDamageBonus(element, value);
        public void AddCriticalChance(float value) => _character.Stats.AddModifier(new StatModifier(StatType.CriticalRate, ModifierType.Flat, value));
        public void AddTemporaryBonus(BuffType type, float value) => _character.CombatCache.AddTemporaryBuff(type, value);
        public void RemoveTemporaryBonus(BuffType type, float value) => _character.CombatCache.RemoveTemporaryBuff(type, value);
    }

    /// <summary>
    /// PassiveManager 到 IPassiveManager 的适配器
    /// </summary>
    public class PassiveManagerAdapter : IPassiveManager
    {
        private readonly PassiveManager _manager;

        public PassiveManagerAdapter(PassiveManager manager)
        {
            _manager = manager;
        }

        public void Register(object skill)
        {
            if (skill is PassiveSkillData psd)
                _manager.Register(psd);
        }

        public void Unregister(object skill)
        {
            if (skill is PassiveSkillData psd)
                _manager.Unregister(psd);
        }
    }

    /// <summary>
    /// VisualManager 到 IVisualManager 的适配器
    /// </summary>
    public class VisualManagerAdapter : IVisualManager
    {
        private readonly VisualManager _manager;

        public VisualManagerAdapter(VisualManager manager)
        {
            _manager = manager;
        }

        public void SetCosmetic(ItemSystem.Cosmetics.CosmeticSlot slot, ItemSystem.Cosmetics.Cosmetic cosmetic)
            => _manager.SetCosmetic(slot, cosmetic);

        public void RemoveCosmetic(ItemSystem.Cosmetics.CosmeticSlot slot)
            => _manager.RemoveCosmetic(slot);
    }

    #endregion

    #region 辅助类型

    public enum CharacterType
    {
        Player,
        Ally,
        Enemy,
        Boss
    }

    public enum EquipmentSlot
    {
        Weapon,
        Head,
        Body,
        Legs,
        Accessory
    }

    /// <summary>
    /// 被动技能管理器
    /// </summary>
    public class PassiveManager
    {
        private readonly List<PassiveSkillData> _passives = new();

        public void Register(PassiveSkillData skill)
        {
            if (skill != null && !_passives.Contains(skill))
                _passives.Add(skill);
        }

        public void Unregister(PassiveSkillData skill)
        {
            _passives.Remove(skill);
        }

        public bool HasPassive(PassiveSkillData skill) => _passives.Contains(skill);
    }

    /// <summary>
    /// 视觉管理器（服装系统）
    /// </summary>
    public class VisualManager
    {
        private readonly Dictionary<ItemSystem.Cosmetics.CosmeticSlot, ItemSystem.Cosmetics.Cosmetic> _cosmetics = new();

        public void SetCosmetic(ItemSystem.Cosmetics.CosmeticSlot slot, ItemSystem.Cosmetics.Cosmetic cosmetic)
        {
            _cosmetics[slot] = cosmetic;
        }

        public void RemoveCosmetic(ItemSystem.Cosmetics.CosmeticSlot slot)
        {
            _cosmetics.Remove(slot);
        }
    }

    /// <summary>
    /// 战斗属性访问器（物品系统兼容层）
    /// </summary>
    public class CombatStats
    {
        private readonly Character _character;

        public CombatStats(Character character)
        {
            _character = character;
        }

        public void AddDamageMultiplier(float value)
        {
            _character.CombatCache.AddDamageMultiplier(value);
        }

        public void AddElementalDamageBonus(ElementType element, float value)
        {
            _character.CombatCache.AddElementalDamageBonus(element, value);
        }

        public void AddCriticalChance(float value)
        {
            _character.Stats.AddModifier(new StatModifier(
                StatType.CriticalRate, ModifierType.Flat, value
            ));
        }

        public void AddTemporaryBonus(BuffType type, float value)
        {
            _character.CombatCache.AddTemporaryBuff(type, value);
        }

        public void RemoveTemporaryBonus(BuffType type, float value)
        {
            _character.CombatCache.RemoveTemporaryBuff(type, value);
        }
    }

    #endregion
}
