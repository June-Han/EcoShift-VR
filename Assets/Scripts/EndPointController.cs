using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndPointController : MonoBehaviour
{
    [Header("Settings")]
    public AudioClip gunShotClip;        
    public DialogueTrigger dialogue;
    public HunterPatrol hunter;          

    private bool hasReached = false;     
    private GameObject player;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player not found in scene!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasReached && other.CompareTag("Player"))
        {
            hasReached = true;
            StartCoroutine(EndSequence());
        }
    }

    IEnumerator EndSequence()
    {
        // Stop hunter movement
        if (hunter != null)
        {
            hunter.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
        }
        
        // Get main camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
            yield break;
        }

        // Fade to black and back, with teleport in between
        yield return StartCoroutine(FadeAndTeleport(mainCamera, 1f, 1f));

        yield return new WaitForSeconds(1.0f);

        // Trigger dialogue
        if (dialogue != null)
        {
            dialogue.TriggerDialogue();
        }

        yield return new WaitForSeconds(1.0f);

        // Play gunshot
        if (gunShotClip != null)
        {
            AudioSource.PlayClipAtPoint(gunShotClip, player.transform.position);
        }
        
    }

    /// <summary>
    /// Fade to black, teleport player, then fade back in, finally delete canvas
    /// </summary>
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

        // --- Delete canvas ---
        Destroy(fadeOverlay);
    }
    
    public void LoadMainMenu()
    {
        System.Diagnostics.Process.Start(Application.dataPath.Replace("_Data", ".exe"));
        Application.Quit();
    }
}