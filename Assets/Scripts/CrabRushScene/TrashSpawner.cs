using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject trashPrefab;

    [Header("Spawn Timing")]
    [SerializeField] private float spawnInterval = 2f;

    [Header("Spawn Position")]
    [SerializeField] private float spawnX = 15f;

    [Tooltip("Y")]
    [SerializeField] private float[] laneYPositions = new float[4];

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnRandomWave();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnRandomWave()
    {
        if (trashPrefab == null || laneYPositions.Length < 4)
        {
            Debug.LogWarning("TrashSpawner: Prefab or lane positions not set correctly.");
            return;
        }

        // spawn 1-3
        int spawnCount = Random.Range(1, 4);

        // 4 choose 3
        List<int> availableLanes = new List<int> { 0, 1, 2, 3 };

        for (int i = 0; i < spawnCount; i++)
        {
            //pick from rest lane
            int randomIndex = Random.Range(0, availableLanes.Count);
            int laneIndex = availableLanes[randomIndex];

            float y = laneYPositions[laneIndex];
            Vector3 spawnPos = new Vector3(spawnX, y, 0f);

            Instantiate(trashPrefab, spawnPos, Quaternion.identity);

            //remove duplicate
            availableLanes.RemoveAt(randomIndex);
        }
    }
}