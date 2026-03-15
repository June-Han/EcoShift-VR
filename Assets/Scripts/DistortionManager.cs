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

        postProcessVolume.profile.TryGet(out lensDistortion);
        postProcessVolume.profile.TryGet(out vignette);
    }

    public void ApplyDistortion()
    {
        StopAllCoroutines();
        StartCoroutine(DistortRoutine());
    }

    IEnumerator DistortRoutine()
    {
        float elapsed = 0f;
        while (elapsed < distortionDuration)
        {
            float t = elapsed / distortionDuration;
            float strength = Mathf.Lerp(distortionStrength, 0f, t);

            if (lensDistortion != null)
                lensDistortion.intensity.value = -strength; // negative = barrel distort

            if (vignette != null)
                vignette.intensity.value = Mathf.Lerp(0.5f, 0.2f, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (lensDistortion != null) lensDistortion.intensity.value = 0f;
        if (vignette != null) vignette.intensity.value = 0.2f;
    }
}