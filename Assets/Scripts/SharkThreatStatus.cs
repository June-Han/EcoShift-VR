using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using System.Linq;

public class SharkThreatStatus : MonoBehaviour
{
    [Header("Player Health")]
    public int maxLives = 3;
    private int currentLives;
    private bool isGameOver = false;

    [Header("Timer")]
    [Tooltip("Total time in seconds the player has to survive.")]
    public float totalTime = 60f;
    [Tooltip("Assign manually, or leave empty — one will be created on the Lives Canvas.")]
    public TMP_Text timerText;
    private float timeRemaining;
    private bool timerRunning = false;

    [Header("UI")]
    [Tooltip("Auto-found at runtime if not assigned. Looks for LivesUI in the scene.")]
    public LivesUI livesUI;

    [Header("Haptic Feedback")]
    [Tooltip("Vibration strength when hit by net (0-1).")]
    [Range(0f, 1f)]
    public float hapticAmplitude = 0.8f;
    [Tooltip("How long the vibration lasts in seconds.")]
    public float hapticDuration = 0.5f;

    [Header("End-Game Dialogues")]
    [Tooltip("Auto-created if not assigned. Edit sentences on the child DialogueTrigger.")]
    public DialogueTrigger survivedDialogue;

    [Tooltip("Auto-created if not assigned. Edit sentences on the child DialogueTrigger.")]
    public DialogueTrigger gameOverDialogue;

    // --- REEL VARIABLES ---
    [Header("Cinematic Reeling Settings")]
    [Tooltip("The Y-level of the ocean surface/boat")]
    public float surfaceHeightY = 30f; 
    [Tooltip("How fast objects are pulled to the surface")]
    public float reelSpeed = 1f;

    [Tooltip("The net prefab used for the ending cinematic")]
    public GameObject cinematicNetPrefab;

    // [Tooltip("The 3D model of the shark to represent the player getting caught")]
    // public GameObject dummyPlayerSharkPrefab;

    [Header("Cinematic Net Visuals")]
    public float openScale = 180f;       // How wide the net opens (X and Y)
    public float stretchScaleZ = 700f;  // How long the tail stretches (Z)
    public float scaleSpeed = 100f;      // How fast it deforms


    [HideInInspector] public Transform boatTarget;

    // --- HIGHLIGHT VARIABLES ---
    [Tooltip("The neon color the boat and fish will glow during the ending")]
    [ColorUsage(true, true)] // This allows you to make the color HDR (super bright!) in the Inspector
    public Color cinematicHighlightColor = Color.cyan; 
    public float glowIntensity = 8f;

    private SceneChanger sceneChanger;

    //void Start()
    // private void Start()
    // {
    //     currentLives = maxLives;

    //     // Auto-find LivesUI
    //     if (livesUI == null)
    //         livesUI = FindFirstObjectByType<LivesUI>();

    //     if (livesUI == null)
    //     {
    //         foreach (LivesUI lui in Resources.FindObjectsOfTypeAll<LivesUI>())
    //         {
    //             if (lui.gameObject.scene.isLoaded || lui.gameObject.scene.name != null)
    //             {
    //                 livesUI = lui;
    //                 if (!lui.gameObject.activeInHierarchy)
    //                     lui.gameObject.SetActive(true);
    //                 break;
    //             }
    //         }
    //     }
        
    //     if (sceneChanger== null)
    //         sceneChanger = FindFirstObjectByType<SceneChanger>();
        
    //     if (sceneChanger == null)
    //     {
    //         Debug.LogWarning("Could not find SceneChanger in the scene! Is the XR Origin loaded?");
    //     }

    //     // Create a timer on the Lives Canvas if not assigned
    //     if (timerText == null && livesUI != null)
    //     {
    //         timerText = CreateTimerOnLivesCanvas();
    //     }

    //     if (survivedDialogue == null)
    //         Debug.LogWarning("[SharkThreatStatus] No SurvivedDialogue assigned. Run Tools → EcoShift → Create Shark End-Game Dialogues.");
    //     if (gameOverDialogue == null)
    //         Debug.LogWarning("[SharkThreatStatus] No GameOverDialogue assigned. Run Tools → EcoShift → Create Shark End-Game Dialogues.");

