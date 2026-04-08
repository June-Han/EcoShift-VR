using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.UI; // Required to talk to UI Buttons

[RequireComponent(typeof(Button))]
public class MainMenuButton : MonoBehaviour
{
    [Tooltip("Type exactly 'Marine' or 'Terrestrial' in the Inspector")]
    public string biome; 
    
    private SceneChanger sceneChanger;

    void Start()
    {
        // Grab the Button component attached to this GameObject
        Button menuButton = GetComponent<Button>();

        // Search the active scene for the SceneChanger on your XR Origin
        sceneChanger = FindFirstObjectByType<SceneChanger>();

        // If we found both, connect them!
        if (sceneChanger != null)
        {
            // Use () => to tell the button to hold onto this command until it is actually clicked!
            menuButton.onClick.AddListener(() => LoadBiome(biome, sceneChanger));
        }
        else
        {
            Debug.LogError("MainMenuButton: Failed to find the SceneChanger in this scene.");
        }
    }

    // This method accepts a text string from the Inspector
    public void LoadBiome(string biomeType, SceneChanger changer)
    {
        if (biomeType == "Marine")
        {
            Debug.Log("Marine button clicked! Loading Marine Scene...");
            changer.LoadMarineScene(); // Fixed the copy-paste error here!
        }
        else if (biomeType == "Terrestrial")
        {
            Debug.Log("Terrestrial button clicked! Loading Terrestrial Scene...");
            changer.LoadTerrestrialScene();
        }
        else
        {
            Debug.LogError("MainMenuButton: Unknown biome type requested -> " + biomeType);
        }
    }
}
