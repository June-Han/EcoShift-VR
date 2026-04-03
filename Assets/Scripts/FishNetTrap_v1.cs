using UnityEngine;

public class FishNetTrap1 : MonoBehaviour
{
    [Header("Sink Settings")]
    [Tooltip("How fast the net drops to the ocean floor")]
    [SerializeField] private float sinkSpeed = 8f;
    [Tooltip("The lowest point the net will go (Ocean floor)")]
    [SerializeField] private float floorY = 0f; 

    [Header("Catch Settings")]
    [Tooltip("How long the net sits on the floor before the boat reels it back up")]
    [SerializeField] private float reelInDelay = 3f;

    private bool isSinking = true;

    void Start()
    {
        // Start the net scrunched up!
        transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
    }

    void Update()
    {
        //  Expand the net as it falls
        if (transform.localScale.x < 1f)
        {
            transform.localScale += Vector3.one * Time.deltaTime * 2f;
        }

        // Sink down from Y:30 to the floor
        if (isSinking)
        {
            transform.Translate(Vector3.down * sinkSpeed * Time.deltaTime, Space.World);

            // Stop sinking if we hit the sea floor bounds
            if (transform.position.y <= floorY)
            {
                isSinking = false;
                // Destroy the net after a few seconds (simulates the boat reeling the full net back up!)
                Destroy(gameObject, reelInDelay);
            }
        }
    }

    // THE TRAP DETECTOR
    private void OnTriggerEnter(Collider other)
    {
        // DID THE NET HIT A FISH?
        if (other.gameObject.layer == LayerMask.NameToLayer("Fish"))
        {
            Debug.Log("The poachers caught a fish!"); // Change to panel
            Destroy(other.gameObject); // The fish is scooped up and removed from the flock
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