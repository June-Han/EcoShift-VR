using UnityEngine;
using UnityEngine.AI;

public class PoacherBoatManager : MonoBehaviour
{
    [Header("References")]
    private Transform playerShark;
    private NavMeshAgent agent;
    
    // Procedural Animation
    [Tooltip("Drag the boat here to control its procedural propeller/audio")]
    [SerializeField] private PoachBoatAnimation boatVisuals; 
    
    [Tooltip("The Net prefab to spawn into the water")]
    [SerializeField] private GameObject netPrefab;
    public GameObject NetPrefab => netPrefab;

    [Tooltip("An empty GameObject placed at the back of the boat where nets fall from")]
    [SerializeField] private Transform netDropPoint;
    public Transform NetDropPoint => netDropPoint;

    [Header("Settings")]
    [Tooltip("How close the shark must be for the boat to notice")]
    [SerializeField] private float detectionRadius = 50f;
    
    [Tooltip("Seconds between dropping each net while chasing")]
    [SerializeField] private float netDropCooldown = 3f;

    private float nextDropTime = 0f;
    private bool isChasing = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Auto-find the VR player using the MainCamera tag we set up earlier!
        GameObject cam = GameObject.FindGameObjectWithTag("MainCamera");
        if (cam != null)
        {
            playerShark = cam.transform;
        }
        else
        {
            Debug.LogError("Poacher cannot find the shark! Is the camera tagged 'MainCamera'?");
        }
    }

    void Update()
    {
        if (playerShark == null) return;

        // FLATTEN THE MATH
        // Only X and Z changes. Force the Y coordinate to be the boat's level.
        Vector3 surfacePlayerPos = new Vector3(playerShark.position.x, transform.position.y, playerShark.position.z);
        
        // Check the distance
        float distanceToPlayer = Vector3.Distance(transform.position, surfacePlayerPos);

        // State Machine Trigger
        if (distanceToPlayer <= detectionRadius)
        {
            ChaseAndNet(surfacePlayerPos);
        }
        else
        {
            IdleAndFish();
        }
    }

    private void ChaseAndNet(Vector3 targetPos)
    {
        // If the boat isn't touching the NavMesh yet, abort the function to prevent the error crash!
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("Poacher Boat is hovering or off the NavMesh! Waiting for gravity...");
            return; 
        }
        
        if (!isChasing)
        {
            isChasing = true;
            
            // TURN THE ENGINE ON AND REV IT!
            if (boatVisuals != null) 
            {
                boatVisuals.behaviorIsOn = true;
                boatVisuals.behaviorIsRevving = true;
            }
        }

        // Drive towards the player's surface shadow
        agent.SetDestination(targetPos);

        // Drop nets on a timer
        if (Time.time >= nextDropTime)
        {
            DropNet();
            nextDropTime = Time.time + netDropCooldown; // Reset timer
        }
    }

    private void IdleAndFish()
    {
        if (isChasing)
        {
            isChasing = false;
            agent.ResetPath(); // Stop the NavMesh pathfinding
            
            // IDLE THE ENGINE
            if (boatVisuals != null) 
            {
                boatVisuals.behaviorIsRevving = false;
                // Set behaviorIsOn to false if you want the engine completely off while idling
            }
        }
    }

    private void DropNet()
    {
        if (netPrefab != null && netDropPoint != null)
        {
            // Spawn the net
            Instantiate(netPrefab, netDropPoint.position, netDropPoint.rotation);
        }
    }

    // --- Draw the radius in the editor to see it ---
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        // Draws a red sphere around the boat in the Unity Scene view
        Gizmos.DrawWireSphere(transform.position, detectionRadius); 
    }
}