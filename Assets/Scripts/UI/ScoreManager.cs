using UnityEngine;
using TMPro;   // <- TMP

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("UI")]
    public TMP_Text scoreText;              // TextMeshProUGUI on Canvas

    [Header("Floating Text")]
    public GameObject floatingTextPrefab;   // prefab with FloatingText + TMP_Text

    int score = 0;
    public int CurrentScore => score;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // NOTE: now takes a Transform, not a Vector3
    public void AddScore(int amount, Transform target)
    {
        score += amount;

        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }

        if (floatingTextPrefab != null && target != null)
        {
            GameObject go = Instantiate(
                floatingTextPrefab,
                target.position,
                Quaternion.identity
            );

            FloatingText ft = go.GetComponent<FloatingText>();
            if (ft != null)
            {
                ft.Initialize("+" + amount, target);
            }
        }
    }
}
