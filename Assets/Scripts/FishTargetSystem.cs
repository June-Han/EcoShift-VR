using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Proximity + line-of-sight targeting system for catching fish.
/// Uses a FloatingArrow (same as the terrestrial scenes) hovering above the locked fish.
///   1. Scans for the nearest FishClickable within a radius and view cone
///   2. Shows a FloatingArrow above the locked fish
///   3. When in capture range, shows "Pull trigger to eat!" prompt
///   4. Stays locked on the same fish until it's eaten, then finds the next one
///
/// Attach to an empty GameObject in the scene (created by the Editor setup script).
/// </summary>
public class FishTargetSystem : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("The player camera. Auto-finds MainCamera if not set.")]
    public Transform playerCamera;

    [Header("Targeting")]
    [Tooltip("Maximum distance to detect fish")]
    public float targetRadius = 12f;

    [Tooltip("Half-angle of the forward cone (degrees) in which fish can be targeted")]
    public float viewConeAngle = 45f;

    [Tooltip("How often to scan for new targets (seconds). Lower = more responsive but costs more.")]
    public float scanInterval = 0.15f;

    [Header("Capture")]
    [Tooltip("Fish must be within this distance to be captured when trigger is pressed")]
    public float captureDistance = 8f;

    [Header("Input")]
    [Tooltip("Bind to the trigger action, e.g. 'XRI Right Interaction/Activate Value'")]
    public InputActionReference triggerAction;

    [Header("Arrow")]
    [Tooltip("How far above the fish the arrow floats")]
    public float arrowYOffset = 0.5f;

    [Header("Line of Sight")]
    [Tooltip("Layers that block line of sight (e.g. terrain, rocks). If empty, defaults to everything except UI and Ignore Raycast.")]
    public LayerMask obstacleMask = ~0; // default: everything

    // Internal state
    private FishClickable lockedFish;
    private InputAction inputAction;
    private bool wasTriggerPressed;
    private float nextScanTime;
    private GameObject arrowInstance;
    private FloatingArrow arrowScript;
    private TMPro.TextMeshPro promptText;

    private void Start()
    {
        // Auto-find player camera
        if (playerCamera == null)
        {
            GameObject cam = GameObject.FindGameObjectWithTag("MainCamera");
            if (cam != null)
                playerCamera = cam.transform;
            else
                Debug.LogWarning("FishTargetSystem: No MainCamera found!");
        }

        // Default obstacle mask: exclude UI (5) and Ignore Raycast (2) layers
        if (obstacleMask == ~0)
            obstacleMask = ~(1 << 5 | 1 << 2);

        // Setup input
        if (triggerAction != null && triggerAction.action != null)
        {
            inputAction = triggerAction.action;
            inputAction.Enable();
        }

        // Create the FloatingArrow (same style as terrestrial scenes)
        CreateArrow();

        // Create the prompt text
        CreatePrompt();
    }

    private void OnDisable()
    {
        if (inputAction != null)
            inputAction.Disable();
    }

    private void Update()
    {
        if (playerCamera == null) return;

        // ── 1. Scan for targets periodically ─────────────────────────────
        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + scanInterval;
            UpdateTarget();
        }

        // ── 2. Validate current lock ─────────────────────────────────────
        if (lockedFish != null && !lockedFish.gameObject.activeInHierarchy)
        {
            ClearLock();
        }

        // ── 3. Update arrow position to follow locked fish ───────────────
        if (lockedFish != null)
        {
            float distToFish = Vector3.Distance(playerCamera.position, lockedFish.transform.position);
            bool inCaptureRange = distToFish <= captureDistance;

            // Move arrow above the locked fish
            Vector3 arrowPos = lockedFish.transform.position + Vector3.up * arrowYOffset;
            arrowInstance.transform.position = arrowPos;

            // Make sure arrow is visible
            if (!arrowInstance.activeSelf)
            {
                arrowInstance.SetActive(true);
                // Reset startPos so FloatingArrow bobs from the new position
                arrowScript.isActive = true;
            }

            // Update the FloatingArrow's internal startPos each frame
            // so it follows the fish smoothly
            UpdateArrowStartPos(arrowPos);

            // Show prompt only when in capture range
            if (promptText != null)
            {
                promptText.gameObject.SetActive(inCaptureRange);
                if (inCaptureRange)
                {
                    promptText.transform.position = arrowPos + Vector3.up * 0.6f;
                    promptText.transform.LookAt(playerCamera);
                    promptText.transform.Rotate(0f, 180f, 0f); // face toward camera
                }
            }
        }
        else
        {
            if (arrowInstance != null)
                arrowInstance.SetActive(false);
            if (promptText != null)
                promptText.gameObject.SetActive(false);
        }

        // ── 4. Handle trigger input ──────────────────────────────────────
        if (inputAction != null)
        {
            float triggerValue = inputAction.ReadValue<float>();
            bool isTriggerPressed = triggerValue >= 0.5f;

            if (isTriggerPressed && !wasTriggerPressed)
            {
                TryCaptureFish();
            }

            wasTriggerPressed = isTriggerPressed;
        }
    }

    /// <summary>
    /// Scans for the best fish target: nearest FishClickable within radius and view cone.
    /// Only switches target if current lock is invalid.
    /// </summary>
    private void UpdateTarget()
    {
        Vector3 camPos = playerCamera.position;

        // Keep current lock if the fish is still valid, in range, AND visible
        if (lockedFish != null && lockedFish.gameObject.activeInHierarchy)
        {
            float dist = Vector3.Distance(camPos, lockedFish.transform.position);
            if (dist <= targetRadius && HasLineOfSight(camPos, lockedFish.transform.position))
                return; // keep current lock
            else
                ClearLock(); // fish out of range or behind terrain
        }

        FishClickable[] allFish = FindObjectsOfType<FishClickable>();

        FishClickable bestTarget = null;
        float bestScore = float.MaxValue;

        Vector3 camForward = playerCamera.forward;

        for (int i = 0; i < allFish.Length; i++)
        {
            FishClickable fish = allFish[i];
            if (fish == null || !fish.gameObject.activeInHierarchy)
                continue;

            Vector3 toFish = fish.transform.position - camPos;
            float dist = toFish.magnitude;

            if (dist > targetRadius)
                continue;

            float angle = Vector3.Angle(camForward, toFish);
            if (angle > viewConeAngle)
                continue;

            // Skip fish that are behind terrain or other solid objects
            if (!HasLineOfSight(camPos, fish.transform.position))
                continue;

            // Prefer closer fish that are more centered in view
            float score = dist + (angle * 0.1f);

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = fish;
            }
        }

        if (bestTarget != null)
        {
            lockedFish = bestTarget;
        }
    }

    /// <summary>
    /// Returns true if there is a clear line of sight between origin and target,
    /// i.e. no terrain, rocks, or other solid objects blocking the view.
    /// Ignores triggers and objects on the UI layer.
    /// </summary>
    private bool HasLineOfSight(Vector3 origin, Vector3 target)
    {
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        // Cast a ray from the camera toward the fish position.
        // If we hit something before reaching the target distance, line of sight is blocked.
        // We use (distance - 0.5f) as max so the ray stops just short of the fish itself,
        // avoiding false positives from the fish's own collider.
        float checkDist = Mathf.Max(distance - 0.5f, 0.1f);

        if (Physics.Raycast(origin, direction.normalized, checkDist, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            // Hit terrain/rock/object before reaching the fish — blocked
            return false;
        }

        // Nothing was hit — clear line of sight
        return true;
    }

    private void TryCaptureFish()
    {
        if (lockedFish == null || !lockedFish.gameObject.activeInHierarchy)
            return;

        float dist = Vector3.Distance(playerCamera.position, lockedFish.transform.position);
        if (dist <= captureDistance)
        {
            Debug.Log($"Fish captured! Distance: {dist:F1}m");
            lockedFish.OnClicked();
            ClearLock();
        }
        else
        {
            Debug.Log($"Fish locked but too far to capture ({dist:F1}m > {captureDistance}m)");
        }
    }

    private void ClearLock()
    {
        lockedFish = null;
        if (arrowInstance != null)
            arrowInstance.SetActive(false);
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Creates a FloatingArrow with a proper downward-pointing arrowhead,
    /// using the fog-immune shader so it's always visible underwater.
    /// </summary>
    private void CreateArrow()
    {
        // Use the Arrow prefab if it exists, otherwise build one from primitives
        GameObject arrowPrefab = Resources.Load<GameObject>("Arrow");

        if (arrowPrefab != null)
        {
            arrowInstance = Instantiate(arrowPrefab);
            arrowInstance.name = "FishTargetArrow";
        }
        else
        {
            arrowInstance = new GameObject("FishTargetArrow");

            // Prepare the fog-immune material
            Material arrowMat = null;
            Shader unlitShader = Shader.Find("EcoShift/UnlitNoFog");
            if (unlitShader != null)
            {
                arrowMat = new Material(unlitShader);
                arrowMat.name = "TargetArrowMat";
                arrowMat.SetColor("_Color", new Color(0f, 1f, 0.3f, 0.9f)); // bright green
                arrowMat.SetFloat("_EmissionStrength", 3f);
            }

            // ── Shaft: thin cylinder ──────────────────────────────────────
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "ArrowShaft";
            shaft.transform.SetParent(arrowInstance.transform, false);
            shaft.transform.localScale = new Vector3(0.04f, 0.12f, 0.04f); // thin and short
            shaft.transform.localPosition = new Vector3(0f, 0.12f, 0f);    // sits above the head
            Object.Destroy(shaft.GetComponent<Collider>());
            if (arrowMat != null) shaft.GetComponent<MeshRenderer>().material = arrowMat;

            // ── Arrowhead: procedural cone mesh pointing down ─────────────
            GameObject head = new GameObject("ArrowHead");
            head.transform.SetParent(arrowInstance.transform, false);
            head.transform.localPosition = Vector3.zero; // tip at the arrow origin

            MeshFilter mf = head.AddComponent<MeshFilter>();
            MeshRenderer mr = head.AddComponent<MeshRenderer>();
            mf.mesh = CreateConeMesh(0.12f, 0.16f, 12); // radius 0.12, height 0.16, 12 sides
            if (arrowMat != null) mr.material = arrowMat;
        }

        // Add the same FloatingArrow script used in the terrestrial scenes
        arrowScript = arrowInstance.AddComponent<FloatingArrow>();
        arrowScript.rotationSpeed = 100f;
        arrowScript.floatAmplitude = 0.3f;  // smaller bob amplitude
        arrowScript.floatFrequency = 1f;
        arrowScript.isActive = false;

        arrowInstance.SetActive(false);
    }

    /// <summary>
    /// Generates a downward-pointing cone mesh (tip at bottom, flat base on top).
    /// </summary>
    private Mesh CreateConeMesh(float radius, float height, int segments)
    {
        Mesh mesh = new Mesh();
        mesh.name = "ArrowCone";

        // Vertices: tip + ring around the base + center of base
        Vector3[] verts = new Vector3[segments + 2];
        verts[0] = new Vector3(0f, -height * 0.5f, 0f); // tip (bottom)

        float baseY = height * 0.5f;
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            verts[i + 1] = new Vector3(Mathf.Cos(angle) * radius, baseY, Mathf.Sin(angle) * radius);
        }
        verts[segments + 1] = new Vector3(0f, baseY, 0f); // base center

        // Triangles: side faces + base cap
        int[] tris = new int[segments * 6];
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;

            // Side triangle (tip → ring[i] → ring[next])
            tris[i * 6 + 0] = 0;
            tris[i * 6 + 1] = i + 1;
            tris[i * 6 + 2] = next + 1;

            // Base triangle (center → ring[next] → ring[i])
            tris[i * 6 + 3] = segments + 1;
            tris[i * 6 + 4] = next + 1;
            tris[i * 6 + 5] = i + 1;
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        return mesh;
    }

    private void UpdateArrowStartPos(Vector3 newPos)
    {
        if (arrowScript != null)
        {
            arrowScript.SetBasePosition(newPos);
        }
    }

    /// <summary>Creates the small "Pull trigger to eat!" prompt text.</summary>
    private void CreatePrompt()
    {
        GameObject go = new GameObject("CapturePrompt");
        promptText = go.AddComponent<TMPro.TextMeshPro>();
        promptText.text = "Pull trigger to eat!";
        promptText.fontSize = 2;
        promptText.color = new Color(0f, 1f, 0.3f, 1f);
        promptText.alignment = TMPro.TextAlignmentOptions.Center;
        promptText.enableWordWrapping = false;
        promptText.faceColor = new Color(0f, 1f, 0.3f, 1f);
        go.SetActive(false);
    }
}
