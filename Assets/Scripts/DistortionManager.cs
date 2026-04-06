using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class DistortionManager : MonoBehaviour
{
    public static DistortionManager Instance;

    [Header("Post Processing")]
    public Volume postProcessVolume;

    [Header("Vignette Settings")]
    public float vignetteStrength = 4f;
    public float duration = 3.0f;
    public Color vignetteColor = Color.red; // red feels more alarming

    private Vignette vignette;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // If the slot in the Inspector is empty, auto-find the Volume safely
        postProcessVolume = Object.FindFirstObjectByType<Volume>();

        if (postProcessVolume == null)
        {
            // Still nothing — create a Global Volume with a fresh profile that has Vignette
            Debug.LogWarning("[DistortionManager] No Volume found in any loaded scene. Creating one.");
            postProcessVolume = CreateGlobalVolume();
        }

        //// To avoid clashing effects
        // if (postProcessVolume == null)
        // {
        //     // Still nothing — create a Global Volume with a fresh profile that has Vignette
        //     Debug.LogWarning("[DistortionManager] No Volume found in any loaded scene. Creating one.");
        //     postProcessVolume = CreateGlobalVolume();
        // }

        SetupVignette();
    }

    private void SetupVignette()
    {
        // Safety check to ensure we actually found a volume before asking for its profile
        if (postProcessVolume == null) return;

        // If the profile already has a Vignette, use it
        if (postProcessVolume.profile.TryGet(out vignette))
        {
            // Turn on Vignette value in inspector (existing profile)
            vignette.active = true;
            vignette.color.overrideState = true;
            vignette.center.overrideState = true;
            vignette.intensity.overrideState = true;
            vignette.smoothness.overrideState = true;
            vignette.rounded.overrideState = true;

            vignette.intensity.Override(0f);
            Debug.Log("[DistortionManager] Found existing Vignette on Volume.");
        }
        else
        {
            // Profile exists but has no Vignette — add one
            vignette = postProcessVolume.profile.Add<Vignette>(overrides: true);
            vignette.intensity.Override(0f);
            vignette.color.Override(vignetteColor);
            vignette.smoothness.Override(0.4f);
            Debug.Log("[DistortionManager] Added Vignette to existing Volume profile.");
        }
    }



    /// <summary>
    /// Searches every loaded scene for a Volume component (covers persistent Main_Scene).
    /// </summary>
    private Volume FindVolumeAcrossScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Volume vol = root.GetComponentInChildren<Volume>(true);
                if (vol != null)
                {
                    Debug.Log($"[DistortionManager] Found Volume in scene '{scene.name}' on '{vol.gameObject.name}'.");
                    return vol;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Creates a new Global Volume with a VolumeProfile containing a Vignette override.
    /// </summary>
    private Volume CreateGlobalVolume()
    {
        GameObject volGO = new GameObject("Global Volume (DistortionManager)");
        volGO.transform.SetParent(transform, false);

        Volume vol = volGO.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 10; // higher priority so it overrides other volumes
        vol.profile = ScriptableObject.CreateInstance<VolumeProfile>();

        return vol;
    }

    public void ApplyDistortion()
    {
        if (vignette == null)
        {
            Debug.LogWarning("[DistortionManager] ApplyDistortion called but no Vignette available.");
            return;
        }
        StopAllCoroutines();
        StartCoroutine(VignetteRoutine());
    }

    IEnumerator VignetteRoutine()
    {
        float elapsed = 0f;

        // Force the color override
        if (vignette != null) vignette.color.Override(vignetteColor);

        // Fade IN
        while (elapsed < duration * 0.3f)
        {
            float t = elapsed / (duration * 0.3f);
            if (vignette != null)
            {
                // Using .Override() so it actually renders on screen
                vignette.intensity.Override(Mathf.Lerp(0f, vignetteStrength, t));
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
            {
                vignette.intensity.Override(Mathf.Lerp(vignetteStrength, 0f, t));
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Final clear screen
        if (vignette != null) vignette.intensity.Override(0f);
    }
}
