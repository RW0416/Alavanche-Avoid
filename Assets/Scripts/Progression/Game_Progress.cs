using UnityEngine;

public class GameProgress : MonoBehaviour
{
    public static GameProgress Instance { get; private set; }

    [Header("Currency")]
    public int coins = 0;

    [Header("Upgrade Levels")]
    public int speedLevel = 0;       // 影响移动速度
    public int trickSpeedLevel = 0;  // 影响flip/SPIN速度
    public int extraLifeLevel = 0;   // 额外生命（之后接到死亡逻辑）

    [Header("Upgrade Effect Per Level")]
    [Tooltip("每一级速度加成，0.1 = +10%")]
    public float speedPerLevel = 0.1f;

    [Tooltip("每一级trick速度加成，0.1 = +10%")]
    public float trickSpeedPerLevel = 0.1f;

    // ======== 事件：金币变化时通知 UI ========
    public System.Action<int> OnCoinsChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    // 小工具：触发事件
    void NotifyCoinsChanged()
    {
        OnCoinsChanged?.Invoke(coins);
    }

    // ======== 金币 ========
    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        coins += amount;
        Save();
        NotifyCoinsChanged();
    }

    public bool TrySpendCoins(int cost)
    {
        if (cost <= 0) return true;
        if (coins < cost) return false;

        coins -= cost;
        Save();
        NotifyCoinsChanged();
        return true;
    }

    // ======== 升级倍率 ========
    public float GetSpeedMultiplier()
    {
        return 1f + speedLevel * speedPerLevel;
    }

    public float GetTrickSpeedMultiplier()
    {
        return 1f + trickSpeedLevel * trickSpeedPerLevel;
    }

    // ======== 存档 / 读档 ========
    const string CoinsKey = "gp_coins";
    const string SpeedKey = "gp_speedLevel";
    const string TrickKey = "gp_trickSpeedLevel";
    const string ExtraLifeKey = "gp_extraLifeLevel";

    public void Save()
    {
        PlayerPrefs.SetInt(CoinsKey, coins);
        PlayerPrefs.SetInt(SpeedKey, speedLevel);
        PlayerPrefs.SetInt(TrickKey, trickSpeedLevel);
        PlayerPrefs.SetInt(ExtraLifeKey, extraLifeLevel);
        PlayerPrefs.Save();
    }

    void Load()
    {
        coins = PlayerPrefs.GetInt(CoinsKey, 0);
        speedLevel = PlayerPrefs.GetInt(SpeedKey, 0);
        trickSpeedLevel = PlayerPrefs.GetInt(TrickKey, 0);
        extraLifeLevel = PlayerPrefs.GetInt(ExtraLifeKey, 0);
    }
}
