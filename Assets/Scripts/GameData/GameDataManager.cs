// ============================================
// GameDataManager.cs - 游戏数据管理器
// ============================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 游戏数据管理器 - 负责加载和查询所有游戏数据
/// </summary>
public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    [Header("数据路径")]
    [SerializeField] private string characterDataPath = "GameData/Characters";
    [SerializeField] private string enemyDataPath = "GameData/Enemies";
    [SerializeField] private string weaponDataPath = "GameData/Weapons";
    [SerializeField] private string accessoryDataPath = "GameData/Accessories";
    [SerializeField] private string skillDataPath = "GameData/Skills";
    [SerializeField] private string attachmentDataPath = "GameData/Attachments";

    // 数据缓存
    private Dictionary<string, CharacterData> characterDataCache;
    private Dictionary<string, EnemyData> enemyDataCache;
    private Dictionary<string, WeaponData> weaponDataCache;
    private Dictionary<string, AccessoryData> accessoryDataCache;
    private Dictionary<string, SkillData> skillDataCache;
    private Dictionary<string, AttachmentData> attachmentDataCache;

    // 按类型分类的索引
    private Dictionary<string, List<EnemyData>> enemiesByRace;
    private Dictionary<WeaponType, List<WeaponData>> weaponsByType;
    private Dictionary<ItemRarity, List<WeaponData>> weaponsByRarity;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 加载所有数据
    /// </summary>
    private void LoadAllData()
    {
        LoadCharacterData();
        LoadEnemyData();
        LoadWeaponData();
        LoadAccessoryData();
        LoadSkillData();
        LoadAttachmentData();

        BuildIndices();

        Debug.Log($"[GameDataManager] 数据加载完成：" +
                  $"角色={characterDataCache.Count}, " +
                  $"敌人={enemyDataCache.Count}, " +
                  $"武器={weaponDataCache.Count}, " +
                  $"饰品={accessoryDataCache.Count}, " +
                  $"技能={skillDataCache.Count}");
    }

    private void LoadCharacterData()
    {
        characterDataCache = new Dictionary<string, CharacterData>();
        var allData = Resources.LoadAll<CharacterData>(characterDataPath);
        foreach (var data in allData)
        {
            if (!string.IsNullOrEmpty(data.characterID))
            {
                characterDataCache[data.characterID] = data;
            }
        }
    }

    private void LoadEnemyData()
    {
        enemyDataCache = new Dictionary<string, EnemyData>();
        var allData = Resources.LoadAll<EnemyData>(enemyDataPath);
        foreach (var data in allData)
        {
            if (!string.IsNullOrEmpty(data.enemyID))
            {
                enemyDataCache[data.enemyID] = data;
            }
        }
    }

    private void LoadWeaponData()
    {
        weaponDataCache = new Dictionary<string, WeaponData>();
        var allData = Resources.LoadAll<WeaponData>(weaponDataPath);
        foreach (var data in allData)
        {
            if (!string.IsNullOrEmpty(data.weaponID))
            {
                weaponDataCache[data.weaponID] = data;
            }
        }
        Debug.Log($"已加载 {weaponDataCache.Count} 个武器数据");
    }

    private void LoadAccessoryData()
    {
        accessoryDataCache = new Dictionary<string, AccessoryData>();
        var allData = Resources.LoadAll<AccessoryData>(accessoryDataPath);
        foreach (var data in allData)
        {
            if (!string.IsNullOrEmpty(data.accessoryID))
            {
                accessoryDataCache[data.accessoryID] = data;
            }
        }
    }

    private void LoadSkillData()
    {
        skillDataCache = new Dictionary<string, SkillData>();
        var allData = Resources.LoadAll<SkillData>(skillDataPath);
        foreach (var data in allData)
        {
            if (!string.IsNullOrEmpty(data.skillID))
            {
                skillDataCache[data.skillID] = data;
            }
        }
    }
    /// <summary>
    /// 加载所有配件数据
    /// </summary>
    private void LoadAttachmentData()
    {
        attachmentDataCache.Clear();
        var attachments = Resources.LoadAll<AttachmentData>(attachmentDataPath);

        foreach (var attachment in attachments)
        {
            if (!string.IsNullOrEmpty(attachment.attachmentID))
            {
                attachmentDataCache[attachment.attachmentID] = attachment;
            }
        }

        Debug.Log($"已加载 {attachmentDataCache.Count} 个配件数据");
    }

    /// <summary>
    /// 构建索引以便快速查询
    /// </summary>
    private void BuildIndices()
    {
        // 按种族分类敌人
        enemiesByRace = new Dictionary<string, List<EnemyData>>();
        foreach (var enemy in enemyDataCache.Values)
        {
            if (!enemiesByRace.ContainsKey(enemy.raceName))
            {
                enemiesByRace[enemy.raceName] = new List<EnemyData>();
            }
            enemiesByRace[enemy.raceName].Add(enemy);
        }

        // 按类型分类武器
        weaponsByType = new Dictionary<WeaponType, List<WeaponData>>();
        weaponsByRarity = new Dictionary<ItemRarity, List<WeaponData>>();

        foreach (var weapon in weaponDataCache.Values)
        {
            if (!weaponsByType.ContainsKey(weapon.weaponType))
            {
                weaponsByType[weapon.weaponType] = new List<WeaponData>();
            }
            weaponsByType[weapon.weaponType].Add(weapon);

            if (!weaponsByRarity.ContainsKey(weapon.rarity))
            {
                weaponsByRarity[weapon.rarity] = new List<WeaponData>();
            }
            weaponsByRarity[weapon.rarity].Add(weapon);
        }
    }

    #region 查询方法

    // ===== 角色 =====
    public CharacterData GetCharacterData(string id) =>
        characterDataCache.TryGetValue(id, out var data) ? data : null;

    public List<CharacterData> GetAllCharacterData() =>
        characterDataCache.Values.ToList();

    public Character CreateCharacter(string id, int level = 1)
    {
        var data = GetCharacterData(id);
        return data?.CreateInstance(level);
    }

    // ===== 敌人 =====
    public EnemyData GetEnemyData(string id) =>
        enemyDataCache.TryGetValue(id, out var data) ? data : null;

    public List<EnemyData> GetEnemiesByRace(string race) =>
        enemiesByRace.TryGetValue(race, out var list) ? list : new List<EnemyData>();

    public List<EnemyData> GetBossEnemies() =>
        enemyDataCache.Values.Where(e => e.isBoss).ToList();

    public List<EnemyData> GetEliteEnemies() =>
        enemyDataCache.Values.Where(e => e.isElite).ToList();

    public Character CreateEnemy(string id, int level)
    {
        var data = GetEnemyData(id);
        return data?.CreateInstance(level);
    }

    /// <summary>
    /// 获取适合指定等级的随机敌人
    /// </summary>
    public EnemyData GetRandomEnemyForLevel(int level, bool includeBoss = false, bool includeElite = true)
    {
        var candidates = enemyDataCache.Values
            .Where(e => level >= e.minLevel && level <= e.maxLevel)
            .Where(e => includeBoss || !e.isBoss)
            .Where(e => includeElite || !e.isElite)
            .ToList();

        if (candidates.Count == 0) return null;
        return candidates[CombatRandom.Range(0, candidates.Count)];
    }

    // ===== 武器 =====
    public WeaponData GetWeaponData(string id) =>
        weaponDataCache.TryGetValue(id, out var data) ? data : null;

    public List<WeaponData> GetWeaponsByType(WeaponType type) =>
        weaponsByType.TryGetValue(type, out var list) ? list : new List<WeaponData>();

    public List<WeaponData> GetWeaponsByRarity(ItemRarity rarity) =>
        weaponsByRarity.TryGetValue(rarity, out var list) ? list : new List<WeaponData>();

    public Weapon CreateWeapon(string id)
    {
        var data = GetWeaponData(id);
        return data?.CreateInstance();
    }

    /// <summary>
    /// 获取随机武器
    /// </summary>
    public WeaponData GetRandomWeapon(ItemRarity? rarity = null, WeaponType? type = null)
    {
        var candidates = weaponDataCache.Values.AsEnumerable();

        if (rarity.HasValue)
            candidates = candidates.Where(w => w.rarity == rarity.Value);
        if (type.HasValue)
            candidates = candidates.Where(w => w.weaponType == type.Value);

        var list = candidates.ToList();
        if (list.Count == 0) return null;
        return list[CombatRandom.Range(0, list.Count)];
    }

    public IEnumerable<WeaponData> GetAllWeaponData()
    {
        return weaponDataCache.Values;
    }

    // ===== 饰品 =====
    public AccessoryData GetAccessoryData(string id) =>
        accessoryDataCache.TryGetValue(id, out var data) ? data : null;

    public Accessory CreateAccessory(string id)
    {
        var data = GetAccessoryData(id);
        return data?.CreateInstance();
    }

    // ===== 技能 =====
    public SkillData GetSkillData(string id) =>
        skillDataCache.TryGetValue(id, out var data) ? data : null;

    public Skill CreateSkill(string id)
    {
        var data = GetSkillData(id);
        return data?.CreateInstance();
    }
    // ===== 配件 =====
    /// <summary>
    /// 获取配件数据
    /// </summary>
    public AttachmentData GetAttachmentData(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return attachmentDataCache.TryGetValue(id, out var data) ? data : null;
    }

    /// <summary>
    /// 获取所有配件数据
    /// </summary>
    public IEnumerable<AttachmentData> GetAllAttachmentData()
    {
        return attachmentDataCache.Values;
    }

    /// <summary>
    /// 根据类型筛选配件
    /// </summary>
    public List<AttachmentData> GetAttachmentsByType(AttachmentType type)
    {
        var result = new List<AttachmentData>();
        foreach (var attachment in attachmentDataCache.Values)
        {
            if (attachment.attachmentType == type)
            {
                result.Add(attachment);
            }
        }
        return result;
    }

    /// <summary>
    /// 获取与指定武器兼容的配件
    /// </summary>
    public List<AttachmentData> GetCompatibleAttachments(Weapon weapon)
    {
        if (weapon == null) return new List<AttachmentData>();

        var result = new List<AttachmentData>();
        var weaponFlag = weapon.GetWeaponTypeFlag();

        foreach (var attachment in attachmentDataCache.Values)
        {
            if (attachment.IsCompatibleWith(weaponFlag))
            {
                result.Add(attachment);
            }
        }
        return result;
    }
    #endregion

    #region 工具方法
    /// <summary>
    /// 重新加载所有数据
    /// </summary>
    public void ReloadAllData()
    {
        LoadAllData();
    }

    /// <summary>
    /// 检查武器ID是否存在
    /// </summary>
    public bool HasWeapon(string id)
    {
        return !string.IsNullOrEmpty(id) && weaponDataCache.ContainsKey(id);
    }

    /// <summary>
    /// 检查配件ID是否存在
    /// </summary>
    public bool HasAttachment(string id)
    {
        return !string.IsNullOrEmpty(id) && attachmentDataCache.ContainsKey(id);
    }
    #endregion
}