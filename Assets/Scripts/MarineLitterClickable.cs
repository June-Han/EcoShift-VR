using UnityEngine;

public class MarineLitterClickable : MonoBehaviour
{
    [Tooltip("Drag a DialogueTrigger object here for first-click dialogue")]
    [HideInInspector]
    public DialogueTrigger firstClickDialogue;
    public AudioClip clickSound;
    private static bool hasTriggeredDialogue;

    void Start()
    {
        // Find the GameObject in the scene named exactly "PollutionDialogue"
        GameObject dialogueObj = GameObject.Find("Dialogues/PollutionDialogue");

        // If we found it, grab the DialogueTrigger script off of it!
        if (dialogueObj != null)
        {
            firstClickDialogue = dialogueObj.GetComponent<DialogueTrigger>();
        }
        else
        {
            Debug.LogWarning("Could not find the PollutionDialogue in the scene!");
        }
    }

    /// <summary>
    /// Called when this litter is "eaten". Set skipScorePenalty=true if the caller
    /// already applied the -3 penalty (e.g. FishTargetSystem handles it directly).
    /// </summary>
    public void OnClicked(bool skipScorePenalty = false)
    {
        if (clickSound != null)
        {
            AudioSource.PlayClipAtPoint(clickSound, transform.position);
        }

        if (!hasTriggeredDialogue && firstClickDialogue != null)
        {
            firstClickDialogue.TriggerDialogue();
            //hasTriggeredDialogue = true;
        }

        if (DistortionManager.Instance != null)
        {
            DistortionManager.Instance.ApplyDistortion();
        }
        else
        {
            Debug.LogError("MarineLitterClickable: DistortionManager instance not found!");
        }

        // Penalty for eating litter — same pattern as FishClickable.AddScore()
        if (!skipScorePenalty)
        {
            ScoreManager.Instance.RemoveScore(3);
        }

        // --- Penalize the internal Cave counter! ---
        // Subtract 3, but use Mathf.Max to prevent the score from dropping below 0
        FishClickable.totalFishEatenInScene = Mathf.Max(0, FishClickable.totalFishEatenInScene - 3);

        gameObject.SetActive(false);
    }
}
