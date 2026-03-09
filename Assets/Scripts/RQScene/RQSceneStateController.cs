using UnityEngine;

public class RQSceneStateController : MonoBehaviour
{
    [SerializeField] private GameObject crabAmbassador;
    [SerializeField] private GameObject crabHouse;

    private void Start()
    {
        if (crabAmbassador != null)
        {
            crabAmbassador.SetActive(GameProgress.crabCompleted);
        }

        if (crabHouse != null)
        {
            crabHouse.SetActive(GameProgress.crabCompleted);
        }
    }
}