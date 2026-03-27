using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// Editor script to set up the dialogue panel system in the Shark_Threat scene.
/// Mirrors the Pangolin_Threat scene's structure with Blue Shark threat content.
///
/// Usage: Open the Shark_Threat scene, then go to Tools > EcoShift > Setup Shark Threat Dialogues
/// </summary>
public class SetupSharkThreatDialogues
{
    [MenuItem("Tools/EcoShift/Setup Shark Threat Dialogues")]
    public static void Setup()
    {
        // ── 1. Create the World-Space Canvas (same as FoodSearch) ───────────────
        GameObject canvasGO = new GameObject("Canvas");
        canvasGO.layer = 5;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.referencePixelsPerUnit = 100;

        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>(); // XR controller support

        HeadFollowUI headFollow = canvasGO.AddComponent<HeadFollowUI>();
        headFollow.distance = 2f;
        headFollow.smoothTime = 0.3f;

        DialogueManager dialogueManager = canvasGO.AddComponent<DialogueManager>();

        // DialogueInputAdvancer — lets the player press A to advance dialogue
        canvasGO.AddComponent<DialogueInputAdvancer>();
        // Note: Wire advanceAction in Inspector → bind to XRI RightHand/PrimaryButton

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.localScale = new Vector3(0.002f, 0.002f, 1f);
        canvasRect.sizeDelta = new Vector2(800f, 300f);
        canvasRect.anchoredPosition = new Vector2(1.121f, 0.313f);

        // ── 2. Background ───────────────────────────────────────────────────────
        GameObject bgGO = new GameObject("Background");
        bgGO.layer = 5;
        bgGO.transform.SetParent(canvasGO.transform, false);

        RectTransform bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.6f);

        // ── 3. Text ─────────────────────────────────────────────────────────────
        GameObject textGO = new GameObject("Text");
        textGO.layer = 5;
        textGO.transform.SetParent(canvasGO.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0.15f);
        textRect.anchorMax = new Vector2(0.95f, 0.95f);
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI uiText = textGO.AddComponent<TextMeshProUGUI>();
        uiText.text = "New Text";
        uiText.fontSize = 36;
        uiText.color = Color.white;
        uiText.alignment = TextAlignmentOptions.TopLeft;
        uiText.textWrappingMode = TextWrappingModes.Normal;

        dialogueManager.uiText = uiText;

        // ── 4. "Next (A)" hint label (non-interactive — input handled by DialogueInputAdvancer)
        GameObject hintGO = new GameObject("NextHint");
        hintGO.layer = 5;
        hintGO.transform.SetParent(canvasGO.transform, false);

        RectTransform hintRect = hintGO.AddComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(1f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.anchoredPosition = new Vector2(-100f, 30f);
        hintRect.sizeDelta = new Vector2(180f, 40f);

        TextMeshProUGUI hintText = hintGO.AddComponent<TextMeshProUGUI>();
        hintText.text = "Next (A)";
        hintText.fontSize = 22;
        hintText.color = new Color(1f, 1f, 1f, 0.7f); // slightly transparent white
        hintText.alignment = TextAlignmentOptions.Right;
        hintText.fontStyle = FontStyles.Italic;

        canvasGO.SetActive(false);

        Debug.Log("[EcoShift] Canvas + DialogueManager + DialogueInputAdvancer created for Shark Threat.");

        // ── 5. Dialogues parent container ───────────────────────────────────────
        GameObject dialoguesParent = new GameObject("Dialogues");
        dialoguesParent.transform.position = Vector3.zero;

        // ── 6. PANEL 7 — Threat Introduction (triggerOnStart) ───────────────────
        GameObject threatIntroGO = new GameObject("ThreatIntroDialogue");
        threatIntroGO.transform.SetParent(dialoguesParent.transform, false);

        DialogueTrigger threatIntroTrigger = threatIntroGO.AddComponent<DialogueTrigger>();
        threatIntroTrigger.dialogueManager = dialogueManager;
        threatIntroTrigger.sentences = new string[]
        {
            "Oh no! That\u2019s a longliner above \u2014 a fishing vessel trailing hundreds of baited hooks across kilometres of ocean.",
            "Longline fishing is the single biggest killer of Blue Sharks worldwide.\r\n",
            "Swim to safety! Dive deep to avoid the hooks!"
        };
        threatIntroTrigger.triggerOnce = true;
        threatIntroTrigger.triggerOnStart = true;

        Debug.Log("[EcoShift] Panel 7 (Threat Intro) created — triggers on scene start.");

        // ── 7. PANEL 8 — Shark Finning Awareness (collider-based mid-scene) ────
        GameObject finningGO = new GameObject("FinningDialogue");
        finningGO.transform.SetParent(dialoguesParent.transform, false);

        // Add a trigger collider so this fires when the player swims through a zone
        BoxCollider finningCollider = finningGO.AddComponent<BoxCollider>();
        finningCollider.isTrigger = true;
        finningCollider.size = new Vector3(10f, 10f, 10f);

        DialogueTrigger finningTrigger = finningGO.AddComponent<DialogueTrigger>();
        finningTrigger.dialogueManager = dialogueManager;
        finningTrigger.sentences = new string[]
        {
            "Blue Sharks are the most heavily fished shark species on the planet.",
            "Their fins are among the most commonly traded in the global shark fin market, where a single fin can sell for hundreds of dollars.\r\n"
        };
        finningTrigger.triggerOnce = true;
        finningTrigger.triggerOnStart = false;

        Debug.Log("[EcoShift] Panel 8 (Finning Awareness) created — position the collider mid-scene.");

        // ── 8. PANEL 9 — Escape / Deep Dive (collider-based near safe zone) ────
        GameObject escapeGO = new GameObject("EscapeDialogue");
        escapeGO.transform.SetParent(dialoguesParent.transform, false);

        BoxCollider escapeCollider = escapeGO.AddComponent<BoxCollider>();
        escapeCollider.isTrigger = true;
        escapeCollider.size = new Vector3(10f, 10f, 10f);

        DialogueTrigger escapeTrigger = escapeGO.AddComponent<DialogueTrigger>();
        escapeTrigger.dialogueManager = dialogueManager;
        escapeTrigger.sentences = new string[]
        {
            "Dive deep to escape! Blue Sharks can dive to depths of over 350 metres to avoid threats.",
            "But as ocean temperatures rise, oxygen-poor \u2018dead zones\u2019 are expanding in the deep sea, shrinking the safe waters sharks depend on.\r\n"
        };
        escapeTrigger.triggerOnce = true;
        escapeTrigger.triggerOnStart = false;

        Debug.Log("[EcoShift] Panel 9 (Escape) created — position the collider near the safe zone.");

        // ── 9. Mark scene dirty ─────────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        Debug.Log("══════════════════════════════════════════════════════════");
        Debug.Log("[EcoShift] Shark Threat dialogue setup complete!");
        Debug.Log("REMAINING MANUAL WIRING:");
        Debug.Log("  1. Position 'FinningDialogue' collider where the player will swim mid-escape");
        Debug.Log("  2. Position 'EscapeDialogue' collider near the deep reef / safe zone");
        Debug.Log("  3. Add a boat/longliner model from Marine/Assets/Boats LowPoly/");
        Debug.Log("  4. (Optional) Add FloatingArrow objects and wire onDialogueFinished events");
        Debug.Log("══════════════════════════════════════════════════════════");

        Selection.activeGameObject = dialoguesParent;
    }
}
