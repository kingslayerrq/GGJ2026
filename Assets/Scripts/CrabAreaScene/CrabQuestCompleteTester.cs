using UnityEngine;
using UnityEngine.InputSystem;

public class CrabQuestCompleteTester : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            GameProgress.crabCompleted = true;
            Debug.Log("Crab quest completed!");
        }
    }
}