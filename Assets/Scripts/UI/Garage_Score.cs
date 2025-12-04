using UnityEngine;
using TMPro;

public class GarageHighestScoreUI : MonoBehaviour
{
    public TMP_Text highScoreText;

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (GameProgress.Instance != null)
        {
            highScoreText.text = "Highest Score: " + GameProgress.Instance.highestScore;
        }
    }
}
