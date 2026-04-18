// using UnityEngine;
// using UnityEditor;
// using UnityEngine.UI;
// using UnityEngine.XR.Interaction.Toolkit.UI;
// using UnityEngine.InputSystem;
// using TMPro;

// /// <summary>
// /// Editor script to set up the dialogue panel system in the Shark_Threat scene.
// /// Uses the Popup_Panels prefab for consistent styling across all scenes.
// ///
// /// Usage: Open the Shark_Threat scene, then go to Tools > EcoShift > Setup Shark Threat Dialogues
// /// </summary>
// public class SetupSharkThreatDialogues
// {
//     // Paths to shared assets used across all scenes
//     private const string PopupPanelsPrefabPath = "Assets/Miscel_Assets/Popup_Panels.prefab";
//     private const string InputActionsPath = "Assets/Samples/XR Interaction Toolkit/3.3.1/Starter Assets/XRI Default Input Actions.inputactions";

//     [MenuItem("Tools/EcoShift/Setup Shark Threat Dialogues")]
//     public static void Setup()
//     {
//         // ── 1. Create the World-Space Canvas (matches Shark_FoodSearch & Pangolin scenes) ──
//         GameObject canvasGO = new GameObject("Canvas");
//         canvasGO.layer = 5; // UI layer

//         Canvas canvas = canvasGO.AddComponent<Canvas>();
//         canvas.renderMode = RenderMode.WorldSpace;

//         CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
//         scaler.referencePixelsPerUnit = 100;

//         canvasGO.AddComponent<GraphicRaycaster>();
//         canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>(); // XR controller support

//         // HeadFollowUI — keeps the canvas in front of the player's head
//         HeadFollowUI headFollow = canvasGO.AddComponent<HeadFollowUI>();
//         headFollow.distance = 2f;
//         headFollow.smoothTime = 0.3f;

//         // DialogueManager — the singleton that drives text display
//         DialogueManager dialogueManager = canvasGO.AddComponent<DialogueManager>();

//         // DialogueInputAdvancer — lets the player press A to advance dialogue
//         DialogueInputAdvancer advancer = canvasGO.AddComponent<DialogueInputAdvancer>();

//         // Wire advanceAction to the XRI Default Input Actions (A button)
//         WireAdvanceAction(advancer);

//         // Set Canvas RectTransform (850×300, scale 0.002 — matches Shark_FoodSearch)
//         RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
//         canvasRect.localScale = new Vector3(0.002f, 0.002f, 1f);
//         canvasRect.sizeDelta = new Vector2(850f, 300f);
//         canvasRect.anchoredPosition = new Vector2(1.121f, 0.313f);

//         // ── 2. Instantiate the Popup_Panels prefab (consistent styling with all scenes) ──
//         GameObject popupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopupPanelsPrefabPath);
//         if (popupPrefab == null)
//         {
//             Debug.LogError($"[EcoShift] Could not load Popup_Panels prefab at: {PopupPanelsPrefabPath}");
//             Debug.LogError("[EcoShift] Aborting setup. Make sure the prefab exists.");
//             return;
//         }

//         GameObject popupInstance = (GameObject)PrefabUtility.InstantiatePrefab(popupPrefab);
//         popupInstance.name = "Popup_Panels";
//         popupInstance.transform.SetParent(canvasGO.transform, false);
//         popupInstance.layer = 5;

//         // Reset the Popup_Panels RectTransform so it sits cleanly inside the Canvas
//         RectTransform popupRect = popupInstance.GetComponent<RectTransform>();
//         popupRect.anchoredPosition = Vector2.zero;

//         // Adjust InnerPanel size to fit the Canvas (matches Shark_FoodSearch overrides)
//         Transform innerPanel = popupInstance.transform.Find("InnerPanel");
//         if (innerPanel != null)
//         {
//             RectTransform innerRect = innerPanel.GetComponent<RectTransform>();
//             innerRect.sizeDelta = new Vector2(800f, 290f);
//             innerRect.anchoredPosition = new Vector2(0f, 40f);
//         }

//         // ── 3. Remove the Buttons child (Shark scenes use controller A button, not clickable button) ──
//         Transform buttonsTransform = popupInstance.transform.Find("Buttons");
//         if (buttonsTransform != null)
//         {
//             Object.DestroyImmediate(buttonsTransform.gameObject);
//             Debug.Log("[EcoShift] Removed Buttons from Popup_Panels (Shark scenes use controller input).");
//         }