    //     if (livesUI == null)
    //         Debug.LogWarning("[SharkThreatStatus] No LivesUI found.");
    //     if (timerText == null)
    //         Debug.LogWarning("[SharkThreatStatus] No timer text created.");
    // }

    // Awake runs exactly once when the XR Rig is born. 
    // Grab all our references so they are ready before anything else happens!
    private void Awake()
    {
        // Using FindObjectsInactive.Include is the modern, cleanest way to find hidden UI!
        if (livesUI == null)
            livesUI = FindFirstObjectByType<LivesUI>(FindObjectsInactive.Include);
        
        if (sceneChanger == null)
            sceneChanger = FindFirstObjectByType<SceneChanger>(FindObjectsInactive.Include);
        
        if (sceneChanger == null)
            Debug.LogWarning("Could not find SceneChanger in the scene! Is the XR Origin loaded?");

        if (survivedDialogue == null || gameOverDialogue == null)
            Debug.LogWarning("[SharkThreatStatus] Missing End-Game Dialogues. Run Tools → EcoShift → Create Shark End-Game Dialogues.");
    }

    private void OnEnable()
    {
        currentLives = maxLives;
        isGameOver = false;
        timerRunning = false;

        if (livesUI != null) 
        {
            if (!livesUI.gameObject.activeInHierarchy)
                livesUI.gameObject.SetActive(true);
                
            livesUI.ResetHearts();
        }
        else
        {
            Debug.LogWarning("[SharkThreatStatus] No LivesUI found.");
        }

        // Create a timer on the Lives Canvas if we don't have one
        if (timerText == null && livesUI != null)
        {
            timerText = CreateTimerOnLivesCanvas();
        }
    }

    void Update()
    {
        if (!timerRunning) return;

        timeRemaining -= Time.deltaTime;
        timeRemaining = Mathf.Max(0f, timeRemaining);
        UpdateTimerUI();

        if (timeRemaining <= 0f)
        {
            timerRunning = false;
            OnTimerSurvived();
        }
    }

    /// <summary>
    /// Call this to start the countdown (e.g. from DialogueTrigger.onDialogueFinished
    /// or from SharkThreatIntro after the intro sequence).
    /// </summary>
    public void StartTimer()
    {
        // Create timer if it wasn't ready during Start
        if (timerText == null && livesUI != null)
            timerText = CreateTimerOnLivesCanvas();

        timeRemaining = totalTime;
        timerRunning = true;

        if (timerText != null)
            timerText.gameObject.SetActive(true);

        Debug.Log($"[SharkThreatStatus] Timer started: {totalTime}s");
    }

    public void StopTimer()
    {
        timerRunning = false;
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        int seconds = Mathf.CeilToInt(timeRemaining);
        timerText.text = $"{seconds}s";
        timerText.color = seconds <= 10 ? Color.red : Color.white;
    }

    public void GetTangled()
    {
        if (isGameOver) return;

        currentLives--;
        Debug.LogWarning($"Blue Shark TANGLED! Lives remaining: {currentLives}");

        if (livesUI != null)
            livesUI.LoseHeart();

        if (DistortionManager.Instance != null)
            DistortionManager.Instance.ApplyDistortion();

        StartCoroutine(VibrateControllers());

        if (currentLives <= 0)
            GameOver();
    }

    //========================================================================

    // Called when the player was caught by poachers (did not survive full timer duration)
    private void GameOver()
    {
        isGameOver = true;
        timerRunning = false;

        if (timerText != null)
            timerText.text = "0s";

        Debug.LogError("THE SHARK HAS BEEN CAPTURED! GAME OVER.");

        //ShowEndGameDialogue(false);
        StartCoroutine(ReelPlayerUpRoutine()); // Start the VR-Safe teleport
        StopBoatAndClearNets(); 
    }

    /// <summary>
    /// Called when the player survives the full timer duration.
    /// </summary>
    private void OnTimerSurvived()
    {
        isGameOver = true; //
        Debug.Log("[SharkThreatStatus] Timer ended — the shark survived!");

        // ShowEndGameDialogue(true);
        StartCoroutine(ReelFishesUpRoutine()); // Pull the fish up first
        StopBoatAndClearNets(); 
    }


