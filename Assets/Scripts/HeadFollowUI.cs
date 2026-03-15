using UnityEngine;

public class HeadFollowUI : MonoBehaviour
{
    [Tooltip("Distance from the camera.")]
    public float distance = 2.0f;
    
    [Tooltip("How smoothly the UI follows the head rotation.")]
    public float smoothTime = 0.3f;
    
    private Vector3 velocity = Vector3.zero;
    private Transform head;

    void Start() {
        if (Camera.main != null) head = Camera.main.transform;
        else Debug.LogWarning("Main Camera not found! Make sure your VR camera is tagged as MainCamera.");
    }

    void LateUpdate() {
        if (head == null) return;
        
        // Calculate the target position in front of the camera
        Vector3 targetPosition = head.position + (head.forward * distance);
        
        // Smoothly move the UI to the target position
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        
        // Make the UI always face the camera with the same up vector
        transform.LookAt(transform.position + head.rotation * Vector3.forward, head.rotation * Vector3.up);
    }
}

