// VignetteTest.cs
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem; // ← new input system

public class VignetteTest : MonoBehaviour
{
    public Volume postProcessVolume;
    private Vignette vignette;

    void Start()
    {
        bool found = postProcessVolume.profile.TryGet(out vignette);
        Debug.Log($"Vignette found: {found}");
    }

    void Update()
    {
        if (Keyboard.current.vKey.wasPressedThisFrame)
        {
            Debug.Log("V pressed - forcing vignette");
            vignette.intensity.value = 0.8f;
            vignette.color.value = Color.red;
        }

        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            vignette.intensity.value = 0f;
        }
    }
}