using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Jobs;
using Unity.Burst;

public class Flock : MonoBehaviour
{
    Transform playerTransform;

    [Header("Spawn Setup")]
    private FlockUnit[] avaliableflockUnitPrefabs;
    [SerializeField] private int flockSize;
    [SerializeField] private Vector3 spawnBounds;

    [Header("Speed Setup")]
    [Range(0, 10)]
    [SerializeField] private float _minSpeed;
    public float minSpeed { get { return _minSpeed; } }
    [Range(0, 10)]
    [SerializeField] private float _maxSpeed;
    public float maxSpeed { get { return _maxSpeed; } }

    [Header("Detection Distances")]
    [Range(0, 10)]
    [SerializeField] private float _cohesionDistance;
    public float cohesionDistance { get { return _cohesionDistance; } }

    [Range(0, 10)]
    [SerializeField] private float _avoidanceDistance;
    public float avoidanceDistance { get { return _avoidanceDistance; } }

    [Range(0, 10)]
    [SerializeField] private float _alignmentDistance;
    public float alignmentDistance { get { return _alignmentDistance; } }

    [Range(0, 10)]
    [SerializeField] private float _obstacleDistance;
    public float obstacleDistance { get { return _obstacleDistance; } }

    [Range(0, 100)]
    [SerializeField] private float _boundsDistance;
    public float boundsDistance { get { return _boundsDistance; } }

    [Header("Behaviour Weights")]
    [Range(0, 10)]
    [SerializeField] private float _cohesionWeight;
    public float cohesionWeight { get { return _cohesionWeight; } }

    [Range(0, 10)]
    [SerializeField] private float _avoidanceWeight;
    public float avoidanceWeight { get { return _avoidanceWeight; } }

    [Range(0, 10)]
    [SerializeField] private float _alignmentWeight;
    public float alignmentWeight { get { return _alignmentWeight; } }

    [Range(0, 100)]
    [SerializeField] private float _boundsWeight;
    public float boundsWeight { get { return _boundsWeight; } }

    [Range(0, 100)]
    [SerializeField] private float _obstacleWeight;
    public float obstacleWeight { get { return _obstacleWeight; } }

    // --- BRINGING THE OBSTACLE MASK BACK ---
    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask _obstacleMask;
    public LayerMask obstacleMask { get { return _obstacleMask; } }

    public FlockUnit[] allUnits { get; set; }
    private FlockUnit flockUnitPrefab;

    private void Start()
    {
		// Grab all prefab that has a FlockUnit script under Resources/FishPrefabs
		avaliableflockUnitPrefabs = Resources.LoadAll<FlockUnit>("FishPrefabs");

		// A quick safety check just in case the folder is empty or misspelled
        if (avaliableflockUnitPrefabs.Length == 0)
        {
            Debug.LogError("No fish found! Make sure your prefabs are inside a folder named exactly: Resources/FishPrefabs");
            return; // Stops the script from crashing
        }
		
		GameObject playerCamera = GameObject.FindGameObjectWithTag("MainCamera");
        if (playerCamera != null)
        {
            playerTransform = playerCamera.transform;
        }
        else
        {
            Debug.LogWarning("Flock could not find the MainCamera! Did you forget to tag it?");
            playerTransform = this.transform; // Fallback so the game doesn't crash
        }
        // SetRandomValues();
        GenerateUnits();
    }

    private void SetRandomValues()
    {
        _boundsDistance = UnityEngine.Random.Range(5f, 30f);
        _cohesionWeight = UnityEngine.Random.Range(1f, 10f);
        _alignmentWeight = UnityEngine.Random.Range(1f, 10f);
        _avoidanceWeight = UnityEngine.Random.Range(1f, 10f);
        _minSpeed = UnityEngine.Random.Range(0.3f, 0.8f);
        _maxSpeed = UnityEngine.Random.Range(0.8f, 1.5f);
    }

