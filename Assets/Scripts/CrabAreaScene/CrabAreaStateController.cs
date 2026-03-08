using UnityEngine;

public class CrabAreaStateController : MonoBehaviour
{
    [SerializeField] private GameObject unfinishedObjects;
    [SerializeField] private GameObject completedObjects;

    private void Start()
    {
        if (GameProgress.crabCompleted)
        {
            unfinishedObjects.SetActive(false);
            completedObjects.SetActive(true);
        }
        else
        {
            unfinishedObjects.SetActive(true);
            completedObjects.SetActive(false);
        }
    }
}