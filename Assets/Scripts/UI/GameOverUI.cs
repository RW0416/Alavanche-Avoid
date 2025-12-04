using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Controls the Game Over panel and its buttons.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Panel Root")]
    public GameObject panelRoot;

    [Header("Game Over Texts")]
    public TMP_Text gameOverText;      // GameOverText
    public TMP_Text finalScoreText;    // FinalScore
    public TMP_Text finalTimeText;     // FinalTime

    [Header("Buttons")]
    public GameObject restartButton;   // Restart button object
    public GameObject extraLifeButton; // Extra Life button object
    public TMP_Text extraLifeCountText;   // (可选) 显示剩余复活次数

    [Header("HUD References")]
    public TMP_Text hudScoreText;      // Canvas 里 HUD/Score 的 Text
    public TMP_Text hudTimeText;       // Canvas 里 HUD/Time 的 Text

    [Header("Optional")]
    public CanvasGroup canvasGroup;    // 加在 panelRoot 上的 CanvasGroup（可选）

    PlayerLifeSystem lifeSystem;

    void Awake()
    {
        lifeSystem = FindFirstObjectByType<PlayerLifeSystem>();

        if (canvasGroup == null && panelRoot != null)
            canvasGroup = panelRoot.GetComponent<CanvasGroup>();

        // 初始隐藏面板
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// 显示 Game Over 面板。
    /// canUseExtraLife 决定是否显示复活按钮（金币不够的情况，在外面把它传 false）。
    /// </summary>
    public void ShowGameOver(bool canUseExtraLife, int extraLivesRemaining)
    {
        Debug.Log($"[GameOverUI] ShowGameOver, canUseExtraLife = {canUseExtraLife}, lives = {extraLivesRemaining}");

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // ---- 文本显示 ----
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
            gameOverText.text = "GAME OVER";
        }

        if (finalScoreText != null)
        {
            finalScoreText.gameObject.SetActive(true);
            finalScoreText.text = BuildFinalScoreString();
        }

        if (finalTimeText != null)
        {
            finalTimeText.gameObject.SetActive(true);
            finalTimeText.text = BuildFinalTimeString();
        }

        // ---- 按钮显示 ----
        if (restartButton != null)
            restartButton.SetActive(true);

        if (extraLifeButton != null)
            extraLifeButton.SetActive(canUseExtraLife);

        if (extraLifeCountText != null)
            extraLifeCountText.text = "Extra Lives: " + extraLivesRemaining;

        // ---- 设置键盘/手柄的默认焦点 ----
        if (EventSystem.current != null)
        {
            GameObject focus = null;

            if (canUseExtraLife && extraLifeButton != null && extraLifeButton.activeInHierarchy)
                focus = extraLifeButton;
            else if (restartButton != null && restartButton.activeInHierarchy)
                focus = restartButton;
            else
                focus = panelRoot;

            EventSystem.current.SetSelectedGameObject(focus);
        }
    }

    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0f;
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    // 挂在 Extra Life 按钮的 OnClick 上
    public void OnUseExtraLifePressed()
    {
        Debug.Log("[GameOverUI] UseExtraLife button pressed.");

        if (lifeSystem != null)
            lifeSystem.UseExtraLife();

        Hide();
    }

    // 挂在 Restart 按钮的 OnClick 上
    public void OnRestartToGaragePressed()
    {
        Debug.Log("[GameOverUI] Restart/BackToGarage button pressed.");

        ScoreManager sm = ScoreManager.Instance;
        if (sm != null)
        {
            GameProgress.Instance.TrySetHighScore(sm.CurrentScore);
        }

        Time.timeScale = 1f;   // 重新加载前把时间恢复正常
        SceneManager.LoadScene("Garage Scene");
    }

    // ---------- Helper 函数 ----------

    string BuildFinalScoreString()
    {
        // 直接读 HUD 上的 Score 文本，避免改 ScoreManager
        if (hudScoreText != null)
        {
            // 比如 HUD 文本是 "Score: 1234"
            // 我们想显示 "Final Score: 1234"
            string raw = hudScoreText.text;
            // 简单处理一下前缀
            raw = raw.Replace("Score:", "").Trim();
            return "Final Score: " + raw;
        }

        return "Final Score: 0";
    }

    string BuildFinalTimeString()
    {
        // 同理，从 HUD/Time 文本里读
        if (hudTimeText != null)
        {
            // 假设 HUD 上已经是 "00:45" 或 "Time: 00:45" 之类
            string raw = hudTimeText.text;

            // 如果前面有 "Time:" 就去掉一下
            raw = raw.Replace("Time:", "").Trim();

            return "Time: " + raw;
        }

        return "Time: 0";
    }
}
