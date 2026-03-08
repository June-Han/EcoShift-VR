using UnityEngine;

public class SwimMotion : MonoBehaviour
{
    [Header("Sway Settings")]
    public float swayAmount = 0.02f;
    public float swaySpeed = 1.2f;
    public float bobAmount = 0.01f;
    public float bobSpeed = 0.8f;
    public float rollAmount = 0.3f;

    [Header("Only Active in Marine Scenes")]
    public bool isActive = false;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        if (!isActive) return;

        float t = Time.time;

        // Gentle side-to-side sway
        float swayX = Mathf.Sin(t * swaySpeed) * swayAmount;

        // Slow vertical bob
        float bobY = Mathf.Sin(t * bobSpeed) * bobAmount;

        // Subtle roll (tilt)
        float rollZ = Mathf.Sin(t * swaySpeed * 0.7f) * rollAmount;

        transform.localPosition = startPos + new Vector3(swayX, bobY, 0f);
        transform.localRotation = Quaternion.Euler(0f, 0f, rollZ);
    }
}