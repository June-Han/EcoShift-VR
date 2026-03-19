using UnityEngine;

public class LitterClickable : MonoBehaviour
{
    [Tooltip("Drag a DialogueTrigger object here for first-click dialogue")]
    public DialogueTrigger firstClickDialogue;

    private static bool hasTriggeredDialogue;

    public void OnClicked()
    {
        if (!hasTriggeredDialogue && firstClickDialogue != null)
        {
            firstClickDialogue.TriggerDialogue();
            hasTriggeredDialogue = true;
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