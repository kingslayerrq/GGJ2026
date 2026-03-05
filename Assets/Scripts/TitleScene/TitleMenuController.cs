using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenuController : MonoBehaviour
{
    

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "RQScene"; 

    private void Start()
    {
    }

    public void OnClickStartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    

}
