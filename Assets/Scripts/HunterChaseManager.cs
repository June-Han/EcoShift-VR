using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HunterChaseManager : MonoBehaviour
{
    public static HunterChaseManager Instance;

    [Header("Timer")]
    public float totalTime = 60f;
    public TextMeshProUGUI timerText;
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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Find timer text
        if (timerText == null)
        {
            GameObject timerObj = GameObject.Find("TimerText");
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

        // Grab vignette AFTER volume is confirmed
        if (postProcessVolume != null)
        {
            postProcessVolume.profile.TryGet(out vignette);
            Debug.Log($"Vignette grabbed: {(vignette != null ? "YES" : "FAILED")}");
        }
        else Debug.LogWarning("Post Process Volume not assigned!");

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
        }

        if (timeRemaining <= 0) OnTimerEnd();
    }

    public void StartChase()
    {
        timeRemaining = totalTime;
        timerRunning = true;
    }

    public void StopChase()
    {
        timerRunning = false;
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
        if (vignette == null) return;

        if (distance > vignetteNearDistance)
        {
            // Hunter far away - no vignette
            vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, 0f, Time.deltaTime * 3f);
        }
        else
        {
            // Hunter nearby - scale vignette strength by distance
            float t = 1f - Mathf.InverseLerp(vignetteDangerDistance, vignetteNearDistance, distance);
            float targetStrength = Mathf.Lerp(0f, maxVignetteStrength, t);
            vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetStrength, Time.deltaTime * 5f);
            vignette.color.value = vignetteColor;
        }
    }

    void OnTimerEnd()
    {
        timerRunning = false;
        // add whatever happens when timer runs out here
        Debug.Log("Timer ended!");
    }
}