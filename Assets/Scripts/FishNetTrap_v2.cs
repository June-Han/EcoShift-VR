using UnityEngine;
using System.Collections.Generic;

public class FishNetTrap2 : MonoBehaviour
{
    [Header("Sink Settings")]
    [Tooltip("How fast the net drops to the ocean floor")]
    [SerializeField] private float sinkSpeed = 10f;
    [Tooltip("The lowest point the net will go (Ocean floor)")]
    [SerializeField] private float floorY = 0f; 

    [Header("Reel Settings")]
    [Tooltip("How fast the boat pulls the net back up")]
    [SerializeField] private float reelSpeed = 8f;
    [Tooltip("Where the boat is located so the net knows when to destroy itself")]
    [SerializeField] private float surfaceY = 30f;
    [SerializeField] private float reelInDelay = 3f;

    [Header("Scale Settings")]
    [SerializeField] private float openScale = 180f;
    [SerializeField] private float stretchScaleZ = 700f;
    [SerializeField] private float scaleSpeed = 100f; // How fast it expands/stretches

    [HideInInspector] public Transform boatTarget;


    // The 3 phases of the net
    private enum NetState { Sinking, Resting, Reeling }
    private NetState currentState = NetState.Sinking;

    // This list remembers every fish the net touch
    private List<GameObject> trappedFishes = new List<GameObject>();
    
    private float restTimer = 0.5f;

    void Start()
    {
        // Start scrunched up
        transform.localScale = new Vector3(20f, 20f, 20f);
    }

    void Update()
    {
        switch (currentState)
        {
            case NetState.Sinking:
                // Expand the net to 180x180x180 as it falls
                Vector3 targetOpenScale = new Vector3(openScale, openScale, openScale);
                transform.localScale = Vector3.MoveTowards(transform.localScale, targetOpenScale, scaleSpeed * Time.deltaTime);

                // Sink down to the floor
                transform.Translate(Vector3.down * sinkSpeed * Time.deltaTime, Space.World);

                if (transform.position.y <= floorY)
                {
                    // We hit the bottom! Switch to the Resting phase.
                    currentState = NetState.Resting;
                    restTimer = reelInDelay; 
                }
                break;

            case NetState.Resting:
                // Wait on the bottom for the fish to swim over it
                restTimer -= Time.deltaTime;
                if (restTimer <= 0f)
                {
                    // THE TIMER IS DONE! SEARCH THE OCEAN FOR THE BOAT NOW:
                    GameObject activeBoat = GameObject.Find("boat_1"); // Searches the whole game for the boat
                    
                    if (activeBoat != null)
                    {
                        // Try to find the exact drop point attached to that boat
                        Transform exactDropPoint = activeBoat.transform.Find("netDropPoint");
                        
                        // If it has the drop point, target it! Otherwise, just target the boat's center.
                        boatTarget = exactDropPoint != null ? exactDropPoint : activeBoat.transform;
                    }
                    else
                    {
                        Debug.LogWarning("Net woke up but couldn't find 'boat_1' in the scene!");
                    }

                    // Timer finished! Switch to the Reeling phase! Start pulling it up!
                    currentState = NetState.Reeling;
                }
                break;

            case NetState.Reeling:
                // Stretch the Z scale to 500 for the water drag effect
                Vector3 targetStretchScale = new Vector3(openScale, openScale, stretchScaleZ);
                transform.localScale = Vector3.MoveTowards(transform.localScale, targetStretchScale, scaleSpeed * 2f * Time.deltaTime);

                // Drag the trapped fish with net
                foreach(GameObject fish in trappedFishes)
                {
                    if (fish != null)
                    {
                        // Pulls the fish directly into the center of the net as it rises
                        fish.transform.position = Vector3.MoveTowards(fish.transform.position, transform.position, reelSpeed * 2f * Time.deltaTime);
                    }
                }

                if (boatTarget != null)
                {
                    // Move diagonally through the water straight toward the back of the driving boat
                    transform.position = Vector3.MoveTowards(transform.position, boatTarget.position, reelSpeed * Time.deltaTime);

                    // Destroy the net once it reaches the boat
                    if (Vector3.Distance(transform.position, boatTarget.position) < 2f)
                    {
                        DestroyTrappedFish();
                        Destroy(gameObject);
                    }
                }
                else 
                {
                    // Failsafe: If the current boatTarget was not found
                    // Pull the net UP towards the boat
                    transform.Translate(Vector3.up * reelSpeed * Time.deltaTime, Space.World);

                    // Destroy the net once it reaches the surface
                    if (transform.position.y >= surfaceY) 
                    {
                        DestroyTrappedFish();
                        Destroy(gameObject);
                    }
                }

                break;
        }
    }

    private void DestroyTrappedFish()
    {
        foreach(GameObject fish in trappedFishes)
        {
            if (fish != null)
            {
                Destroy(fish);
            }
        }
    }

    // THE TRAP DETECTOR
    private void OnTriggerEnter(Collider other)
    {
        // DID THE NET HIT A FISH?
        if (other.gameObject.layer == LayerMask.NameToLayer("Fish"))
        {
            trappedFishes.Add(other.gameObject);
            MonoBehaviour fishAI = other.GetComponent<MonoBehaviour>();
            if (fishAI != null) fishAI.enabled = false;
            Debug.Log("The poachers caught a fish!"); // Change to panel
            // Destroy(other.gameObject); // The fish is scooped up and removed from the flock
        }
        
        // Did THE NET HIT THE PLAYER?
        else if (other.CompareTag("Player") || other.CompareTag("MainCamera"))
        {
            // Find the health script on the XR Origin
            SharkThreatStatus shark = other.GetComponentInParent<SharkThreatStatus>();
            if (shark != null)
            {
                shark.GetTangled(); // Lose 1 life!
            }

            //Trigger motion slowdown (for 5 seconds)
            SwimMotion motion = other.GetComponentInParent<SwimMotion>();
            if (motion != null) motion.SlowDownPlayer(5f);
            
            // The net tears on the shark, destroying the net instantly!
            Destroy(gameObject);
        }
    }
}