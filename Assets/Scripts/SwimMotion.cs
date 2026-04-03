using UnityEngine;
using System.Collections; // REQUIRED FOR COROUTINES

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
    

    [Header("Trap Penalty Settings")]
    [Tooltip("How much speed is kept when tangled (0.2 = 20% speed)")]
    public float tangledSpeedMultiplier = 0.4f;
    private bool isTangled = false;

    // Memorize original visual speeds
    private float originalSwaySpeed;
    private float originalBobSpeed;
    private float originalDriftSpeed;


    void Start()
    {
        startPos = transform.localPosition;
        // Random offset so motion feels organic, not synchronized
        timeOffset = Random.Range(0f, 100f);

        // Save the normal speeds before any nets hit the shark
        originalSwaySpeed = swaySpeed;
        originalBobSpeed = bobSpeed;
        originalDriftSpeed = driftSpeed;
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

    // --- TRAP PENALTY SYSTEM ---
    
    public void SlowDownPlayer(float penaltyDuration)
    {
        if (!isTangled)
        {
            StartCoroutine(TangledRoutine(penaltyDuration));
        }
    }

    private IEnumerator TangledRoutine(float duration)
    {
        isTangled = true;
        Debug.LogWarning("SHARK TANGLED! Slashing physical and visual speeds...");

        // Slash the visual ambient speeds
        swaySpeed *= tangledSpeedMultiplier;
        bobSpeed *= tangledSpeedMultiplier;
        driftSpeed *= tangledSpeedMultiplier;

        // Find YOUR custom locomotion script
        SwimLocomotion locomotion = GetComponentInParent<SwimLocomotion>();
        float originalPhysicalSpeed = 0f;

        // Slash the ACTUAL physical swim speed
        if (locomotion != null)
        {
            originalPhysicalSpeed = locomotion.swimSpeed;
            locomotion.swimSpeed *= tangledSpeedMultiplier; 
        }

        // Wait for the net to break (e.g., 5 seconds)
        yield return new WaitForSeconds(duration);

        // Restore visual speeds
        swaySpeed = originalSwaySpeed;
        bobSpeed = originalBobSpeed;
        driftSpeed = originalDriftSpeed;

        // Restore physical speed
        if (locomotion != null)
        {
            locomotion.swimSpeed = originalPhysicalSpeed;
        }

        Debug.Log("SHARK FREE! Speeds restored.");
        isTangled = false;
    }
}
