using UnityEngine;

[System.Serializable]
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    [Header("基本属性")]
    public float maxHealth = 100f;
    public float maxHunger = 100f;
    public float maxThirst = 100f;
    public float maxSanity = 100f;
    public float currentHealth = 100f;

    [Range(0, 100)] public float curHunger = 100f;
    [Range(0, 100)] public float curThirst = 100f;
    [Range(0, 100)] public float curSanity = 100f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        UpdateUI();
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UpdateUI();
    }

    // 其他状态更新方法...

    void UpdateUI()
    {
        // 通知UI更新
        if (PlayerCurr.Instance != null)
        {
            PlayerCurr.Instance.UpdateHealthUI(currentHealth, maxHealth);
        }
    }
}