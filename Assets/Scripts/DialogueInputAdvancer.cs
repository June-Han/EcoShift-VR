using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Listens for a controller button press (e.g. A button) to advance dialogue
/// instead of requiring the player to point and click a UI button.
/// Attach this to the same Canvas GameObject that has the DialogueManager.
/// </summary>
public class DialogueInputAdvancer : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Bind to the A button action, e.g. 'XRI RightHand Interaction/PrimaryButton'")]
    public InputActionReference advanceAction;

    private DialogueManager dialogueManager;
    private bool wasPressed;

    private void Awake()
    {
        dialogueManager = GetComponent<DialogueManager>();
        if (dialogueManager == null)
        {
            dialogueManager = DialogueManager.Instance;
        }
    }

    private void OnEnable()
    {
        if (advanceAction != null && advanceAction.action != null)
        {
            advanceAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (advanceAction != null && advanceAction.action != null)
        {
            advanceAction.action.Disable();
        }
    }

    private void Update()
    {
        if (advanceAction == null || advanceAction.action == null || dialogueManager == null)
            return;

        // Only advance on button-down (not held)
        bool isPressed = advanceAction.action.IsPressed();

        if (isPressed && !wasPressed)
        {
            dialogueManager.NextSentence();
        }

        wasPressed = isPressed;
    }
}
