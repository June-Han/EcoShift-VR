using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HunterPatrol : MonoBehaviour {
    public float speed = 1.5f;
    public AudioClip gunShotSound;
    public DialogueTrigger failDialogue;
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

    public void TriggerShoot(bool isChangingScene = false)
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
        
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }
        
        // ONLY run the local fade if we aren't using the SceneChanger to leave the level!
        if (!isChangingScene)
        {
            StartCoroutine(FadeAndTeleport(mainCamera, 1f, 1f));
        }
        
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
    
    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
    
    private IEnumerator FadeAndTeleport(Camera camera, float fadeToDuration, float fadeFromDuration)
    {
        float timer = 0f;
        
        // Create fade overlay
        GameObject fadeOverlay = new GameObject("FadeOverlay");
        fadeOverlay.transform.SetParent(camera.transform, false);
        fadeOverlay.transform.localPosition = new Vector3(0f, 0f, 0.1f);

        Canvas canvas = fadeOverlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 1000;

        RectTransform rectTransform = fadeOverlay.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(1920f, 1080f);

        Image image = fadeOverlay.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = false;

        // --- Phase 1: Fade to black (0 -> 1) ---
        timer = 0f;
        while (timer < fadeToDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / fadeToDuration);
            Color color = image.color;
            color.a = Mathf.Lerp(0f, 1f, progress);
            image.color = color;
            yield return null;
        }

        // Ensure fully black
        Color blackColor = image.color;
        blackColor.a = 1f;
        image.color = blackColor;

        // --- Teleport player while screen is black ---
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(-100f, -100f, 100f);
        }

        // --- Phase 2: Fade from black (1 -> 0) ---
        timer = 0f;
        while (timer < fadeFromDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / fadeFromDuration);
            Color color = image.color;
            color.a = Mathf.Lerp(1f, 0f, progress);
            image.color = color;
            yield return null;
        }

        // Ensure fully transparent
        Color transparentColor = image.color;
        transparentColor.a = 0f;
        image.color = transparentColor;
        
        failDialogue.TriggerDialogue();

        // --- Delete canvas ---
        Destroy(fadeOverlay);
    }
}
