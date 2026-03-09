using UnityEngine;
using UnityEngine.SceneManagement;

public class CrabLeaderInteraction : MonoBehaviour, IInteractable
{
    [SerializeField] private string returnSceneName = "RQScene";
    [SerializeField] private string returnSpawnPointName = "Spawn_CrabReturn";

    public void Interact()
    {
        if (GameProgress.crabCompleted)
        {
            Debug.Log("Crab Leader: Hi my lovely soldier!");
            return;
        }
        if (CrabQuestManager.Instance == null)
        {
            Debug.LogWarning("CrabQuestManager not found.");
            return;
        }

        if (!CrabQuestManager.Instance.QuestAccepted)
        {
            CrabQuestManager.Instance.AcceptQuest();
            Debug.Log("Crab Leader: Please help us clear the trash.");
            return;
        }

        if (!CrabQuestManager.Instance.AllTrashCleared)
        {
            Debug.Log("Crab Leader: There are still " + CrabQuestManager.Instance.RemainingTrash + " trash left.");
            return;
        }
        

        CompleteQuestAndReturn();
    }

    private void CompleteQuestAndReturn()
    {
        GameProgress.crabCompleted = true;

        SceneSpawnData.returnSpawnPointName = returnSpawnPointName;
        SceneManager.LoadScene(returnSceneName);
    }
}