    private void Update()
    {
        // --- THE "DEAD FISH" RESPAWN SYSTEM ---
        // Checks for both destroyed (null) AND deactivated (eaten via FishClickable) fish.
        for (int i = 0; i < allUnits.Length; i++)
        {
            bool needsRespawn = allUnits[i] == null || !allUnits[i].gameObject.activeInHierarchy;
            if (needsRespawn)
            {
                // Clean up the deactivated fish object before spawning a replacement
                if (allUnits[i] != null)
                {
                    Destroy(allUnits[i].gameObject);
                    allUnits[i] = null;
                }

                // Calculate a random spawn position near the Flock Manager
                Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * (boundsDistance * 0.5f);
                Vector3 spawnPos = transform.position + randomOffset;
                Quaternion spawnRot = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

                // Spawn the new fish!
                var newFish = Instantiate(flockUnitPrefab, spawnPos, spawnRot);

                // Keep your Hierarchy clean by organizing it under the manager
                newFish.transform.SetParent(this.transform);

                // Plug the hole in the array!
                allUnits[i] = newFish;

                Debug.Log("A poached fish was replaced by the ecosystem!");
            }
        }
        // -------------------------------------------
        NativeArray<Vector3> unitForwardDirections = new NativeArray<Vector3>(allUnits.Length, Allocator.TempJob);
        NativeArray<Vector3> unitPositions = new NativeArray<Vector3>(allUnits.Length, Allocator.TempJob);
        NativeArray<Vector3> currentVelocities = new NativeArray<Vector3>(allUnits.Length, Allocator.TempJob);
        NativeArray<Vector3> cohesionNeighbours = new NativeArray<Vector3>(allUnits.Length, Allocator.TempJob);
        NativeArray<Vector3> avoidanceNeighbours = new NativeArray<Vector3>(allUnits.Length, Allocator.TempJob);
        NativeArray<Vector3> alignmentNeighbours = new NativeArray<Vector3>(allUnits.Length, Allocator.TempJob);
        NativeArray<float> allUnitsSpeeds = new NativeArray<float>(allUnits.Length, Allocator.TempJob);
        NativeArray<float> neighbourSpeeds = new NativeArray<float>(allUnits.Length, Allocator.TempJob);
        
        // --- ARRAYS FOR THE THREADED RAYCASTS ---
        NativeArray<RaycastCommand> raycastCommands = new NativeArray<RaycastCommand>(allUnits.Length, Allocator.TempJob);
        NativeArray<RaycastHit> raycastHits = new NativeArray<RaycastHit>(allUnits.Length, Allocator.TempJob);

        QueryParameters queryParams = new QueryParameters();
        queryParams.layerMask = obstacleMask;
        queryParams.hitTriggers = QueryTriggerInteraction.Ignore;

        for (int i = 0; i < allUnits.Length; i++)
        {
            unitForwardDirections[i] = allUnits[i].myTransform.forward;
            unitPositions[i] = allUnits[i].myTransform.position;
            currentVelocities[i] = allUnits[i].currentVelocity;
            cohesionNeighbours[i] = Vector3.zero;
            avoidanceNeighbours[i] = Vector3.zero;
            alignmentNeighbours[i] = Vector3.zero;
            allUnitsSpeeds[i] = allUnits[i].speed;
            neighbourSpeeds[i] = 0f;

            // Load a RaycastCommand for every single fish!
            raycastCommands[i] = new RaycastCommand(unitPositions[i], unitForwardDirections[i], queryParams, obstacleDistance);
        }
        
        // 1. SCHEDULE THE RAYCASTS FIRST
        JobHandle raycastHandle = RaycastCommand.ScheduleBatch(raycastCommands, raycastHits, 64, default);

        // 2. PASS THE RAYCAST HITS TO THE MOVE JOB
        MoveJob moveJob = new MoveJob
        {
            unitForwardDirections = unitForwardDirections,
            unitPositions = unitPositions,
            currentVelocities = currentVelocities,
            allUnitsSpeeds = allUnitsSpeeds,
            flockPosition = transform.position,
            cohesionDistance = cohesionDistance,
            alignmentDistance = alignmentDistance,
            avoidanceDistance = avoidanceDistance,
            obstacleDistance = obstacleDistance,
            boundsDistance = boundsDistance,
            cohesionWeight = cohesionWeight,
            alignmentWeight = alignmentWeight,
            avoidanceWeight = avoidanceWeight,
            obstacleWeight = obstacleWeight,
            boundsWeight = boundsWeight,
            angle = flockUnitPrefab.FOVAngle,
            minSpeed = minSpeed,
            maxSpeed = maxSpeed,
            smoothDamp = flockUnitPrefab.smoothDamp,
            deltaTime = Time.deltaTime,
            cohesionNeighbours = cohesionNeighbours,
            avoidanceNeighbours = avoidanceNeighbours,
            alignmentNeighbours = alignmentNeighbours,
            neighbourSpeeds = neighbourSpeeds,
            playerPosition = playerTransform.position,
            randomDirection = UnityEngine.Random.insideUnitSphere,
            
            // Pass the hits!
            raycastHits = raycastHits 
        };

        // 3. TELL THE MOVE JOB IT HAS TO WAIT FOR THE RAYCASTS TO FINISH
        JobHandle jobHandle = moveJob.Schedule(allUnits.Length, 64, raycastHandle);
        jobHandle.Complete();

        // Apply Data and Animations
        for (int i = 0; i < allUnits.Length; i++)
        {
			// Save the old forward direction before changing to turn
            Vector3 previousForward = allUnits[i].myTransform.forward;

			// Apply the new transforms from the Burst Job
            allUnits[i].myTransform.forward = unitForwardDirections[i];
            allUnits[i].myTransform.position = unitPositions[i];
            allUnits[i].currentVelocity = currentVelocities[i];
            allUnits[i].speed = allUnitsSpeeds[i];

			// ANIMATOR ADAPTATION (Driving the Blend Trees)
            if (allUnits[i].animator != null)
            {
				// InverseLerp to map your minSpeed and maxSpeed to a 0.0 - 1.0 range.
                // Multiply maxSpeed by 3f because of speedboost by 3x when avoiding obstacles!
                float normalizedSpeed = Mathf.InverseLerp(minSpeed, maxSpeed * 3f, allUnitsSpeeds[i]);
                allUnits[i].animator.SetFloat("swimSpeed", normalizedSpeed);

				// -- CALCULATE TURN (0 = Left, 0.5 = Center, 1 = Right) --
                // Find out how many degrees the fish rotated this frame
                float signedAngle = Vector3.SignedAngle(previousForward, unitForwardDirections[i], Vector3.up);
                
				// Assuming a maximum turn angle of about 5 degrees per frame.
                // Clamp the angle between -5 and 5, then map it to -1 to 1.
				float turnRate = Mathf.Clamp(signedAngle / 5f, -1f, 1f); 

				// Convert that -1 to 1 range into the 0 to 1 range your Animator expects!
                float targetTurnParam = (turnRate + 1f) / 2f; 


				// Smoothly blend the turn so the fish doesn't snap unnaturally
                float currentTurn = allUnits[i].animator.GetFloat("turn");
                float smoothedTurn = Mathf.Lerp(currentTurn, targetTurnParam, Time.deltaTime * 5f);
                
                allUnits[i].animator.SetFloat("turn", smoothedTurn);
            }
        }
        
        // Dispose of all temporary job data to prevent memory leaks!
        unitForwardDirections.Dispose();
        unitPositions.Dispose();
        currentVelocities.Dispose();
        allUnitsSpeeds.Dispose();
        cohesionNeighbours.Dispose();
        avoidanceNeighbours.Dispose();
        alignmentNeighbours.Dispose();
        neighbourSpeeds.Dispose();
        raycastCommands.Dispose();
        raycastHits.Dispose();
    }

