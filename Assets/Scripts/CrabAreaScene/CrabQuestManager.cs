using UnityEngine;

public class CrabQuestManager : MonoBehaviour
{
    public static CrabQuestManager Instance { get; private set; }

    [Header("Quest State")]
    [SerializeField] private bool questAccepted = false;
    [SerializeField] private int remainingTrash = 10;

    [Header("Trash Root")]
    [SerializeField] private GameObject trashGroup;

    public bool QuestAccepted => questAccepted;
    public int RemainingTrash => remainingTrash;
    public bool AllTrashCleared => questAccepted && remainingTrash <= 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (trashGroup != null)
        {
            trashGroup.SetActive(false);
        }
    }

    public void AcceptQuest()
    {
        if (questAccepted) return;

        questAccepted = true;
        remainingTrash = 10;

        if (trashGroup != null)
        {
            trashGroup.SetActive(true);
        }

        Debug.Log("Crab quest accepted. 10 trash remaining.");
    }

    public void ClearOneTrash()
    {
        if (!questAccepted) return;

        remainingTrash--;
        if (remainingTrash < 0)
            remainingTrash = 0;

        Debug.Log("Trash cleared. Remaining: " + remainingTrash);

        if (remainingTrash == 0)
        {
            Debug.Log("All trash cleared. Return to the Crab Leader.");
        }
    }

    public void CompleteQuest()
    {
        if (!AllTrashCleared) return;

        GameProgress.crabCompleted = true;
        Debug.Log("Crab quest completed!");
    }
}