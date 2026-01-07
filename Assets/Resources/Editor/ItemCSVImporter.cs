#if UNITY_EDITOR
using ItemSystem.Consumables;
using ItemSystem.Core;
using ItemSystem.Cosmetics;
using ItemSystem.Equipment;
using ItemSystem.Materials;
using ItemSystem.Quest;
using ItemSystem.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;

namespace ItemSystem.Editor
{
    /// <summary>
    /// CSV物品导入器 - Unity编辑器窗口
    /// </summary>
    public class ItemCSVImporter : EditorWindow
    {
        private string csvFolderPath = "Assets/Data/Items/CSV";
        private string outputFolderPath = "Assets/ScriptableObjects/Items";
        private bool overwriteExisting = false;
        private Vector2 scrollPosition;
        private string logMessage = "";

        // CSV文件路径
        private string weaponCsvPath = "";
        private string armorCsvPath = "";
        private string accessoryCsvPath = "";
        private string healingItemCsvPath = "";
        private string buffItemCsvPath = "";
        private string toolCsvPath = "";
        private string materialCsvPath = "";
        private string cosmeticCsvPath = "";
        private string questItemCsvPath = "";

        [MenuItem("Tools/Item System/CSV Importer")]
        public static void ShowWindow()
        {
            var window = GetWindow<ItemCSVImporter>("物品CSV导入器");
            window.minSize = new Vector2(500, 600);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("物品CSV批量导入工具", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // 路径设置
            EditorGUILayout.LabelField("路径设置", EditorStyles.boldLabel);
            csvFolderPath = EditorGUILayout.TextField("CSV文件夹", csvFolderPath);
            outputFolderPath = EditorGUILayout.TextField("输出文件夹", outputFolderPath);
            overwriteExisting = EditorGUILayout.Toggle("覆盖已存在文件", overwriteExisting);

            EditorGUILayout.Space(10);

            // 各类物品CSV文件
            EditorGUILayout.LabelField("CSV文件路径（留空则跳过）", EditorStyles.boldLabel);

            weaponCsvPath = FileField("武器 (Weapons)", weaponCsvPath);
            armorCsvPath = FileField("护甲 (Armors)", armorCsvPath);
            accessoryCsvPath = FileField("饰品 (Accessories)", accessoryCsvPath);
            healingItemCsvPath = FileField("回复品 (HealingItems)", healingItemCsvPath);
            buffItemCsvPath = FileField("增益品 (BuffItems)", buffItemCsvPath);
            toolCsvPath = FileField("工具 (Tools)", toolCsvPath);
            materialCsvPath = FileField("材料 (Materials)", materialCsvPath);
            cosmeticCsvPath = FileField("服装 (Cosmetics)", cosmeticCsvPath);
            questItemCsvPath = FileField("剧情道具 (QuestItems)", questItemCsvPath);

            EditorGUILayout.Space(20);

            // 操作按钮
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("生成CSV模板", GUILayout.Height(30)))
            {
                GenerateCSVTemplates();
            }

            if (GUILayout.Button("导入所有CSV", GUILayout.Height(30)))
            {
                ImportAllCSV();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 单独导入按钮
            EditorGUILayout.LabelField("单独导入", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("武器")) ImportWeapons();
            if (GUILayout.Button("护甲")) ImportArmors();
            if (GUILayout.Button("饰品")) ImportAccessories();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("回复品")) ImportHealingItems();
            if (GUILayout.Button("增益品")) ImportBuffItems();
            if (GUILayout.Button("工具")) ImportTools();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("材料")) ImportMaterials();
            if (GUILayout.Button("服装")) ImportCosmetics();
            if (GUILayout.Button("剧情道具")) ImportQuestItems();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            // 日志显示
            EditorGUILayout.LabelField("导入日志", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(logMessage, GUILayout.Height(150));

            EditorGUILayout.EndScrollView();
        }

        private string FileField(string label, string path)
        {
            EditorGUILayout.BeginHorizontal();
            path = EditorGUILayout.TextField(label, path);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string selected = EditorUtility.OpenFilePanel($"选择{label}CSV", csvFolderPath, "csv");
                if (!string.IsNullOrEmpty(selected))
                {
                    path = selected;
                }
            }
            EditorGUILayout.EndHorizontal();
            return path;
        }

        private void Log(string message)
        {
            logMessage += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
            Debug.Log($"[ItemCSVImporter] {message}");
        }

        private void ClearLog()
        {
            logMessage = "";
        }

        #region CSV模板生成

        private void GenerateCSVTemplates()
        {
            ClearLog();

            string templatePath = Path.Combine(csvFolderPath, "Templates");
            Directory.CreateDirectory(templatePath);

            // 生成各类物品的CSV模板
            GenerateWeaponTemplate(templatePath);
            GenerateArmorTemplate(templatePath);
            GenerateAccessoryTemplate(templatePath);
            GenerateHealingItemTemplate(templatePath);
            GenerateBuffItemTemplate(templatePath);
            GenerateToolTemplate(templatePath);
            GenerateMaterialTemplate(templatePath);
            GenerateCosmeticTemplate(templatePath);
            GenerateQuestItemTemplate(templatePath);

            AssetDatabase.Refresh();
            Log($"CSV模板已生成到: {templatePath}");
        }

