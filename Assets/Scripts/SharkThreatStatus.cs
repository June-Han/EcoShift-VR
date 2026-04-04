using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;

public class SharkThreatStatus : MonoBehaviour
{
    [Header("Player Health")]
    public int maxLives = 3;
    private int currentLives;
    private bool isGameOver = false;

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

        // Auto-find LivesUI — search all objects including inactive ones
        // (the Lives Canvas prefab may have just been activated by HandSwapper)
        if (livesUI == null)
        {
            livesUI = FindFirstObjectByType<LivesUI>();
        }

        // Also search inactive objects in case HandSwapper hasn't activated it yet
        if (livesUI == null)
        {
            foreach (LivesUI lui in Resources.FindObjectsOfTypeAll<LivesUI>())
            {
                // Filter out prefab assets — only want scene instances
                if (lui.gameObject.scene.isLoaded || lui.gameObject.scene.name != null)
                {
                    livesUI = lui;
                    // Make sure it's active
                    if (!lui.gameObject.activeInHierarchy)
                        lui.gameObject.SetActive(true);
                    break;
                }
            }
        }

        if (livesUI == null)
        {
            Debug.LogWarning("[SharkThreatStatus] No LivesUI found. Make sure the Lives Canvas prefab is assigned in HandSwapper.");
        }
    }

    public void GetTangled()
    {
        // Don't take damage after game over
        if (isGameOver) return;

        currentLives--;
        Debug.LogWarning($"Blue Shark TANGLED! Lives remaining: {currentLives}");

        // Grey out a heart on the fin
        if (livesUI != null)
        {
            livesUI.LoseHeart();
        }

        if (DistortionManager.Instance != null)
        {
            DistortionManager.Instance.ApplyDistortion();
        }

        // Vibrate both controllers
        StartCoroutine(VibrateControllers());

        if (currentLives <= 0)
        {
            GameOver();
        }
    }

    private void GameOver()
    {
        isGameOver = true;
        Debug.LogError("THE SHARK HAS BEEN CAPTURED! GAME OVER.");
        // TODO: Game Over UI scene!
    }

    /// <summary>
    /// Sends a haptic impulse to both VR controllers for impact feedback.
    /// </summary>
    private IEnumerator VibrateControllers()
    {
        List<InputDevice> devices = new List<InputDevice>();

        // Get both hand controllers
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
            devices);
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            devices);

        foreach (InputDevice device in devices)
        {
            if (device.isValid)
            {
                device.SendHapticImpulse(0, hapticAmplitude, hapticDuration);
            }
        }

        yield return new WaitForSeconds(hapticDuration);
    }
}
