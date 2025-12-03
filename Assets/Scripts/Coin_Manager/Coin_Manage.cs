using UnityEngine;
using System;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("当前金币数")]
    [SerializeField] private int totalCoins = 0;
    public int TotalCoins => totalCoins;

    // 当金币数量变化时触发，用于刷新 UI
    public event Action<int> OnCoinsChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 如果希望跨游戏局（退出再进）也保留，可以用 PlayerPrefs
        // totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        // OnCoinsChanged?.Invoke(totalCoins);
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        totalCoins += amount;
        OnCoinsChanged?.Invoke(totalCoins);

        // PlayerPrefs.SetInt("TotalCoins", totalCoins);
        // PlayerPrefs.Save();
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0) return false;
        if (totalCoins < amount) return false;

        totalCoins -= amount;
        OnCoinsChanged?.Invoke(totalCoins);

        // PlayerPrefs.SetInt("TotalCoins", totalCoins);
        // PlayerPrefs.Save();

        return true;
    }

    public void SetCoins(int value)
    {
        totalCoins = Mathf.Max(0, value);
        OnCoinsChanged?.Invoke(totalCoins);
    }
}