        private void GenerateWeaponTemplate(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 武器CSV模板 - 以#开头的行为注释");
            sb.AppendLine("# ItemId,ItemName,Description,IconPath,Rarity,Flags,BuyPrice,SellPrice,LevelRequirement,WeaponCategory,BaseDamage,CriticalChance,CriticalMultiplier,Element,DamageCategory,MaxDurability,RangeType,TargetPositions,SkillIds");
            sb.AppendLine("# Rarity: Common/Uncommon/Rare/Epic/Legendary");
            sb.AppendLine("# Flags: 用|分隔，如 Reforgeable|Socketable|HasDurability|Sellable");
            sb.AppendLine("# WeaponCategory: Blunt/Sharp/Bow/Explosive/Gun/Magic");
            sb.AppendLine("# Element: None/Fire/Ice/Lightning/Poison/Holy/Dark");
            sb.AppendLine("# DamageCategory: Physical/Magic/True");
            sb.AppendLine("# RangeType: Melee/Ranged/All/Adjacent/Self");
            sb.AppendLine("# TargetPositions: 用|分隔的位置索引，如 0|1 表示前两个位置");
            sb.AppendLine("ItemId,ItemName,Description,IconPath,Rarity,Flags,BuyPrice,SellPrice,LevelRequirement,WeaponCategory,BaseDamage,CriticalChance,CriticalMultiplier,Element,DamageCategory,MaxDurability,RangeType,TargetPositions,SkillIds");
            sb.AppendLine("1001,铁剑,一把普通的铁剑,Icons/Weapons/iron_sword,Common,Reforgeable|Sellable|HasDurability,100,50,1,Sharp,15,0.05,1.5,None,Physical,100,Melee,0|1,");
            sb.AppendLine("1002,火焰法杖,蕴含火焰之力的法杖,Icons/Weapons/fire_staff,Rare,Reforgeable|Socketable|Sellable,500,250,10,Magic,25,0.08,1.8,Fire,Magic,80,All,0|1|2|3|4,101|102");

            File.WriteAllText(Path.Combine(path, "Weapons_Template.csv"), sb.ToString(), Encoding.UTF8);
            Log("生成: Weapons_Template.csv");
        }

        private void GenerateArmorTemplate(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 护甲CSV模板");
            sb.AppendLine("# ArmorSlot: Head/Body/Legs");
            sb.AppendLine("# Resistances: 用|分隔，顺序为 None|Fire|Ice|Lightning|Poison|Holy|Dark");
            sb.AppendLine("ItemId,ItemName,Description,IconPath,Rarity,Flags,BuyPrice,SellPrice,LevelRequirement,ArmorSlot,PhysicalDefense,MagicDefense,Resistances,MaxDurability");
            sb.AppendLine("2001,铁头盔,基础的铁制头盔,Icons/Armors/iron_helmet,Common,Socketable|Sellable|HasDurability,80,40,1,Head,5,2,0|0|0|0|0|0|0,120");
            sb.AppendLine("2002,皮甲,轻便的皮革护甲,Icons/Armors/leather_armor,Common,Sellable|HasDurability,150,75,1,Body,8,3,0|0|0|0|0|0|0,100");
            sb.AppendLine("2003,火焰胸甲,抵御火焰的胸甲,Icons/Armors/fire_chest,Rare,Socketable|Sellable|HasDurability,800,400,15,Body,20,15,0|0.3|0|0|0|0|0,150");

            File.WriteAllText(Path.Combine(path, "Armors_Template.csv"), sb.ToString(), Encoding.UTF8);
            Log("生成: Armors_Template.csv");
        }

        private void GenerateAccessoryTemplate(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 饰品CSV模板");
            sb.AppendLine("# StatModifiers格式: StatType:ModifierType:Value|... 例如 PhysicalAttack:Flat:10|CriticalRate:PercentAdd:0.05");
            sb.AppendLine("# SpecialEffects格式: EffectType:Value:Element|... 例如 DamageBonus:0.1:None|ElementalDamageBonus:0.2:Fire");
            sb.AppendLine("# StatType: Constitution/Strength/Perception/Reaction/Wisdom/Luck/MaxHealth/PhysicalAttack/MagicAttack/PhysicalDefense/MagicDefense/Resistance/CriticalRate/Speed/Accuracy/Evasion");
            sb.AppendLine("# ModifierType: Flat/PercentAdd/PercentMult");
            sb.AppendLine("# EffectType: DamageBonus/ElementalDamageBonus/CriticalChanceBonus/CriticalDamageBonus/ResistanceBonus/SkillPointRecovery/StressReduction/FatigueReduction");
            sb.AppendLine("ItemId,ItemName,Description,IconPath,Rarity,Flags,BuyPrice,SellPrice,LevelRequirement,StatModifiers,SpecialEffects,PassiveSkillId");
            sb.AppendLine("3001,力量戒指,增加力量的戒指,Icons/Accessories/str_ring,Uncommon,Sellable,200,100,5,Strength:Flat:5,,");
            sb.AppendLine("3002,暴击护符,提高暴击率,Icons/Accessories/crit_amulet,Rare,Sellable,600,300,10,CriticalRate:PercentAdd:0.1,CriticalChanceBonus:0.05:None,");
            sb.AppendLine("3003,火焰之心,增强火焰伤害,Icons/Accessories/fire_heart,Epic,Sellable,1500,750,20,MagicAttack:Flat:15,ElementalDamageBonus:0.25:Fire,201");

            File.WriteAllText(Path.Combine(path, "Accessories_Template.csv"), sb.ToString(), Encoding.UTF8);
            Log("生成: Accessories_Template.csv");
        }

        private void GenerateHealingItemTemplate(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 回复类消耗品CSV模板");
            sb.AppendLine("# HealingType: Health/SkillPoints/Stress/Fatigue/Revive/All");
            sb.AppendLine("# TargetType: Self/SingleAlly/AllAllies/SingleEnemy/AllEnemies/Position/All");
            sb.AppendLine("# DebuffsToRemove: 用|分隔，如 Poisoned|Bleeding");
            sb.AppendLine("ItemId,ItemName,Description,IconPath,Rarity,Flags,BuyPrice,SellPrice,MaxStackSize,HealingType,HealAmount,HealPercent,HealOverTime,Duration,TickCount,StressReduction,FatigueReduction,RemoveDebuffs,DebuffsToRemove,ATBCost,TargetType,Cooldown");
            sb.AppendLine("4001,小型生命药水,恢复少量生命,Icons/Consumables/hp_potion_s,Common,Stackable|UsableInCombat|CanCarryToBattle|Sellable,50,25,20,Health,50,0,false,0,0,0,0,false,,50,SingleAlly,0");
            sb.AppendLine("4002,中型生命药水,恢复中量生命,Icons/Consumables/hp_potion_m,Uncommon,Stackable|UsableInCombat|CanCarryToBattle|Sellable,150,75,10,Health,150,0,false,0,0,0,0,false,,50,SingleAlly,0");
            sb.AppendLine("4003,再生药剂,持续恢复生命,Icons/Consumables/regen_potion,Rare,Stackable|UsableInCombat|CanCarryToBattle|Sellable,300,150,5,Health,200,0,true,10,5,0,0,false,,60,SingleAlly,0");
            sb.AppendLine("4004,解毒药,移除中毒效果,Icons/Consumables/antidote,Common,Stackable|UsableInCombat|CanCarryToBattle|Sellable,80,40,10,Health,0,0,false,0,0,0,0,true,Poisoned,30,SingleAlly,0");
            sb.AppendLine("4005,镇定剂,减少压力,Icons/Consumables/calm_potion,Uncommon,Stackable|UsableInCombat|CanCarryToBattle|Sellable,120,60,10,Stress,0,0,false,0,0,30,0,false,,40,SingleAlly,0");
            sb.AppendLine("4006,复活卷轴,复活倒下的队友,Icons/Consumables/revive_scroll,Epic,Stackable|UsableInCombat|CanCarryToBattle|Sellable,1000,500,3,Revive,100,0.3,false,0,0,0,0,false,,80,SingleAlly,0");

            File.WriteAllText(Path.Combine(path, "HealingItems_Template.csv"), sb.ToString(), Encoding.UTF8);
            Log("生成: HealingItems_Template.csv");
        }

