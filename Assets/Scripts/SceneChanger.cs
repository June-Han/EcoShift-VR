using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes
using Unity.XR.CoreUtils;
using UnityEngine.UI; // Fixes the "Image" error
using System.Collections; // Fixes the Coroutine/yield error
using Unity.VisualScripting;

public class SceneChanger : MonoBehaviour
{
    [Header("Physics & Audio")]
    public CharacterController playerBody;
    public AudioSource playerAudio;

    [Header("XR Origin")]
    public float TerrestrialXRCameraOffset = 0.30f;
    public float MarineXRCameraOffset = 0.50f;

    [Header("Underwater")]
    [SerializeField] private ParticleSystem breathingBubbles;

    private float fadeTime = 1f;
    private float mainSceneHeight = 2.3f;
    Vector3 existingHeadOffset;

    // XR Origin reference
    private Transform xrOrigin;
    
    [Header("Main Menu Lockdown")]
    [Tooltip("Drag the movement components here (Continuous Move, Snap Turn, Footsteps, etc.) that should be turned OFF in the Main Menu.")]
    public Behaviour[] menuKillSwitch;

    public void LoadMainScene()
    {
        StartCoroutine(StartLoadMainScene());
    }

    public IEnumerator StartLoadMainScene()
    {
         // Create the VR-Safe Blackout Fader
        GameObject fader = CreateVRFader();
        Image fadeImage = fader.GetComponentInChildren<Image>();
        // Prefer the assigned playerBody, otherwise fallback to attached component
        CharacterController cc = GetComponent<CharacterController>();

        // Fade to black over specified seconds
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, elapsed / fadeTime);
            yield return null; // Pauses here until the next frame
        }

        // Load the scene ASYNCHRONOUSLY and wait for it to finish
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Main_Scene");

        if (asyncLoad == null)
        {
            Debug.LogError("FADER CRASH: Unity cannot find 'Main_Scene'! Please add it to File > Build Settings.");
            yield break; // Safely exit the coroutine so it doesn't freeze the game
        }

        while (!asyncLoad.isDone)
        {
            yield return null; // Wait here until the new scene is 100% loaded
        }

        xrOrigin = FindXROrigin();
        if (xrOrigin != null) 
        {
            existingHeadOffset = xrOrigin.transform.localPosition;
        }

        //Transform back to default hands
        HandSwapper handSwapper = FindFirstObjectByType<HandSwapper>();
        if (handSwapper != null) handSwapper.ResetToDefaultHands();

        WakeUpGameplaySystems(true, mainSceneHeight); //- existingHeadOffset.y

        //For controller mapping resets
        WakeUpInputs();

        // Find the player's physical body and disable it
        if (cc != null) cc.enabled = false;
        // TRIGGER THE KILL SWITCH: Force all assigned locomotion scripts OFF
        foreach (Behaviour script in menuKillSwitch)
        {
            if (script != null) script.enabled = false;
        }

        Debug.Log("Entered main scene: CharacterController and all movement scripts aggressively locked OFF.");

        SwimMotion swimMotion = FindFirstObjectByType<SwimMotion>();
        if (swimMotion != null) swimMotion.isActive = false;

        WaterClamp waterClamp = FindFirstObjectByType<WaterClamp>();
        if (waterClamp != null) waterClamp.enabled = false;

        SharkThreatStatus sharkThreatStatus = FindFirstObjectByType<SharkThreatStatus>();
        if (sharkThreatStatus != null)
        {
            sharkThreatStatus.enabled = false;
        }

        if (breathingBubbles != null)
        {
            breathingBubbles.gameObject.SetActive(false);
        }

        LivesUI lui = FindFirstObjectByType<LivesUI>();
        if (lui != null) lui.gameObject.SetActive(false); 

        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            // Subtract from 1 to go from Black (1) to Clear (0)
            fadeImage.color = new Color(0, 0, 0, 1f - (elapsed / fadeTime));
            yield return null;
        }

        // Destroy the fader completely so it doesn't block future UI clicks!
        Destroy(fader);

    }

    public void LoadTerrestrialScene()
    {
        StartCoroutine(StartLoadTerrestrialScene());
    } 

    // Function for the Terrestrial Scene
    //public void StartLoadTerrestrialScene()
    private IEnumerator StartLoadTerrestrialScene()
    {
         // Create the VR-Safe Blackout Fader
        GameObject fader = CreateVRFader();
        Image fadeImage = fader.GetComponentInChildren<Image>();

        // Fade to black over specified seconds
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, elapsed / fadeTime);
            yield return null; // Pauses here until the next frame
        }

        // Load the scene ASYNCHRONOUSLY and wait for it to finish
        Debug.Log("FADER: 2. Screen is black. Loading Terrestrial Scene asynchronously...");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Pangolin_FoodSearch");

        if (asyncLoad == null)
        {
            Debug.LogError("FADER CRASH: Unity cannot find 'Pangolin_FoodSearch'! Please add it to File > Build Settings.");
            yield break; // Safely exit the coroutine so it doesn't freeze the game
        }

        while (!asyncLoad.isDone)
        {
            yield return null; // Wait here until the new scene is 100% loaded
        }

        //Transform player into a Pangolin!
        HandSwapper handSwapper = FindFirstObjectByType<HandSwapper>();
        if (handSwapper != null) handSwapper.EquipPawsWithScoreUI();

        SwimMotion swimMotion = FindFirstObjectByType<SwimMotion>();
        if (swimMotion != null) swimMotion.isActive = false;

        WaterClamp waterClamp = FindFirstObjectByType<WaterClamp>();
        if (waterClamp != null) waterClamp.enabled = false;
        
        if (breathingBubbles != null)
        {
            breathingBubbles.gameObject.SetActive(false);
        }

        WakeUpGameplaySystems(true, TerrestrialXRCameraOffset);

        //For controller mapping resets
        WakeUpInputs();

        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            // Subtract from 1 to go from Black (1) to Clear (0)
            fadeImage.color = new Color(0, 0, 0, 1f - (elapsed / fadeTime));
            yield return null;
        }

        // Destroy the fader completely so it doesn't block future UI clicks!
        Destroy(fader);
    }

    public void LoadMarineScene()
    {
        StartCoroutine(StartLoadMarineScene());
    }    

    // Function for the Marine Scene
    private IEnumerator StartLoadMarineScene()
    {
        // Create the VR-Safe Blackout Fader
        Debug.Log("FADER: 1. Spawning fader and going to black...");
        GameObject fader = CreateVRFader();
        Image fadeImage = fader.GetComponentInChildren<Image>();

        // Fade to black over specified seconds
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, elapsed / fadeTime);
            yield return null; // Pauses here until the next frame
        }

        // Load the scene ASYNCHRONOUSLY and wait for it to finish
        Debug.Log("FADER: 2. Screen is black. Loading Marine Scene asynchronously...");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Shark_FoodSearch");

        if (asyncLoad == null)
        {
            Debug.LogError("FADER CRASH: Unity cannot find 'Shark_FoodSearch'! Please add it to File > Build Settings.");
            yield break; // Safely exit the coroutine so it doesn't freeze the game
        }

        while (!asyncLoad.isDone)
        {
            yield return null; // Wait here until the new scene is 100% loaded
        }

        Debug.Log("FADER: 3. Scene loaded! Waking up Shark components...");
        //Transform player into a Shark!
        HandSwapper handSwapper = FindFirstObjectByType<HandSwapper>();
        if (handSwapper != null) handSwapper.EquipFinsWithScoreUI();

        WakeUpGameplaySystems(false, MarineXRCameraOffset);
        WakeUpInputs();

        SwimMotion swimMotion = FindFirstObjectByType<SwimMotion>();
        if (swimMotion != null) swimMotion.isActive = true;

        WaterClamp waterClamp = FindFirstObjectByType<WaterClamp>();
        if (waterClamp != null) waterClamp.enabled = true;

        SharkThreatStatus sharkThreatStatus = FindFirstObjectByType<SharkThreatStatus>();
        if (sharkThreatStatus != null)
        {
            sharkThreatStatus.enabled = false;
        }

        
        if (breathingBubbles != null)
        {
            breathingBubbles.gameObject.SetActive(true);
            breathingBubbles.Play();
        }

        LivesUI lui = FindFirstObjectByType<LivesUI>();
        if (lui != null) lui.gameObject.SetActive(false); 
        
        Debug.Log("FADER: 4. Shark setup complete. Fading back to clear...");
        // Fade back to clear!
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            // Subtract from 1 to go from Black (1) to Clear (0)
            fadeImage.color = new Color(0, 0, 0, 1f - (elapsed / fadeTime));
            yield return null;
        }

        // Destroy the fader completely so it doesn't block future UI clicks!
        Destroy(fader);

        //SceneManager.LoadScene("Shark_FoodSearch");
    }
    
    // Function for the Terrestrial Threat Scene
    public void LoadTerrestrialThreatScene()
    {
        StartCoroutine(StartLoadTerrestrialThreatScene());
    }

    private IEnumerator StartLoadTerrestrialThreatScene()
    {
        // Create the VR-Safe Blackout Fader
        GameObject fader = CreateVRFader();
        Image fadeImage = fader.GetComponentInChildren<Image>();

        // Fade to black over specified seconds
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, elapsed / fadeTime);
            yield return null; // Pauses here until the next frame
        }

        // Load the scene ASYNCHRONOUSLY and wait for it to finish
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Pangolin_Threat");

        if (asyncLoad == null)
        {
            Debug.LogError("FADER CRASH: Unity cannot find 'Pangolin_Threat'! Please add it to File > Build Settings.");
            yield break; // Safely exit the coroutine so it doesn't freeze the game
        }

        while (!asyncLoad.isDone)
        {
            yield return null; // Wait here until the new scene is 100% loaded
        }
        
        HandSwapper handSwapper = FindFirstObjectByType<HandSwapper>();
        if (handSwapper != null) handSwapper.EquipPawsWithTimerUI();

        SwimMotion swimMotion = FindFirstObjectByType<SwimMotion>();
        if (swimMotion != null) swimMotion.isActive = false;

        WaterClamp waterClamp = FindFirstObjectByType<WaterClamp>();
        if (waterClamp != null) waterClamp.enabled = false;
        
        if (breathingBubbles != null)
        {
            breathingBubbles.gameObject.SetActive(false);
        }

        WakeUpGameplaySystems(true, TerrestrialXRCameraOffset);
        WakeUpInputs();

        // Fade back to clear!
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            // Subtract from 1 to go from Black (1) to Clear (0)
            fadeImage.color = new Color(0, 0, 0, 1f - (elapsed / fadeTime));
            yield return null;
        }

        // Destroy the fader completely so it doesn't block future UI clicks!
        Destroy(fader);
    }

    // Function for the Shark Threat Scene
    // public void LoadSharkThreatScene()
    // Changed from 'public void' to 'private IEnumerator' so 'yield return' works
    public IEnumerator LoadSharkThreatScene()
    {
        // Create the VR-Safe Blackout Fader
        GameObject fader = CreateVRFader();
        Image fadeImage = fader.GetComponentInChildren<Image>();

        // Fade to black over specfied seconds
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, elapsed / fadeTime);
            yield return null; // Pauses here until the next frame
        }

        // Load the scene ASYNCHRONOUSLY and wait for it to finish
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Shark_Threat");

        if (asyncLoad == null)
        {
            Debug.LogError("FADER CRASH: Unity cannot find 'Shark_Threat'! Please add it to File > Build Settings.");
            yield break; // Safely exit the coroutine so it doesn't freeze the game
        }

        while (!asyncLoad.isDone)
        {
            yield return null; // Wait here until the new scene is 100% loaded
        }

        HandSwapper handSwapper = FindFirstObjectByType<HandSwapper>();
        if (handSwapper != null) handSwapper.EquipFinsWithLivesUI();

        WakeUpGameplaySystems(false, MarineXRCameraOffset);
        WakeUpInputs();

        SwimMotion swimMotion = FindFirstObjectByType<SwimMotion>();
        if (swimMotion != null) swimMotion.isActive = true;

        WaterClamp waterClamp = FindFirstObjectByType<WaterClamp>();
        if (waterClamp != null) waterClamp.enabled = true;

        SharkThreatStatus sharkThreatStatus = FindFirstObjectByType<SharkThreatStatus>();
        if (sharkThreatStatus != null)
        {
            sharkThreatStatus.enabled = true;
        }

        if (breathingBubbles != null)
        {
            breathingBubbles.gameObject.SetActive(true);
            breathingBubbles.Play();
        }

        LivesUI lui = FindFirstObjectByType<LivesUI>();
        if (lui != null) lui.gameObject.SetActive(true); 

        // Fade back to clear!
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            // Subtract from 1 to go from Black (1) to Clear (0)
            fadeImage.color = new Color(0, 0, 0, 1f - (elapsed / fadeTime));
            yield return null;
        }

        // Destroy the fader completely so it doesn't block future UI clicks!
        Destroy(fader);

        //SceneManager.LoadScene("Shark_Threat");
    }

    void WakeUpGameplaySystems(bool isTerrestrial, float XROrigin_CameraOffset)
    {
        // Get the XR Origin object that holds all the scripts
        GameObject xrOriginObject = playerBody.gameObject;
        XROrigin originScript = xrOriginObject.GetComponent<XROrigin>();

        // Turn on the audio system
        if (playerAudio != null)
        {
            playerAudio.enabled = true;
        }

        // Turn on Gravity and find the Movement Scripts
        if (playerBody != null)
        {
            playerBody.enabled = true;

            if (originScript != null)
            {
                // Device: For sitting, Floor: For real world floor
                originScript.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;

                // Set XR Origin Camera Offset
                originScript.CameraYOffset = XROrigin_CameraOffset;
            }

            // Enable/disable ground-based movement depending on scene type
            // ContinuousMoveProvider applies gravity and walks on ground — only for terrestrial
            Behaviour continuousMove = (Behaviour)xrOriginObject.GetComponent("ContinuousMoveProvider");
            if (continuousMove != null)
            {
                continuousMove.enabled = isTerrestrial;
            }

            // SwimLocomotion provides free 3D movement — only for marine
            SwimLocomotion swimLocomotion = xrOriginObject.GetComponent<SwimLocomotion>();
            if (swimLocomotion != null)
            {
                swimLocomotion.isActive = !isTerrestrial;
                swimLocomotion.canMove = !isTerrestrial;
            }

            // These are needed for both scenes
            EnableScriptByName(xrOriginObject, "XRBodyTransformer");
            EnableScriptByName(xrOriginObject, "LocomotionMediator");
            EnableScriptByName(xrOriginObject, "SnapTurnProvider");

            // Only enable footsteps if we are in the Terrestrial scene
            Behaviour footsteps = (Behaviour)xrOriginObject.GetComponent("PlayerFootsteps");
            if (footsteps != null)
            {
                footsteps.enabled = isTerrestrial;
            }
        }

    }

    //To enable the scripts on XR Origin
    void EnableScriptByName(GameObject targetObject, string scriptName)
    {
        Behaviour scriptToEnable = (Behaviour)targetObject.GetComponent(scriptName);
        if (scriptToEnable != null)
        {
            scriptToEnable.enabled = true;
        }
    }

    void WakeUpInputs()
    {
        GameObject manager = GameObject.Find("XR Interaction Manager");
        if (manager != null)
        {
            manager.SetActive(false);
            manager.SetActive(true);
        }
    }

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
}


