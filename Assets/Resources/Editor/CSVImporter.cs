using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameData.Import
{
    using ItemSystem.Core;
    using CombatSystem;
    
    /// <summary>
    /// CSV导入器 - 批量从CSV文件创建ScriptableObject实例
    /// 
    /// 使用方法：
    /// 1. 在Unity编辑器菜单中选择 Tools > CSV Import > Import Characters/Enemies/Skills
    /// 2. 选择对应的CSV文件
    /// 3. 系统会自动创建或更新ScriptableObject资源
    /// 
    /// CSV格式要求：
    /// - 第一行为列标题（字段名）
    /// - 使用逗号分隔
    /// - 字符串包含逗号时需用双引号包围
    /// - 支持注释行（以#开头）
    /// </summary>
    public static class CSVImporter
    {
        #region CSV解析核心
        
        /// <summary>
        /// 解析CSV文件为字典列表
        /// 每个字典代表一行数据，键为列标题
        /// </summary>
        public static List<Dictionary<string, string>> ParseCSV(string csvContent)
        {
            var result = new List<Dictionary<string, string>>();
            var lines = csvContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            if (lines.Length < 2) return result; // 至少需要标题行和一行数据
            
            // 解析标题行
            var headers = ParseCSVLine(lines[0]);
            
            // 解析数据行
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // 跳过空行和注释行
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;
                
                var values = ParseCSVLine(line);
                var row = new Dictionary<string, string>();
                
                for (int j = 0; j < headers.Count && j < values.Count; j++)
                {
                    row[headers[j].Trim()] = values[j].Trim();
                }
                
                result.Add(row);
            }
            
            return result;
        }
        
        /// <summary>
        /// 解析单行CSV，正确处理引号内的逗号
        /// </summary>
        private static List<string> ParseCSVLine(string line)
        {
            var result = new List<string>();
            var regex = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");
            var values = regex.Split(line);
            
            foreach (var value in values)
            {
                // 移除首尾的引号
                var trimmed = value.Trim();
                if (trimmed.StartsWith("\"") && trimmed.EndsWith("\""))
                {
                    trimmed = trimmed.Substring(1, trimmed.Length - 2);
                }
                result.Add(trimmed);
            }
            
            return result;
        }
        
        /// <summary>
        /// 安全获取字典值，支持默认值
        /// </summary>
        public static T GetValue<T>(Dictionary<string, string> row, string key, T defaultValue = default)
        {
            if (!row.TryGetValue(key, out var stringValue) || string.IsNullOrEmpty(stringValue))
                return defaultValue;
            
            try
            {
                var targetType = typeof(T);
                
                // 处理可空类型
                if (Nullable.GetUnderlyingType(targetType) != null)
                    targetType = Nullable.GetUnderlyingType(targetType);
                
                // 处理枚举类型
                if (targetType.IsEnum)
                    return (T)Enum.Parse(targetType, stringValue, ignoreCase: true);
                
                // 处理布尔类型（支持多种格式）
                if (targetType == typeof(bool))
                {
                    var lower = stringValue.ToLower();
                    return (T)(object)(lower == "true" || lower == "1" || lower == "yes" || lower == "是");
                }
                
                // 处理数组类型（用分号分隔）
                if (targetType.IsArray)
                {
                    var elementType = targetType.GetElementType();
                    var items = stringValue.Split(';').Where(s => !string.IsNullOrEmpty(s)).ToArray();
                    var array = Array.CreateInstance(elementType, items.Length);
                    
                    for (int i = 0; i < items.Length; i++)
                    {
                        if (elementType.IsEnum)
                            array.SetValue(Enum.Parse(elementType, items[i].Trim(), true), i);
                        else
                            array.SetValue(Convert.ChangeType(items[i].Trim(), elementType), i);
                    }
                    
                    return (T)(object)array;
                }
                
                // 通用类型转换
                return (T)Convert.ChangeType(stringValue, targetType);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"转换失败: 键={key}, 值={stringValue}, 类型={typeof(T).Name}, 错误={e.Message}");
                return defaultValue;
            }
        }
        
        #endregion
    }
    
    #region 角色数据定义
    
    /// <summary>
    /// 角色数据配置 - ScriptableObject
    /// 这是CSV导入的目标类型
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterData", menuName = "GameData/CharacterData")]
    public class CharacterData : ScriptableObject
    {
        [Header("基础信息")]
        public string characterId;          // 唯一标识符
        public string characterName;        // 显示名称
        public string description;          // 角色描述
        public CharacterType characterType; // 角色类型
        public int baseLevel = 1;           // 初始等级
        
        [Header("基础属性（6维）")]
        public int constitution = 10;   // 体质 - 生命、物理格挡
        public int strength = 10;       // 力量 - 物攻、负重
        public int perception = 10;     // 感知 - 命中、暴击
        public int reaction = 10;       // 反应 - 速度、闪避
        public int wisdom = 10;         // 智慧 - 魔攻、魔防
        public int luck = 10;           // 幸运 - 暴击、掉落
        
        [Header("成长率（每级增加）")]
        public float constitutionGrowth = 1.0f;
        public float strengthGrowth = 1.0f;
        public float perceptionGrowth = 1.0f;
        public float reactionGrowth = 1.0f;
        public float wisdomGrowth = 1.0f;
        public float luckGrowth = 0.5f;
        
        [Header("战斗设置")]
        public float baseATBSpeed = 100f;   // 基础ATB速度
        public int basePhysicalSP = 5;      // 基础物理技能点
        public int baseMagicSP = 5;         // 基础魔法技能点
        
        [Header("元素抗性（-1.0到1.0，负数为弱点）")]
        public float fireResistance = 0f;
        public float iceResistance = 0f;
        public float lightningResistance = 0f;
        public float poisonResistance = 0f;
        public float holyResistance = 0f;
        public float darkResistance = 0f;
        
        [Header("初始装备/技能（ID列表）")]
        public string[] defaultWeaponIds;   // 默认武器ID
        public string[] defaultArmorIds;    // 默认护甲ID
        public string[] defaultSkillIds;    // 默认技能ID
        
        [Header("视觉资源")]
        public string spriteAtlasPath;      // 精灵图集路径
        public string portraitPath;         // 头像路径
        public string animatorPath;         // 动画控制器路径
        
        [Header("音效")]
        public string voicePackId;          // 语音包ID
        public string attackSoundId;        // 攻击音效ID
        public string hurtSoundId;          // 受伤音效ID
        
        /// <summary>
        /// 获取指定等级的属性值
        /// </summary>
        public int GetStatAtLevel(StatType statType, int level)
        {
            int baseValue = statType switch
            {
                StatType.Constitution => constitution,
                StatType.Strength => strength,
                StatType.Perception => perception,
                StatType.Reaction => reaction,
                StatType.Wisdom => wisdom,
                StatType.Luck => luck,
                _ => 10
            };
            
            float growth = statType switch
            {
                StatType.Constitution => constitutionGrowth,
                StatType.Strength => strengthGrowth,
                StatType.Perception => perceptionGrowth,
                StatType.Reaction => reactionGrowth,
                StatType.Wisdom => wisdomGrowth,
                StatType.Luck => luckGrowth,
                _ => 1f
            };
            
            return Mathf.RoundToInt(baseValue + growth * (level - 1));
        }
    }
    
    /// <summary>
    /// 角色类型
    /// </summary>
    public enum CharacterType
    {
        Player,         // 玩家角色
        Companion,      // 同伴NPC
        Enemy,          // 普通敌人
        Elite,          // 精英敌人
        Boss,           // Boss
        Summon          // 召唤物
    }
    
    #endregion
    
    #region 敌人数据定义
    
    /// <summary>
    /// 敌人数据配置 - 继承自角色数据，增加敌人特有属性
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "GameData/EnemyData")]
    public class EnemyData : CharacterData
    {
        [Header("敌人特有属性")]
        public EnemyCategory category;      // 敌人分类
        public int threatLevel = 1;         // 威胁等级 (1-10)
        public bool isBoss = false;         // 是否为Boss
        
        [Header("战斗AI")]
        public AIBehaviorType aiBehavior;   // AI行为模式
        public float aggroRange = 5f;       // 仇恨范围
        public float deaggroRange = 15f;    // 脱离仇恨范围
        public string[] priorityTargets;    // 优先攻击目标类型
        
        [Header("掉落设置")]
        public int baseExpReward = 10;      // 基础经验奖励
        public int baseGoldReward = 5;      // 基础金币奖励
        public LootEntry[] lootTable;       // 掉落表
        
        [Header("生成设置")]
        public string[] spawnLocations;     // 可出现的地点ID
        public int minSpawnLevel = 1;       // 最小生成等级
        public int maxSpawnLevel = 99;      // 最大生成等级
        public float spawnWeight = 1f;      // 生成权重
        
        [Header("特殊机制")]
        public bool hasPhases = false;          // 是否有阶段战
        public int phaseCount = 1;              // 阶段数量
        public float[] phaseThresholds;         // 阶段血量阈值
        public string[] phaseSkillSets;         // 各阶段技能组ID
    }
    
    /// <summary>
    /// 敌人分类
    /// </summary>
    public enum EnemyCategory
    {
        Beast,          // 野兽
        Humanoid,       // 人形
        Undead,         // 亡灵
        Demon,          // 恶魔
        Elemental,      // 元素
        Mechanical,     // 机械
        Dragon,         // 龙类
        Aberration      // 异形
    }
    
    /// <summary>
    /// AI行为类型
    /// </summary>
    public enum AIBehaviorType
    {
        Aggressive,     // 主动攻击型
        Defensive,      // 防御型
        Support,        // 辅助型
        Balanced,       // 平衡型
        Berserker,      // 狂暴型（低血量时增强）
        Tactical,       // 战术型（会根据情况选择技能）
        Coward          // 胆小型（低血量时逃跑）
    }
    
    /// <summary>
    /// 掉落条目
    /// </summary>
    [Serializable]
    public class LootEntry
    {
        public string itemId;           // 物品ID
        public float dropRate;          // 掉落概率 (0-1)
        public int minCount = 1;        // 最小数量
        public int maxCount = 1;        // 最大数量
        public bool guaranteedOnBoss;   // Boss战是否必掉
    }
    
    #endregion
    
    #region 技能数据定义
    
    /// <summary>
    /// 技能数据配置 - 用于CSV导入的数值部分
    /// 技能的具体效果逻辑由SkillEffect类实现
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkillData", menuName = "GameData/SkillData")]
    public class SkillDataConfig : ScriptableObject
    {
        [Header("基础信息")]
        public string skillId;              // 唯一标识符
        public string skillName;            // 技能名称
        public string description;          // 技能描述
        public SkillCategory category;      // 技能分类
        public SkillType skillType;         // 技能类型
        
        [Header("消耗与冷却")]
        public int spCost = 1;              // 技能点消耗
        public int mpCost = 0;              // 魔法值消耗（如果有）
        public int hpCost = 0;              // 生命值消耗（如果有）
        public int atbCost = 100;           // ATB消耗
        public int cooldown = 0;            // 冷却回合数
        
        [Header("伤害/效果数值")]
        public float basePower = 100f;      // 基础威力
        public float powerScaling = 1.0f;   // 属性加成系数
        public ScalingAttribute scalingAttribute; // 加成属性
        public DamageCategory damageType;   // 伤害类型
        public ElementType element;         // 元素类型
        
        [Header("目标设置")]
        public TargetType targetType;       // 目标类型
        public int maxTargets = 1;          // 最大目标数
        public float range = 1f;            // 射程
        public float aoeRadius = 0f;        // AOE半径（0为单体）
        
        [Header("命中与暴击")]
        public float accuracyModifier = 1.0f;   // 命中修正
        public float critRateBonus = 0f;        // 额外暴击率
        public float critDamageBonus = 0f;      // 额外暴击伤害
        
        [Header("附加效果")]
        public StatusEffectType[] applyEffects;     // 附加状态效果
        public float[] effectChances;               // 效果触发概率
        public float[] effectDurations;             // 效果持续时间
        
        [Header("连击/追加攻击")]
        public int hitCount = 1;            // 攻击次数
        public float hitInterval = 0.1f;    // 攻击间隔
        public string[] comboSkillIds;      // 连携技能ID
        
        [Header("解锁条件")]
        public int requiredLevel = 1;           // 需求等级
        public string requiredWeaponType;       // 需求武器类型
        public string[] prerequisiteSkillIds;   // 前置技能ID
        
        [Header("资源引用（路径）")]
        public string iconPath;             // 图标路径
        public string animationClipPath;    // 动画片段路径
        public string vfxPrefabPath;        // 特效预制体路径
        public string sfxPath;              // 音效路径
        
        /// <summary>
        /// 计算技能最终威力
        /// </summary>
        public float CalculatePower(int attributeValue)
        {
            return basePower + (attributeValue * powerScaling);
        }
    }
    
    /// <summary>
    /// 技能分类
    /// </summary>
    public enum SkillCategory
    {
        WeaponSkill,    // 武器技能
        MagicSkill,     // 魔法技能
        PhysicalSkill,  // 物理技能
        SupportSkill,   // 辅助技能
        PassiveSkill,   // 被动技能
        UltimateSkill   // 终极技能
    }
    
    /// <summary>
    /// 技能类型
    /// </summary>
    public enum SkillType
    {
        Active,         // 主动技能
        Passive,        // 被动技能
        Toggle,         // 切换技能
        Reaction        // 反应技能
    }
    
    /// <summary>
    /// 属性加成来源
    /// </summary>
    public enum ScalingAttribute
    {
        Strength,       // 力量
        Wisdom,         // 智慧
        Perception,     // 感知
        Constitution,   // 体质
        Reaction,       // 反应
        Luck,           // 幸运
        WeaponDamage,   // 武器伤害
        MaxHP,          // 最大生命值
        CurrentHP,      // 当前生命值
        MissingHP       // 损失生命值
    }
    
    #endregion
    
    #region 武器技能数据定义
    
    /// <summary>
    /// 武器技能数据 - 与特定武器类型绑定的技能
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeaponSkillData", menuName = "GameData/WeaponSkillData")]
    public class WeaponSkillData : SkillDataConfig
    {
        [Header("武器技能特有属性")]
        public WeaponCategory requiredWeaponCategory;   // 需要的武器类型
        public int proficiencyRequired = 0;             // 需要的熟练度等级
        public int proficiencyGainOnUse = 1;            // 使用时获得的熟练度
        
        [Header("武器特殊效果")]
        public bool consumesDurability = true;          // 是否消耗耐久
        public int durabilityConsumption = 1;           // 耐久消耗量
        public bool canTriggerWeaponEffect = true;      // 是否触发武器特效
        
        [Header("连击设置")]
        public bool isComboStarter = false;             // 是否为连击起手
        public bool isComboFinisher = false;            // 是否为连击终结
        public string[] validComboPredecessors;         // 有效的前置连击
    }
    
    #endregion

