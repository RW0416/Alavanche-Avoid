using UnityEngine;

public class GameProgress : MonoBehaviour
{
    public static GameProgress Instance { get; private set; }

    [Header("Currency")]
    public int coins = 0;

    [Header("Upgrade Levels")]
    public int speedLevel = 0; 
    public int trickSpeedLevel = 0;  
    public int extraLifeLevel = 0;  

    [Header("Upgrade Effect Per Level")]
    [Tooltip("speed = +10%")]
    public float speedPerLevel = 0.1f;

    [Tooltip("trickSpeed = +10%")]
    public float trickSpeedPerLevel = 0.1f;

    [Header("Score")]
    public int highestScore = 0;

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

    void NotifyCoinsChanged()
    {
        OnCoinsChanged?.Invoke(coins);
    }

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

    public float GetSpeedMultiplier()
    {
        return 1f + speedLevel * speedPerLevel;
    }

    public float GetTrickSpeedMultiplier()
    {
        return 1f + trickSpeedLevel * trickSpeedPerLevel;
    }

    const string CoinsKey = "gp_coins";
    const string SpeedKey = "gp_speedLevel";
    const string TrickKey = "gp_trickSpeedLevel";
    const string ExtraLifeKey = "gp_extraLifeLevel";
    const string HighScoreKey = "gp_highScore";
    const string IntroWatchedKey = "IntroCutsceneWatched"; 



    public static void WipeAllProgress()
    {
        PlayerPrefs.DeleteKey(CoinsKey);
        PlayerPrefs.DeleteKey(SpeedKey);
        PlayerPrefs.DeleteKey(TrickKey);
        PlayerPrefs.DeleteKey(ExtraLifeKey);
        PlayerPrefs.DeleteKey(HighScoreKey);
        PlayerPrefs.DeleteKey(IntroWatchedKey);
        PlayerPrefs.Save();

        if (Instance != null)
        {
            Instance.coins = 0;
            Instance.speedLevel = 0;
            Instance.trickSpeedLevel = 0;
            Instance.extraLifeLevel = 0;
            Instance.highestScore = 0;
        }
    }
    public static bool HasAnySave()
    {
        return PlayerPrefs.HasKey(CoinsKey) ||
               PlayerPrefs.HasKey(SpeedKey) ||
               PlayerPrefs.HasKey(TrickKey) ||
               PlayerPrefs.HasKey(ExtraLifeKey);
    }

    public void Save()
    {
        PlayerPrefs.SetInt(CoinsKey, coins);
        PlayerPrefs.SetInt(SpeedKey, speedLevel);
        PlayerPrefs.SetInt(TrickKey, trickSpeedLevel);
        PlayerPrefs.SetInt(ExtraLifeKey, extraLifeLevel);
        PlayerPrefs.SetInt(HighScoreKey, highestScore);
        PlayerPrefs.Save();
    }


    void Load()
    {
        coins = PlayerPrefs.GetInt(CoinsKey, 0);
        speedLevel = PlayerPrefs.GetInt(SpeedKey, 0);
        trickSpeedLevel = PlayerPrefs.GetInt(TrickKey, 0);
        extraLifeLevel = PlayerPrefs.GetInt(ExtraLifeKey, 0);
        highestScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    public void TrySetHighScore(int newScore)
    {
        if (newScore > highestScore)
        {
            highestScore = newScore;
            Save();
            Debug.Log($"[GameProgress] New Highest Score = {highestScore}");
        }
    }


}
