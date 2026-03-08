using UnityEngine;
using UnityEngine.SceneManagement;

public class CrabRushManager : MonoBehaviour
{
    [SerializeField] private float rushDuration = 60f;

    private float timer;
    private bool finished = false;

    private void Start()
    {
        timer = rushDuration;
    }

    private void Update()
    {
        if (finished) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            finished = true;
            EndRush();
        }
    }

    private void EndRush()
    {
        PlayerEnterState.movementMode = PlayerMovementMode.Free;
        SceneManager.LoadScene("CrabAreaScene");
    }
}