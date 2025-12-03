using UnityEngine;

public class SpeedScoreGenerator : MonoBehaviour
{
    [Header("References")]
    public SnowboarderController controller; // 拖玩家身上的 SnowboarderController
    public Transform scoreTarget;            // 飘字位置(通常拖玩家Transform)

    [Header("Score Settings")]
    [Tooltip("每秒得分 ≈ speed × multiplier")]
    public float scoreMultiplier = 5f;

    [Header("Options")]
    [Tooltip("是否每次加分都生成飘字")]
    public bool useFloatingText = false;

    float accumulatedScore = 0f;  // 累计得分(小数)
    int lastSentScore = 0;        // 已经发给ScoreManager的整数分
    bool isGameOver = false;

    void Update()
    {
        if (isGameOver) return;
        if (controller == null) return;
        if (ScoreManager.Instance == null) return;

        // 1. 获取速度：来自SnowboarderController的刚体
        float speed = controller.Body.linearVelocity.magnitude;

        // 2. 按速度累积分数
        accumulatedScore += speed * scoreMultiplier * Time.deltaTime;

        // 3. 只在“整分变化”时发给ScoreManager
        int totalInt = Mathf.FloorToInt(accumulatedScore);
        int delta = totalInt - lastSentScore;

        if (delta > 0)
        {
            Transform target = useFloatingText ? scoreTarget : null;

            // 喂分给ScoreManager
            ScoreManager.Instance.AddScore(delta, target);

            lastSentScore = totalInt;
        }
    }

    // ======== GameOver Hooks =========

    public void SetGameOver(bool value)
    {
        isGameOver = value;
    }

    public void ResetRun()
    {
        isGameOver = false;
        accumulatedScore = 0f;
        lastSentScore = 0;
    }
}
