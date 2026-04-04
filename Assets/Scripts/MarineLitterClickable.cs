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

    public void OnClicked()
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
            Debug.LogError("LitterClickable: CameraDistortionEffect instance not found!");
        }

        gameObject.SetActive(false);
    }
}