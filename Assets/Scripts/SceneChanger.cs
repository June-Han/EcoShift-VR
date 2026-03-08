using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes

public class SceneChanger : MonoBehaviour
{
    [Header("Physics & Audio")]
    public CharacterController playerBody;
    public AudioSource playerAudio;

    [Header("Movement Scripts")]
    public Behaviour[] movementScripts;

    // Function for the Terrestrial Scene
    public void LoadTerrestrialScene()
    {
        SceneManager.LoadScene("Pangolin_FoodSearch"); 

        //Transform player into a Pangolin!
        FindFirstObjectByType<HandSwapper>().EquipPaws();

        WakeUpGameplaySystems();
        WakeUpInputs();
    }

    // Function for the Marine Scene
    public void LoadMarineScene()
    {
        SceneManager.LoadScene("Shark_FoodSearch"); 

        //Transform player into a Shark!
        FindFirstObjectByType<HandSwapper>().EquipFins();
        
        WakeUpInputs();
    }

    void WakeUpGameplaySystems()
    {
        // Turn on gravity and collisions
        if (playerBody != null) playerBody.enabled = true;
        
        // Turn on the audio system
        if (playerAudio != null) playerAudio.enabled = true;

        // Turn on all the movement and footstep scripts!
        foreach (Behaviour script in movementScripts)
        {
            if (script != null) script.enabled = true;
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