#if UNITY_EDITOR
    
    #region 编辑器导入工具
    
    /// <summary>
    /// CSV导入编辑器窗口
    /// </summary>
    public class CSVImportWindow : EditorWindow
    {
        private enum ImportType
        {
            Characters,
            Enemies,
            Skills,
            WeaponSkills
        }
        
        private ImportType _importType = ImportType.Characters;
        private string _csvPath = "";
        private string _outputFolder = "Assets/Data/Generated";
        private bool _overwriteExisting = true;
        private Vector2 _scrollPosition;
        private string _previewText = "";
        
        [MenuItem("Tools/CSV Import/Import Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<CSVImportWindow>("CSV导入工具");
            window.minSize = new Vector2(500, 400);
        }
        
        [MenuItem("Tools/CSV Import/Generate CSV Templates")]
        public static void GenerateTemplates()
        {
            var templateFolder = "Assets/Data/CSVTemplates";
            if (!Directory.Exists(templateFolder))
                Directory.CreateDirectory(templateFolder);
            
            // 生成角色模板
            GenerateCharacterTemplate(Path.Combine(templateFolder, "CharacterTemplate.csv"));
            
            // 生成敌人模板
            GenerateEnemyTemplate(Path.Combine(templateFolder, "EnemyTemplate.csv"));
            
            // 生成技能模板
            GenerateSkillTemplate(Path.Combine(templateFolder, "SkillTemplate.csv"));
            
            // 生成武器技能模板
            GenerateWeaponSkillTemplate(Path.Combine(templateFolder, "WeaponSkillTemplate.csv"));
            
            AssetDatabase.Refresh();
            Debug.Log($"CSV模板已生成到: {templateFolder}");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("CSV数据导入工具", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 导入类型选择
            _importType = (ImportType)EditorGUILayout.EnumPopup("导入类型", _importType);
            
            // CSV文件选择
            EditorGUILayout.BeginHorizontal();
            _csvPath = EditorGUILayout.TextField("CSV文件路径", _csvPath);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFilePanel("选择CSV文件", Application.dataPath, "csv");
                if (!string.IsNullOrEmpty(path))
                {
                    _csvPath = path;
                    PreviewCSV();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 输出文件夹
            EditorGUILayout.BeginHorizontal();
            _outputFolder = EditorGUILayout.TextField("输出文件夹", _outputFolder);
            if (GUILayout.Button("浏览", GUILayout.Width(60)))
            {
                var path = EditorUtility.OpenFolderPanel("选择输出文件夹", Application.dataPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    _outputFolder = "Assets" + path.Replace(Application.dataPath, "");
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 选项
            _overwriteExisting = EditorGUILayout.Toggle("覆盖已存在文件", _overwriteExisting);
            
            EditorGUILayout.Space();
            
            // 预览区域
            EditorGUILayout.LabelField("CSV预览：");
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(150));
            EditorGUILayout.TextArea(_previewText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space();
            
            // 导入按钮
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("生成CSV模板"))
            {
                GenerateTemplates();
            }
            
            GUI.enabled = !string.IsNullOrEmpty(_csvPath) && File.Exists(_csvPath);
            if (GUILayout.Button("开始导入"))
            {
                ImportCSV();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void PreviewCSV()
        {
            if (File.Exists(_csvPath))
            {
                var lines = File.ReadAllLines(_csvPath).Take(10);
                _previewText = string.Join("\n", lines);
                if (File.ReadAllLines(_csvPath).Length > 10)
                    _previewText += "\n... (更多行未显示)";
            }
        }
        
        private void ImportCSV()
        {
            if (!File.Exists(_csvPath))
            {
                EditorUtility.DisplayDialog("错误", "CSV文件不存在", "确定");
                return;
            }
            
            var csvContent = File.ReadAllText(_csvPath);
            var rows = CSVImporter.ParseCSV(csvContent);
            
            if (rows.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "CSV文件为空或格式错误", "确定");
                return;
            }
            
            // 确保输出文件夹存在
            if (!Directory.Exists(_outputFolder))
                Directory.CreateDirectory(_outputFolder);
            
            int successCount = 0;
            int failCount = 0;
            
            foreach (var row in rows)
            {
                try
                {
                    switch (_importType)
                    {
                        case ImportType.Characters:
                            ImportCharacterRow(row);
                            break;
                        case ImportType.Enemies:
                            ImportEnemyRow(row);
                            break;
                        case ImportType.Skills:
                            ImportSkillRow(row);
                            break;
                        case ImportType.WeaponSkills:
                            ImportWeaponSkillRow(row);
                            break;
                    }
                    successCount++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"导入失败: {e.Message}\n行数据: {string.Join(", ", row.Select(kv => $"{kv.Key}={kv.Value}"))}");
                    failCount++;
                }
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("导入完成", 
                $"成功: {successCount}\n失败: {failCount}", "确定");
        }
        
        private void ImportCharacterRow(Dictionary<string, string> row)
        {
            var id = CSVImporter.GetValue<string>(row, "characterId", "");
            if (string.IsNullOrEmpty(id)) return;
            
            var assetPath = $"{_outputFolder}/Characters/{id}.asset";
            var folderPath = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            
            CharacterData data;
            
            if (File.Exists(assetPath) && _overwriteExisting)
            {
                data = AssetDatabase.LoadAssetAtPath<CharacterData>(assetPath);
            }
            else if (File.Exists(assetPath))
            {
                return; // 跳过已存在的
            }
            else
            {
                data = ScriptableObject.CreateInstance<CharacterData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }
            
            // 填充数据
            data.characterId = id;
            data.characterName = CSVImporter.GetValue<string>(row, "characterName", id);
            data.description = CSVImporter.GetValue<string>(row, "description", "");
            data.characterType = CSVImporter.GetValue<CharacterType>(row, "characterType", CharacterType.Player);
            data.baseLevel = CSVImporter.GetValue<int>(row, "baseLevel", 1);
            
            // 基础属性
            data.constitution = CSVImporter.GetValue<int>(row, "constitution", 10);
            data.strength = CSVImporter.GetValue<int>(row, "strength", 10);
            data.perception = CSVImporter.GetValue<int>(row, "perception", 10);
            data.reaction = CSVImporter.GetValue<int>(row, "reaction", 10);
            data.wisdom = CSVImporter.GetValue<int>(row, "wisdom", 10);
            data.luck = CSVImporter.GetValue<int>(row, "luck", 10);
            
            // 成长率
            data.constitutionGrowth = CSVImporter.GetValue<float>(row, "constitutionGrowth", 1f);
            data.strengthGrowth = CSVImporter.GetValue<float>(row, "strengthGrowth", 1f);
            data.perceptionGrowth = CSVImporter.GetValue<float>(row, "perceptionGrowth", 1f);
            data.reactionGrowth = CSVImporter.GetValue<float>(row, "reactionGrowth", 1f);
            data.wisdomGrowth = CSVImporter.GetValue<float>(row, "wisdomGrowth", 1f);
            data.luckGrowth = CSVImporter.GetValue<float>(row, "luckGrowth", 0.5f);
            
            // 战斗设置
            data.baseATBSpeed = CSVImporter.GetValue<float>(row, "baseATBSpeed", 100f);
            data.basePhysicalSP = CSVImporter.GetValue<int>(row, "basePhysicalSP", 5);
            data.baseMagicSP = CSVImporter.GetValue<int>(row, "baseMagicSP", 5);
            
            // 元素抗性
            data.fireResistance = CSVImporter.GetValue<float>(row, "fireResistance", 0f);
            data.iceResistance = CSVImporter.GetValue<float>(row, "iceResistance", 0f);
            data.lightningResistance = CSVImporter.GetValue<float>(row, "lightningResistance", 0f);
            data.poisonResistance = CSVImporter.GetValue<float>(row, "poisonResistance", 0f);
            data.holyResistance = CSVImporter.GetValue<float>(row, "holyResistance", 0f);
            data.darkResistance = CSVImporter.GetValue<float>(row, "darkResistance", 0f);
            
            // 初始装备/技能（分号分隔的ID列表）
            data.defaultWeaponIds = CSVImporter.GetValue<string[]>(row, "defaultWeaponIds", Array.Empty<string>());
            data.defaultArmorIds = CSVImporter.GetValue<string[]>(row, "defaultArmorIds", Array.Empty<string>());
            data.defaultSkillIds = CSVImporter.GetValue<string[]>(row, "defaultSkillIds", Array.Empty<string>());
            
            // 资源路径
            data.spriteAtlasPath = CSVImporter.GetValue<string>(row, "spriteAtlasPath", "");
            data.portraitPath = CSVImporter.GetValue<string>(row, "portraitPath", "");
            data.animatorPath = CSVImporter.GetValue<string>(row, "animatorPath", "");
            
            EditorUtility.SetDirty(data);
        }
        
        private void ImportEnemyRow(Dictionary<string, string> row)
        {
            var id = CSVImporter.GetValue<string>(row, "characterId", "");
            if (string.IsNullOrEmpty(id)) return;
            
            var assetPath = $"{_outputFolder}/Enemies/{id}.asset";
            var folderPath = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            
            EnemyData data;
            
            if (File.Exists(assetPath) && _overwriteExisting)
            {
                data = AssetDatabase.LoadAssetAtPath<EnemyData>(assetPath);
            }
            else if (File.Exists(assetPath))
            {
                return;
            }
            else
            {
                data = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }
            
            // 基础角色数据（复用角色导入逻辑）
            data.characterId = id;
            data.characterName = CSVImporter.GetValue<string>(row, "characterName", id);
            data.description = CSVImporter.GetValue<string>(row, "description", "");
            data.characterType = CharacterType.Enemy;
            data.baseLevel = CSVImporter.GetValue<int>(row, "baseLevel", 1);
            
            // 基础属性
            data.constitution = CSVImporter.GetValue<int>(row, "constitution", 10);
            data.strength = CSVImporter.GetValue<int>(row, "strength", 10);
            data.perception = CSVImporter.GetValue<int>(row, "perception", 10);
            data.reaction = CSVImporter.GetValue<int>(row, "reaction", 10);
            data.wisdom = CSVImporter.GetValue<int>(row, "wisdom", 10);
            data.luck = CSVImporter.GetValue<int>(row, "luck", 10);
            
            // 成长率
            data.constitutionGrowth = CSVImporter.GetValue<float>(row, "constitutionGrowth", 1f);
            data.strengthGrowth = CSVImporter.GetValue<float>(row, "strengthGrowth", 1f);
            data.perceptionGrowth = CSVImporter.GetValue<float>(row, "perceptionGrowth", 1f);
            data.reactionGrowth = CSVImporter.GetValue<float>(row, "reactionGrowth", 1f);
            data.wisdomGrowth = CSVImporter.GetValue<float>(row, "wisdomGrowth", 1f);
            data.luckGrowth = CSVImporter.GetValue<float>(row, "luckGrowth", 0.5f);
            
            // 敌人特有属性
            data.category = CSVImporter.GetValue<EnemyCategory>(row, "category", EnemyCategory.Beast);
            data.threatLevel = CSVImporter.GetValue<int>(row, "threatLevel", 1);
            data.isBoss = CSVImporter.GetValue<bool>(row, "isBoss", false);
            
            // AI设置
            data.aiBehavior = CSVImporter.GetValue<AIBehaviorType>(row, "aiBehavior", AIBehaviorType.Balanced);
            data.aggroRange = CSVImporter.GetValue<float>(row, "aggroRange", 5f);
            data.deaggroRange = CSVImporter.GetValue<float>(row, "deaggroRange", 15f);
            
            // 奖励
            data.baseExpReward = CSVImporter.GetValue<int>(row, "baseExpReward", 10);
            data.baseGoldReward = CSVImporter.GetValue<int>(row, "baseGoldReward", 5);
            
            // 生成设置
            data.spawnLocations = CSVImporter.GetValue<string[]>(row, "spawnLocations", Array.Empty<string>());
            data.minSpawnLevel = CSVImporter.GetValue<int>(row, "minSpawnLevel", 1);
            data.maxSpawnLevel = CSVImporter.GetValue<int>(row, "maxSpawnLevel", 99);
            data.spawnWeight = CSVImporter.GetValue<float>(row, "spawnWeight", 1f);
            
            // 技能
            data.defaultSkillIds = CSVImporter.GetValue<string[]>(row, "defaultSkillIds", Array.Empty<string>());
            
            // 元素抗性
            data.fireResistance = CSVImporter.GetValue<float>(row, "fireResistance", 0f);
            data.iceResistance = CSVImporter.GetValue<float>(row, "iceResistance", 0f);
            data.lightningResistance = CSVImporter.GetValue<float>(row, "lightningResistance", 0f);
            data.poisonResistance = CSVImporter.GetValue<float>(row, "poisonResistance", 0f);
            data.holyResistance = CSVImporter.GetValue<float>(row, "holyResistance", 0f);
            data.darkResistance = CSVImporter.GetValue<float>(row, "darkResistance", 0f);
            
            EditorUtility.SetDirty(data);
        }
        
        private void ImportSkillRow(Dictionary<string, string> row)
        {
            var id = CSVImporter.GetValue<string>(row, "skillId", "");
            if (string.IsNullOrEmpty(id)) return;
            
            var assetPath = $"{_outputFolder}/Skills/{id}.asset";
            var folderPath = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            
            SkillDataConfig data;
            
            if (File.Exists(assetPath) && _overwriteExisting)
            {
                data = AssetDatabase.LoadAssetAtPath<SkillDataConfig>(assetPath);
            }
            else if (File.Exists(assetPath))
            {
                return;
            }
            else
            {
                data = ScriptableObject.CreateInstance<SkillDataConfig>();
                AssetDatabase.CreateAsset(data, assetPath);
            }
            
            // 基础信息
            data.skillId = id;
            data.skillName = CSVImporter.GetValue<string>(row, "skillName", id);
            data.description = CSVImporter.GetValue<string>(row, "description", "");
            data.category = CSVImporter.GetValue<SkillCategory>(row, "category", SkillCategory.PhysicalSkill);
            data.skillType = CSVImporter.GetValue<SkillType>(row, "skillType", SkillType.Active);
            
            // 消耗与冷却
            data.spCost = CSVImporter.GetValue<int>(row, "spCost", 1);
            data.mpCost = CSVImporter.GetValue<int>(row, "mpCost", 0);
            data.hpCost = CSVImporter.GetValue<int>(row, "hpCost", 0);
            data.atbCost = CSVImporter.GetValue<int>(row, "atbCost", 100);
            data.cooldown = CSVImporter.GetValue<int>(row, "cooldown", 0);
            
            // 伤害数值
            data.basePower = CSVImporter.GetValue<float>(row, "basePower", 100f);
            data.powerScaling = CSVImporter.GetValue<float>(row, "powerScaling", 1f);
            data.scalingAttribute = CSVImporter.GetValue<ScalingAttribute>(row, "scalingAttribute", ScalingAttribute.Strength);
            data.damageType = CSVImporter.GetValue<DamageCategory>(row, "damageType", DamageCategory.Physical);
            data.element = CSVImporter.GetValue<ElementType>(row, "element", ElementType.None);
            
            // 目标设置
            data.targetType = CSVImporter.GetValue<TargetType>(row, "targetType", TargetType.SingleEnemy);
            data.maxTargets = CSVImporter.GetValue<int>(row, "maxTargets", 1);
            data.range = CSVImporter.GetValue<float>(row, "range", 1f);
            data.aoeRadius = CSVImporter.GetValue<float>(row, "aoeRadius", 0f);
            
            // 命中与暴击
            data.accuracyModifier = CSVImporter.GetValue<float>(row, "accuracyModifier", 1f);
            data.critRateBonus = CSVImporter.GetValue<float>(row, "critRateBonus", 0f);
            data.critDamageBonus = CSVImporter.GetValue<float>(row, "critDamageBonus", 0f);
            
            // 连击
            data.hitCount = CSVImporter.GetValue<int>(row, "hitCount", 1);
            data.hitInterval = CSVImporter.GetValue<float>(row, "hitInterval", 0.1f);
            
            // 解锁条件
            data.requiredLevel = CSVImporter.GetValue<int>(row, "requiredLevel", 1);
            data.prerequisiteSkillIds = CSVImporter.GetValue<string[]>(row, "prerequisiteSkillIds", Array.Empty<string>());
            
            // 资源路径
            data.iconPath = CSVImporter.GetValue<string>(row, "iconPath", "");
            data.animationClipPath = CSVImporter.GetValue<string>(row, "animationClipPath", "");
            data.vfxPrefabPath = CSVImporter.GetValue<string>(row, "vfxPrefabPath", "");
            data.sfxPath = CSVImporter.GetValue<string>(row, "sfxPath", "");
            
            EditorUtility.SetDirty(data);
        }
        
        private void ImportWeaponSkillRow(Dictionary<string, string> row)
        {
            var id = CSVImporter.GetValue<string>(row, "skillId", "");
            if (string.IsNullOrEmpty(id)) return;
            
            var assetPath = $"{_outputFolder}/WeaponSkills/{id}.asset";
            var folderPath = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            
            WeaponSkillData data;
            
            if (File.Exists(assetPath) && _overwriteExisting)
            {
                data = AssetDatabase.LoadAssetAtPath<WeaponSkillData>(assetPath);
            }
            else if (File.Exists(assetPath))
            {
                return;
            }
            else
            {
                data = ScriptableObject.CreateInstance<WeaponSkillData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }
            
            // 基础技能数据
            data.skillId = id;
            data.skillName = CSVImporter.GetValue<string>(row, "skillName", id);
            data.description = CSVImporter.GetValue<string>(row, "description", "");
            data.category = SkillCategory.WeaponSkill;
            data.skillType = CSVImporter.GetValue<SkillType>(row, "skillType", SkillType.Active);
            
            // 消耗
            data.spCost = CSVImporter.GetValue<int>(row, "spCost", 1);
            data.atbCost = CSVImporter.GetValue<int>(row, "atbCost", 100);
            data.cooldown = CSVImporter.GetValue<int>(row, "cooldown", 0);
            
            // 伤害数值
            data.basePower = CSVImporter.GetValue<float>(row, "basePower", 100f);
            data.powerScaling = CSVImporter.GetValue<float>(row, "powerScaling", 1f);
            data.scalingAttribute = ScalingAttribute.WeaponDamage;
            data.damageType = CSVImporter.GetValue<DamageCategory>(row, "damageType", DamageCategory.Physical);
            data.element = CSVImporter.GetValue<ElementType>(row, "element", ElementType.None);
            
            // 目标
            data.targetType = CSVImporter.GetValue<TargetType>(row, "targetType", TargetType.SingleEnemy);
            data.maxTargets = CSVImporter.GetValue<int>(row, "maxTargets", 1);
            
            // 武器技能特有
            data.requiredWeaponCategory = CSVImporter.GetValue<WeaponCategory>(row, "requiredWeaponCategory", WeaponCategory.Blunt);
            data.proficiencyRequired = CSVImporter.GetValue<int>(row, "proficiencyRequired", 0);
            data.proficiencyGainOnUse = CSVImporter.GetValue<int>(row, "proficiencyGainOnUse", 1);
            data.consumesDurability = CSVImporter.GetValue<bool>(row, "consumesDurability", true);
            data.durabilityConsumption = CSVImporter.GetValue<int>(row, "durabilityConsumption", 1);
            
            // 连击
            data.isComboStarter = CSVImporter.GetValue<bool>(row, "isComboStarter", false);
            data.isComboFinisher = CSVImporter.GetValue<bool>(row, "isComboFinisher", false);
            data.hitCount = CSVImporter.GetValue<int>(row, "hitCount", 1);
            
            EditorUtility.SetDirty(data);
        }
        
        #region CSV模板生成
        
        private static void GenerateCharacterTemplate(string path)
        {
            var lines = new List<string>
            {
                "# 角色数据模板 - 可玩角色与同伴",
                "# 字段说明见文档，多值字段使用分号分隔",
                "characterId,characterName,description,characterType,baseLevel,constitution,strength,perception,reaction,wisdom,luck,constitutionGrowth,strengthGrowth,perceptionGrowth,reactionGrowth,wisdomGrowth,luckGrowth,baseATBSpeed,basePhysicalSP,baseMagicSP,fireResistance,iceResistance,lightningResistance,poisonResistance,holyResistance,darkResistance,defaultWeaponIds,defaultArmorIds,defaultSkillIds,spriteAtlasPath,portraitPath,animatorPath",
                "hero_001,勇者,冒险的主角,Player,1,12,14,10,11,8,10,1.2,1.5,1.0,1.1,0.8,0.5,100,6,4,0,0,0,0,0.1,0,weapon_sword_001,armor_light_001,skill_slash;skill_guard,Sprites/Characters/Hero,Sprites/Portraits/Hero,Animators/HeroAnimator",
                "companion_001,治疗师,擅长恢复魔法,Companion,1,10,8,12,9,15,11,1.0,0.8,1.2,0.9,1.5,0.6,90,3,8,0,0,0,0,0.2,-0.1,weapon_staff_001,armor_robe_001,skill_heal;skill_cure,Sprites/Characters/Healer,Sprites/Portraits/Healer,Animators/HealerAnimator"
            };
            
            File.WriteAllLines(path, lines);
        }
        
        private static void GenerateEnemyTemplate(string path)
        {
            var lines = new List<string>
            {
                "# 敌人数据模板",
                "# 字段说明见文档，多值字段使用分号分隔",
                "characterId,characterName,description,baseLevel,constitution,strength,perception,reaction,wisdom,luck,constitutionGrowth,strengthGrowth,perceptionGrowth,reactionGrowth,wisdomGrowth,luckGrowth,category,threatLevel,isBoss,aiBehavior,aggroRange,deaggroRange,baseExpReward,baseGoldReward,spawnLocations,minSpawnLevel,maxSpawnLevel,spawnWeight,defaultSkillIds,fireResistance,iceResistance,lightningResistance,poisonResistance,holyResistance,darkResistance",
                "enemy_slime_001,史莱姆,最基础的敌人,1,8,6,5,7,4,5,0.8,0.6,0.5,0.7,0.4,0.3,Beast,1,false,Aggressive,5,15,10,5,forest_01;plains_01,1,10,1.0,skill_tackle,0,-0.2,0,0.3,0,0",
                "enemy_goblin_001,哥布林,狡猾的小型敌人,3,10,12,8,11,6,8,1.0,1.2,0.8,1.1,0.6,0.5,Humanoid,2,false,Tactical,6,12,25,15,cave_01;forest_02,3,15,0.8,skill_stab;skill_throw,0.1,0,0,0,-0.1,0",
                "boss_dragon_001,火焰巨龙,森林深处的古老巨龙,20,50,45,30,25,35,20,2.0,1.8,1.5,1.2,1.6,1.0,Dragon,10,true,Berserker,15,30,500,200,dragon_lair,15,99,0.1,skill_fire_breath;skill_tail_swipe;skill_fly,0.5,-0.3,0.2,0.3,0,-0.2"
            };
            
            File.WriteAllLines(path, lines);
        }
        
        private static void GenerateSkillTemplate(string path)
        {
            var lines = new List<string>
            {
                "# 技能数据模板",
                "# category: WeaponSkill/MagicSkill/PhysicalSkill/SupportSkill/PassiveSkill/UltimateSkill",
                "# skillType: Active/Passive/Toggle/Reaction",
                "# damageType: Physical/Magic/True",
                "# element: None/Fire/Ice/Lightning/Poison/Holy/Dark",
                "# targetType: Self/SingleAlly/SingleEnemy/AllAllies/AllEnemies/Area",
                "# scalingAttribute: Strength/Wisdom/Perception/Constitution/Reaction/Luck/WeaponDamage/MaxHP/CurrentHP/MissingHP",
                "skillId,skillName,description,category,skillType,spCost,mpCost,hpCost,atbCost,cooldown,basePower,powerScaling,scalingAttribute,damageType,element,targetType,maxTargets,range,aoeRadius,accuracyModifier,critRateBonus,critDamageBonus,hitCount,hitInterval,requiredLevel,prerequisiteSkillIds,iconPath,animationClipPath,vfxPrefabPath,sfxPath",
                "skill_slash,斩击,基础剑技,PhysicalSkill,Active,1,0,0,100,0,120,1.0,Strength,Physical,None,SingleEnemy,1,1.5,0,1.0,0,0,1,0,1,,Icons/Skills/Slash,Animations/Skills/Slash,VFX/Skills/Slash,SFX/Skills/Slash",
                "skill_fireball,火球术,投掷火焰,MagicSkill,Active,0,15,0,100,0,150,1.2,Wisdom,Magic,Fire,SingleEnemy,1,5,0,0.95,0.05,0,1,0,5,,Icons/Skills/Fireball,Animations/Skills/Fireball,VFX/Skills/Fireball,SFX/Skills/Fireball",
                "skill_heal,治疗术,恢复生命,SupportSkill,Active,0,10,0,80,0,100,0.8,Wisdom,Magic,Holy,SingleAlly,1,3,0,1.0,0,0,1,0,1,,Icons/Skills/Heal,Animations/Skills/Heal,VFX/Skills/Heal,SFX/Skills/Heal",
                "skill_multi_strike,连斩,连续三次攻击,PhysicalSkill,Active,2,0,0,150,2,80,0.8,Strength,Physical,None,SingleEnemy,1,1.5,0,0.9,0.1,0.2,3,0.15,10,skill_slash,Icons/Skills/MultiStrike,Animations/Skills/MultiStrike,VFX/Skills/MultiStrike,SFX/Skills/MultiStrike"
            };
            
            File.WriteAllLines(path, lines);
        }
        
        private static void GenerateWeaponSkillTemplate(string path)
        {
            var lines = new List<string>
            {
                "# 武器技能数据模板",
                "# requiredWeaponCategory: Blunt/Sharp/Bow/Explosive/Gun/Magic",
                "skillId,skillName,description,skillType,spCost,atbCost,cooldown,basePower,powerScaling,damageType,element,targetType,maxTargets,requiredWeaponCategory,proficiencyRequired,proficiencyGainOnUse,consumesDurability,durabilityConsumption,isComboStarter,isComboFinisher,hitCount",
                "wskill_sword_slash,剑斩,基础剑技,Active,1,100,0,110,1.0,Physical,None,SingleEnemy,1,Sharp,0,2,true,1,true,false,1",
                "wskill_sword_thrust,突刺,高穿透攻击,Active,1,100,0,130,1.1,Physical,None,SingleEnemy,1,Sharp,10,3,true,1,false,false,1",
                "wskill_sword_combo,连击终结,连击最后一击,Active,2,120,1,200,1.3,Physical,None,SingleEnemy,1,Sharp,25,5,true,2,false,true,1",
                "wskill_bow_shot,瞄准射击,精准远程攻击,Active,1,120,0,100,1.0,Physical,None,SingleEnemy,1,Bow,0,2,true,1,false,false,1",
                "wskill_bow_rain,箭雨,范围攻击,Active,3,180,3,80,0.8,Physical,None,AllEnemies,5,Bow,30,8,true,3,false,false,5"
            };
            
            File.WriteAllLines(path, lines);
        }
        
        #endregion
    }
    
    #endregion
    
#endif
}