//         // ── 4. Wire the Text inside Popup_Panels to DialogueManager ─────────────
//         TextMeshProUGUI uiText = null;
//         if (innerPanel != null)
//         {
//             Transform textTransform = innerPanel.Find("Text");
//             if (textTransform != null)
//             {
//                 uiText = textTransform.GetComponent<TextMeshProUGUI>();
//             }
//         }

//         if (uiText != null)
//         {
//             uiText.text = "";
//             dialogueManager.uiText = uiText;
//             Debug.Log("[EcoShift] Wired Popup_Panels/InnerPanel/Text to DialogueManager.uiText.");
//         }
//         else
//         {
//             Debug.LogWarning("[EcoShift] Could not find Text inside Popup_Panels/InnerPanel. Wire uiText manually.");
//         }

//         // ── 5. "Next(A)" hint label (non-interactive — matches Shark_FoodSearch) ──
//         GameObject hintGO = new GameObject("NextHint");
//         hintGO.transform.SetParent(canvasGO.transform, false);

//         RectTransform hintRect = hintGO.AddComponent<RectTransform>();
//         hintRect.anchorMin = new Vector2(1f, 0f);
//         hintRect.anchorMax = new Vector2(1f, 0f);
//         hintRect.pivot = new Vector2(1f, 0f);
//         hintRect.anchoredPosition = new Vector2(-10f, -15f);
//         hintRect.sizeDelta = new Vector2(180f, 40f);

//         TextMeshProUGUI hintText = hintGO.AddComponent<TextMeshProUGUI>();
//         hintText.text = "Next(A)";
//         hintText.fontSize = 36;
//         hintText.color = new Color(0.451f, 0.325f, 0.220f, 1f); // warm brown matching Popup_Panels text
//         hintText.alignment = TextAlignmentOptions.Center;
//         hintText.overflowMode = TextOverflowModes.Ellipsis;
//         hintText.textWrappingMode = TextWrappingModes.Normal;
//         hintText.lineSpacing = 17.1f;

//         // Start the Canvas hidden (DialogueManager activates it via SetActive)
//         canvasGO.SetActive(false);

//         Debug.Log("[EcoShift] Canvas + Popup_Panels + DialogueInputAdvancer created for Shark Threat.");

//         // ── 5. Dialogues parent container ───────────────────────────────────────
//         GameObject dialoguesParent = new GameObject("Dialogues");
//         dialoguesParent.transform.position = Vector3.zero;

//         // ── 6. PANEL 7 — Threat Introduction (triggerOnStart) ───────────────────
//         GameObject threatIntroGO = new GameObject("ThreatIntroDialogue");
//         threatIntroGO.transform.SetParent(dialoguesParent.transform, false);

//         DialogueTrigger threatIntroTrigger = threatIntroGO.AddComponent<DialogueTrigger>();
//         threatIntroTrigger.dialogueManager = dialogueManager;
//         threatIntroTrigger.sentences = new string[]
//         {
//             "Listen\u2026 hear that rumbling? Those are the engines of commercial fishing boats overhead. The noise disrupts how marine animals communicate, navigate, and find food.",
//             "These trawlers drag massive nets through the water to catch fish \u2014 but sharks, dolphins, turtles, and other ocean life get swept up too. This is called bycatch, and it\u2019s one of the greatest threats to marine biodiversity.",
//             "Blue Sharks are one of the most frequently caught bycatch species in the world. An estimated 20 million are killed each year, most of them unintentionally.",
//             "The fishermen above are casting their nets down. Dodge them! If you get caught three times, it\u2019s game over. Survive until the boats move on!"
//         };
//         threatIntroTrigger.triggerOnce = true;
//         threatIntroTrigger.triggerOnStart = true;

//         Debug.Log("[EcoShift] Panel 7 (Threat Intro) created — triggers on scene start.");

//         // ── 7. PANEL 8 — Shark Finning Awareness (collider-based mid-scene) ────
//         GameObject finningGO = new GameObject("FinningDialogue");
//         finningGO.transform.SetParent(dialoguesParent.transform, false);

//         // Add a trigger collider so this fires when the player swims through a zone
//         BoxCollider finningCollider = finningGO.AddComponent<BoxCollider>();
//         finningCollider.isTrigger = true;
//         finningCollider.size = new Vector3(10f, 10f, 10f);

//         DialogueTrigger finningTrigger = finningGO.AddComponent<DialogueTrigger>();
//         finningTrigger.dialogueManager = dialogueManager;
//         finningTrigger.sentences = new string[]
//         {
//             "Blue Sharks are the most heavily fished shark species on the planet.",
//             "Their fins are among the most commonly traded in the global shark fin market, where a single fin can sell for hundreds of dollars.\r\n"
//         };
//         finningTrigger.triggerOnce = true;
//         finningTrigger.triggerOnStart = false;

