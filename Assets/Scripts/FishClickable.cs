using UnityEngine;

public class FishClickable : MonoBehaviour
{
    [Tooltip("Drag a DialogueTrigger object here for first-click dialogue. If left empty, it will auto-find 'FirstCatchDialogue' in the scene.")]
    public DialogueTrigger firstClickDialogue;

    private static bool hasTriggeredDialogue = false; // shared across all fish

    private void Start()
    {
        // Auto-find the dialogue trigger if not assigned (needed for runtime-spawned fish)
        if (firstClickDialogue == null)
        {
            GameObject dialogueObj = GameObject.Find("FirstCatchDialogue");
            if (dialogueObj != null)
            {
                firstClickDialogue = dialogueObj.GetComponent<DialogueTrigger>();
            }
        }
    }

    public void OnClicked()
    {
        // Trigger dialogue only on first ever fish click
        if (!hasTriggeredDialogue && firstClickDialogue != null)
        {
            firstClickDialogue.TriggerDialogue();
            hasTriggeredDialogue = true;
        }

        ScoreManager.Instance.AddScore();
        gameObject.SetActive(false);
    }
}
