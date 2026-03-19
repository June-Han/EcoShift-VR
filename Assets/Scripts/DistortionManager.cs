using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // or .HighDefinition

public class DistortionManager : MonoBehaviour
{
    public static DistortionManager Instance;

    [Header("Post Processing")]
    public Volume postProcessVolume;
    public float distortionStrength = 0.4f;
    public float distortionDuration = 2.0f;

    private LensDistortion lensDistortion;
    private Vignette vignette;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Auto-find PostProcessVolume if not assigned
        if (postProcessVolume == null)
        {
            postProcessVolume = FindFirstObjectByType<Volume>();
            if (postProcessVolume == null)
            {
                Debug.LogError("DistortionManager: Cannot find PostProcessVolume in scene!");
                return;
            }
            Debug.Log("DistortionManager: Auto-found PostProcessVolume");
        }

        // Get LensDistortion and Vignette components from Volume profile
        if (postProcessVolume.profile != null)
        {
            if (!postProcessVolume.profile.TryGet(out lensDistortion))
            {
                Debug.LogWarning("DistortionManager: LensDistortion not found in Volume profile");
            }
            
            if (!postProcessVolume.profile.TryGet(out vignette))
            {
                Debug.LogWarning("DistortionManager: Vignette not found in Volume profile");
            }
            
            if (lensDistortion != null && vignette != null)
            {
                Debug.Log("DistortionManager: LensDistortion and Vignette initialized successfully");
            }
        }
        else
        {
            Debug.LogError("DistortionManager: PostProcessVolume profile is null!");
        }
    }

    public void ApplyDistortion()
    {
        // Check if components are initialized
        if (postProcessVolume == null)
        {
            Debug.LogError("DistortionManager: PostProcessVolume is null! Cannot apply distortion effect.");
            return;
        }

        if (lensDistortion == null && vignette == null)
        {
            Debug.LogError("DistortionManager: Neither LensDistortion nor Vignette is initialized!");
            return;
        }

        Debug.Log("DistortionManager: Applying distortion effect");
        StopAllCoroutines();
        StartCoroutine(DistortRoutine());
    }

    IEnumerator DistortRoutine()
    {
        // Enable the effects
        if (lensDistortion != null)
        {
            lensDistortion.active = true;
            lensDistortion.intensity.value = -distortionStrength;
            Debug.Log($"LensDistortion enabled with intensity: {-distortionStrength}");
        }
        
        if (vignette != null)
        {
            vignette.active = true;
            vignette.intensity.value = 0.5f;
            Debug.Log($"Vignette enabled with intensity: 0.5");
        }

        float elapsed = 0f;
        while (elapsed < distortionDuration)
        {
            float t = elapsed / distortionDuration;
            float strength = Mathf.Lerp(distortionStrength, 0f, t);

            if (lensDistortion != null)
                lensDistortion.intensity.value = -strength;

            if (vignette != null)
                vignette.intensity.value = Mathf.Lerp(0.5f, 0.2f, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset values and disable effects
        if (lensDistortion != null)
        {
            lensDistortion.intensity.value = 0f;
            lensDistortion.active = false;
            Debug.Log("LensDistortion disabled");
        }

        if (vignette != null)
        {
            vignette.intensity.value = 0.2f;
            vignette.active = false;
            Debug.Log("Vignette disabled");
        }
    }
}