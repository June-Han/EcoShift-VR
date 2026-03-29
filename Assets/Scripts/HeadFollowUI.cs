using UnityEngine;

public class HeadFollowUI : MonoBehaviour
{
    [Tooltip("Distance from the camera.")]
    public float distance = 2.0f;

    [Tooltip("Vertical offset. Use a negative value (e.g., -0.5) to move it downwards.")]
    public float verticalOffset = -0.3f;
    
    [Tooltip("How smoothly the UI follows the head rotation.")]
    public float smoothTime = 0.3f;
    
    private Vector3 velocity = Vector3.zero;
    private Transform head;
    private bool initialized;

    void Start() {
        TryFindCamera();
    }

    void TryFindCamera() {
        // Try Camera.main first, then fall back to any camera in the scene
        Camera cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();

        if (cam != null) {
            head = cam.transform;
            // Snap to correct position immediately (no SmoothDamp drift)
            Vector3 startPos = head.position + (head.forward * distance) + (head.up * verticalOffset);
            transform.position = startPos;
            transform.LookAt(transform.position + head.forward, head.up);
            velocity = Vector3.zero;
            initialized = true;

            // Assign the Event Camera on the world-space Canvas so UI events work
            Canvas canvasComp = GetComponent<Canvas>();
            if (canvasComp != null && canvasComp.renderMode == RenderMode.WorldSpace) {
                canvasComp.worldCamera = cam;
            }
        }
    }

    void LateUpdate() {
        // Keep trying to find the camera if it wasn't available on Start
        // (persistent XR Origin may not be ready on the first frame after scene load)
        if (head == null || !head.gameObject.activeInHierarchy) {
            initialized = false;
            TryFindCamera();
            if (head == null) return;
        }

        // On first valid frame, snap directly instead of smoothing from a bad position
        if (!initialized) {
            TryFindCamera();
            return;
        }

        // Calculate the target position in front of the camera
        Vector3 targetPosition = head.position + (head.forward * distance) + (head.up * verticalOffset);

        // Smoothly move the UI to the target position
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

        // Make the UI always face the camera with the same up vector (Tilting HUD Style)
        transform.LookAt(transform.position + head.forward, head.up); //Billboard Vector3.up
    }
}

