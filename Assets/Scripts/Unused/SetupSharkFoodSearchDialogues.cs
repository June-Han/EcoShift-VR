// using UnityEngine;
// using UnityEditor;
// using UnityEngine.UI;
// using UnityEngine.XR.Interaction.Toolkit.UI;
// using TMPro;
// using UnityEngine.Events;

// /// <summary>
// /// Editor script to set up the dialogue panel system in the Shark_FoodSearch scene.
// /// Mirrors the Pangolin_FoodSearch scene's Canvas + DialogueManager + DialogueTrigger structure.
// ///
// /// Usage: Open the Shark_FoodSearch scene, then go to Tools > EcoShift > Setup Shark FoodSearch Dialogues
// /// </summary>
// public class SetupSharkFoodSearchDialogues
// {
//     [MenuItem("Tools/EcoShift/Setup Shark FoodSearch Dialogues")]
//     public static void Setup()
//     {
//         // ── 1. Create the World-Space Canvas (mirrors Pangolin Canvas) ──────────
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
//         canvasGO.AddComponent<DialogueInputAdvancer>();
//         // Note: Wire advanceAction in Inspector → bind to XRI RightHand/PrimaryButton

//         // Set Canvas RectTransform (same as Pangolin: 800×300, scale 0.002)
//         RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
//         canvasRect.localScale = new Vector3(0.002f, 0.002f, 1f);
//         canvasRect.sizeDelta = new Vector2(800f, 300f);
//         canvasRect.anchoredPosition = new Vector2(1.121f, 0.313f);

//         // ── 2. Background child (semi-transparent panel behind text) ────────────
//         GameObject bgGO = new GameObject("Background");
//         bgGO.layer = 5;
//         bgGO.transform.SetParent(canvasGO.transform, false);

//         RectTransform bgRect = bgGO.AddComponent<RectTransform>();
//         bgRect.anchorMin = Vector2.zero;
//         bgRect.anchorMax = Vector2.one;
//         bgRect.sizeDelta = Vector2.zero;

//         Image bgImage = bgGO.AddComponent<Image>();
//         bgImage.color = new Color(0f, 0f, 0f, 0.6f); // semi-transparent black

//         // ── 3. Text child (TextMeshProUGUI for dialogue display) ────────────────
//         GameObject textGO = new GameObject("Text");
//         textGO.layer = 5;
//         textGO.transform.SetParent(canvasGO.transform, false);

//         RectTransform textRect = textGO.AddComponent<RectTransform>();
//         textRect.anchorMin = new Vector2(0.05f, 0.15f);
//         textRect.anchorMax = new Vector2(0.95f, 0.95f);
//         textRect.sizeDelta = Vector2.zero;

//         TextMeshProUGUI uiText = textGO.AddComponent<TextMeshProUGUI>();
//         uiText.text = "New Text";
//         uiText.fontSize = 36;
//         uiText.color = Color.white;
//         uiText.alignment = TextAlignmentOptions.TopLeft;
//         uiText.textWrappingMode = TextWrappingModes.Normal;

//         // Wire the DialogueManager's uiText reference
//         dialogueManager.uiText = uiText;

//         // ── 4. "Next (A)" hint label (non-interactive — input handled by DialogueInputAdvancer)
//         GameObject hintGO = new GameObject("NextHint");
//         hintGO.layer = 5;
//         hintGO.transform.SetParent(canvasGO.transform, false);

//         RectTransform hintRect = hintGO.AddComponent<RectTransform>();
//         hintRect.anchorMin = new Vector2(1f, 0f);
//         hintRect.anchorMax = new Vector2(1f, 0f);
//         hintRect.anchoredPosition = new Vector2(-100f, 30f);
//         hintRect.sizeDelta = new Vector2(180f, 40f);

//         TextMeshProUGUI hintText = hintGO.AddComponent<TextMeshProUGUI>();
//         hintText.text = "Next (A)";
//         hintText.fontSize = 22;
//         hintText.color = new Color(1f, 1f, 1f, 0.7f); // slightly transparent white
//         hintText.alignment = TextAlignmentOptions.Right;
//         hintText.fontStyle = FontStyles.Italic;

//         // Start the Canvas hidden (DialogueManager activates it via SetActive)
//         canvasGO.SetActive(false);

//         Debug.Log("[EcoShift] Canvas + DialogueManager + DialogueInputAdvancer created.");

//         // ── 5. Dialogues parent container ───────────────────────────────────────
//         GameObject dialoguesParent = new GameObject("Dialogues");
//         dialoguesParent.transform.position = Vector3.zero;

//         // ── 6. PANEL 1 — Welcome / Introduction (triggerOnStart) ────────────────
//         GameObject welcomeGO = new GameObject("WelcomeDialogue");
//         welcomeGO.transform.SetParent(dialoguesParent.transform, false);

