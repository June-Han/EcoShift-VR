using UnityEngine;

/// <summary>
/// A compass-style guide arrow that hovers near the player and rotates to point
/// toward the nearest Flock object. Stays close enough to always be visible
/// through underwater fog.
///
/// Activate/deactivate via ActiveArrow() / DeactiveArrow() — same API as FloatingArrow
/// so it can be wired to DialogueTrigger.onDialogueFinished events.
/// </summary>
public class FishGuideArrow : MonoBehaviour
{
    [Header("Follow Settings")]
    [Tooltip("The player camera/head transform. Auto-finds MainCamera if not set.")]
    public Transform playerTransform;

    [Tooltip("How far in front of the player the arrow floats")]
    public float forwardOffset = 2.5f;

    [Tooltip("Vertical offset below the player's eye level")]
    public float downOffset = 0.8f;

    [Header("Arrow Motion")]
    [Tooltip("How fast the arrow bobs up and down")]
    public float floatFrequency = 1f;

    [Tooltip("How much the arrow bobs up and down")]
    public float floatAmplitude = 0.15f;

    [Tooltip("How smoothly the arrow follows the player")]
    public float followSmoothTime = 0.2f;

    [Tooltip("How smoothly the arrow rotates to point at the target")]
    public float rotationSpeed = 5f;

    [Header("State")]
    public bool isActive = false;

    private Transform[] flockTransforms;
    private Vector3 currentVelocity;

    private void Start()
    {
        // Auto-find player camera
        if (playerTransform == null)
        {
            GameObject cam = GameObject.FindGameObjectWithTag("MainCamera");
            if (cam != null)
                playerTransform = cam.transform;
            else
                Debug.LogWarning("FishGuideArrow: No MainCamera found!");
        }

        // Find all Flock objects in the scene
        Flock[] flocks = FindObjectsOfType<Flock>();
        flockTransforms = new Transform[flocks.Length];
        for (int i = 0; i < flocks.Length; i++)
        {
            flockTransforms[i] = flocks[i].transform;
        }

        gameObject.SetActive(isActive);
    }

    private void Update()
    {
        if (playerTransform == null || flockTransforms == null || flockTransforms.Length == 0)
            return;

        // Find the nearest flock
        Transform nearest = null;
        float nearestDist = float.MaxValue;

        for (int i = 0; i < flockTransforms.Length; i++)
        {
            if (flockTransforms[i] == null) continue;
            float dist = Vector3.Distance(playerTransform.position, flockTransforms[i].position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = flockTransforms[i];
            }
        }

        if (nearest == null) return;

        // Position: float in front of and slightly below the player
        Vector3 targetPos = playerTransform.position
            + playerTransform.forward * forwardOffset
            - Vector3.up * downOffset;

        // Add gentle bobbing
        targetPos.y += Mathf.Sin(Time.time * Mathf.PI * floatFrequency) * floatAmplitude;

        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref currentVelocity, followSmoothTime);

        // Rotation: point toward the nearest flock
        Vector3 directionToFlock = (nearest.position - transform.position).normalized;
        if (directionToFlock != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToFlock);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    /// <summary>Same API as FloatingArrow — call from DialogueTrigger.onDialogueFinished</summary>
    public void ActiveArrow()
    {
        isActive = true;
        gameObject.SetActive(true);
    }

    /// <summary>Same API as FloatingArrow</summary>
    public void DeactiveArrow()
    {
        isActive = false;
        gameObject.SetActive(false);
    }
}
