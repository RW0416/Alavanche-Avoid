using UnityEngine;
using UnityEngine.SceneManagement;

public class QuickReturnToGarage : MonoBehaviour
{
    [Tooltip("你的 GarageScene 名字（确保添加到 Build Settings）")]
    public string garageSceneName = "Garage Scene";

    void Update()
    {
        // 按下 B
        if (Input.GetKeyDown(KeyCode.B))
        {
            ReturnToGarage();
        }
    }

    public void ReturnToGarage()
    {
        // 重设时间加速（如果游戏被暂停了）
        Time.timeScale = 1f;

        SceneManager.LoadScene(garageSceneName);
    }
}
