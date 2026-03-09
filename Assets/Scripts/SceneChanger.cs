using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes
using Unity.XR.CoreUtils;

public class SceneChanger : MonoBehaviour
{
    [Header("Physics & Audio")]
    public CharacterController playerBody;
    public AudioSource playerAudio;

    [Header("XR Origin")]
    public float TerrestrialXRCameraOffset = 0.30f;
    public float MarineXRCameraOffset = 1.00f;

    // Function for the Terrestrial Scene
    public void LoadTerrestrialScene()
    {
        SceneManager.LoadScene("Pangolin_FoodSearch"); 

        //Transform player into a Pangolin!
        FindFirstObjectByType<HandSwapper>().EquipPaws();

        FindFirstObjectByType<SwimMotion>().isActive = false;

        WakeUpGameplaySystems(true, TerrestrialXRCameraOffset);

        //For controller mapping resets
        WakeUpInputs();
    }

    // Function for the Marine Scene
    public void LoadMarineScene()
    {
        SceneManager.LoadScene("Shark_FoodSearch"); 

        //Transform player into a Shark!
        FindFirstObjectByType<HandSwapper>().EquipFins();

        FindFirstObjectByType<SwimMotion>().isActive = true;
        
        WakeUpGameplaySystems(false, MarineXRCameraOffset);
        WakeUpInputs();
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
            
            // Automatically find and enable the core movement scripts by their names!
            EnableScriptByName(xrOriginObject, "ContinuousMoveProvider");
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