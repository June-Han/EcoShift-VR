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

    // Safety lock to prevent spam-triggering if the player wiggles on the boundary
    private bool isCurrentlyPlaying = false;

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

        if (isPlayer)
        {
            // If we only want it once, and it already fired, stop here.
            if (triggerOnce && hasTriggered) return;

            // If the dialogue is already active on the screen, don't restart it.
            if (isCurrentlyPlaying) return;

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
        // If it's a one-time trigger and we've already fired it, do nothing.
        if (triggerOnce && hasTriggered) 
        {
            Debug.Log($"[{gameObject.name}] TriggerOnce is true and has already fired. Ignoring click.");
            return;
        }

        // 2. Safety Override: If the UI was forcefully closed or disabled, reset our lock!
        if (dialogueManager != null && !dialogueManager.gameObject.activeInHierarchy)
        {
            isCurrentlyPlaying = false;
        }

        // 3. Protect against double-clicks while the text is actively on screen
        if (isCurrentlyPlaying) 
        {
            Debug.Log($"[{gameObject.name}] Dialogue is actively playing on screen. Ignoring extra click.");
            return;
        }

        if (dialogueManager != null)
        {
            // Lock the trigger so spam-clicking doesn't restart the text
            isCurrentlyPlaying = true;
            hasTriggered = true;
            
            Debug.Log($"[{gameObject.name}] Sending sentences to Dialogue Manager...");

            // Pass a callback to trigger the event when dialogue ends
            dialogueManager.ShowDialogue(sentences, () => {
                
                // This unlocks the object so it can be clicked again!
                Debug.Log($"[{gameObject.name}] Dialogue finished! Unlocking object for next click.");
                isCurrentlyPlaying = false; 
                
                if(onDialogueFinished != null) onDialogueFinished.Invoke();
            });
        }
        else
        {
            Debug.LogWarning("Dialogue Manager not assigned on " + gameObject.name);
        }
    }
}
