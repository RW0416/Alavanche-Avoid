using UnityEngine;
using TMPro;

public class SurvivalTimerUI : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("场景里显示时间的 TextMeshPro 文本")]
    public TMP_Text timeText;

    // 生存时间（秒）
    public float TimePassed { get; private set; } = 0f;

    bool isGameOver = false;

    void Update()
    {
        if (isGameOver) return;
        if (timeText == null) return;

        // 累计时间
        TimePassed += Time.deltaTime;

        // 格式化成 mm:ss
        int minutes = Mathf.FloorToInt(TimePassed / 60f);
        int seconds = Mathf.FloorToInt(TimePassed % 60f);

        timeText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
    }

    /// <summary>
    /// 游戏结束时调用，停止计时
    /// </summary>
    public void SetGameOver(bool value)
    {
        isGameOver = value;
    }

    /// <summary>
    /// 开始新一局时调用，重置计时
    /// </summary>
    public void ResetTimer()
    {
        isGameOver = false;
        TimePassed = 0f;

        if (timeText != null)
        {
            timeText.text = "Time: 00:00";
        }
    }
}