    private void GenerateUnits()
    {
        allUnits = new FlockUnit[flockSize];
        flockUnitPrefab = avaliableflockUnitPrefabs[UnityEngine.Random.Range(0, avaliableflockUnitPrefabs.Length)];
        for (int i = 0; i < flockSize; i++)
        {
            var randomVector = UnityEngine.Random.insideUnitSphere;
            randomVector = new Vector3(randomVector.x * spawnBounds.x, randomVector.y * spawnBounds.y, randomVector.z * spawnBounds.z);
            var spawnPosition = transform.position + randomVector;
            var rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
            allUnits[i] = Instantiate(flockUnitPrefab, spawnPosition, rotation);
            allUnits[i].myTransform.localScale = Vector3.one * UnityEngine.Random.Range(0.5f, 0.6f);
            allUnits[i].AssignFlock(this);
            allUnits[i].InitializeSpeed(UnityEngine.Random.Range(minSpeed, maxSpeed));
        }
    }
}

[BurstCompile]
public struct MoveJob : IJobParallelFor
{
    public NativeArray<Vector3> unitForwardDirections;
    public NativeArray<Vector3> currentVelocities;

    [NativeDisableParallelForRestriction]
    public NativeArray<Vector3> unitPositions;
    [NativeDisableParallelForRestriction]
    public NativeArray<Vector3> cohesionNeighbours;
    [NativeDisableParallelForRestriction]
    public NativeArray<Vector3> avoidanceNeighbours;
    [NativeDisableParallelForRestriction]
    public NativeArray<Vector3> alignmentNeighbours;
    [NativeDisableParallelForRestriction]
    public NativeArray<float> allUnitsSpeeds;
    [NativeDisableParallelForRestriction]
    public NativeArray<float> neighbourSpeeds;