        private void GenerateBuffItemTemplate(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 增益类消耗品CSV模板");
            sb.AppendLine("# BuffEffects格式: BuffType:Value:IsPercentage|... 例如 Attack:10:false|Speed:0.2:true");
            sb.AppendLine("# BuffType: Attack/Defense/MagicAttack/MagicDefense/Speed/CriticalChance/CriticalDamage/Accuracy/Evasion/AllStats");
            sb.AppendLine("ItemId,ItemName,Description,IconPath,Rarity,Flags,BuyPrice,SellPrice,MaxStackSize,BuffEffects,Duration,IsPermanent,ATBCost,TargetType,Cooldown");
            sb.AppendLine("5001,力量药剂,暂时提升攻击力,Icons/Consumables/str_potion,Common,Stackable|UsableInCombat|CanCarryToBattle|Sellable,100,50,10,Attack:15:false,60,false,50,SingleAlly,0");
            sb.AppendLine("5002,敏捷药剂,暂时提升速度,Icons/Consumables/agi_potion,Common,Stackable|UsableInCombat|CanCarryToBattle|Sellable,100,50,10,Speed:0.2:true,60,false,50,SingleAlly,0");
            sb.AppendLine("5003,全能药剂,提升所有属性,Icons/Consumables/all_potion,Epic,Stackable|UsableInCombat|CanCarryToBattle|Sellable,500,250,5,AllStats:0.1:true,90,false,60,SingleAlly,0");
            sb.AppendLine("5004,狂暴药水,大幅提升攻击但降低防御,Icons/Consumables/rage_potion,Rare,Stackable|UsableInCombat|CanCarryToBattle|Sellable,200,100,5,Attack:0.5:true|Defense:-0.3:true,45,false,50,Self,0");

            File.WriteAllText(Path.Combine(path, "BuffItems_Template.csv"), sb.ToString(), Encoding.UTF8);
            Log("生成: BuffItems_Template.csv");
        }

        private void GenerateToolTemplate(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 工具类物品CSV模板");
            sb.AppendLine("# ToolType: Key/Bomb/Torch/Rope/Pickaxe/Shovel/FishingRod/Compass/Map/Teleporter");
            sb.AppendLine("# UseCount: -1表示无限使用");
            sb.AppendLine("ItemId,ItemName,Description,IconPath,Rarity,Flags,BuyPrice,SellPrice,MaxStackSize,ToolType,UseCount,Cooldown,EffectData");
            sb.AppendLine("6001,铁钥匙,打开铁门的钥匙,Icons/Tools/iron_key,Common,Stackable|Sellable,50,25,10,Key,1,0,door_iron");
            sb.AppendLine("6002,炸弹,可以炸开障碍物,Icons/Tools/bomb,Uncommon,Stackable|Sellable,80,40,20,Bomb,1,0,damage:50|radius:2");
            sb.AppendLine("6003,火把,照亮黑暗区域,Icons/Tools/torch,Common,Stackable|Sellable,10,5,50,Torch,-1,0,light:5|duration:300");
            sb.AppendLine("6004,传送石,传送回城镇,Icons/Tools/teleport_stone,Rare,Stackable|Sellable,500,250,5,Teleporter,1,0,target:town_center");

            File.WriteAllText(Path.Combine(path, "Tools_Template.csv"), sb.ToString(), Encoding.UTF8);
            Log("生成: Tools_Template.csv");
        }

        private void GenerateMaterialTemplate(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 材料类物品CSV模板");
            sb.AppendLine("# MaterialCategory: Ore/Wood/Herb/Gem/Fabric/Leather/Bone/Monster/Essence/Other");
            sb.AppendLine("# Tier: 材料等级，影响可合成物品的等级");
            sb.AppendLine("ItemId,ItemName,Description,IconPath,Rarity,Flags,BuyPrice,SellPrice,MaxStackSize,MaterialCategory,Tier");
            sb.AppendLine("7001,铁矿石,普通的铁矿石,Icons/Materials/iron_ore,Common,Stackable|Sellable|Craftable,20,10,99,Ore,1");
            sb.AppendLine("7002,秘银矿石,稀有的秘银矿石,Icons/Materials/mithril_ore,Rare,Stackable|Sellable|Craftable,200,100,99,Ore,3");
            sb.AppendLine("7003,橡木,坚固的橡木,Icons/Materials/oak_wood,Common,Stackable|Sellable|Craftable,15,7,99,Wood,1");
            sb.AppendLine("7004,治疗草,具有治疗效果的草药,Icons/Materials/heal_herb,Common,Stackable|Sellable|Craftable,30,15,99,Herb,1");
            sb.AppendLine("7005,红宝石,闪耀的红宝石,Icons/Materials/ruby,Rare,Stackable|Sellable|Craftable,300,150,50,Gem,2");
            sb.AppendLine("7006,史莱姆凝胶,史莱姆掉落的凝胶,Icons/Materials/slime_gel,Common,Stackable|Sellable|Craftable,10,5,99,Monster,1");
            sb.AppendLine("7007,火焰精华,蕴含火焰之力的精华,Icons/Materials/fire_essence,Epic,Stackable|Sellable|Craftable,500,250,30,Essence,4");

            File.WriteAllText(Path.Combine(path, "Materials_Template.csv"), sb.ToString(), Encoding.UTF8);
            Log("生成: Materials_Template.csv");
        }

