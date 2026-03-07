using UnityEngine;
using UnityEngine.SceneManagement; // Required for loading scenes

public class SceneChanger : MonoBehaviour
{
    // Function for the Terrestrial Scene
    public void LoadTerrestrialScene()
    {
        SceneManager.LoadScene("Pangolin_FoodSearch"); 
    }

    // Function for the Marine Scene
    public void LoadMarineScene()
    {
        SceneManager.LoadScene("Shark_FoodSearch"); 
    }
}