    // --- NEW: THE RESULTS FROM OUR THREADED RAYCASTS ---
    [ReadOnly] public NativeArray<RaycastHit> raycastHits;

    public Vector3 flockPosition;
    public Vector3 playerPosition;
    public Vector3 randomDirection;
    public float cohesionDistance;
    public float alignmentDistance;
    public float avoidanceDistance;
    public float boundsDistance;
    public float obstacleDistance;
    public float cohesionWeight;
    public float alignmentWeight;
    public float avoidanceWeight;
    public float boundsWeight;
    public float obstacleWeight;
    public float angle;
    public float minSpeed;
    public float maxSpeed;
    public float smoothDamp;
    public float deltaTime;

    public void Execute(int index)
    {

        int cohesionIndex = 0;
        int avoidanceIndex = 0;
        int aligemetIndex = 0;
        for (int i = 0; i < unitPositions.Length; i++)
        {
            var currentUnitPos = unitPositions[i];
            if (unitPositions[index] != currentUnitPos)
            {
                float currentNeighbourDistanceSqr = Vector3.SqrMagnitude(unitPositions[index] - unitPositions[i]);
                if (currentNeighbourDistanceSqr <= cohesionDistance * cohesionDistance)
                {
                    cohesionNeighbours[cohesionIndex] = currentUnitPos;
                    neighbourSpeeds[cohesionIndex] = allUnitsSpeeds[i];
                    cohesionIndex++;
                }
                if (currentNeighbourDistanceSqr <= avoidanceDistance * avoidanceDistance)
                {
                    avoidanceNeighbours[avoidanceIndex] = currentUnitPos;
                    avoidanceIndex++;
                }
                if (currentNeighbourDistanceSqr <= alignmentDistance * alignmentDistance)
                {
                    alignmentNeighbours[aligemetIndex] = currentUnitPos;
                    aligemetIndex++;
                }
            }
        }
        float speed = 0;
        if (cohesionNeighbours.Length != 0)
        {

            for (int i = 0; i < cohesionNeighbours.Length; i++)
            {
                if (neighbourSpeeds[i] != 0f)
                    speed += neighbourSpeeds[i];
            }

            speed /= cohesionNeighbours.Length;
        }
        speed = Mathf.Clamp(speed, minSpeed, maxSpeed);

        var cohesionVector = Vector3.zero;
        if (cohesionNeighbours.Length != 0)
        {
            int cohesionNeighboursInFOV = 0;
            for (int i = 0; i <= cohesionIndex; i++)
            {
                if (IsInFOV(unitForwardDirections[index], unitPositions[index], cohesionNeighbours[i], angle) && (cohesionNeighbours[i] != Vector3.zero))
                {
                    cohesionNeighboursInFOV++;
                    cohesionVector += cohesionNeighbours[i];
                }
            }

            cohesionVector /= cohesionNeighboursInFOV;
            cohesionVector -= unitPositions[index];
            cohesionVector = cohesionVector.normalized * cohesionWeight;
        }


        var avoidanceVector = Vector3.zero;
        if (avoidanceNeighbours.Length != 0)
        {
            int avoidanceNeighboursInFOV = 0;
            for (int i = 0; i <= avoidanceIndex; i++)
            {
                if (IsInFOV(unitForwardDirections[index], unitPositions[index], avoidanceNeighbours[i], angle) && (avoidanceNeighbours[i] != Vector3.zero))
                {
                    avoidanceNeighboursInFOV++;
                    avoidanceVector += (unitPositions[index] - avoidanceNeighbours[i]);
                }
            }

            avoidanceVector /= avoidanceNeighboursInFOV;
            avoidanceVector = avoidanceVector.normalized * avoidanceWeight;
        }

        var alignmentVector = Vector3.zero;
        if (alignmentNeighbours.Length != 0)
        {

            int alignmentNeighboursInFOV = 0;
            for (int i = 0; i <= aligemetIndex; i++)
            {
                if (IsInFOV(unitForwardDirections[index], unitPositions[index], alignmentNeighbours[i], angle) && (alignmentNeighbours[i] != Vector3.zero))
                {
                    alignmentNeighboursInFOV++;
                    alignmentVector += alignmentNeighbours[i];
                }
            }

            alignmentVector /= alignmentNeighboursInFOV;
            alignmentVector = alignmentVector.normalized * alignmentWeight;
        }

        var offsetToCenter = flockPosition - unitPositions[index];
        bool isNearCenter = (offsetToCenter.magnitude >= boundsDistance * 0.9f);
        var boundsVector = isNearCenter ? offsetToCenter.normalized : Vector3.zero;
        boundsVector *= boundsWeight;

        // --- AVOIDANCE LOGIC ---
        var obstacleVector = Vector3.zero;
        
        // Avoid the Player
        var offsetToPlayer = playerPosition - unitPositions[index];
        if (Vector3.SqrMagnitude(offsetToPlayer) <= obstacleDistance * obstacleDistance)
        {
            offsetToPlayer = new Vector3(
                offsetToPlayer.x + (index - unitPositions.Length / 2) / 30,
                offsetToPlayer.y + (index - unitPositions.Length / 2) / 30,
                offsetToPlayer.z + (index - unitPositions.Length / 2) / 30);
            obstacleVector = -offsetToPlayer;
            speed *= 3f;
        }
        
        // Avoid the Terrain (Using our Raycast Command Hits)
        RaycastHit hit = raycastHits[index];
        if (hit.distance > 0) // Meaning the raycast hit a valid obstacle
        {
            // Steer aggressively along the angle of the terrain to bounce off it
            obstacleVector += hit.normal * 5f; 
            speed *= 2f; // Speed up to escape the wall
        }

        obstacleVector = obstacleVector.normalized * obstacleWeight;
        // ------------------------------------

        Vector3 currentVelocity = currentVelocities[index];
        var moveVector = cohesionVector + avoidanceVector + alignmentVector + boundsVector + obstacleVector;
        moveVector = Vector3.RotateTowards(unitForwardDirections[index], moveVector, 1f * deltaTime, 0f);
        moveVector = moveVector.normalized * speed;
        if (moveVector == Vector3.zero)
        {
            moveVector = unitForwardDirections[index];
        }
        unitPositions[index] = unitPositions[index] + moveVector * deltaTime;
        unitForwardDirections[index] = moveVector.normalized;
        allUnitsSpeeds[index] = speed;
        currentVelocities[index] = currentVelocity;

    }

    private bool IsInFOV(Vector3 forward, Vector3 unitPosition, Vector3 targetPosition, float angle)
    {
        return Vector3.Angle(forward, targetPosition - unitPosition) <= angle;
    }
}