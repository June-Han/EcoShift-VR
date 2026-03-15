using UnityEngine;
using TMPro;
using UnityEngine.Events; 
public class DialogueManager : MonoBehaviour {
    public static DialogueManager Instance { get; private set; }

    public TextMeshProUGUI uiText;
    private int currentStep = 0;

    public string[] dialogues;
    public UnityEvent onDialogueEnd; 
    private UnityAction currentDialogueCallback;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    // Add this function to start a dialogue from external triggers
    // Now accepts an optional callback to run when this specific dialogue ends
    public void ShowDialogue(string[] newDialogues, UnityAction onComplete = null) {
        dialogues = newDialogues;
        currentDialogueCallback = onComplete;
        currentStep = 0;
        gameObject.SetActive(true); // Ensure the UI is shown
        NextSentence(); // Display the first sentence
    }

    public void NextSentence() {
        if (currentStep < dialogues.Length) {
            uiText.text = dialogues[currentStep];
            currentStep++;
        } else {
            // Trigger the global end event
            if (onDialogueEnd != null) onDialogueEnd.Invoke(); 

            // Trigger the specific callback for this dialogue instance
            if (currentDialogueCallback != null) {
                currentDialogueCallback.Invoke();
                currentDialogueCallback = null; // Clear it after use
            }

            gameObject.SetActive(false);
        }
    }
}