//         DialogueTrigger welcomeTrigger = welcomeGO.AddComponent<DialogueTrigger>();
//         welcomeTrigger.dialogueManager = dialogueManager;
//         welcomeTrigger.sentences = new string[]
//         {
//             "Welcome to the open ocean!",
//             "Tilt your head for direction, and use your left joystick to swim",
//             "You are a Blue Shark, one of the most widespread yet increasingly threatened shark species on Earth.",
//             "Let\u2019s hunt for food! Blue Sharks are sleek, long-distance hunters that feed on squid, fish, and smaller sharks. Look for a school of fish.\r\n"
//         };
//         welcomeTrigger.triggerOnce = true;
//         welcomeTrigger.triggerOnStart = true;

//         Debug.Log("[EcoShift] Panel 1 (Welcome) created — triggers on scene start.");

//         // ── 7. PANEL 2 — First Prey Interaction ────────────────────────────────
//         // This panel is triggered by FishClickable.firstClickDialogue, not by a collider.
//         // Create it as a standalone GameObject; wire it to fish prefabs via FishClickable.
//         GameObject firstCatchGO = new GameObject("FirstCatchDialogue");
//         firstCatchGO.transform.SetParent(dialoguesParent.transform, false);

//         DialogueTrigger firstCatchTrigger = firstCatchGO.AddComponent<DialogueTrigger>();
//         firstCatchTrigger.dialogueManager = dialogueManager;
//         firstCatchTrigger.sentences = new string[]
//         {
//             "Blue Sharks are built for endurance \u2014 their long pectoral fins act like glider wings, letting them cruise vast distances with minimal effort. They can detect prey using electroreceptors called ampullae of Lorenzini.",
//             "Swim close and click the right trigger to strike!"
//         };
//         firstCatchTrigger.triggerOnce = true;
//         firstCatchTrigger.triggerOnStart = false;

//         Debug.Log("[EcoShift] Panel 2 (First Catch) created — wire to FishClickable.firstClickDialogue.");

//         // ── 8. PANEL 3 — Ecology / Successful Catch ────────────────────────────
//         // This is triggered by the first insect click callback in Pangolin;
//         // here we create it as a standalone trigger for Score1Dialogue equivalent.
//         GameObject ecologyGO = new GameObject("EcologyDialogue");
//         ecologyGO.transform.SetParent(dialoguesParent.transform, false);

//         DialogueTrigger ecologyTrigger = ecologyGO.AddComponent<DialogueTrigger>();
//         ecologyTrigger.dialogueManager = dialogueManager;
//         ecologyTrigger.sentences = new string[]
//         {
//             "Nice catch! Blue Sharks help maintain healthy ocean food webs by keeping prey populations in check. Without them, fish and squid populations can explode, destabilising marine ecosystems from the top down.\r\n"
//         };
//         ecologyTrigger.triggerOnce = true;
//         ecologyTrigger.triggerOnStart = false;

//         Debug.Log("[EcoShift] Panel 3 (Ecology) created.");

//         // ── 9. PANEL 4 — Debris / Ghost Net Encounter (collider-based) ─────────
//         // In Pangolin this is on the Dialogue2 object with a collider.
//         // Position near where debris objects will be placed.
//         GameObject debrisGO = new GameObject("DebrisDialogue");
//         debrisGO.transform.SetParent(dialoguesParent.transform, false);

//         DialogueTrigger debrisTrigger = debrisGO.AddComponent<DialogueTrigger>();
//         debrisTrigger.dialogueManager = dialogueManager;
//         debrisTrigger.sentences = new string[]
//         {
//             "Watch out \u2014 that\u2019s discarded fishing gear, also known as \u2018ghost nets.\u2019",
//             "Blue Sharks are one of the most frequently caught bycatch species in the world, often entangled in longlines and drift nets set for tuna and swordfish.\r\n"
//         };
//         debrisTrigger.triggerOnce = true;
//         debrisTrigger.triggerOnStart = false;

//         Debug.Log("[EcoShift] Panel 4 (Debris Encounter) created — wire to DebrisClickable.firstClickDialogue.");

//         // ── 10. PANEL 5 — Pollution Warning ────────────────────────────────────
//         // Standalone trigger, activated by DebrisClickable on subsequent clicks
//         // or via a separate trigger zone.
//         GameObject pollutionGO = new GameObject("PollutionDialogue");
//         pollutionGO.transform.SetParent(dialoguesParent.transform, false);

