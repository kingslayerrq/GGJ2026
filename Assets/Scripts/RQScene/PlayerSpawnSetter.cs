using UnityEngine;

public class PlayerSpawnSetter : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;

    private void Start()
    {
        if (string.IsNullOrEmpty(SceneSpawnData.returnSpawnPointName))
            return;

        GameObject spawnPoint = GameObject.Find(SceneSpawnData.returnSpawnPointName);

        if (spawnPoint != null)
        {
            playerTransform.position = spawnPoint.transform.position;
        }

        SceneSpawnData.returnSpawnPointName = null;
    }
}