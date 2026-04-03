using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DistortionManager : MonoBehaviour
{
    public static DistortionManager Instance;

    [Header("Post Processing")]
    public Volume postProcessVolume;

    [Header("Vignette Settings")]
    public float vignetteStrength = 0.7f;
    public float duration = 2.0f;
    public Color vignetteColor = Color.red; // red feels more alarming

    private Vignette vignette;

    // void Awake()
    // {
    //     if (Instance == null) Instance = this;
    //     else Destroy(gameObject);

    //     postProcessVolume.profile.TryGet(out vignette);
    // }

    void Start()
    {
        // Moved from Awake to Start to ensure the Volume is fully loaded
        if (postProcessVolume.profile.TryGet(out vignette))
        {
            // Force the master switch ON, just in case it was disabled in the Inspector!
            vignette.active = true; 
            vignette.color.overrideState = true;
            vignette.center.overrideState = true;
            vignette.intensity.overrideState = true;
            vignette.smoothness.overrideState = true;
            vignette.rounded.overrideState = true;
            
            // Set it to 0 immediately so the screen starts clear
            vignette.intensity.Override(0f);
        }
        else
        {
            Debug.LogError("DistortionManager could not find Vignette in the Volume Profile!");
        }
    }

    public void ApplyDistortion()
    {
        StopAllCoroutines();
        StartCoroutine(VignetteRoutine());
    }

    IEnumerator VignetteRoutine()
    {
        float elapsed = 0f;

        // Fade IN
        while (elapsed < duration * 0.3f)
        {
            float t = elapsed / (duration * 0.3f);
            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Lerp(0f, vignetteStrength, t);
                vignette.color.value = vignetteColor;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;

        // Fade OUT
        while (elapsed < duration * 0.7f)
        {
            float t = elapsed / (duration * 0.7f);
            if (vignette != null)
                vignette.intensity.value = Mathf.Lerp(vignetteStrength, 0f, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (vignette != null) vignette.intensity.value = 0f;
    }
}