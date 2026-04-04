using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// After the Shark_Threat dialogue panels finish, fades to black,
/// rotates the player to face the boat, fades back in, then starts gameplay.
///
/// Usage: Wire DialogueTrigger's onDialogueFinished → StartIntroSequence()
/// </summary>
public class SharkThreatIntro : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The boat GameObject to look at. Auto-found via PoacherBoatManager if not assigned.")]
    public Transform boatTransform;

    [Tooltip("The PoacherBoatManager to disable during intro. Auto-found if not assigned.")]
    public PoacherBoatManager boatManager;

    [Header("Timing")]
    [Tooltip("Duration of each fade transition in seconds.")]
    public float fadeDuration = 0.6f;

    // Fade overlay
    private Canvas fadeCanvas;
    private Image fadeImage;

    // XR Origin reference
    private Transform xrOrigin;

    void Start()
    {
        if (boatManager == null)
            boatManager = FindFirstObjectByType<PoacherBoatManager>();

        if (boatManager != null)
        {
            if (boatTransform == null)
                boatTransform = boatManager.transform;

            // Disable the boat AI until the intro finishes
            boatManager.enabled = false;
        }

        xrOrigin = FindXROrigin();
        CreateFadeCanvas();
    }

    /// <summary>
    /// Call this from DialogueTrigger's onDialogueFinished event.
    /// </summary>
    public void StartIntroSequence()
    {
        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        if (xrOrigin == null || boatTransform == null)
        {
            Debug.LogWarning("[SharkThreatIntro] Missing XR Origin or boat — skipping intro.");
            EnableGameplay();
            yield break;
        }

        // Fade to black → rotate to face boat → fade in
        yield return StartCoroutine(FadeOut());
        RotateXROriginToFace(boatTransform.position);
        yield return StartCoroutine(FadeIn());

        // Start gameplay with the player already looking at the boat
        EnableGameplay();

        Debug.Log("[SharkThreatIntro] Intro complete — player is facing the boat.");
    }

    private void EnableGameplay()
    {
        if (boatManager != null)
            boatManager.enabled = true;
    }

    /// <summary>
    /// Rotates the XR Origin so the player's forward direction faces the target.
    /// Only rotates on the Y axis to keep the player upright.
    /// Accounts for where the headset is actually looking.
    /// </summary>
    private void RotateXROriginToFace(Vector3 targetPosition)
    {
        if (xrOrigin == null) return;

        Vector3 direction = targetPosition - xrOrigin.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f) return;

        Camera cam = Camera.main;
        if (cam != null)
        {
            Quaternion desiredWorldRot = Quaternion.LookRotation(direction.normalized, Vector3.up);

            Vector3 camWorldForward = cam.transform.forward;
            camWorldForward.y = 0f;
            Quaternion currentCamRot = Quaternion.LookRotation(camWorldForward.normalized, Vector3.up);

            Quaternion offset = desiredWorldRot * Quaternion.Inverse(currentCamRot);
            xrOrigin.rotation = offset * xrOrigin.rotation;
        }
        else
        {
            xrOrigin.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private Transform FindXROrigin()
    {
        var xrOriginType = System.Type.GetType("Unity.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils");
        if (xrOriginType != null)
        {
            var xrComp = FindFirstObjectByType(xrOriginType) as Component;
            if (xrComp != null)
                return xrComp.transform;
        }

        GameObject go = GameObject.Find("XR Origin");
        if (go == null) go = GameObject.Find("XR Origin (XR Rig)");
        if (go == null) go = GameObject.Find("XROrigin");

        return go != null ? go.transform : null;
    }

    #region Fade Overlay

    private void CreateFadeCanvas()
    {
        GameObject canvasGO = new GameObject("IntroFadeCanvas");
        canvasGO.transform.SetParent(transform, false);

        fadeCanvas = canvasGO.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999;

        canvasGO.AddComponent<CanvasScaler>();

        GameObject imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(canvasGO.transform, false);

        fadeImage = imageGO.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;

        RectTransform rt = fadeImage.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private IEnumerator FadeOut()
    {
        yield return StartCoroutine(Fade(0f, 1f));
    }

    private IEnumerator FadeIn()
    {
        yield return StartCoroutine(Fade(1f, 0f));
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;
        Color c = fadeImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = to;
        fadeImage.color = c;
    }

    #endregion
}