    //========================================================================
    //****
    /// <summary>
    /// Creates a TMP timer text on the same Canvas as the LivesUI hearts.
    /// Positioned to the left so it doesn't overlap with the hearts.
    /// This is independent of the Pangolin Timer Canvas — moving this won't affect Pangolin.
    /// </summary>
    private TextMeshProUGUI CreateTimerOnLivesCanvas()
    {
        Canvas livesCanvas = livesUI.GetComponentInParent<Canvas>();
        if (livesCanvas == null)
        {
            Debug.LogWarning("[SharkThreatStatus] Lives Canvas not found — can't create timer.");
            return null;
        }

        GameObject timerGO = new GameObject("SharkTimer");
        timerGO.layer = livesCanvas.gameObject.layer;
        timerGO.transform.SetParent(livesCanvas.transform, false);

        RectTransform rect = timerGO.AddComponent<RectTransform>();
        // Anchor to the left side of the canvas so it doesn't overlap the hearts on the right
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(20f, 0f);
        rect.sizeDelta = new Vector2(250f, 80f);

        TextMeshProUGUI tmp = timerGO.AddComponent<TextMeshProUGUI>();
        tmp.text = $"{Mathf.CeilToInt(totalTime)}s";
        tmp.fontSize = 60;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;

        Debug.Log("[SharkThreatStatus] Shark timer created on Lives Canvas.");
        return tmp;
    }

    // Spawns a WorldSpace Canvas directly in front of the main camera to act as a blindfold.
    private GameObject CreateVRFader()
    {
        GameObject fader = new GameObject("VRFader");
        
        // Attach to the player's head/camera
        Transform camTransform = Camera.main != null ? Camera.main.transform : transform;
        fader.transform.SetParent(camTransform, false);
        fader.transform.localPosition = new Vector3(0f, 0f, 0.4f); // 40cm right in front of their eyes
        
        Canvas canvas = fader.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 999; // Ensure it renders over everything
        
        RectTransform rect = fader.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(5f, 5f); // Massive 5-meter square to cover peripheral vision
        
        Image img = fader.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0); // Start completely transparent
        
