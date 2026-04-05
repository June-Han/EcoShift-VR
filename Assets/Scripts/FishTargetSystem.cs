using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Proximity + line-of-sight targeting system for catching fish (and sometimes debris/litter).
/// Uses a FloatingArrow hovering above the locked target.
///
/// At configurable score thresholds (default 5, 7, 9) a piece of debris/litter is
/// spawned directly in front of the player and the arrow immediately locks onto it,
/// misleading the player into potentially eating it (-3 score penalty).
///
/// Trap spawning is event-driven (fires the instant the score changes) so it cannot
/// be missed by the periodic scan interval.
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

    [Tooltip("If the locked fish is farther than this, allow switching to a nearer target")]
    public float retargetDistance = 10f;

    [Header("Capture")]
    [Tooltip("Fish must be within this distance to be captured when trigger is pressed")]
    public float captureDistance = 8f;

    [Header("Mislead / Trap")]
    [Tooltip("Score thresholds at which debris/litter is spawned near the player as a trap")]
    public int[] misleadAtScores = new int[] { 5, 7, 9 };

    [Tooltip("How far in front of the player the trap spawns")]
    public float trapSpawnDistance = 6f;

    [Header("Input")]
    [Tooltip("Bind to the trigger action, e.g. 'XRI Right Interaction/Activate Value'")]
    public InputActionReference triggerAction;

    [Header("Arrow")]
    [Tooltip("How far above the target the arrow floats")]
    public float arrowYOffset = 0.5f;

    [Header("Line of Sight")]
    [Tooltip("Layers that block line of sight (e.g. terrain, rocks).")]
    public LayerMask obstacleMask = ~0;

    // ── Internal state ───────────────────────────────────────────────────
    private GameObject lockedTarget;
    private InputAction inputAction;
    private bool wasTriggerPressed;
    private float nextScanTime;
    private GameObject arrowInstance;
    private FloatingArrow arrowScript;
    private TMPro.TextMeshPro promptText;

    // Mislead tracking
    private HashSet<int> usedMisleadScores = new HashSet<int>();
    private bool isMisleading;
    private bool pendingMislead;               // set by the score event, consumed by Update
    private GameObject[] resolvedTrapPrefabs;   // cached at Start

    // ─────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────

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

        // Default obstacle mask
        if (obstacleMask == ~0)
            obstacleMask = ~(1 << 5 | 1 << 2);

        ResolveTrapPrefabs();
        SetupInput();
        CreateArrow();
        CreatePrompt();

        // Subscribe to score changes so we can react instantly
        SubscribeToScoreManager();
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.onScoreChanged -= OnScoreChanged;
    }

    private void SubscribeToScoreManager()
    {
        if (hasSubscribed) return;
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.onScoreChanged += OnScoreChanged;
            hasSubscribed = true;
            Debug.Log("FishTargetSystem: Subscribed to ScoreManager.onScoreChanged");
        }
        else
        {
            Debug.LogWarning("FishTargetSystem: ScoreManager not available yet — will retry.");
        }
    }

    private void OnDisable()
    {
        if (inputAction != null)
            inputAction.Disable();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Input setup
    // ─────────────────────────────────────────────────────────────────────

    private void SetupInput()
    {
        if (triggerAction != null && triggerAction.action != null)
        {
            inputAction = triggerAction.action;
        }
        else
        {
            string[] candidateNames = new string[]
            {
                "XRI Right Interaction/Activate Value",
                "XRI RightHand Interaction/Activate Value",
                "XRI RightHand/Activate",
                "XRI Right/Activate"
            };

            foreach (var action in UnityEngine.InputSystem.InputSystem.ListEnabledActions())
            {
                for (int i = 0; i < candidateNames.Length; i++)
                {
                    if (action.name == candidateNames[i].Substring(candidateNames[i].LastIndexOf('/') + 1)
                        && action.actionMap != null
                        && candidateNames[i].StartsWith(action.actionMap.name))
                    {
                        inputAction = action;
                        Debug.Log($"FishTargetSystem: Auto-bound trigger to '{action.actionMap.name}/{action.name}'");
                        break;
                    }
                }
                if (inputAction != null) break;
            }

            if (inputAction == null)
            {
                foreach (var action in UnityEngine.InputSystem.InputSystem.ListEnabledActions())
                {
                    if (action.name.Contains("Activate") && action.expectedControlType == "Axis")
                    {
                        inputAction = action;
                        Debug.Log($"FishTargetSystem: Fallback — bound trigger to '{action.actionMap?.name}/{action.name}'");
                        break;
                    }
                }
            }

            if (inputAction == null)
                Debug.LogWarning("FishTargetSystem: Could not find a trigger input action!");
        }

        if (inputAction != null)
            inputAction.Enable();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Trap prefab resolution
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves trap prefabs from multiple sources:
    ///   1. TrashSpawner in the scene (already has prefabs wired in Inspector)
    ///   2. Resources/TrashPrefabs folder
    ///   3. Last resort: we'll build a primitive at spawn time
    /// </summary>
    private void ResolveTrapPrefabs()
    {
        // Strategy 1: Grab from the TrashSpawner already in the scene
        TrashSpawner spawner = FindFirstObjectByType<TrashSpawner>();
        if (spawner != null && spawner.trashPrefabs != null && spawner.trashPrefabs.Length > 0)
        {
            resolvedTrapPrefabs = spawner.trashPrefabs;
            Debug.Log($"FishTargetSystem: Using {resolvedTrapPrefabs.Length} trap prefabs from TrashSpawner.");
            return;
        }

        // Strategy 2: Load from Resources
        GameObject[] fromResources = Resources.LoadAll<GameObject>("TrashPrefabs");
        if (fromResources != null && fromResources.Length > 0)
        {
            resolvedTrapPrefabs = fromResources;
            Debug.Log($"FishTargetSystem: Using {resolvedTrapPrefabs.Length} trap prefabs from Resources/TrashPrefabs.");
            return;
        }

        // Strategy 3: null — SpawnTrap will create a primitive fallback
        resolvedTrapPrefabs = null;
        Debug.LogWarning("FishTargetSystem: No trap prefabs found. Will use primitive fallback for traps.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Score event handler
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called immediately by ScoreManager when the score changes.
    /// If the new score matches a mislead threshold, flags a pending mislead
    /// that will be consumed on the very next Update frame.
    /// </summary>
    private void OnScoreChanged(int newScore)
    {
        for (int i = 0; i < misleadAtScores.Length; i++)
        {
            if (newScore == misleadAtScores[i] && !usedMisleadScores.Contains(misleadAtScores[i]))
            {
                pendingMislead = true;
                usedMisleadScores.Add(misleadAtScores[i]);
                Debug.Log($"FishTargetSystem: Score hit {newScore} — trap will spawn next frame.");
                return;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Main update loop
    // ─────────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (playerCamera == null) return;

        // Late-subscribe if ScoreManager wasn't ready at Start
        if (!hasSubscribed)
            SubscribeToScoreManager();

        // ── 0. Handle pending mislead (event-driven, immediate) ──────────
        if (pendingMislead)
        {
            pendingMislead = false;
            ClearLock();
            SpawnTrap();
        }

        // ── 1. Scan for targets periodically ─────────────────────────────
        if (!isMisleading && Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + scanInterval;
            UpdateTarget();
        }

        // ── 2. Validate current lock ─────────────────────────────────────
        if (lockedTarget != null && !lockedTarget.activeInHierarchy)
        {
            ClearLock();
        }

        // ── 3. Validate mislead lock (drop if player swims away) ─────────
        if (isMisleading && lockedTarget != null)
        {
            float dist = Vector3.Distance(playerCamera.position, lockedTarget.transform.position);
            if (dist > targetRadius * 1.5f) // generous range before giving up
            {
                Debug.Log("FishTargetSystem: Player swam away from trap — returning to fish targeting.");
                isMisleading = false;
                ClearLock();
            }
        }

        // ── 4. Update arrow position ─────────────────────────────────────
        if (lockedTarget != null)
        {
            float distToTarget = Vector3.Distance(playerCamera.position, lockedTarget.transform.position);
            bool inCaptureRange = distToTarget <= captureDistance;

            Vector3 arrowPos = lockedTarget.transform.position + Vector3.up * arrowYOffset;
            arrowInstance.transform.position = arrowPos;

            if (!arrowInstance.activeSelf)
            {
                arrowInstance.SetActive(true);
                arrowScript.isActive = true;
            }

            UpdateArrowStartPos(arrowPos);

            if (promptText != null)
            {
                promptText.gameObject.SetActive(inCaptureRange);
                if (inCaptureRange)
                {
                    promptText.transform.position = arrowPos + Vector3.up * 0.6f;
                    promptText.transform.LookAt(playerCamera);
                    promptText.transform.Rotate(0f, 180f, 0f);
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

        // ── 5. Handle trigger input ──────────────────────────────────────
        if (inputAction != null)
        {
            float triggerValue = inputAction.ReadValue<float>();
            bool isTriggerPressed = triggerValue >= 0.5f;

            if (isTriggerPressed && !wasTriggerPressed)
            {
                TryCaptureTarget();
            }

            wasTriggerPressed = isTriggerPressed;
        }
    }

    private bool hasSubscribed;

    // ─────────────────────────────────────────────────────────────────────
    // Trap spawning
    // ─────────────────────────────────────────────────────────────────────

    private void SpawnTrap()
    {
        GameObject trap;

        if (resolvedTrapPrefabs != null && resolvedTrapPrefabs.Length > 0)
        {
            // Pick a random prefab from the resolved list
            GameObject prefab = resolvedTrapPrefabs[Random.Range(0, resolvedTrapPrefabs.Length)];

            Vector3 spawnPos = GetTrapSpawnPosition();

            Quaternion spawnRot;
            if (prefab.CompareTag("FishNet"))
                spawnRot = Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f);
            else
                spawnRot = Random.rotation;

            trap = Instantiate(prefab, spawnPos, spawnRot);
            trap.name = $"Trap_{prefab.name}";
            Debug.Log($"FishTargetSystem: Spawned trap from prefab '{prefab.name}' at {spawnPos}");
        }
        else
        {
            // Fallback: create a visible primitive so the feature still works
            trap = CreateFallbackTrap();
            Debug.Log($"FishTargetSystem: Spawned fallback trap at {trap.transform.position}");
        }

        // Ensure clickable component exists for the -3 penalty
        EnsureClickableComponent(trap);

        // Ensure collider exists
        if (trap.GetComponent<Collider>() == null)
            trap.AddComponent<BoxCollider>();

        // Lock onto it immediately
        lockedTarget = trap;
        isMisleading = true;
    }

    private Vector3 GetTrapSpawnPosition()
    {
        Vector3 forward = playerCamera.forward;
        forward.y *= 0.3f; // flatten somewhat but allow slight vertical
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float lateralOffset = Random.Range(-2f, 2f);
        float verticalOffset = Random.Range(-1f, 1f);

        return playerCamera.position
            + forward * trapSpawnDistance
            + right * lateralOffset
            + Vector3.up * verticalOffset;
    }

    private void EnsureClickableComponent(GameObject trap)
    {
        // Already has one?
        if (trap.GetComponent<MarineDebrisClickable>() != null) return;
        if (trap.GetComponent<MarineLitterClickable>() != null) return;

        // Check children too (some prefabs have the component on a child)
        if (trap.GetComponentInChildren<MarineDebrisClickable>() != null) return;
        if (trap.GetComponentInChildren<MarineLitterClickable>() != null) return;

        // Add one — nets get debris, everything else gets litter
        bool isNet = trap.name.ToLower().Contains("net");
        if (isNet)
            trap.AddComponent<MarineDebrisClickable>();
        else
            trap.AddComponent<MarineLitterClickable>();
    }

    /// <summary>
    /// Creates a simple glowing cube as a fallback trap when no prefabs are available.
    /// </summary>
    private GameObject CreateFallbackTrap()
    {
        Vector3 spawnPos = GetTrapSpawnPosition();
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Trap_FallbackDebris";
        cube.transform.position = spawnPos;
        cube.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        cube.transform.rotation = Random.rotation;

        // Give it a murky color so it looks like debris
        Renderer rend = cube.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material = new Material(Shader.Find("Standard"));
            rend.material.color = new Color(0.3f, 0.25f, 0.15f, 1f);
        }

        return cube;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Fish targeting
    // ─────────────────────────────────────────────────────────────────────

    private void UpdateTarget()
    {
        Vector3 camPos = playerCamera.position;
        Vector3 camForward = playerCamera.forward;

        float lockedDist = float.MaxValue;
        bool hasValidLock = false;

        if (lockedTarget != null && lockedTarget.activeInHierarchy)
        {
            lockedDist = Vector3.Distance(camPos, lockedTarget.transform.position);
            bool inRange = lockedDist <= targetRadius;
            bool hasLOS = HasLineOfSight(camPos, lockedTarget.transform.position);

            if (inRange && hasLOS)
                hasValidLock = true;
            else
                ClearLock();
        }

        if (hasValidLock && lockedDist <= retargetDistance)
            return;

        FishClickable[] allFish = FindObjectsOfType<FishClickable>();

        GameObject bestTarget = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < allFish.Length; i++)
        {
            FishClickable fish = allFish[i];
            if (fish == null || !fish.gameObject.activeInHierarchy)
                continue;

            if (lockedTarget != null && fish.gameObject == lockedTarget)
                continue;

            Vector3 toFish = fish.transform.position - camPos;
            float dist = toFish.magnitude;

            if (dist > targetRadius)
                continue;

            float angle = Vector3.Angle(camForward, toFish);
            if (angle > viewConeAngle)
                continue;

            if (!HasLineOfSight(camPos, fish.transform.position))
                continue;

            float score = dist + (angle * 0.1f);

            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = fish.gameObject;
            }
        }

        if (bestTarget != null)
        {
            if (!hasValidLock)
            {
                lockedTarget = bestTarget;
            }
            else
            {
                float bestDist = Vector3.Distance(camPos, bestTarget.transform.position);
                if (bestDist < lockedDist - 2f)
                    lockedTarget = bestTarget;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Line of sight
    // ─────────────────────────────────────────────────────────────────────

    private bool HasLineOfSight(Vector3 origin, Vector3 target)
    {
        Vector3 direction = target - origin;
        float distance = direction.magnitude;
        float checkDist = Mathf.Max(distance - 0.5f, 0.1f);

        RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, checkDist, obstacleMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.GetComponent<FlockUnit>() != null) continue;
            if (hits[i].collider.GetComponent<FishClickable>() != null) continue;
            if (hits[i].collider.GetComponent<MarineDebrisClickable>() != null) continue;
            if (hits[i].collider.GetComponent<MarineLitterClickable>() != null) continue;

            return false;
        }

        return true;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Capture
    // ─────────────────────────────────────────────────────────────────────

    private void TryCaptureTarget()
    {
        if (lockedTarget == null || !lockedTarget.activeInHierarchy)
            return;

        float dist = Vector3.Distance(playerCamera.position, lockedTarget.transform.position);
        if (dist > captureDistance)
        {
            Debug.Log($"FishTargetSystem: Target locked but too far ({dist:F1}m > {captureDistance}m)");
            return;
        }

        // Try fish
        FishClickable fish = lockedTarget.GetComponent<FishClickable>();
        if (fish != null)
        {
            Debug.Log($"FishTargetSystem: Fish captured! Distance: {dist:F1}m");
            fish.OnClicked();
            ClearLock();
            return;
        }

        // ── Debris or litter (trap) ──────────────────────────────────────
        // Mirror exactly what FishClickable does with AddScore:
        // just call ScoreManager.Instance directly, no fallbacks.

        MarineDebrisClickable debris = lockedTarget.GetComponent<MarineDebrisClickable>();
        if (debris == null) debris = lockedTarget.GetComponentInChildren<MarineDebrisClickable>();
        if (debris != null)
        {
            Debug.Log($"FishTargetSystem: Debris eaten! Distance: {dist:F1}m");
            ScoreManager.Instance.RemoveScore(3);
            debris.OnClicked(skipScorePenalty: true);
            ClearLock();
            return;
        }

        MarineLitterClickable litter = lockedTarget.GetComponent<MarineLitterClickable>();
        if (litter == null) litter = lockedTarget.GetComponentInChildren<MarineLitterClickable>();
        if (litter != null)
        {
            Debug.Log($"FishTargetSystem: Litter eaten! Distance: {dist:F1}m");
            ScoreManager.Instance.RemoveScore(3);
            litter.OnClicked(skipScorePenalty: true);
            ClearLock();
            return;
        }

        // Nothing matched — log everything about the target to help debug
        Debug.LogWarning($"FishTargetSystem: Locked target '{lockedTarget.name}' has no clickable component! " +
            $"Components: {string.Join(", ", System.Array.ConvertAll(lockedTarget.GetComponents<Component>(), c => c.GetType().Name))}");
        ClearLock();
    }

    private void ClearLock()
    {
        lockedTarget = null;
        isMisleading = false;
        if (arrowInstance != null)
            arrowInstance.SetActive(false);
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Arrow visuals
    // ─────────────────────────────────────────────────────────────────────

    private void CreateArrow()
    {
        GameObject arrowPrefab = Resources.Load<GameObject>("Arrow");

        if (arrowPrefab != null)
        {
            arrowInstance = Instantiate(arrowPrefab);
            arrowInstance.name = "FishTargetArrow";
        }
        else
        {
            arrowInstance = new GameObject("FishTargetArrow");

            Material arrowMat = null;
            Shader unlitShader = Shader.Find("EcoShift/UnlitNoFog");
            if (unlitShader != null)
            {
                arrowMat = new Material(unlitShader);
                arrowMat.name = "TargetArrowMat";
                arrowMat.SetColor("_Color", new Color(0f, 1f, 0.3f, 0.9f));
                arrowMat.SetFloat("_EmissionStrength", 3f);
            }

            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "ArrowShaft";
            shaft.transform.SetParent(arrowInstance.transform, false);
            shaft.transform.localScale = new Vector3(0.04f, 0.12f, 0.04f);
            shaft.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            Object.Destroy(shaft.GetComponent<Collider>());
            if (arrowMat != null) shaft.GetComponent<MeshRenderer>().material = arrowMat;

            GameObject head = new GameObject("ArrowHead");
            head.transform.SetParent(arrowInstance.transform, false);
            head.transform.localPosition = Vector3.zero;

            MeshFilter mf = head.AddComponent<MeshFilter>();
            MeshRenderer mr = head.AddComponent<MeshRenderer>();
            mf.mesh = CreateConeMesh(0.12f, 0.16f, 12);
            if (arrowMat != null) mr.material = arrowMat;
        }

        arrowScript = arrowInstance.AddComponent<FloatingArrow>();
        arrowScript.rotationSpeed = 100f;
        arrowScript.floatAmplitude = 0.3f;
        arrowScript.floatFrequency = 1f;
        arrowScript.isActive = false;

        arrowInstance.SetActive(false);
    }

    private Mesh CreateConeMesh(float radius, float height, int segments)
    {
        Mesh mesh = new Mesh();
        mesh.name = "ArrowCone";

        Vector3[] verts = new Vector3[segments + 2];
        verts[0] = new Vector3(0f, -height * 0.5f, 0f);

        float baseY = height * 0.5f;
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            verts[i + 1] = new Vector3(Mathf.Cos(angle) * radius, baseY, Mathf.Sin(angle) * radius);
        }
        verts[segments + 1] = new Vector3(0f, baseY, 0f);

        int[] tris = new int[segments * 6];
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            tris[i * 6 + 0] = 0;
            tris[i * 6 + 1] = i + 1;
            tris[i * 6 + 2] = next + 1;
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
            arrowScript.SetBasePosition(newPos);
    }

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
