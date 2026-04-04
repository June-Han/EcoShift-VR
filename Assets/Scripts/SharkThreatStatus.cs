using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SharkThreatStatus : MonoBehaviour
{
    [Header("Player Health")]
    public int maxLives = 3;
    private int currentLives;
    private bool isGameOver = false;

    [Header("Timer")]
    [Tooltip("Total time in seconds the player has to survive.")]
    public float totalTime = 60f;
    [Tooltip("Assign manually, or leave empty — one will be created on the Lives Canvas.")]
    public TMP_Text timerText;
    private float timeRemaining;
    private bool timerRunning = false;

    [Header("UI")]
    [Tooltip("Auto-found at runtime if not assigned. Looks for LivesUI in the scene.")]
    public LivesUI livesUI;

    [Header("Haptic Feedback")]
    [Tooltip("Vibration strength when hit by net (0-1).")]
    [Range(0f, 1f)]
    public float hapticAmplitude = 0.8f;
    [Tooltip("How long the vibration lasts in seconds.")]
    public float hapticDuration = 0.5f;

    void Start()
    {
        currentLives = maxLives;

        // Auto-find LivesUI
        if (livesUI == null)
            livesUI = FindFirstObjectByType<LivesUI>();

        if (livesUI == null)
        {
            foreach (LivesUI lui in Resources.FindObjectsOfTypeAll<LivesUI>())
            {
                if (lui.gameObject.scene.isLoaded || lui.gameObject.scene.name != null)
                {
                    livesUI = lui;
                    if (!lui.gameObject.activeInHierarchy)
                        lui.gameObject.SetActive(true);
                    break;
                }
            }
        }

        // Create a timer on the Lives Canvas if not assigned
        if (timerText == null && livesUI != null)
        {
            timerText = CreateTimerOnLivesCanvas();
        }

        if (livesUI == null)
            Debug.LogWarning("[SharkThreatStatus] No LivesUI found.");
        if (timerText == null)
            Debug.LogWarning("[SharkThreatStatus] No timer text created.");
    }

    void Update()
    {
        if (!timerRunning) return;

        timeRemaining -= Time.deltaTime;
        timeRemaining = Mathf.Max(0f, timeRemaining);
        UpdateTimerUI();

        if (timeRemaining <= 0f)
        {
            timerRunning = false;
            OnTimerSurvived();
        }
    }

    /// <summary>
    /// Call this to start the countdown (e.g. from DialogueTrigger.onDialogueFinished
    /// or from SharkThreatIntro after the intro sequence).
    /// </summary>
    public void StartTimer()
    {
        // Create timer if it wasn't ready during Start
        if (timerText == null && livesUI != null)
            timerText = CreateTimerOnLivesCanvas();

        timeRemaining = totalTime;
        timerRunning = true;

        if (timerText != null)
            timerText.gameObject.SetActive(true);

        Debug.Log($"[SharkThreatStatus] Timer started: {totalTime}s");
    }

    public void StopTimer()
    {
        timerRunning = false;
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        int seconds = Mathf.CeilToInt(timeRemaining);
        timerText.text = $"{seconds}s";
        timerText.color = seconds <= 10 ? Color.red : Color.white;
    }

    /// <summary>
    /// Called when the player survives the full timer duration.
    /// </summary>
    private void OnTimerSurvived()
    {
        Debug.Log("[SharkThreatStatus] Timer ended — the shark survived!");
        // TODO: trigger success dialogue / transition to next scene
    }

    /// <summary>
    /// Creates a TMP timer text on the same Canvas as the LivesUI hearts.
    /// Positioned to the left so it doesn't overlap with the hearts.
    /// This is independent of the Pangolin Timer Canvas — moving this won't affect Pangolin.
    /// </summary>
    private TextMeshProUGUI CreateTimerOnLivesCanvas()
    {
        Canvas livesCanvas = livesUI.GetComponentInParent<Canvas>();
        if (livesCanvas == null)
        {
            Debug.LogWarning("[SharkThreatStatus] Lives Canvas not found — can't create timer.");
            return null;
        }

        GameObject timerGO = new GameObject("SharkTimer");
        timerGO.layer = livesCanvas.gameObject.layer;
        timerGO.transform.SetParent(livesCanvas.transform, false);

        RectTransform rect = timerGO.AddComponent<RectTransform>();
        // Anchor to the left side of the canvas so it doesn't overlap the hearts on the right
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(20f, 0f);
        rect.sizeDelta = new Vector2(250f, 80f);

        TextMeshProUGUI tmp = timerGO.AddComponent<TextMeshProUGUI>();
        tmp.text = $"{Mathf.CeilToInt(totalTime)}s";
        tmp.fontSize = 60;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;

        Debug.Log("[SharkThreatStatus] Shark timer created on Lives Canvas.");
        return tmp;
    }

    public void GetTangled()
    {
        if (isGameOver) return;

        currentLives--;
        Debug.LogWarning($"Blue Shark TANGLED! Lives remaining: {currentLives}");

        if (livesUI != null)
            livesUI.LoseHeart();

        if (DistortionManager.Instance != null)
            DistortionManager.Instance.ApplyDistortion();

        StartCoroutine(VibrateControllers());

        if (currentLives <= 0)
            GameOver();
    }

    private void GameOver()
    {
        isGameOver = true;
        timerRunning = false;

        if (timerText != null)
            timerText.text = "0s";

        Debug.LogError("THE SHARK HAS BEEN CAPTURED! GAME OVER.");
        // TODO: Game Over UI / scene transition
    }

    private IEnumerator VibrateControllers()
    {
        List<InputDevice> devices = new List<InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
            devices);
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            devices);

        foreach (InputDevice device in devices)
        {
            if (device.isValid)
                device.SendHapticImpulse(0, hapticAmplitude, hapticDuration);
        }

        yield return new WaitForSeconds(hapticDuration);
    }
}
