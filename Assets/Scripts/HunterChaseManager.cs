using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

public class HunterChaseManager : MonoBehaviour
{
    public static HunterChaseManager Instance;

    [Header("Timer")]
    public float totalTime = 60f;
    public TMP_Text timerText;
    private float timeRemaining;
    private bool timerRunning = false;

    [Header("Vignette")]
    public Volume postProcessVolume;
    public float vignetteNearDistance = 8f;    // starts fading in at this distance
    public float vignetteDangerDistance = 3.5f; // matches hunter detectionRadius
    public float maxVignetteStrength = 0.75f;
    public Color vignetteColor = Color.red;
    private Vignette vignette;

    [Header("References")]
    public HunterPatrol hunter;
    public Transform player;
    public DialogueTrigger failDialogue;

    [Header("Heartbeat Audio")]
    public AudioSource heartbeatSource;
    public AudioClip heartbeatClip;
    public float heartbeatFarInterval = 1.5f;   // slow when far
    public float heartbeatNearInterval = 0.4f;  // fast when close
    private float heartbeatTimer = 0f;

    private SceneChanger sceneChanger;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        postProcessVolume.profile.TryGet(out vignette);
    }

    void Start()
    {
        sceneChanger = FindFirstObjectByType<SceneChanger>();

        // Find timer text
        if (timerText == null)
        {
            GameObject timerObj = GameObject.FindGameObjectWithTag("Timer");
            if (timerObj != null) timerText = timerObj.GetComponent<TextMeshProUGUI>();
            else Debug.LogWarning("TimerText not found!");
        }

        // Find player
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            else Debug.LogWarning("Player tag not found!");
        }

        // Find hunter
        if (hunter == null)
        {
            hunter = FindFirstObjectByType<HunterPatrol>();
            if (hunter == null) Debug.LogWarning("HunterPatrol not found!");
        }

        // Debug all references
        Debug.Log($"timerText: {(timerText != null ? "YES" : "MISSING")}");
        Debug.Log($"player: {(player != null ? "YES" : "MISSING")}");
        Debug.Log($"hunter: {(hunter != null ? "YES" : "MISSING")}");
    }

    void Update()
    {
        if (!timerRunning) return;

        // Update timer
        timeRemaining -= Time.deltaTime;
        timeRemaining = Mathf.Max(0, timeRemaining);
        UpdateTimerUI();

        // Update vignette based on hunter distance
        if (hunter != null && player != null)
        {
            float distance = Vector3.Distance(hunter.transform.position, player.position);
            UpdateVignette(distance);
            UpdateHeartbeat(distance);
        }

        if (timeRemaining <= 0) OnTimerEnd();
    }

    public void StartTimer()
    {
        // Find timer text now that UI is active
        if (timerText == null)
        {
            TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (TextMeshProUGUI tmp in allTexts)
            {
                if (tmp.CompareTag("Timer"))
                {
                    timerText = tmp;
                    break;
                }
            }
        }
        timeRemaining = totalTime;
        timerRunning = true;
    }

    public void StartChase()
    {
        timeRemaining = totalTime;
        timerRunning = true;
    }

    public void StopChase()
    {
        timerRunning = false;
        if (heartbeatSource != null) heartbeatSource.Stop();
        if (vignette != null) vignette.intensity.value = 0f;
    }

    void UpdateTimerUI()
    {
        int seconds = Mathf.CeilToInt(timeRemaining);
        timerText.text = $"{seconds}s";

        // Turn text red when under 10 seconds
        timerText.color = seconds <= 10 ? Color.red : Color.white;
    }

    void UpdateVignette(float distance)
    {
        float t = 1f - Mathf.InverseLerp(vignetteDangerDistance, vignetteNearDistance, distance);
        float targetStrength = Mathf.Lerp(0f, maxVignetteStrength, t);
        
        vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetStrength, Time.deltaTime * 5f);
        vignette.color.value = vignetteColor;
    }

    void UpdateHeartbeat(float distance)
    {
        if (heartbeatClip == null || heartbeatSource == null) return;

        heartbeatTimer -= Time.deltaTime;

        if (heartbeatTimer <= 0f)
        {
            // Calculate interval and volume based on distance
            float t = 1f - Mathf.InverseLerp(vignetteDangerDistance, vignetteNearDistance, distance);
            float interval = Mathf.Lerp(heartbeatFarInterval, heartbeatNearInterval, t);
            float volume = Mathf.Lerp(0.2f, 1.0f, t);

            heartbeatSource.volume = volume;
            heartbeatSource.PlayOneShot(heartbeatClip);
            heartbeatTimer = interval;
        }
    }

    void OnTimerEnd()
    {
        timerRunning = false;
        if (heartbeatSource != null) heartbeatSource.Stop();
        if (vignette != null) vignette.intensity.value = 0f;

        if (hunter != null)
        {
            hunter.TriggerShoot(true);
            failDialogue.TriggerDialogue();
            sceneChanger.LoadMainScene();
        }
    }
    
    public void TimerEnd()
    {
        timerRunning = false;
        if (heartbeatSource != null) heartbeatSource.Stop();
        if (vignette != null) vignette.intensity.value = 0f;
        
    }

    private void OnDestroy()
    {
        if (vignette != null)
        {
            vignette.intensity.value = 0f;
        }
    }

    /// <summary>
    /// Pause the game: stops the timer, hunter movement, and player movement
    /// </summary>
    public void PauseGame()
    {
        timerRunning = false;
        player.GetComponent<ContinuousMoveProvider>().enabled = false;
        
        // Stop hunter movement
        if (hunter != null && hunter.GetComponent<NavMeshAgent>() != null)
        {
            hunter.GetComponent<NavMeshAgent>().enabled = false;
        }
        // Stop heartbeat sound
        if (heartbeatSource != null) heartbeatSource.Stop();
        
        Debug.Log("Game paused");
    }

    /// <summary>
    /// Resume the game: resumes the timer, hunter movement, and player movement
    /// </summary>
    public void ResumeGame()
    {
        player.GetComponent<ContinuousMoveProvider>().enabled = true;
        
        // Resume hunter movement
        if (hunter != null && hunter.GetComponent<NavMeshAgent>() != null)
        {
            hunter.GetComponent<NavMeshAgent>().enabled = true;
        }
        
        // Resume timer
        timerRunning = true;
        
        Debug.Log("Game resumed");
    }
}