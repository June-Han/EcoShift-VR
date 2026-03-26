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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        postProcessVolume.profile.TryGet(out vignette);
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