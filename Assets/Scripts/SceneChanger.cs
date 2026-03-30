using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes
using Unity.XR.CoreUtils;
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

    // Function for the Terrestrial Scene
    public void LoadTerrestrialScene()
    {
        //FindAnyObjectByType<VRFader>().FadeOutToNewScene();

        //Transform player into a Pangolin!
        HandSwapper handSwapper = FindFirstObjectByType<HandSwapper>();
        if (handSwapper != null) handSwapper.EquipPawsWithScoreUI();

        SwimMotion swimMotion = FindFirstObjectByType<SwimMotion>();
        if (swimMotion != null) swimMotion.isActive = false;

        WakeUpGameplaySystems(true, TerrestrialXRCameraOffset);

        //For controller mapping resets
        WakeUpInputs();

        SceneManager.LoadScene("Pangolin_FoodSearch");
    }

        

    // Function for the Marine Scene
    public void LoadMarineScene()
    {
        //FindAnyObjectByType<VRFader>().FadeOutToNewScene();

        //Transform player into a Shark!
        HandSwapper handSwapper = FindFirstObjectByType<HandSwapper>();
        if (handSwapper != null) handSwapper.EquipFinsWithScoreUI();

        SwimMotion swimMotion = FindFirstObjectByType<SwimMotion>();
        if (swimMotion != null) swimMotion.isActive = true;
        
        if (breathingBubbles != null)
        {
            breathingBubbles.Play();
        }

        WakeUpGameplaySystems(false, MarineXRCameraOffset);
        WakeUpInputs();

        SceneManager.LoadScene("Shark_FoodSearch");
    }
    
    // Function for the Terrestrial Threat Scene
    public void LoadTerrestrialThreatScene()
    {
        HandSwapper handSwapper = FindFirstObjectByType<HandSwapper>();
        if (handSwapper != null) handSwapper.EquipPawsWithTimerUI();

        SwimMotion swimMotion = FindFirstObjectByType<SwimMotion>();
        if (swimMotion != null) swimMotion.isActive = false;

        WakeUpGameplaySystems(true, TerrestrialXRCameraOffset);
        WakeUpInputs();

        SceneManager.LoadScene("Pangolin_Threat");
    }

    // Function for the Shark Threat Scene
    public void LoadSharkThreatScene()
    {
        HandSwapper handSwapper = FindFirstObjectByType<HandSwapper>();
        if (handSwapper != null) handSwapper.EquipFinsWithScoreUI();

        SwimMotion swimMotion = FindFirstObjectByType<SwimMotion>();
        if (swimMotion != null) swimMotion.isActive = true;

        if (breathingBubbles != null)
        {
            breathingBubbles.Play();
        }

        WakeUpGameplaySystems(false, MarineXRCameraOffset);
        WakeUpInputs();

        SceneManager.LoadScene("Shark_Threat");
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
}