        return fader;
    }


    //**** commented clear nets
    /// <summary>
    /// Triggers the appropriate DialogueTrigger (survived or game-over).
    /// Wire each trigger's onDialogueFinished → LoadMainMenu() in the Inspector,
    /// or it will auto-load Main_Scene if no event is wired.
    /// </summary>
    private void ShowEndGameDialogue(bool survived)
    {
        DialogueTrigger trigger = survived ? survivedDialogue : gameOverDialogue;

        // If not assigned (e.g. SharkThreatStatus is on the persistent XR Origin
        // but the DialogueTriggers live in the Shark_Threat scene), find by name
        if (trigger == null)
        {
            string searchName = survived ? "SurvivedDialogue" : "GameOverDialogue";
            GameObject found = GameObject.Find(searchName);
            if (found != null)
                trigger = found.GetComponent<DialogueTrigger>();
        }

        if (trigger != null)
        {
            // StopBoatAndClearNets();
            trigger.onDialogueFinished.AddListener(ReturnToMainMenu);
            trigger.TriggerDialogue();
        }
        else
        {
            Debug.LogWarning($"[SharkThreatStatus] No {(survived ? "SurvivedDialogue" : "GameOverDialogue")} found — returning to main menu.");
            ReturnToMainMenu();
        }
    }

    //****
    /// <summary>
    /// Disables the poacher boat so it stops chasing and dropping nets,
    /// and destroys any nets already in the scene.
    /// </summary>
    private void StopBoatAndClearNets()
    {
        PoacherBoatManager boat = FindFirstObjectByType<PoacherBoatManager>();
        if (boat != null)
        {
            boat.enabled = false;
        }

        // Destroy all existing nets in the scene
        // Nets are instantiated from netPrefab, so find them by the prefab name
        foreach (GameObject net in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (net.name.Contains("Net") && net.GetComponent<Collider>() != null)
                Destroy(net);
        }

        Debug.Log("[SharkThreatStatus] Boat stopped and nets cleared.");
    }

    //****
    public void ReturnToMainMenu()
    {
        Debug.Log("[SharkThreatStatus] End-game dialogue finished — returning to main menu.");
        if (sceneChanger != null) 
        {
            sceneChanger.LoadMainScene();
        }
    }

    //****
    private IEnumerator VibrateControllers()
    {
        List<InputDevice> devices = new List<InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
            devices);
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            devices);

        foreach (InputDevice device in devices)
        {
            if (device.isValid)
                device.SendHapticImpulse(0, hapticAmplitude, hapticDuration);
        }

        yield return new WaitForSeconds(hapticDuration);
    }

    private IEnumerator ReelPlayerUpRoutine()
    {
        // ==========================================
        // LOCK PLAYER MOVEMENT (BUT KEEP AUDIO)
        // ==========================================
        SwimLocomotion playerLocomotion = FindFirstObjectByType<SwimLocomotion>();
        if (playerLocomotion != null)
        {
            // Ignore the joysticks, but keep the ambient audio playing!
            playerLocomotion.canMove = false; 
        }

        // ==========================================
        // FIND THE BOAT (For framing)
        // ==========================================
        Transform boatTarget = null;
        GameObject activeBoat = GameObject.Find("boat_1");
        if (activeBoat != null)
        {
            Transform exactDropPoint = activeBoat.transform.Find("netDropPoint");
            boatTarget = exactDropPoint != null ? exactDropPoint : activeBoat.transform;
            
            PoacherBoatManager boatManager = activeBoat.GetComponent<PoacherBoatManager>();
            if (boatManager != null) ApplyCinematicGlow(boatManager.gameObject);
        }

        // Record the exact spot the player "died"
        Vector3 deathPos = transform.position;

        // ==========================================
        // FADE TO BLACK
        // ==========================================
        GameObject fader = CreateVRFader();
        Image fadeImage = fader.GetComponentInChildren<Image>();

        float fadeTime = 1.5f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, elapsed / fadeTime);
            yield return null;
        }
        // ==========================================
        // SPAWN THE CINEMATIC SHARK & NET THE OCEAN FLOOR (Y = 0)
        // ==========================================
        GameObject cinematicNet = null;
        //GameObject dummyShark = null;

        float seabedHeight = 0f; // Default fallback height
        
        // Start our measurement at the surface, directly above where the player died
        // We add +1f to Y just in case the player's center is slightly clipping into the mud.
        // Vector3 rayStart = new Vector3(deathPos.x, surfaceHeightY, deathPos.z);
        Vector3 rayStart = new Vector3(deathPos.x, deathPos.y + 1f, deathPos.z);
        
        // Shoot an invisible laser straight down. If it hits something, record that Y height!
        //if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, surfaceHeightY + 100f))
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 100f))
        {
            seabedHeight = hit.point.y;
        }

        // Apply our dynamically found seabed height
        Vector3 bottomPos = new Vector3(deathPos.x, seabedHeight, deathPos.z);
        
        // Use the death X and Z, but force Y to 0f
        //Vector3 bottomPos = new Vector3(deathPos.x, 0f, deathPos.z);

        // Tweak this 1.5f up or down depending on the pivot of your net model so it sits nicely in the basket
        Vector3 netCenterOffset = Vector3.up * 1.5f; 
        Vector3 netCenter = bottomPos + netCenterOffset;

        if (cinematicNetPrefab != null)
        {
            // Spawn the net at the bottom of the ocean
            cinematicNet = Instantiate(cinematicNetPrefab, bottomPos, Quaternion.Euler(-90f, 0f, 0f));
            cinematicNet.transform.localScale = new Vector3(openScale, openScale, openScale); 
            ApplyCinematicGlow(cinematicNet);

            Rigidbody netRb = cinematicNet.GetComponent<Rigidbody>();
            if (netRb != null) { netRb.isKinematic = true; netRb.useGravity = false; }
        }
        // // ---  THE SHARK SPAWNING ---
        // if (dummyPlayerSharkPrefab != null)
        // {
        //     dummyShark = Instantiate(dummyPlayerSharkPrefab, netCenter, transform.rotation);
            
        //     MonoBehaviour[] allScripts = dummyShark.GetComponents<MonoBehaviour>();
        //     foreach (var script in allScripts) { if (script != this) script.enabled = false; }
            
        //     ApplyCinematicGlow(dummyShark);
        // }

        // ==========================================
        //  POSITION THE VR CAMERA (SIDE PROFILE)
        // ==========================================
        Vector3 boatPos = boatTarget != null ? boatTarget.position : transform.position;
        Vector3 directionToBoat = (boatPos - bottomPos).normalized;
        directionToBoat.y = 0; 
        if (directionToBoat == Vector3.zero) directionToBoat = Vector3.forward;

        // --- CROSS PRODUCT MATH FOR SIDE VIEW ---
        Vector3 sideDirection = Vector3.Cross(Vector3.up, directionToBoat).normalized;

        // Removed the + Vector3.up so the camera sits perfectly level with the net instead of above it
        Vector3 cameraOffset = (sideDirection * 5f) + (-directionToBoat * 1.5f) + (Vector3.down * 0.1f);
        
        // Start the camera at the bottom of the ocean with the net
        transform.position = bottomPos + cameraOffset;
        transform.LookAt(netCenter);

        yield return new WaitForSeconds(0.5f);

        // ==========================================
        //  FADE BACK IN TO REVEAL THE SCENE
        // ==========================================
        // FadeOut
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, 1f - (elapsed / fadeTime));
            yield return null;
        }
        Destroy(fader);

        // ==========================================
        // REEL SHARK AND CAMERA UP TO BOAT
        // ==========================================
        bool netReachedBoat = false;
        Vector3 finalDestination = new Vector3(
            boatTarget != null ? boatTarget.position.x : deathPos.x, 
            surfaceHeightY, 
            boatTarget != null ? boatTarget.position.z : deathPos.z
        );

        while (!netReachedBoat)
        {
            netReachedBoat = true; 

            if (cinematicNet != null)
            {
                // Stretch the net
                Vector3 targetStretchScale = new Vector3(openScale, openScale, stretchScaleZ);
                cinematicNet.transform.localScale = Vector3.MoveTowards(cinematicNet.transform.localScale, targetStretchScale, scaleSpeed * 2f * Time.deltaTime);

                // Ascend logic
                if (cinematicNet.transform.position.y < surfaceHeightY - 0.1f)
                {
                    netReachedBoat = false; 
                    cinematicNet.transform.position = Vector3.MoveTowards(cinematicNet.transform.position, finalDestination, reelSpeed * Time.deltaTime);
                }

                // Update the magnetic center position
                Vector3 currentNetCenter = cinematicNet.transform.position + netCenterOffset; //(Vector3.up * 2f); //currently netCenterOffset = (Vector3.up * 1.5f);

                // Lock Dummy Shark inside the net
                // if (dummyShark != null)
                // {
                //     dummyShark.transform.position = currentNetCenter;
                //     dummyShark.transform.Rotate(Vector3.up * 45f * Time.deltaTime); // Squirm effect
                // }

                // Move the Camera alongside the net using the side-profile offset
                transform.position = cinematicNet.transform.position + cameraOffset;
                transform.LookAt(currentNetCenter); 
            }
            yield return null;
        }

        // ==========================================
        // FINAL TRANSITION (RETURN TO DEATH POS)
        // ==========================================
        // Create a brand new fader for the ending transition
        GameObject finalFader = CreateVRFader();
        Image finalFadeImage = finalFader.GetComponentInChildren<Image>();

        // Fade OUT to black
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            finalFadeImage.color = new Color(0, 0, 0, elapsed / fadeTime);
            yield return null;
        }

        // Clean up the models while the screen is pitch black
        if (cinematicNet != null) Destroy(cinematicNet);
        // if (dummyShark != null) Destroy(dummyShark);

        // Teleport the player back to where they actually died
        transform.position = deathPos;
        // Face the camera towards where the boat/action was, so they aren't staring at a random wall
        transform.LookAt(new Vector3(boatPos.x, deathPos.y, boatPos.z));

        // Fade BACK IN
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            finalFadeImage.color = new Color(0, 0, 0, 1f - (elapsed / fadeTime));
            yield return null;
        }
        Destroy(finalFader);

        // Show Game Over UI/Dialogue
        ShowEndGameDialogue(false);
    }


    private IEnumerator ReelFishesUpRoutine()
    {
        // ==========================================
        // LOCK PLAYER MOVEMENT (BUT KEEP AUDIO)
        // ==========================================
        SwimLocomotion playerLocomotion = FindFirstObjectByType<SwimLocomotion>();
        if (playerLocomotion != null)
        {
            // Ignore the joysticks, but keep the ambient audio playing!
            playerLocomotion.canMove = false; 
        }

        // ==========================================
        // FIND THE BOAT EARLY (For Glow & Camera Math)
        // ==========================================
        Transform boatTarget = null;
        GameObject activeBoat = GameObject.Find("boat_1");
        if (activeBoat != null)
        {
            // Try to find the exact drop point attached to that boat
            Transform exactDropPoint = activeBoat.transform.Find("netDropPoint");

            // If it has the drop point, target it! Otherwise, just target the boat's center.
            boatTarget = exactDropPoint != null ? exactDropPoint : activeBoat.transform;

            // ==========================================
            // CINEMATIC GLOW BOAT
            // ==========================================
            PoacherBoatManager boatManager = FindFirstObjectByType<PoacherBoatManager>();
            if (boatManager != null)
            {
                ApplyCinematicGlow(boatManager.gameObject);
            }
        }

        // ==========================================
        // SELECT THE FIRST 10 FISHES
        // ==========================================
        GameObject[] allFishes = GameObject.FindGameObjectsWithTag("Fish");
        List<GameObject> fishesToReel = new List<GameObject>();

        // Limit the fishes being picked up.
        int maxFish = Mathf.Min(10, allFishes.Length);

        // Turn off AI for all surviving fish and add them to our active list
        for (int i = 0; i < maxFish; i++)
        {
            GameObject fish = allFishes[i]; 
            if (fish != null)
            {
                // Disable the flocking/swimming script (change "FlockManager" or "MonoBehaviour" to your specific fish script if needed)
                MonoBehaviour[] allScripts = fish.GetComponents<MonoBehaviour>();
                foreach (var script in allScripts)
                {
                    // Don't disable the animator or renderer, just custom scripts
                    if (script != this) script.enabled = false; 
                }
                fishesToReel.Add(fish);

                // Glowing Fishes
                ApplyCinematicGlow(fish);

                // Optimization: Stop searching the scene once we have our 10 cinematic fish!
                if (fishesToReel.Count >= 10) break;
            }
        }


        // ==========================================
        // FIX PLAYER CAMERA (CINEMATIC VIEW)
        // ==========================================

        // Find the center point of our 10 selected fish
        Vector3 centerPoint = Vector3.zero;
        foreach(var f in fishesToReel) centerPoint += f.transform.position;
        if (fishesToReel.Count > 0) centerPoint /= fishesToReel.Count;

        // Create the VR-Safe Blackout Fader
        GameObject fader = CreateVRFader();
        Image fadeImage = fader.GetComponentInChildren<Image>();

        // Fade out to black over specified seconds
        float fadeTime = 2f;
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, elapsed / fadeTime);
            yield return null;
        }

        // TELEPORT THE PLAYER TO A SPECTATOR POSITION
        // Find the direction from the fish to the player, and push the player 15 meters back
        // Set their Y position to halfway up the water column for the best view
        Vector3 boatPos = boatTarget != null ? boatTarget.position : transform.position;
        Vector3 pullBackDirection = (centerPoint - boatPos).normalized;
        pullBackDirection.y = 0; // Keep the push strictly horizontal

        if (pullBackDirection == Vector3.zero) 
        {
            pullBackDirection = (transform.position - centerPoint).normalized;
            pullBackDirection.y = 0;
        }
        
        Vector3 spectatorPos = centerPoint + (pullBackDirection * 15f);
        spectatorPos.y = surfaceHeightY * 0.3f; // Position them slightly lower in the water looking up
        
        transform.position = spectatorPos;

        // Force the player's XR Rig to look directly at the fish column
        transform.LookAt(new Vector3(centerPoint.x, spectatorPos.y, centerPoint.z));

        yield return new WaitForSeconds(0.5f);

        // Fade back in to reveal the cinematic angle
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, 1f - (elapsed / fadeTime));
            yield return null;
        }
        Destroy(fader);

        // ==========================================
        // REEL THEM UP (WITH THE NET!)
        // ==========================================
        // Spawn the cinematic net right in the middle of our fish cluster!
        GameObject cinematicNet = null;
        if (cinematicNetPrefab != null)
        {
            // Spawn it slightly below the center point so it looks like it's scooping them
            Vector3 netSpawnPos = centerPoint + (Vector3.down * 3f);
            cinematicNet = Instantiate(cinematicNetPrefab, netSpawnPos, Quaternion.Euler(-90f, 0f, 0f));
            
            // Make the net massive so it easily visually surrounds the 10 fish
            cinematicNet.transform.localScale = new Vector3(10f, 10f, 10f); 

            // Make the net glow neon so it matches the boat and fish!
            ApplyCinematicGlow(cinematicNet); 
        }

        // ==========================================
        // SCOOP FISHES (WITH THE NET!)
        // ==========================================
        // Spend 1.5 seconds pulling the fish tightly into the net before we start moving to the boat
        float scoopTimer = 0f;
        while (scoopTimer < 1.5f)
        {
            scoopTimer += Time.deltaTime;
            
            if (cinematicNet != null)
            {
                // STRETCH THE NET FOR WATER DRAG
                Vector3 targetStretchScale = new Vector3(openScale, openScale, stretchScaleZ);
                cinematicNet.transform.localScale = Vector3.MoveTowards(cinematicNet.transform.localScale, targetStretchScale, scaleSpeed * 2f * Time.deltaTime);

                cinematicNet.transform.Translate(Vector3.up * (reelSpeed * 0.2f) * Time.deltaTime, Space.World);
                Vector3 netCenter = cinematicNet.transform.position + (Vector3.up * 2f);

                foreach (GameObject fish in fishesToReel)
                {
                    if (fish != null)
                    {
                        fish.transform.position = Vector3.Lerp(fish.transform.position, netCenter, Time.deltaTime * 4f);
                    }
                }
            }
            yield return null;
        }
        
        // ==========================================
        // REEL NET OF FISHES UP (WITH THE NET!)
        // ==========================================
        // Reeling fishes
        bool netReachedBoat = false;

        while (!netReachedBoat)
        {
            netReachedBoat = true; 

            // 1. MOVE & STRETCH THE NET
            if (cinematicNet != null)
            {
                // KEEP STRETCHING THE NET (in case it didn't finish stretching during Phase 1)
                Vector3 targetStretchScale = new Vector3(openScale, openScale, stretchScaleZ);
                cinematicNet.transform.localScale = Vector3.MoveTowards(cinematicNet.transform.localScale, targetStretchScale, scaleSpeed * 2f * Time.deltaTime);

                if (cinematicNet.transform.position.y < surfaceHeightY)
                {
                    netReachedBoat = false; 
                    
                    if (boatTarget != null)
                    {
                        cinematicNet.transform.position = Vector3.MoveTowards(cinematicNet.transform.position, boatTarget.position, reelSpeed * Time.deltaTime);
                    }
                    else
                    {
                        cinematicNet.transform.Translate(Vector3.up * reelSpeed * Time.deltaTime, Space.World);
                    }
                }
            }

            // 2. LOCK THE FISH INSIDE THE NET
            for (int i = fishesToReel.Count - 1; i >= 0; i--)
            {
                GameObject fish = fishesToReel[i];
                if (fish != null)
                {
                    if (!netReachedBoat && cinematicNet != null)
                    {
                        Vector3 netCenter = cinematicNet.transform.position + (Vector3.up * 2f);
                        fish.transform.position = Vector3.Lerp(fish.transform.position, netCenter, Time.deltaTime * 15f);
                    }
                    else
                    {
                        Destroy(fish);
                        fishesToReel.RemoveAt(i);
                    }
                }
                else
                {
                    fishesToReel.RemoveAt(i); 
                }
            }
            
            yield return null;
        }
        if (cinematicNet != null) Destroy(cinematicNet);

        // All fish are gone. Trigger Victory Dialogue!
        ShowEndGameDialogue(true);
    }

    // Forces the target object's materials to emit a bright neon glow (existing materials without extra render passes).
    private void ApplyCinematicGlow(GameObject target)
    {
        if (target == null) return;

        // Find every 3D mesh attached to this object or its children
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer r in renderers)
        {
            foreach (Material mat in r.materials)
            {
                // Turn on the material's Emission master switch
                mat.EnableKeyword("_EMISSION");
                
                // Set the color and multiply it by our intensity to make it pop
                mat.SetColor("_EmissionColor", cinematicHighlightColor * glowIntensity);
            }
        }
    }
}


