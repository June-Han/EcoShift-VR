using UnityEngine;

public class LitterClickable : MonoBehaviour
{
    [Tooltip("Drag a DialogueTrigger object here for first-click dialogue")]
    public DialogueTrigger firstClickDialogue;

    private static bool hasTriggeredDialogue = false; // shared across all litter

    public void OnClicked()
    {
        if (!hasTriggeredDialogue && firstClickDialogue != null)
        {
            firstClickDialogue.TriggerDialogue();
            hasTriggeredDialogue = true;
        }

        DistortionManager.Instance.ApplyDistortion();
        gameObject.SetActive(false);
    }
}