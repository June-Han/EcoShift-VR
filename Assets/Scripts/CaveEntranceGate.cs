using UnityEngine;

public class CaveEntranceGate : MonoBehaviour
{
    [Header("Requirements")]
    [Tooltip("The score required to make the wall disappear.")]
    public int requiredScore = 10;

    public DialogueTrigger caveEntranceDialogue;

    private void Start()
    {
        if (caveEntranceDialogue == null)
        {
            GameObject dialogueObj = GameObject.Find("CaveEntranceDialogue");
            if (dialogueObj != null)
            {
                caveEntranceDialogue = dialogueObj.GetComponent<DialogueTrigger>();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if it is the player touching the zone
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            // Retrieve current score
            int currentScore = GetPlayerScore();

            // Check if score is 100%
            if (currentScore >= requiredScore)
            {
                Debug.Log("Score is 10/10! Opening the cave...");
                
                // TODO: Play a sound effect here?
                
                // Disable this entire blocking object so they can swim through
                gameObject.SetActive(false); 
            }
            else
            {
                // Score too low, dialogue pop up
                if (caveEntranceDialogue != null)
                {
                    caveEntranceDialogue.TriggerDialogue();
                }
            }
        }
    }

    // --- Link to Score ---
    private int GetPlayerScore()
    {
        return FindFirstObjectByType<ScoreManager>().GetPlayerScore();
    }
}