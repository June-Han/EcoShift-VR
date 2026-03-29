using System;
using UnityEngine;
using UnityEngine.AI;

public class HunterPatrol : MonoBehaviour {
    public float speed = 1.5f;
    public AudioClip gunShotSound;
    public ScreenFader screenFader;
    public Transform[] waypoints; 
    private int currentPoint = 0;
    private NavMeshAgent agent;
    [SerializeField] private bool startMove = false;
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
        
        if (agent.enabled && !agent.pathPending && agent.remainingDistance < 0.5f)
            GoToNextPoint();
        
        if (!hasShot && player != null && Vector3.Distance(transform.position, player.transform.position) < detectionRadius)
        {
            TriggerShoot();
            // agent.enabled = false; 
            // Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
            // transform.rotation = Quaternion.LookRotation(directionToPlayer);
            // animator.SetBool("IsWalking", false);
            // animator.SetTrigger("Shoot");
            // hasShot = true;
            // AudioSource.PlayClipAtPoint(gunShotSound, transform.position);
            // screenFader.StartFadeToBlack(1.5f);
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
    }

    void GoToNextPoint() {
        if (waypoints.Length == 0) return;
        agent.destination = waypoints[currentPoint].position;
        agent.speed = speed;
        currentPoint = (currentPoint + 1) % waypoints.Length;
    }
    
    public void StartPatrol()
    {
        startMove = true;
        agent.enabled = true; 
        agent.speed = speed;
        if (waypoints.Length > 0)
        {
            GoToNextPoint(); 
        }

        // Start the chase timer and vignette
        if (HunterChaseManager.Instance != null)
        HunterChaseManager.Instance.StartChase();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
