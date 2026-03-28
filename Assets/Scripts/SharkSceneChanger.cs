using UnityEngine;
using UnityEngine.SceneManagement;

public class SharkSceneChanger : MonoBehaviour
{
    // Import in scene changer script
    private SceneChanger sceneChanger;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the box is the player
        if (other.CompareTag("Player")) 
        {
            sceneChanger.LoadSharkThreatScene();
        }
    }
}