using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 主選單：點畫面任何位置就切換到下一個場景
/// Main menu: click anywhere on screen to load the next scene
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Tooltip("要切換到的場景名稱（需已加入 Build Settings）")]
    [SerializeField] private string sceneToLoad;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            SceneManager.LoadScene(sceneToLoad);
    }
}