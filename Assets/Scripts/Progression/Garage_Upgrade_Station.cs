using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(20)]
public class GarageUpgradeStation : MonoBehaviour
{
    public enum UpgradeType
    {
        Speed,
        TrickSpeed,
        ExtraLife
    }

    [Header("Upgrade")]
    public UpgradeType upgradeType;

    [Tooltip("等级 0→1 的基础价格")]
    public int baseCost = 100;

    [Tooltip("每一级额外增加的价格：cost = baseCost + level * costPerLevel")]
    public int costPerLevel = 50;

    [Tooltip("0 = 无限等级")]
    public int maxLevel = 5;

    [Header("Interaction")]
    [Tooltip("用于检测交互的半径（可以和 WorldText.showRadius 保持差不多）")]
    public float interactRadius = 2.5f;

    public string playerTag = "Player";
    public Key interactKey = Key.E;

    [Header("UI")]
    public WorldText worldText;  // 挂在同一个物体上的 WorldText

    Transform player;

    void Start()
    {
        if (!worldText)
            worldText = GetComponent<WorldText>();

        FindPlayer();
        RefreshText();
    }

    void FindPlayer()
    {
        try
        {
            GameObject go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null)
                player = go.transform;
        }
        catch { }
    }

    void Update()
    {
        if (GameProgress.Instance == null) return;

        if (player == null)
            FindPlayer();
        if (player == null) return;

        // 距离检测（交互）
        float dist = Vector3.Distance(player.position, transform.position);
        if (dist > interactRadius) return;

        // 输入检测
        if (Keyboard.current == null) return;
        if (!Keyboard.current[interactKey].wasPressedThisFrame) return;

        TryPurchase();
        RefreshText();
    }

    int GetLevel()
    {
        var p = GameProgress.Instance;
        switch (upgradeType)
        {
            case UpgradeType.Speed: return p.speedLevel;
            case UpgradeType.TrickSpeed: return p.trickSpeedLevel;
            case UpgradeType.ExtraLife: return p.extraLifeLevel;
        }
        return 0;
    }

    void SetLevel(int newLevel)
    {
        var p = GameProgress.Instance;
        switch (upgradeType)
        {
            case UpgradeType.Speed: p.speedLevel = newLevel; break;
            case UpgradeType.TrickSpeed: p.trickSpeedLevel = newLevel; break;
            case UpgradeType.ExtraLife: p.extraLifeLevel = newLevel; break;
        }
        p.Save();
    }

    int GetCost(int currentLevel)
    {
        return Mathf.Max(0, baseCost + currentLevel * costPerLevel);
    }

    void TryPurchase()
    {
        var p = GameProgress.Instance;
        int level = GetLevel();

        if (maxLevel > 0 && level >= maxLevel)
        {
            Debug.Log($"[{name}] Max level for {upgradeType} already reached.");
            return;
        }

        int cost = GetCost(level);

        if (!p.TrySpendCoins(cost))
        {
            Debug.Log($"[{name}] Not enough coins. Need {cost}, have {p.coins}");
            return;
        }

        SetLevel(level + 1);
        Debug.Log($"[{name}] Bought {upgradeType} Lv.{level + 1} for {cost} coins.");
    }

    void RefreshText()
    {
        if (!worldText || GameProgress.Instance == null) return;

        int level = GetLevel();
        int cost = GetCost(level);
        int coins = GameProgress.Instance.coins;

        string label = upgradeType switch
        {
            UpgradeType.Speed => "Upgrade Speed",
            UpgradeType.TrickSpeed => "Upgrade Trick Speed",
            UpgradeType.ExtraLife => "Buy Extra Life",
            _ => "Upgrade"
        };

        // ---- 文本根据类型区分 ----
        bool isExtraLife = (upgradeType == UpgradeType.ExtraLife);

        if (maxLevel > 0 && level >= maxLevel)
        {
            // 达到上限
            if (isExtraLife)
            {
                worldText.textContent =
                    $"{label}\n" +
                    $"Max Owned: {level}";
            }
            else
            {
                worldText.textContent =
                    $"{label}\n" +
                    $"Max Level Reached (Lv.{level})";
            }
        }
        else
        {
            // 还可以继续购买 / 升级
            if (isExtraLife)
            {
                worldText.textContent =
                    $"Press E to {label}\n" +
                    $"Cost: {cost} Coins (You: {coins})\n" +
                    $"Current Owned: {level}";
            }
            else
            {
                worldText.textContent =
                    $"Press E to {label}\n" +
                    $"Cost: {cost} Coins (You: {coins})\n" +
                    $"Current Level: {level}";
            }
        }
    }
}
