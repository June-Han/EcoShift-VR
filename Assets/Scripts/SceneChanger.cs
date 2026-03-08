using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes

public class SceneChanger : MonoBehaviour
{
    // Function for the Terrestrial Scene
    public void LoadTerrestrialScene()
    {
        SceneManager.LoadScene("Pangolin_FoodSearch"); 

        //Transform player into a Pangolin!
        FindFirstObjectByType<HandSwapper>().EquipPaws();

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