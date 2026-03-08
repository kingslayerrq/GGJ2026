using UnityEngine;
using UnityEngine.SceneManagement;

public class MapUI : MonoBehaviour
{
    public void OnClickClose()
    {
        Debug.Log("Close clicked");
        SceneManager.LoadScene("RQScene");
    }
    public void OnClickCrabArea()
    {
        Debug.Log("Crab Area clicked");
        if (GameProgress.crabCompleted)
        {
            SceneManager.LoadScene("CrabAreaScene");
        }
        else
        {
            PlayerEnterState.movementMode = PlayerMovementMode.VerticalOnly;
            SceneManager.LoadScene("CrabRushScene");
        }
    }

    public void OnClickDolphinArea()
    {
        Debug.Log("Dolphin Area clicked");
    }

    public void OnClickSharkArea()
    {
        Debug.Log("Shark Area clicked");
    }

    public void OnClickOctopusArea()
    {
        Debug.Log("Octopus Area clicked");
    }

    public void OnClickSeaCucumberArea()
    {
        Debug.Log("Sea Cucumber Area clicked");
    }
}