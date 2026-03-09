using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentXROrigin : MonoBehaviour
{
    // This variable holds the one true instance of the XR Origin
    public static PersistentXROrigin Instance { get; private set; }

    void Awake()
    {
        // Check if an XR Origin already exists in the game
        if (Instance == null)
        {
            // If not, this becomes the main XR Origin
            Instance = this;
            
            // Tell Unity to keep this specific object alive when loading new scenes
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If an XR Origin already exists, destroy this duplicate immediately!
            Destroy(gameObject);
        }
    }

    // OnEnable is called when the script is turned on
    void OnEnable()
    {
        // "Subscribe" to the sceneLoaded event so Unity informs when a load finishes
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // OnDisable is called if the script is destroyed (prevent memory leaks)
    void OnDisable()
    {
        // "Unsubscribe" from the event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // This custom function runs automatically every time a new scene finishes loading
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Look for the object tagged "Respawn"
        GameObject spawnPoint = GameObject.FindGameObjectWithTag("Respawn");
        
        if (spawnPoint != null)
        {
            CharacterController cc = GetComponent<CharacterController>();
            // Find the player's physical body and temporarily disable it
            if (cc != null) cc.enabled = false;
            
            // Teleport the XR Origin to the spawn point's exact location and rotation
            transform.position = spawnPoint.transform.position;
            transform.rotation = spawnPoint.transform.rotation;

            //Turn the physical body back on
            if (cc != null) cc.enabled = true;
        }
        else
        {
            // A helpful warning in the console just in case no Spawn Point !
            Debug.LogWarning("No Spawn Point found! Did you forget to tag an object 'Respawn' in this scene?");
        }
    }
}
