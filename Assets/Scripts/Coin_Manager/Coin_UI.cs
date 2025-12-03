using UnityEngine;
using TMPro;

public class CoinUI : MonoBehaviour
{
    [Header("金币文本显示")]
    public TextMeshProUGUI coinText;


    void OnEnable()
    {
        if (CoinManager.Instance != null)
        {
            UpdateCoinText(CoinManager.Instance.TotalCoins);

            CoinManager.Instance.OnCoinsChanged += UpdateCoinText;
        }
    }

    void OnDisable()
    {
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.OnCoinsChanged -= UpdateCoinText;
        }
    }

    void UpdateCoinText(int amount)
    {
        if (coinText != null)
        {
            coinText.text = amount.ToString();
        }
    }
}
