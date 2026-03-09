using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public static bool IsDialogueActive { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;

    private List<string> currentLines = new List<string>();
    private int currentLineIndex = 0;

    private string currentSpeaker = "";

    private System.Action onDialogueFinished;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        IsDialogueActive = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        if (!IsDialogueActive) return;

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            ShowNextLine();
        }
    }

    public void StartDialogue(string speaker, List<string> lines, System.Action onFinish = null)
    {
        if (lines == null || lines.Count == 0)
        {
            Debug.LogWarning("No dialogue lines provided.");
            return;
        }

        if (IsDialogueActive)
            return;

        currentSpeaker = speaker;
        currentLines = lines;
        currentLineIndex = 0;

        onDialogueFinished = onFinish;

        IsDialogueActive = true;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (dialogueText != null)
            dialogueText.text = currentLines[currentLineIndex];
    }

    private void ShowNextLine()
    {
        currentLineIndex++;

        if (currentLineIndex < currentLines.Count)
        {
            dialogueText.text = currentLines[currentLineIndex];
        }
        else
        {
            EndDialogue();
        }
    }

    public void EndDialogue()
    {
        IsDialogueActive = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (dialogueText != null)
            dialogueText.text = "";

        currentLines.Clear();
        currentLineIndex = 0;

        Debug.Log($"Dialogue with {currentSpeaker} ended");

        onDialogueFinished?.Invoke();
    }
}