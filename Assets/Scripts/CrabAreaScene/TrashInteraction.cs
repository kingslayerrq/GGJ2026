using UnityEngine;

public class TrashInteraction : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        if (CrabQuestManager.Instance != null)
        {
            CrabQuestManager.Instance.ClearOneTrash();
        }

        Destroy(gameObject);
    }
}