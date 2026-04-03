using UnityEngine;

public class InsectClickable : MonoBehaviour
{
    [Tooltip("Drag a DialogueTrigger object here for first-click dialogue")]
    public DialogueTrigger firstClickDialogue;
    public AudioClip clickSound;

    private static bool hasTriggeredDialogue = false; // shared across all insects

    public void OnClicked()
    {

        if (clickSound != null)
        {
            AudioSource.PlayClipAtPoint(clickSound, transform.position);
        }
        
        // Trigger dialogue only on first ever insect click
        if (!hasTriggeredDialogue && firstClickDialogue != null)
        {
            firstClickDialogue.TriggerDialogue();
            hasTriggeredDialogue = true;
        }

        ScoreManager.Instance.AddScore();
        gameObject.SetActive(false);
    }
}