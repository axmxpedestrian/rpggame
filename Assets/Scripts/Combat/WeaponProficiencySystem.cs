using System;
using System.Collections.Generic;
using UnityEngine;

namespace CombatSystem
{
    using ItemSystem.Core;
    using ItemSystem.Equipment;
    
    /// <summary>
    /// 武器熟练度系统
    /// 每个角色对不同武器类型有不同的熟练度
    /// 熟练度影响伤害、暴击率、特殊技能解锁
    /// </summary>
    [Serializable]
    public class WeaponProficiencySystem
    {
        [SerializeField] 
        private List<WeaponProficiencyData> proficiencies = new();
        
        // 快速查找
        private Dictionary<WeaponCategory, WeaponProficiencyData> _proficiencyLookup;
        
        public event Action<WeaponCategory, int, int> OnProficiencyLevelUp;
        public event Action<WeaponCategory, int> OnExperienceGained;
        
        #region 初始化
        
        public void Initialize()
        {
            _proficiencyLookup = new Dictionary<WeaponCategory, WeaponProficiencyData>();
            
            // 确保所有武器类型都有熟练度数据
            foreach (WeaponCategory category in Enum.GetValues(typeof(WeaponCategory)))
            {
                var existing = proficiencies.Find(p => p.Category == category);
                if (existing != null)
                {
                    _proficiencyLookup[category] = existing;
                }
                else
                {
                    var newData = new WeaponProficiencyData { Category = category };
                    proficiencies.Add(newData);
                    _proficiencyLookup[category] = newData;
                }
            }
        }
        
        /// <summary>
        /// 从配置初始化（设置初始熟练度）
        /// </summary>
        public void InitializeFromConfig(WeaponProficiencyConfig config)
        {
            Initialize();
            
            if (config?.initialProficiencies == null) return;
            
            foreach (var initial in config.initialProficiencies)
            {
                if (_proficiencyLookup.TryGetValue(initial.category, out var data))
                {
                    data.Level = initial.level;
                    data.CurrentExp = 0;
                }
            }
        }
        
        #endregion
        
        #region 熟练度查询
        
        /// <summary>
        /// 获取武器类型的熟练度等级
        /// </summary>
        public int GetProficiencyLevel(WeaponCategory category)
        {
            EnsureLookup();
            return _proficiencyLookup.TryGetValue(category, out var data) ? data.Level : 0;
        }
        
        /// <summary>
        /// 获取熟练度数据
        /// </summary>
        public WeaponProficiencyData GetProficiencyData(WeaponCategory category)
        {
            EnsureLookup();
            return _proficiencyLookup.TryGetValue(category, out var data) ? data : null;
        }
        
        /// <summary>
        /// 获取熟练度等级名称
        /// </summary>
        public string GetProficiencyRankName(WeaponCategory category)
        {
            int level = GetProficiencyLevel(category);
            return GetRankName(level);
        }
        
        public static string GetRankName(int level)
        {
            return level switch
            {
                0 => "生疏",
                1 => "入门",
                2 => "熟练",
                3 => "精通",
                4 => "大师",
                >= 5 => "宗师",
                _ => "未知"
            };
        }
        
        #endregion
        
        #region 熟练度加成
        
        /// <summary>
        /// 获取伤害加成（百分比）
        /// </summary>
        public float GetDamageBonus(WeaponCategory category)
        {
            int level = GetProficiencyLevel(category);
            // 每级 +5% 伤害
            return level * 0.05f;
        }
        
        /// <summary>
        /// 获取暴击率加成（绝对值）
        /// </summary>
        public float GetCriticalBonus(WeaponCategory category)
        {
            int level = GetProficiencyLevel(category);
            // 每级 +2% 暴击率
            return level * 0.02f;
        }
        
        /// <summary>
        /// 获取攻击速度加成（百分比）
        /// </summary>
        public float GetSpeedBonus(WeaponCategory category)
        {
            int level = GetProficiencyLevel(category);
            // 每级 +3% 攻击速度
            return level * 0.03f;
        }
        
        /// <summary>
        /// 获取技能点消耗减少（绝对值）
        /// </summary>
        public int GetSkillCostReduction(WeaponCategory category)
        {
            int level = GetProficiencyLevel(category);
            // 3级开始每级减少1点消耗
            return Mathf.Max(0, level - 2);
        }
        
