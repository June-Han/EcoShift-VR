using UnityEngine;
public class SharkSceneChanger : MonoBehaviour
{
    // Import in scene changer script
    [SerializeField] private SceneChanger sceneChanger;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the box is the player
        if (other.CompareTag("Player") && sceneChanger != null) 
        {
            sceneChanger.LoadSharkThreatScene();
        }
        else if (sceneChanger == null)
        {
            Debug.LogWarning("SceneChanger is not assigned on " + gameObject.name);
        }
    }
}