        private void GenerateCosmeticTemplate(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 服装/时装CSV模板");
            sb.AppendLine("# CosmeticSlot: Hat/Hair/Face/Back/Outfit/Pet");
            sb.AppendLine("# HasStats: true/false - 是否有属性加成");
            sb.AppendLine("# StatModifiers格式: StatType:ModifierType:Value|...");
            sb.AppendLine("# VisualSprites: 用|分隔的精灵路径");
            sb.AppendLine("ItemId,ItemName,Description,IconPath,Rarity,Flags,BuyPrice,SellPrice,CosmeticSlot,HasStats,StatModifiers,TintColor,VisualSprites");
            sb.AppendLine("8001,红色披风,一件漂亮的红色披风,Icons/Cosmetics/red_cape,Uncommon,Sellable,300,150,Back,false,,FF0000,Sprites/Cosmetics/red_cape_0|Sprites/Cosmetics/red_cape_1");
            sb.AppendLine("8002,勇者头盔,传说中勇者的头盔,Icons/Cosmetics/hero_helmet,Epic,Sellable,1000,500,Hat,true,Strength:Flat:3|Luck:Flat:2,FFFFFF,Sprites/Cosmetics/hero_helmet");
            sb.AppendLine("8003,小精灵宠物,跟随你的小精灵,Icons/Cosmetics/fairy_pet,Legendary,Sellable,5000,2500,Pet,true,Luck:Flat:5,00FF00,Sprites/Cosmetics/fairy_pet");

            File.WriteAllText(Path.Combine(path, "Cosmetics_Template.csv"), sb.ToString(), Encoding.UTF8);
            Log("生成: Cosmetics_Template.csv");
        }

        private void GenerateQuestItemTemplate(string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 剧情道具CSV模板");
            sb.AppendLine("# QuestItemType: KeyItem/Clue/Letter/Photo/Artifact/Memory");
            sb.AppendLine("# 剧情道具默认不可丢弃、不可出售");
            sb.AppendLine("ItemId,ItemName,Description,IconPath,Rarity,QuestId,QuestItemType,AutoRemoveOnQuestComplete,LoreText");
            sb.AppendLine("9001,神秘钥匙,打开古代遗迹的钥匙,Icons/Quest/mystery_key,Epic,quest_ancient_ruins,KeyItem,true,这把钥匙上刻着古老的符文，似乎是通往某个神秘地方的入口。");
            sb.AppendLine("9002,王室信件,来自王室的密信,Icons/Quest/royal_letter,Rare,quest_royal_mission,Letter,true,信上写着一些机密内容，需要亲自交给指定的人。");
            sb.AppendLine("9003,记忆碎片,某人的记忆片段,Icons/Quest/memory_shard,Legendary,quest_lost_memory,Memory,false,触碰它时能看到模糊的画面，似乎是很久以前的事情...");

            File.WriteAllText(Path.Combine(path, "QuestItems_Template.csv"), sb.ToString(), Encoding.UTF8);
            Log("生成: QuestItems_Template.csv");
        }

        #endregion

        #region CSV导入

        private void ImportAllCSV()
        {
            ClearLog();
            Log("开始批量导入...");

            ImportWeapons();
            ImportArmors();
            ImportAccessories();
            ImportHealingItems();
            ImportBuffItems();
            ImportTools();
            ImportMaterials();
            ImportCosmetics();
            ImportQuestItems();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Log("批量导入完成！");
        }

        private List<string[]> ParseCSV(string filePath)
        {
            var result = new List<string[]>();

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return result;
            }

            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            foreach (var line in lines)
            {
                // 跳过注释和空行
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    continue;

                // 简单CSV解析（不处理引号内的逗号）
                var values = line.Split(',');
                result.Add(values);
            }

            // 跳过表头
            if (result.Count > 0)
                result.RemoveAt(0);

