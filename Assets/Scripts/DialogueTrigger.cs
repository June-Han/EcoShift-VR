using UnityEngine;
using System.Collections;

public class DialogueTrigger : MonoBehaviour
{
    [Tooltip("Reference to the dialogue manager in the scene.")]
    public DialogueManager dialogueManager;

    [Tooltip("The sentences to update the dialogue manager with.")]
    [TextArea(3, 10)]
    public string[] sentences;

    [Tooltip("If true, this dialogue will only trigger once.")]
    public bool triggerOnce = true;

    [Tooltip("If true, dialogue starts immediately when the scene or object loads.")]
    public bool triggerOnStart = false;

    [Tooltip("Event triggered when this specific dialogue finishes.")]
    public UnityEngine.Events.UnityEvent onDialogueFinished;

    private bool hasTriggered = false;

    private void Start()
    {
        if (dialogueManager == null)
        {
            dialogueManager = DialogueManager.Instance;
        }

        if (triggerOnStart)
        {
            // Wait for Camera.main to be available before showing the dialogue
            // The persistent XR Origin's camera may not be ready on the first frame
            StartCoroutine(WaitForCameraThenTrigger());
        }
    }

    private IEnumerator WaitForCameraThenTrigger()
    {
        // Wait until Camera.main is available (up to 3 seconds)
        float timeout = 3f;
        float elapsed = 0f;
        while (Camera.main == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (Camera.main == null)
        {
            Debug.LogWarning($"DialogueTrigger ({name}): Camera.main not found after {timeout}s — showing dialogue anyway.");
        }

        TriggerDialogue();
    }

    // This method is called when another collider enters the trigger collider attached to this object
    private void OnTriggerEnter(Collider other)
    {
        // Check if the player entered the trigger area
        // Make sure your player object has the tag "Player"
        bool isPlayer = other.CompareTag("Player");
        
        // If the tag is not correct, try to find the Player tag or CharacterController on the parent object
        if (!isPlayer)
        {
            if (other.transform.root.CompareTag("Player") || 
                other.GetComponent<CharacterController>() != null || 
                other.GetComponentInParent<CharacterController>() != null)
            {
                isPlayer = true;
            }
        }

        if (isPlayer && !hasTriggered)
        {
            TriggerDialogue();
        }
        else if (!hasTriggered)
        {
            // Log for debugging purposes (can be commented out in release)
            Debug.Log($"DialogueTrigger hit by non-player: {other.name} (Tag: {other.tag})");
        }
    }

    // Public method to be called by events or other scripts
    public void TriggerDialogue()
    {
        if (triggerOnce && hasTriggered) return;

        if (dialogueManager != null)
        {
            // Pass a callback to trigger the event when dialogue ends
            dialogueManager.ShowDialogue(sentences, () => {
                if(onDialogueFinished != null) onDialogueFinished.Invoke();
            });
            hasTriggered = true;
        }
        else
        {
            Debug.LogWarning("Dialogue Manager not assigned on " + gameObject.name);
        }
    }
}
