using UnityEngine;

public class WaterClamp : MonoBehaviour
{
    [Header("Water Settings")]
    [Tooltip("The highest Y-coordinate the player's head can go.")]
    public float maxWaterHeight = 30f; 

    [Tooltip("Reference to the player's Main Camera.")]
    public Transform xrCamera;
    private CharacterController cc;

    private void Start()
    {
        if (xrCamera == null && Camera.main != null)
        {
            xrCamera = Camera.main.transform;
        }
        // Grab the controller
        cc = GetComponent<CharacterController>();
    }

    private void LateUpdate()
    {
        if (xrCamera == null) return;

        // If the camera (head) goes above the water line...
        if (xrCamera.position.y > maxWaterHeight)
        {
            // Calculate exactly how far above the water they are
            float overflow = xrCamera.position.y - maxWaterHeight;

            if (cc != null)
            {
                // Push the rig down using Unity's safe physics method
                cc.Move(Vector3.down * overflow);
            }
            else
            {
                //Shove the entire XR Origin down by that exact amount (Fallback)
                transform.position = new Vector3(
                    transform.position.x, 
                    transform.position.y - overflow, 
                    transform.position.z
                );
            }
        }
            
    }
}