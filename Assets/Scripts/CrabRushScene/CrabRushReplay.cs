using UnityEngine;
using UnityEngine.SceneManagement;

public class CrabRushReplay : MonoBehaviour
{
    [SerializeField] private string sceneName = "CrabRushScene";

    public void Replay()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}