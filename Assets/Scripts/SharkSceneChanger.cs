using UnityEngine;

public class SharkSceneChanger : MonoBehaviour
{
    private SceneChanger sceneChanger;

    private void Start()
    {
        // This tells Unity to search the active scene for the SceneChanger script
        // the moment this black box loads in.
        sceneChanger = FindFirstObjectByType<SceneChanger>();
        
        if (sceneChanger == null)
        {
            Debug.LogWarning("Could not find SceneChanger in the scene! Is the XR Origin loaded?");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the box is the player
        if (other.CompareTag("Player") && sceneChanger != null) 
        {
            sceneChanger.StartCoroutine(sceneChanger.LoadSharkThreatScene());
        }
    }
}