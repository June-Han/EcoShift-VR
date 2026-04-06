using Unity.VisualScripting;
using UnityEngine;

public class CaveEntranceGate : MonoBehaviour
{
    [Header("Requirements")]
    [Tooltip("The score required to make the wall disappear.")]
    public int requiredScore = 10;

    [Tooltip("Drag the GameManagers object holding the ScoreManager here.")]
    public ScoreManager scoreManager;

    public DialogueTrigger caveEntranceDialogue;

    [Header("Audio")]
    [Tooltip("The sound effect to play when the cave unlocks.")]
    private AudioClip unlockSound;
    
    [Tooltip("The sound effect to play when the player is rejected (dialogue pops up).")]
    private AudioClip lockedSound;


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
        Debug.Log($"[CaveGate] SENSOR HIT BY: {other.name} (Tag: {other.tag})");
        bool isPlayer = other.CompareTag("Player") || 
                        other.transform.root.CompareTag("Player") ||
                        other.GetComponentInParent<CharacterController>() != null;
        // Check if it is the player touching the zone
        if (isPlayer)
        {
            Debug.Log("[CaveGate] Player recognized! Checking score...");
            // Retrieve current score
            int currentScore = GetPlayerScore();

            // Check if score is 100%
            if (currentScore >= requiredScore)
            {
                Debug.Log("Score is 10/10! Opening the cave...");

                // Generate a high, quick "Ding!" (880 Hz for 0.5 seconds)
                unlockSound = GenerateProceduralTone("UnlockDing", 880f, 0.5f);
        
                
                // --- PLAY THE SUCCESS SOUND ---
                if (unlockSound != null)
                {
                    AudioSource.PlayClipAtPoint(unlockSound, transform.position);
                }
                
                // Disable this entire blocking object so they can swim through
                gameObject.SetActive(false); 
            }
            else
            {
                Debug.Log("[CaveGate] Not enough score! Playing locked sound and showing dialogue...");
                // Generate a low, buzzing "Boop" (150 Hz for 0.3 seconds)
                lockedSound = GenerateProceduralTone("LockedBoop", 150f, 0.3f);

                // --- PLAY THE LOCKED/ERROR SOUND ---
                if (lockedSound != null)
                {
                    AudioSource.PlayClipAtPoint(lockedSound, transform.position);
                }
                
                // Score too low, dialogue pop up
                if (caveEntranceDialogue != null)
                {
                    caveEntranceDialogue.TriggerDialogue();
                }
                gameObject.SetActive(true); 
            }
        }
        else
        {
            Debug.Log("[CaveGate] Ignored object because it does not have the 'Player' tag or a CharacterController.");
        }
    }

    // --- Link to Score ---
    private int GetPlayerScore()
    {
        // // Safely use ScoreManager's built-in Singleton Instance!
        // // Try to use the one dragged into the Inspector first
        // if (scoreManager != null)
        // {
        //     return scoreManager.GetPlayerScore();
        // }
        // // If the Inspector slot is empty, fallback to the Singleton!
        // else if (ScoreManager.Instance != null)
        // {
        //     return ScoreManager.Instance.GetPlayerScore();
        // }
        // // Search the entire loaded game for ScoreManager
        // ScoreManager survivingManager = FindFirstObjectByType<ScoreManager>();
        // if (survivingManager != null)
        // {
        //     // Re-link it so we don't have to search again next time
        //     scoreManager = survivingManager; 
        //     return survivingManager.GetPlayerScore();
        // }

        // // If absolutely everything fails, log the error
        // Debug.LogError("CaveEntranceGate: ScoreManager is completely missing! Check your Scene transitions.");
        // return 0;
        
        // Read directly from the shared fish counter!
        return FishClickable.totalFishEatenInScene;
    }

    // --- MATHEMATICAL AUDIO GENERATOR ---
    private AudioClip GenerateProceduralTone(string clipName, float frequency, float duration)
    {
        int sampleRate = 44100; // Standard audio sample rate
        int sampleCount = (int)(sampleRate * duration);
        
        // Create an empty audio clip
        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        float[] samples = new float[sampleCount];

        // Draw the soundwave using a Sine Wave mathematical formula
        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            
            // Formula: Amplitude * sin(2 * PI * Frequency * Time)
            // We multiply by 0.5f so the sound isn't deafeningly loud
            samples[i] = 0.5f * Mathf.Sin(2 * Mathf.PI * frequency * time);
        }

        // Apply the wave data to the clip
        clip.SetData(samples, 0);
        return clip;
    }
}