//         DialogueTrigger pollutionTrigger = pollutionGO.AddComponent<DialogueTrigger>();
//         pollutionTrigger.dialogueManager = dialogueManager;
//         pollutionTrigger.sentences = new string[]
//         {
//             "Careful! Blue Sharks have been found with microplastics and heavy metals like mercury accumulated in their tissue. As a top predator, toxins biomagnify through every level of the food chain \u2014 eventually reaching humans who consume seafood.\r\n"
//         };
//         pollutionTrigger.triggerOnce = true;
//         pollutionTrigger.triggerOnStart = false;

//         Debug.Log("[EcoShift] Panel 5 (Pollution Warning) created.");

//         // ── 11. PANEL 6 — Completion / Fun Fact (triggered by ScoreManager) ────
//         GameObject completionGO = new GameObject("CompletionDialogue");
//         completionGO.transform.SetParent(dialoguesParent.transform, false);

//         DialogueTrigger completionTrigger = completionGO.AddComponent<DialogueTrigger>();
//         completionTrigger.dialogueManager = dialogueManager;
//         completionTrigger.sentences = new string[]
//         {
//             "Congrats! You\u2019ve found enough food.\r\n",
//             "Fun fact: Blue Sharks are one of the greatest travellers in the ocean \u2014 a single shark can migrate over 9,000 kilometres across the Atlantic! Yet despite their range, an estimated 20 million Blue Sharks are killed each year, mostly as bycatch.\r\n"
//         };
//         completionTrigger.triggerOnce = true;
//         completionTrigger.triggerOnStart = false;

//         Debug.Log("[EcoShift] Panel 6 (Completion) created — wire to ScoreManager.gameCompleteDialogue.");

//         // ── 12. ScoreManager + DistortionManager holder ────────────────────────
//         GameObject managerGO = new GameObject("GameManagers");

//         ScoreManager scoreManager = managerGO.AddComponent<ScoreManager>();
//         scoreManager.maxScore = 10;
//         scoreManager.gameCompleteDialogue = completionTrigger;

//         DistortionManager distortionManager = managerGO.AddComponent<DistortionManager>();
//         // Note: postProcessVolume needs to be wired manually to the scene's Global Volume

//         Debug.Log("[EcoShift] ScoreManager (maxScore=10) + DistortionManager created.");

//         // ── 13. Fish Target System — lock-on + trigger capture ─────────────────
//         GameObject targetSystemGO = new GameObject("FishTargetSystem");
//         FishTargetSystem targetSystem = targetSystemGO.AddComponent<FishTargetSystem>();
//         targetSystem.targetRadius = 12f;
//         targetSystem.viewConeAngle = 45f;
//         targetSystem.captureDistance = 8f;
//         targetSystem.scanInterval = 0.15f;
//         // Note: triggerAction needs manual wiring → XRI RightHand Interaction/Activate Value

//         Debug.Log("[EcoShift] FishTargetSystem created — wire triggerAction to XRI RightHand/Activate Value.");

//         // ── 14. Score UI ─────────────────────────────────────────────────────
//         // The Score Canvas lives on the persistent XR Origin in Main_Scene (like Pangolin).
//         // The existing scoreUIPrefab on HandSwapper is reused — just call EquipFinsWithScoreUI()
//         // when entering this scene. ScoreManager auto-binds to TextMeshProUGUI tagged "Score".
//         // See manual wiring steps below for setup in Main_Scene.

//         // ── 15. Mark scene dirty so changes can be saved ────────────────────────
//         UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
//             UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
//         );

//         Debug.Log("══════════════════════════════════════════════════════════");
//         Debug.Log("[EcoShift] Shark FoodSearch dialogue setup complete!");
//         Debug.Log("REMAINING MANUAL WIRING:");
//         Debug.Log("  1. On each Fish prefab: Add FishClickable (firstClickDialogue auto-finds at runtime)");
//         Debug.Log("  2. On each Debris prefab: Add DebrisClickable (firstClickDialogue auto-finds at runtime)");
//         Debug.Log("  3. DistortionManager.postProcessVolume: Drag in the scene's Global Volume (optional)");
//         Debug.Log("  4. Canvas → DialogueInputAdvancer → advanceAction: Bind to XRI RightHand/Jump (A button)");
//         Debug.Log("  5. FishTargetSystem → triggerAction: Bind to XRI Right Interaction/Activate Value (trigger)");
//         Debug.Log("  6. SCORE UI (in Main_Scene): The scoreUIPrefab on HandSwapper already exists on the persistent XR Origin.");
//         Debug.Log("     Use EquipFinsWithScoreUI() instead of EquipFins() when entering this scene to show the score.");
//         Debug.Log("     Make sure the score text object is tagged 'Score' so ScoreManager auto-binds at runtime.");
//         Debug.Log("══════════════════════════════════════════════════════════");

//         // Select the Dialogues parent so the user can see the hierarchy
//         Selection.activeGameObject = dialoguesParent;
//     }
// }
