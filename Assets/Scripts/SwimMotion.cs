using UnityEngine;

public class SwimMotion : MonoBehaviour
{
    [Header("Ambient Sway (idle floating feel)")]
    public float swayAmount = 0.015f;
    public float swaySpeed = 0.8f;
    public float bobAmount = 0.02f;
    public float bobSpeed = 0.5f;
    public float rollAmount = 0.4f;
    public float pitchAmount = 0.2f;

    [Header("Drift Current (subtle push like ocean current)")]
    public float driftAmount = 0.005f;
    public float driftSpeed = 0.3f;

    [Header("Breathing Rhythm (slow expansion feel)")]
    public float breatheAmount = 0.003f;
    public float breatheSpeed = 0.25f;

    [Header("Only Active in Marine Scenes")]
    public bool isActive = false;

    private Vector3 startPos;
    private float timeOffset;

    void Start()
    {
        startPos = transform.localPosition;
        // Random offset so motion feels organic, not synchronized
        timeOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (!isActive) return;

        float t = Time.time + timeOffset;

        // --- POSITION ---

        // Primary sway: gentle side-to-side like floating in a current
        float swayX = Mathf.Sin(t * swaySpeed) * swayAmount;
        // Secondary sway: layered with a different frequency for organic feel
        swayX += Mathf.Sin(t * swaySpeed * 1.7f) * (swayAmount * 0.3f);

        // Vertical bob: slow rise and fall like breathing underwater
        float bobY = Mathf.Sin(t * bobSpeed) * bobAmount;
        // Secondary bob: longer wave layered on top
        bobY += Mathf.Sin(t * bobSpeed * 0.6f) * (bobAmount * 0.5f);

        // Drift current: very slow forward/backward push
        float driftZ = Mathf.Sin(t * driftSpeed) * driftAmount;

        // Breathing rhythm: subtle expansion/contraction of position
        float breathe = Mathf.Sin(t * breatheSpeed) * breatheAmount;
        bobY += breathe;

        transform.localPosition = startPos + new Vector3(swayX, bobY, driftZ);

        // --- ROTATION ---

        // Roll: gentle tilting side to side
        float rollZ = Mathf.Sin(t * swaySpeed * 0.7f) * rollAmount;
        rollZ += Mathf.Sin(t * swaySpeed * 1.3f) * (rollAmount * 0.2f);

        // Pitch: slight nose-up / nose-down motion
        float pitchX = Mathf.Sin(t * bobSpeed * 0.9f) * pitchAmount;

        // Very subtle yaw drift
        float yawY = Mathf.Sin(t * driftSpeed * 0.5f) * 0.1f;

        transform.localRotation = Quaternion.Euler(pitchX, yawY, rollZ);
    }
}
