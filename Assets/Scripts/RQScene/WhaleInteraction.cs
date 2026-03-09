using System.Collections.Generic;
using UnityEngine;

public class WhaleInteraction : MonoBehaviour, IInteractable
{
    [SerializeField] private string speakerName = "Whale";

    [TextArea(2, 5)]
    [SerializeField] private string[] dialogueLines;

    [SerializeField] private GameObject objectToActivate;

    public void Interact()
    {
        if (DialogueManager.IsDialogueActive)
            return;

        DialogueManager.Instance.StartDialogue(
            speakerName,
            new List<string>(dialogueLines),
            OnDialogueFinished
        );
    }

    private void OnDialogueFinished()
    {
        Debug.Log("Whale dialogue finished");

        if (objectToActivate != null)
        {
            objectToActivate.SetActive(true);
        }
    }
}