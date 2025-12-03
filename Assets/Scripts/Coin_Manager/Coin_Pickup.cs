using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    [Header("这枚金币的价值")]
    public int coinValue = 1;

    [Header("可选：拾取音效 / 特效")]
    public AudioSource pickupSound;
    public GameObject pickupEffectPrefab;

    void OnTriggerEnter(Collider other)
    {
        // 确认是玩家
        if (!other.CompareTag("Player"))
            return;

        // 确认有 CoinManager
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(coinValue);
        }

        // 播放音效
        if (pickupSound != null)
        {
            pickupSound.Play();
            pickupSound.transform.parent = null; // 播完再销毁
            Destroy(pickupSound.gameObject, 3f);
        }

        // 生成特效
        if (pickupEffectPrefab != null)
        {
            Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
        }

        // 删除金币本体
        Destroy(gameObject);
    }
}