        /// <summary>
        /// 应用熟练度加成到伤害计算
        /// </summary>
        public DamageModifiers GetDamageModifiers(WeaponCategory category)
        {
            return new DamageModifiers
            {
                DamageMultiplier = 1f + GetDamageBonus(category),
                CriticalRateBonus = GetCriticalBonus(category),
                SpeedMultiplier = 1f + GetSpeedBonus(category)
            };
        }
        
        #endregion
        
        #region 经验获取
        
        /// <summary>
        /// 使用武器攻击后获得经验
        /// </summary>
        public void GainExperience(WeaponCategory category, int exp)
        {
            EnsureLookup();
            
            if (!_proficiencyLookup.TryGetValue(category, out var data))
                return;
            
            int oldLevel = data.Level;
            data.CurrentExp += exp;
            
            OnExperienceGained?.Invoke(category, exp);
            
            // 检查升级
            while (data.CurrentExp >= GetExpForNextLevel(data.Level) && data.Level < MaxLevel)
            {
                data.CurrentExp -= GetExpForNextLevel(data.Level);
                data.Level++;
                OnProficiencyLevelUp?.Invoke(category, oldLevel, data.Level);
            }
        }
        
        /// <summary>
        /// 战斗中使用武器自动获得经验
        /// </summary>
        public void OnWeaponUsed(WeaponCategory category, bool isSkill = false, bool hitTarget = true)
        {
            int baseExp = hitTarget ? 2 : 1;
            if (isSkill) baseExp += 1;
            
            GainExperience(category, baseExp);
        }
        
        /// <summary>
        /// 击杀敌人获得额外经验
        /// </summary>
        public void OnEnemyKilled(WeaponCategory category, int enemyLevel)
        {
            int bonusExp = 5 + enemyLevel;
            GainExperience(category, bonusExp);
        }
        
        #endregion
        
        #region 经验计算
        
        public const int MaxLevel = 10;
        
        /// <summary>
        /// 获取升到下一级需要的经验
        /// </summary>
        public static int GetExpForNextLevel(int currentLevel)
        {
            // 经验曲线：50, 100, 200, 350, 550, 800, 1100, 1450, 1850, 2300
            return 50 + currentLevel * 50 + (currentLevel * currentLevel * 25);
        }
        
        /// <summary>
        /// 获取当前等级的经验进度（0-1）
        /// </summary>
        public float GetLevelProgress(WeaponCategory category)
        {
            var data = GetProficiencyData(category);
            if (data == null || data.Level >= MaxLevel) return 1f;
            
            int required = GetExpForNextLevel(data.Level);
            return (float)data.CurrentExp / required;
        }
        
        #endregion
        
        #region 技能解锁检查
        
        /// <summary>
        /// 检查是否解锁了武器技能
        /// </summary>
        public bool IsSkillUnlocked(WeaponCategory category, int requiredLevel)
        {
            return GetProficiencyLevel(category) >= requiredLevel;
        }
        
        /// <summary>
        /// 获取已解锁的技能槽位数
        /// </summary>
        public int GetUnlockedSkillSlots(WeaponCategory category)
        {
            int level = GetProficiencyLevel(category);
            // 1级解锁1个，3级解锁2个，5级解锁3个
            if (level >= 5) return 3;
            if (level >= 3) return 2;
            if (level >= 1) return 1;
            return 0;
        }
        
        #endregion
        
        private void EnsureLookup()
        {
            if (_proficiencyLookup == null)
                Initialize();
        }
    }
    
    /// <summary>
    /// 单个武器类型的熟练度数据
    /// </summary>
    [Serializable]
    public class WeaponProficiencyData
    {
        public WeaponCategory Category;
        public int Level;
        public int CurrentExp;
        public int TotalExpGained;  // 历史总经验（用于统计）
    }
    
    /// <summary>
    /// 熟练度加成结构
    /// </summary>
    public struct DamageModifiers
    {
        public float DamageMultiplier;
        public float CriticalRateBonus;
        public float SpeedMultiplier;
    }
    
    /// <summary>
    /// 武器熟练度配置 - ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewProficiencyConfig", menuName = "CombatSystem/WeaponProficiencyConfig")]
    public class WeaponProficiencyConfig : ScriptableObject
    {
        [Header("初始熟练度")]
        public InitialProficiency[] initialProficiencies;
        
        [Header("成长配置")]
        public float expMultiplier = 1f;  // 经验倍率
        public bool enablePassiveGain = true;  // 是否允许被动获得经验
    }
    
    [Serializable]
    public struct InitialProficiency
    {
        public WeaponCategory category;
        public int level;
    }
}
