using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class HunterPatrol : MonoBehaviour {
    public float speed = 1.5f;
    public AudioClip gunShotSound;
    public ScreenFader screenFader;
    private NavMeshAgent agent;
    [SerializeField] private bool startMove = false;
    private bool isChasing = false;
    private Animator animator;
    private GameObject player;
    private float detectionRadius = 3.5f;
    private bool hasShot = false;

    void Start() {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        agent.enabled = false; 
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update() {
        animator.SetBool("IsWalking", agent.enabled && agent.hasPath && agent.velocity.sqrMagnitude > 0);
        
        if (isChasing && player != null && agent.enabled) {
            // Update destination to player's current position
            agent.destination = player.transform.position;
            
            // Check if close enough to shoot
            if (!hasShot && Vector3.Distance(transform.position, player.transform.position) < detectionRadius) {
                TriggerShoot();
            }
        }
    }

    public void TriggerShoot()
    {
        if (hasShot) return; // prevent double triggering
        
        agent.enabled = false;
        if (player != null)
        {
            Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }
        animator.SetBool("IsWalking", false);
        animator.SetTrigger("Shoot");
        hasShot = true;
        AudioSource.PlayClipAtPoint(gunShotSound, transform.position);
        screenFader.StartFadeToBlack(1.5f);
        
        // Reload scene after 1 second
        Invoke("ReloadScene", 1f);
    }

    private void ReloadScene()
    {
        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void StartPatrol()
    {
        startMove = true;
        isChasing = true;
        agent.enabled = true; 
        agent.speed = speed;
        
        // Start chasing the player
        if (player != null) {
            agent.destination = player.transform.position;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