//         Debug.Log("[EcoShift] Panel 8 (Finning Awareness) created — position the collider mid-scene.");

//         // ── 8. PANEL 9 — Escape / Deep Dive (collider-based near safe zone) ────
//         GameObject escapeGO = new GameObject("EscapeDialogue");
//         escapeGO.transform.SetParent(dialoguesParent.transform, false);

//         BoxCollider escapeCollider = escapeGO.AddComponent<BoxCollider>();
//         escapeCollider.isTrigger = true;
//         escapeCollider.size = new Vector3(10f, 10f, 10f);

//         DialogueTrigger escapeTrigger = escapeGO.AddComponent<DialogueTrigger>();
//         escapeTrigger.dialogueManager = dialogueManager;
//         escapeTrigger.sentences = new string[]
//         {
//             "Dive deep to escape! Blue Sharks can dive to depths of over 350 metres to avoid threats.",
//             "But as ocean temperatures rise, oxygen-poor \u2018dead zones\u2019 are expanding in the deep sea, shrinking the safe waters sharks depend on.\r\n"
//         };
//         escapeTrigger.triggerOnce = true;
//         escapeTrigger.triggerOnStart = false;

//         Debug.Log("[EcoShift] Panel 9 (Escape) created — position the collider near the safe zone.");

//         // ── 9. Mark scene dirty ─────────────────────────────────────────────────
//         UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
//             UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
//         );

//         Debug.Log("══════════════════════════════════════════════════════════");
//         Debug.Log("[EcoShift] Shark Threat dialogue setup complete!");
//         Debug.Log("REMAINING MANUAL WIRING:");
//         Debug.Log("  1. Position 'FinningDialogue' collider where the player will swim mid-escape");
//         Debug.Log("  2. Position 'EscapeDialogue' collider near the deep reef / safe zone");
//         Debug.Log("  3. If advanceAction was not auto-wired, bind DialogueInputAdvancer → XRI RightHand/PrimaryButton");
//         Debug.Log("  4. (Optional) Add FloatingArrow objects and wire onDialogueFinished events");
//         Debug.Log("══════════════════════════════════════════════════════════");

//         Selection.activeGameObject = dialoguesParent;
//     }

//     /// <summary>
//     /// Attempts to wire the DialogueInputAdvancer's advanceAction to the XRI Default Input Actions'
//     /// right-hand A button (PrimaryButton), matching the Shark_FoodSearch scene's configuration.
//     /// </summary>
//     private static void WireAdvanceAction(DialogueInputAdvancer advancer)
//     {
//         // Load all sub-assets from the XRI Default Input Actions asset
//         // InputActionReferences are stored as sub-assets of the .inputactions file
//         Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath);

//         if (allAssets == null || allAssets.Length == 0)
//         {
//             Debug.LogWarning("[EcoShift] Could not load XRI Default Input Actions. Wire advanceAction manually.");
//             return;
//         }

//         foreach (Object asset in allAssets)
//         {
//             InputActionReference actionRef = asset as InputActionReference;
//             if (actionRef != null && actionRef.action != null)
//             {
//                 // Look for the right-hand A button (PrimaryButton) action
//                 // This matches the binding used in Shark_FoodSearch
//                 string actionName = actionRef.action.name;
//                 string mapName = actionRef.action.actionMap?.name ?? "";

//                 if (actionName == "PrimaryButton" && mapName.Contains("RightHand"))
//                 {
//                     advancer.advanceAction = actionRef;
//                     Debug.Log($"[EcoShift] Auto-wired advanceAction → {mapName}/{actionName}");
//                     return;
//                 }
//             }
//         }

//         // Fallback: try any PrimaryButton action
//         foreach (Object asset in allAssets)
//         {
//             InputActionReference actionRef = asset as InputActionReference;
//             if (actionRef != null && actionRef.action != null && actionRef.action.name == "PrimaryButton")
//             {
//                 advancer.advanceAction = actionRef;
//                 Debug.Log($"[EcoShift] Auto-wired advanceAction → {actionRef.action.actionMap?.name}/{actionRef.action.name} (fallback)");
//                 return;
//             }
//         }

//         Debug.LogWarning("[EcoShift] Could not find PrimaryButton action. Wire advanceAction manually in Inspector.");
//     }
// }
