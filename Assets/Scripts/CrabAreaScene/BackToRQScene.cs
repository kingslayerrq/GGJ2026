using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToMapScene : MonoBehaviour
{
    public void BackToMap()
    {
        SceneManager.LoadScene("MapScene");
    }
}