            return result;
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private T CreateOrLoadAsset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                if (overwriteExisting)
                {
                    return existing;
                }
                else
                {
                    Log($"跳过已存在: {path}");
                    return null;
                }
            }

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private ItemFlags ParseFlags(string flagsStr)
        {
            ItemFlags flags = ItemFlags.None;
            if (string.IsNullOrEmpty(flagsStr)) return flags;

            var parts = flagsStr.Split('|');
            foreach (var part in parts)
            {
                if (Enum.TryParse<ItemFlags>(part.Trim(), out var flag))
                {
                    flags |= flag;
                }
            }
            return flags;
        }

        private T ParseEnum<T>(string value, T defaultValue = default) where T : struct
        {
            if (Enum.TryParse<T>(value?.Trim(), out var result))
                return result;
            return defaultValue;
        }

        private int[] ParseIntArray(string str, char separator = '|')
        {
            if (string.IsNullOrEmpty(str)) return Array.Empty<int>();
            return str.Split(separator)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => int.TryParse(s.Trim(), out int v) ? v : 0)
                .ToArray();
        }

        private float[] ParseFloatArray(string str, char separator = '|')
        {
            if (string.IsNullOrEmpty(str)) return Array.Empty<float>();
            return str.Split(separator)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => float.TryParse(s.Trim(), out float v) ? v : 0f)
                .ToArray();
        }

        #endregion

        #region 各类物品导入实现

        private void ImportWeapons()
        {
            var rows = ParseCSV(weaponCsvPath);
            if (rows.Count == 0) { Log("武器: 无数据或文件不存在"); return; }

            string folder = Path.Combine(outputFolderPath, "Weapons");
            EnsureDirectoryExists(folder);

            int count = 0;
            foreach (var row in rows)
            {
                try
                {
                    if (row.Length < 18) continue;

                    int idx = 0;
                    int itemId = int.Parse(row[idx++]);
                    string itemName = row[idx++];
                    string description = row[idx++];
                    string iconPath = row[idx++];
                    ItemRarity rarity = ParseEnum<ItemRarity>(row[idx++]);
                    ItemFlags flags = ParseFlags(row[idx++]);
                    int buyPrice = int.Parse(row[idx++]);
                    int sellPrice = int.Parse(row[idx++]);
                    int levelReq = int.Parse(row[idx++]);
                    WeaponCategory weaponCat = ParseEnum<WeaponCategory>(row[idx++]);
                    int baseDamage = int.Parse(row[idx++]);
                    float critChance = float.Parse(row[idx++]);
                    float critMult = float.Parse(row[idx++]);
                    ElementType element = ParseEnum<ElementType>(row[idx++]);
                    DamageCategory dmgCat = ParseEnum<DamageCategory>(row[idx++]);
                    int maxDurability = int.Parse(row[idx++]);
                    AttackRangeType rangeType = ParseEnum<AttackRangeType>(row[idx++]);
                    int[] targetPos = ParseIntArray(row[idx++]);

                    string assetPath = Path.Combine(folder, $"{itemName}_{itemId}.asset");
                    var weapon = CreateOrLoadAsset<Weapon>(assetPath);
                    if (weapon == null) continue;

                    // 使用反射设置私有字段
                    SetPrivateField(weapon, "itemId", itemId);
                    SetPrivateField(weapon, "itemName", itemName);
                    SetPrivateField(weapon, "description", description);
                    SetPrivateField(weapon, "icon", LoadSprite(iconPath));
                    SetPrivateField(weapon, "itemType", ItemType.Equipment);
                    SetPrivateField(weapon, "rarity", rarity);
                    SetPrivateField(weapon, "flags", flags);
                    SetPrivateField(weapon, "buyPrice", buyPrice);
                    SetPrivateField(weapon, "sellPrice", sellPrice);
                    SetPrivateField(weapon, "levelRequirement", levelReq);
                    SetPrivateField(weapon, "equipSubType", EquipmentSubType.Weapon);
                    SetPrivateField(weapon, "weaponCategory", weaponCat);
                    SetPrivateField(weapon, "baseDamage", baseDamage);
                    SetPrivateField(weapon, "criticalChance", critChance);
                    SetPrivateField(weapon, "criticalMultiplier", critMult);
                    SetPrivateField(weapon, "element", element);
                    SetPrivateField(weapon, "damageCategory", dmgCat);
                    SetPrivateField(weapon, "maxDurability", maxDurability);

                    var rangeConfig = new AttackRangeConfig
                    {
                        rangeType = rangeType,
                        targetablePositions = targetPos
                    };
                    SetPrivateField(weapon, "attackRange", rangeConfig);

                    EditorUtility.SetDirty(weapon);
                    count++;
                }
                catch (Exception ex)
                {
                    Log($"武器导入错误: {ex.Message}");
                }
            }

            Log($"武器: 导入 {count} 个");
        }

        private void ImportArmors()
        {
            var rows = ParseCSV(armorCsvPath);
            if (rows.Count == 0) { Log("护甲: 无数据或文件不存在"); return; }

            string folder = Path.Combine(outputFolderPath, "Armors");
            EnsureDirectoryExists(folder);

            int count = 0;
            foreach (var row in rows)
            {
                try
                {
                    if (row.Length < 14) continue;

                    int idx = 0;
                    int itemId = int.Parse(row[idx++]);
                    string itemName = row[idx++];
                    string description = row[idx++];
                    string iconPath = row[idx++];
                    ItemRarity rarity = ParseEnum<ItemRarity>(row[idx++]);
                    ItemFlags flags = ParseFlags(row[idx++]);
                    int buyPrice = int.Parse(row[idx++]);
                    int sellPrice = int.Parse(row[idx++]);
                    int levelReq = int.Parse(row[idx++]);
                    ArmorSlot slot = ParseEnum<ArmorSlot>(row[idx++]);
                    int physDef = int.Parse(row[idx++]);
                    int magDef = int.Parse(row[idx++]);
                    float[] resistances = ParseFloatArray(row[idx++]);
                    int maxDurability = int.Parse(row[idx++]);

                    string assetPath = Path.Combine(folder, $"{itemName}_{itemId}.asset");
                    var armor = CreateOrLoadAsset<Armor>(assetPath);
                    if (armor == null) continue;

                    SetPrivateField(armor, "itemId", itemId);
                    SetPrivateField(armor, "itemName", itemName);
                    SetPrivateField(armor, "description", description);
                    SetPrivateField(armor, "icon", LoadSprite(iconPath));
                    SetPrivateField(armor, "itemType", ItemType.Equipment);
                    SetPrivateField(armor, "rarity", rarity);
                    SetPrivateField(armor, "flags", flags);
                    SetPrivateField(armor, "buyPrice", buyPrice);
                    SetPrivateField(armor, "sellPrice", sellPrice);
                    SetPrivateField(armor, "levelRequirement", levelReq);
                    SetPrivateField(armor, "equipSubType", EquipmentSubType.Armor);
                    SetPrivateField(armor, "armorSlot", slot);
                    SetPrivateField(armor, "physicalDefense", physDef);
                    SetPrivateField(armor, "magicDefense", magDef);
                    SetPrivateField(armor, "resistances", resistances);
                    SetPrivateField(armor, "maxDurability", maxDurability);

                    EditorUtility.SetDirty(armor);
                    count++;
                }
                catch (Exception ex)
                {
                    Log($"护甲导入错误: {ex.Message}");
                }
            }

            Log($"护甲: 导入 {count} 个");
        }

        private void ImportAccessories()
        {
            var rows = ParseCSV(accessoryCsvPath);
            if (rows.Count == 0) { Log("饰品: 无数据或文件不存在"); return; }

            string folder = Path.Combine(outputFolderPath, "Accessories");
            EnsureDirectoryExists(folder);

            int count = 0;
            foreach (var row in rows)
            {
                try
                {
                    if (row.Length < 12) continue;

                    int idx = 0;
                    int itemId = int.Parse(row[idx++]);
                    string itemName = row[idx++];
                    string description = row[idx++];
                    string iconPath = row[idx++];
                    ItemRarity rarity = ParseEnum<ItemRarity>(row[idx++]);
                    ItemFlags flags = ParseFlags(row[idx++]);
                    int buyPrice = int.Parse(row[idx++]);
                    int sellPrice = int.Parse(row[idx++]);
                    int levelReq = int.Parse(row[idx++]);
                    var statMods = ParseStatModifiers(row[idx++]);
                    var specialEffects = ParseAccessoryEffects(row[idx++]);

                    string assetPath = Path.Combine(folder, $"{itemName}_{itemId}.asset");
                    var accessory = CreateOrLoadAsset<Accessory>(assetPath);
                    if (accessory == null) continue;

                    SetPrivateField(accessory, "itemId", itemId);
                    SetPrivateField(accessory, "itemName", itemName);
                    SetPrivateField(accessory, "description", description);
                    SetPrivateField(accessory, "icon", LoadSprite(iconPath));
                    SetPrivateField(accessory, "itemType", ItemType.Equipment);
                    SetPrivateField(accessory, "rarity", rarity);
                    SetPrivateField(accessory, "flags", flags);
                    SetPrivateField(accessory, "buyPrice", buyPrice);
                    SetPrivateField(accessory, "sellPrice", sellPrice);
                    SetPrivateField(accessory, "levelRequirement", levelReq);
                    SetPrivateField(accessory, "equipSubType", EquipmentSubType.Accessory);
                    SetPrivateField(accessory, "baseStatModifiers", statMods);
                    SetPrivateField(accessory, "specialEffects", specialEffects);

                    EditorUtility.SetDirty(accessory);
                    count++;
                }
                catch (Exception ex)
                {
                    Log($"饰品导入错误: {ex.Message}");
                }
            }

            Log($"饰品: 导入 {count} 个");
        }

        private void ImportHealingItems()
        {
            var rows = ParseCSV(healingItemCsvPath);
            if (rows.Count == 0) { Log("回复品: 无数据或文件不存在"); return; }

            string folder = Path.Combine(outputFolderPath, "Consumables/Healing");
            EnsureDirectoryExists(folder);

            int count = 0;
            foreach (var row in rows)
            {
                try
                {
                    if (row.Length < 22) continue;

                    int idx = 0;
                    int itemId = int.Parse(row[idx++]);
                    string itemName = row[idx++];
                    string description = row[idx++];
                    string iconPath = row[idx++];
                    ItemRarity rarity = ParseEnum<ItemRarity>(row[idx++]);
                    ItemFlags flags = ParseFlags(row[idx++]);
                    int buyPrice = int.Parse(row[idx++]);
                    int sellPrice = int.Parse(row[idx++]);
                    int maxStack = int.Parse(row[idx++]);
                    HealingType healType = ParseEnum<HealingType>(row[idx++]);
                    int healAmount = int.Parse(row[idx++]);
                    float healPercent = float.Parse(row[idx++]);
                    bool healOverTime = bool.Parse(row[idx++]);
                    float duration = float.Parse(row[idx++]);
                    int tickCount = int.Parse(row[idx++]);
                    int stressRed = int.Parse(row[idx++]);
                    int fatigueRed = int.Parse(row[idx++]);
                    bool removeDebuffs = bool.Parse(row[idx++]);
                    var debuffsToRemove = ParseStatusEffectTypes(row[idx++]);
                    int atbCost = int.Parse(row[idx++]);
                    TargetType targetType = ParseEnum<TargetType>(row[idx++]);
                    float cooldown = float.Parse(row[idx++]);

                    string assetPath = Path.Combine(folder, $"{itemName}_{itemId}.asset");
                    var item = CreateOrLoadAsset<HealingItem>(assetPath);
                    if (item == null) continue;

                    SetPrivateField(item, "itemId", itemId);
                    SetPrivateField(item, "itemName", itemName);
                    SetPrivateField(item, "description", description);
                    SetPrivateField(item, "icon", LoadSprite(iconPath));
                    SetPrivateField(item, "itemType", ItemType.Consumable);
                    SetPrivateField(item, "rarity", rarity);
                    SetPrivateField(item, "flags", flags);
                    SetPrivateField(item, "buyPrice", buyPrice);
                    SetPrivateField(item, "sellPrice", sellPrice);
                    SetPrivateField(item, "maxStackSize", maxStack);
                    SetPrivateField(item, "consumableSubType", ConsumableSubType.Healing);
                    SetPrivateField(item, "healingType", healType);
                    SetPrivateField(item, "healAmount", healAmount);
                    SetPrivateField(item, "healPercent", healPercent);
                    SetPrivateField(item, "healOverTime", healOverTime);
                    SetPrivateField(item, "duration", duration);
                    SetPrivateField(item, "tickCount", tickCount);
                    SetPrivateField(item, "stressReduction", stressRed);
                    SetPrivateField(item, "fatigueReduction", fatigueRed);
                    SetPrivateField(item, "removeDebuffs", removeDebuffs);
                    SetPrivateField(item, "debuffsToRemove", debuffsToRemove);
                    SetPrivateField(item, "atbCost", atbCost);
                    SetPrivateField(item, "targetType", targetType);
                    SetPrivateField(item, "cooldown", cooldown);

                    EditorUtility.SetDirty(item);
                    count++;
                }
                catch (Exception ex)
                {
                    Log($"回复品导入错误: {ex.Message}");
                }
            }

            Log($"回复品: 导入 {count} 个");
        }

        private void ImportBuffItems()
        {
            var rows = ParseCSV(buffItemCsvPath);
            if (rows.Count == 0) { Log("增益品: 无数据或文件不存在"); return; }

            string folder = Path.Combine(outputFolderPath, "Consumables/Buffs");
            EnsureDirectoryExists(folder);

            int count = 0;
            foreach (var row in rows)
            {
                try
                {
                    if (row.Length < 15) continue;

                    int idx = 0;
                    int itemId = int.Parse(row[idx++]);
                    string itemName = row[idx++];
                    string description = row[idx++];
                    string iconPath = row[idx++];
                    ItemRarity rarity = ParseEnum<ItemRarity>(row[idx++]);
                    ItemFlags flags = ParseFlags(row[idx++]);
                    int buyPrice = int.Parse(row[idx++]);
                    int sellPrice = int.Parse(row[idx++]);
                    int maxStack = int.Parse(row[idx++]);
                    var buffEffects = ParseBuffEffects(row[idx++]);
                    float duration = float.Parse(row[idx++]);
                    bool isPermanent = bool.Parse(row[idx++]);
                    int atbCost = int.Parse(row[idx++]);
                    TargetType targetType = ParseEnum<TargetType>(row[idx++]);
                    float cooldown = float.Parse(row[idx++]);

                    string assetPath = Path.Combine(folder, $"{itemName}_{itemId}.asset");
                    var item = CreateOrLoadAsset<BuffItem>(assetPath);
                    if (item == null) continue;

                    SetPrivateField(item, "itemId", itemId);
                    SetPrivateField(item, "itemName", itemName);
                    SetPrivateField(item, "description", description);
                    SetPrivateField(item, "icon", LoadSprite(iconPath));
                    SetPrivateField(item, "itemType", ItemType.Consumable);
                    SetPrivateField(item, "rarity", rarity);
                    SetPrivateField(item, "flags", flags);
                    SetPrivateField(item, "buyPrice", buyPrice);
                    SetPrivateField(item, "sellPrice", sellPrice);
                    SetPrivateField(item, "maxStackSize", maxStack);
                    SetPrivateField(item, "consumableSubType", ConsumableSubType.Buff);
                    SetPrivateField(item, "buffEffects", buffEffects);
                    SetPrivateField(item, "duration", duration);
                    SetPrivateField(item, "isPermanent", isPermanent);
                    SetPrivateField(item, "atbCost", atbCost);
                    SetPrivateField(item, "targetType", targetType);
                    SetPrivateField(item, "cooldown", cooldown);

                    EditorUtility.SetDirty(item);
                    count++;
                }
                catch (Exception ex)
                {
                    Log($"增益品导入错误: {ex.Message}");
                }
            }

            Log($"增益品: 导入 {count} 个");
        }

        private void ImportTools()
        {
            var rows = ParseCSV(toolCsvPath);
            if (rows.Count == 0) { Log("工具: 无数据或文件不存在"); return; }

            string folder = Path.Combine(outputFolderPath, "Tools");
            EnsureDirectoryExists(folder);

            int count = 0;
            foreach (var row in rows)
            {
                try
                {
                    if (row.Length < 13) continue;

                    int idx = 0;
                    int itemId = int.Parse(row[idx++]);
                    string itemName = row[idx++];
                    string description = row[idx++];
                    string iconPath = row[idx++];
                    ItemRarity rarity = ParseEnum<ItemRarity>(row[idx++]);
                    ItemFlags flags = ParseFlags(row[idx++]);
                    int buyPrice = int.Parse(row[idx++]);
                    int sellPrice = int.Parse(row[idx++]);
                    int maxStack = int.Parse(row[idx++]);
                    ToolType toolType = ParseEnum<ToolType>(row[idx++]);
                    int useCount = int.Parse(row[idx++]);
                    float cooldown = float.Parse(row[idx++]);

                    string assetPath = Path.Combine(folder, $"{itemName}_{itemId}.asset");
                    var item = CreateOrLoadAsset<Tool>(assetPath);
                    if (item == null) continue;

                    SetPrivateField(item, "itemId", itemId);
                    SetPrivateField(item, "itemName", itemName);
                    SetPrivateField(item, "description", description);
                    SetPrivateField(item, "icon", LoadSprite(iconPath));
                    SetPrivateField(item, "itemType", ItemType.Tool);
                    SetPrivateField(item, "rarity", rarity);
                    SetPrivateField(item, "flags", flags);
                    SetPrivateField(item, "buyPrice", buyPrice);
                    SetPrivateField(item, "sellPrice", sellPrice);
                    SetPrivateField(item, "maxStackSize", maxStack);
                    SetPrivateField(item, "toolType", toolType);
                    SetPrivateField(item, "useCount", useCount);
                    SetPrivateField(item, "cooldown", cooldown);

                    EditorUtility.SetDirty(item);
                    count++;
                }
                catch (Exception ex)
                {
                    Log($"工具导入错误: {ex.Message}");
                }
            }

            Log($"工具: 导入 {count} 个");
        }

        private void ImportMaterials()
        {
            var rows = ParseCSV(materialCsvPath);
            if (rows.Count == 0) { Log("材料: 无数据或文件不存在"); return; }

            string folder = Path.Combine(outputFolderPath, "Materials");
            EnsureDirectoryExists(folder);

            int count = 0;
            foreach (var row in rows)
            {
                try
                {
                    if (row.Length < 11) continue;

                    int idx = 0;
                    int itemId = int.Parse(row[idx++]);
                    string itemName = row[idx++];
                    string description = row[idx++];
                    string iconPath = row[idx++];
                    ItemRarity rarity = ParseEnum<ItemRarity>(row[idx++]);
                    ItemFlags flags = ParseFlags(row[idx++]);
                    int buyPrice = int.Parse(row[idx++]);
                    int sellPrice = int.Parse(row[idx++]);
                    int maxStack = int.Parse(row[idx++]);
                    MaterialCategory category = ParseEnum<MaterialCategory>(row[idx++]);
                    int tier = int.Parse(row[idx++]);

                    string assetPath = Path.Combine(folder, $"{itemName}_{itemId}.asset");
                    var item = CreateOrLoadAsset<Material>(assetPath);
                    if (item == null) continue;

                    SetPrivateField(item, "itemId", itemId);
                    SetPrivateField(item, "itemName", itemName);
                    SetPrivateField(item, "description", description);
                    SetPrivateField(item, "icon", LoadSprite(iconPath));
                    SetPrivateField(item, "itemType", ItemType.Material);
                    SetPrivateField(item, "rarity", rarity);
                    SetPrivateField(item, "flags", flags);
                    SetPrivateField(item, "buyPrice", buyPrice);
                    SetPrivateField(item, "sellPrice", sellPrice);
                    SetPrivateField(item, "maxStackSize", maxStack);
                    SetPrivateField(item, "category", category);
                    SetPrivateField(item, "tier", tier);

                    EditorUtility.SetDirty(item);
                    count++;
                }
                catch (Exception ex)
                {
                    Log($"材料导入错误: {ex.Message}");
                }
            }

            Log($"材料: 导入 {count} 个");
        }

        private void ImportCosmetics()
        {
            var rows = ParseCSV(cosmeticCsvPath);
            if (rows.Count == 0) { Log("服装: 无数据或文件不存在"); return; }

            string folder = Path.Combine(outputFolderPath, "Cosmetics");
            EnsureDirectoryExists(folder);

            int count = 0;
            foreach (var row in rows)
            {
                try
                {
                    if (row.Length < 13) continue;

                    int idx = 0;
                    int itemId = int.Parse(row[idx++]);
                    string itemName = row[idx++];
                    string description = row[idx++];
                    string iconPath = row[idx++];
                    ItemRarity rarity = ParseEnum<ItemRarity>(row[idx++]);
                    ItemFlags flags = ParseFlags(row[idx++]);
                    int buyPrice = int.Parse(row[idx++]);
                    int sellPrice = int.Parse(row[idx++]);
                    CosmeticSlot slot = ParseEnum<CosmeticSlot>(row[idx++]);
                    bool hasStats = bool.Parse(row[idx++]);
                    var statMods = ParseStatModifiers(row[idx++]);
                    Color tintColor = ParseColor(row[idx++]);

                    string assetPath = Path.Combine(folder, $"{itemName}_{itemId}.asset");
                    var item = CreateOrLoadAsset<Cosmetic>(assetPath);
                    if (item == null) continue;

                    SetPrivateField(item, "itemId", itemId);
                    SetPrivateField(item, "itemName", itemName);
                    SetPrivateField(item, "description", description);
                    SetPrivateField(item, "icon", LoadSprite(iconPath));
                    SetPrivateField(item, "itemType", ItemType.Cosmetic);
                    SetPrivateField(item, "rarity", rarity);
                    SetPrivateField(item, "flags", flags);
                    SetPrivateField(item, "buyPrice", buyPrice);
                    SetPrivateField(item, "sellPrice", sellPrice);
                    SetPrivateField(item, "slot", slot);
                    SetPrivateField(item, "hasStats", hasStats);
                    SetPrivateField(item, "statModifiers", statMods);
                    SetPrivateField(item, "tintColor", tintColor);

                    EditorUtility.SetDirty(item);
                    count++;
                }
                catch (Exception ex)
                {
                    Log($"服装导入错误: {ex.Message}");
                }
            }

            Log($"服装: 导入 {count} 个");
        }

        private void ImportQuestItems()
        {
            var rows = ParseCSV(questItemCsvPath);
            if (rows.Count == 0) { Log("剧情道具: 无数据或文件不存在"); return; }

            string folder = Path.Combine(outputFolderPath, "QuestItems");
            EnsureDirectoryExists(folder);

            int count = 0;
            foreach (var row in rows)
            {
                try
                {
                    if (row.Length < 9) continue;

                    int idx = 0;
                    int itemId = int.Parse(row[idx++]);
                    string itemName = row[idx++];
                    string description = row[idx++];
                    string iconPath = row[idx++];
                    ItemRarity rarity = ParseEnum<ItemRarity>(row[idx++]);
                    string questId = row[idx++];
                    QuestItemType questType = ParseEnum<QuestItemType>(row[idx++]);
                    bool autoRemove = bool.Parse(row[idx++]);
                    string loreText = row.Length > idx ? row[idx] : "";

                    string assetPath = Path.Combine(folder, $"{itemName}_{itemId}.asset");
                    var item = CreateOrLoadAsset<QuestItem>(assetPath);
                    if (item == null) continue;

                    // 剧情道具默认不可丢弃、不可出售
                    ItemFlags flags = ItemFlags.None;

                    SetPrivateField(item, "itemId", itemId);
                    SetPrivateField(item, "itemName", itemName);
                    SetPrivateField(item, "description", description);
                    SetPrivateField(item, "icon", LoadSprite(iconPath));
                    SetPrivateField(item, "itemType", ItemType.QuestItem);
                    SetPrivateField(item, "rarity", rarity);
                    SetPrivateField(item, "flags", flags);
                    SetPrivateField(item, "maxStackSize", 1);
                    SetPrivateField(item, "questId", questId);
                    SetPrivateField(item, "questItemType", questType);
                    SetPrivateField(item, "autoRemoveOnQuestComplete", autoRemove);
                    SetPrivateField(item, "loreText", loreText);

                    EditorUtility.SetDirty(item);
                    count++;
                }
                catch (Exception ex)
                {
                    Log($"剧情道具导入错误: {ex.Message}");
                }
            }

            Log($"剧情道具: 导入 {count} 个");
        }

        #endregion

        #region 辅助方法

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(obj, value);
                    return;
                }
                type = type.BaseType;
            }
        }

        private Sprite LoadSprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/{path}.png");
        }

        private Color ParseColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.white;
            if (ColorUtility.TryParseHtmlString($"#{hex}", out Color color))
                return color;
            return Color.white;
        }

        private StatModifier[] ParseStatModifiers(string str)
        {
            if (string.IsNullOrEmpty(str)) return Array.Empty<StatModifier>();

            var result = new List<StatModifier>();
            var parts = str.Split('|');

            foreach (var part in parts)
            {
                var segments = part.Split(':');
                if (segments.Length < 3) continue;

                if (Enum.TryParse<StatType>(segments[0], out var statType) &&
                    Enum.TryParse<ModifierType>(segments[1], out var modType) &&
                    float.TryParse(segments[2], out float value))
                {
                    result.Add(new StatModifier(statType, modType, value));
                }
            }

            return result.ToArray();
        }

        private AccessoryEffect[] ParseAccessoryEffects(string str)
        {
            if (string.IsNullOrEmpty(str)) return Array.Empty<AccessoryEffect>();

            var result = new List<AccessoryEffect>();
            var parts = str.Split('|');

            foreach (var part in parts)
            {
                var segments = part.Split(':');
                if (segments.Length < 3) continue;

                if (Enum.TryParse<AccessoryEffectType>(segments[0], out var effectType) &&
                    float.TryParse(segments[1], out float value))
                {
                    var effect = new AccessoryEffect
                    {
                        effectType = effectType,
                        value = value,
                        element = ParseEnum<ElementType>(segments[2])
                    };
                    result.Add(effect);
                }
            }

            return result.ToArray();
        }

        private BuffEffect[] ParseBuffEffects(string str)
        {
            if (string.IsNullOrEmpty(str)) return Array.Empty<BuffEffect>();

            var result = new List<BuffEffect>();
            var parts = str.Split('|');

            foreach (var part in parts)
            {
                var segments = part.Split(':');
                if (segments.Length < 3) continue;

                if (Enum.TryParse<BuffType>(segments[0], out var buffType) &&
                    float.TryParse(segments[1], out float value) &&
                    bool.TryParse(segments[2], out bool isPercent))
                {
                    result.Add(new BuffEffect
                    {
                        buffType = buffType,
                        value = value,
                        isPercentage = isPercent
                    });
                }
            }

            return result.ToArray();
        }

        private StatusEffectType[] ParseStatusEffectTypes(string str)
        {
            if (string.IsNullOrEmpty(str)) return Array.Empty<StatusEffectType>();

            return str.Split('|')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => ParseEnum<StatusEffectType>(s))
                .ToArray();
        }

        #endregion
    }
}
#endif