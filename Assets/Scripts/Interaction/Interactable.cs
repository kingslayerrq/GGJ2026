using UnityEngine;
using UnityEngine.InputSystem;

public class Interactable : MonoBehaviour
{
    [SerializeField] private GameObject promptObject;

    private bool playerInRange = false;
    private IInteractable interactAction;

    private void Start()
    {
        interactAction = GetComponent<IInteractable>();

        if (promptObject != null)
            promptObject.SetActive(false);
    }

    private void Update()
    {
        if (!playerInRange) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (interactAction != null)
            {
                interactAction.Interact();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            if (promptObject != null)
                promptObject.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (promptObject != null)
                promptObject.SetActive(false);
        }
    }
}