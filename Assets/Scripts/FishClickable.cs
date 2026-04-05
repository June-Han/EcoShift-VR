using UnityEngine;

public class FishClickable : MonoBehaviour
{
    [Tooltip("Drag a DialogueTrigger object here for first-click dialogue. If left empty, it will auto-find 'FirstCatchDialogue' in the scene.")]
    public DialogueTrigger firstClickDialogue;

    [Tooltip("Chomp sound played when a fish is eaten. If left empty, a procedural chomp is generated automatically.")]
    public AudioClip chompSound;

    [Tooltip("Volume of the chomp sound effect (0–1)")]
    [Range(0f, 1f)]
    public float chompVolume = 0.8f;

    private static bool hasTriggeredDialogue = false; // shared across all fish
    private static AudioClip sharedChompClip;          // generated once, reused by all fish

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

        // Auto-load chomp clip from Resources if not assigned in Inspector
        if (chompSound == null && sharedChompClip == null)
        {
            sharedChompClip = Resources.Load<AudioClip>("Audio/shark_crunch");
            if (sharedChompClip == null)
            {
                Debug.LogWarning("FishClickable: Could not load 'Resources/Audio/shark_crunch'. Generating procedural chomp instead.");
                sharedChompClip = GenerateChompClip();
            }
        }
    }

    public void OnClicked()
    {
        // Play chomp sound at the fish's position
        AudioClip clip = chompSound != null ? chompSound : sharedChompClip;
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, chompVolume);
        }

        // Trigger dialogue only on first ever fish click
        if (!hasTriggeredDialogue && firstClickDialogue != null)
        {
            firstClickDialogue.TriggerDialogue();
            hasTriggeredDialogue = true;
        }

        ScoreManager.Instance.AddScore();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Generates a short procedural "chomp" sound effect so the game works
    /// out of the box without needing an external audio asset.
    /// Replace this by assigning a real AudioClip to chompSound in the Inspector.
    /// </summary>
    private static AudioClip GenerateChompClip()
    {
        int sampleRate = 44100;
        float duration = 0.25f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float normalizedTime = t / duration; // 0 → 1

            // Fast amplitude envelope: sharp attack, quick decay
            float envelope = Mathf.Pow(1f - normalizedTime, 3f);

            // Low-frequency "jaw snap" thump (80 Hz) with a downward pitch sweep
            float freq = Mathf.Lerp(120f, 50f, normalizedTime);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t);

            // Short noise burst for the "crunch" texture (first 30% of the clip)
            float noise = 0f;
            if (normalizedTime < 0.3f)
            {
                // Simple deterministic noise using a hash-like function
                float hash = Mathf.Sin(i * 12.9898f + 78.233f) * 43758.5453f;
                noise = (hash - Mathf.Floor(hash)) * 2f - 1f;
                noise *= (1f - normalizedTime / 0.3f); // fade noise out
            }

            samples[i] = Mathf.Clamp((tone * 0.7f + noise * 0.5f) * envelope, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("ProceduralChomp", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
