using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class CoinPickup : MonoBehaviour
{
    [Header("这枚金币的价值")]
    public int coinValue = 1;

    [Header("可选：拾取音效 / 特效")]
    public AudioSource pickupSound;
    public GameObject pickupEffectPrefab;

    void OnTriggerEnter(Collider other)
    {

        if (!other.CompareTag("Player")) return;
        if (GameProgress.Instance != null)
        {
            GameProgress.Instance.AddCoins(coinValue);
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
