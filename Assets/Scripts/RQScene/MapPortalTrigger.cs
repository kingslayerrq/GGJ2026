using UnityEngine;
using UnityEngine.SceneManagement;

public class MapPortalTrigger : MonoBehaviour
{
    [SerializeField] private string mapSceneName = "MapScene";
    [SerializeField] private string returnSpawnPointName = "Spawn_MapReturn";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SceneSpawnData.returnSpawnPointName = returnSpawnPointName;
            SceneManager.LoadScene(mapSceneName);
        }
    }
}