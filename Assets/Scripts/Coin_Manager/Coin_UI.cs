using UnityEngine;
using TMPro;

public class CoinUI : MonoBehaviour
{
    [Header("金币文本显示")]
    public TextMeshProUGUI coinText;

    int lastShownValue = int.MinValue;

    void Update()
    {
        // 1. 没有 GameProgress 或 没有绑定文本，直接退出
        if (GameProgress.Instance == null || coinText == null)
            return;

        int current = GameProgress.Instance.coins;

        // 2. 只有数值变化时才更新 UI（避免每帧改文本）
        if (current != lastShownValue)
        {
            lastShownValue = current;
            coinText.text = current.ToString();
        }
    }
}
