using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// GameOverUI manages the game over screen in VR environment.
/// Creates a full-screen black canvas with a Restart button positioned in front of the player.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [SerializeField] private float canvasDistance = 2f; // Distance from camera
    [SerializeField] private float canvasScale = 0.01f; // Canvas world space scale
    [SerializeField] private float fadeInDuration = 1f; // Duration to fade in the canvas
    
    private Canvas gameOverCanvas;
    private Image fadeImage;
    private Button restartButton;
    private GameObject canvasObject;
    private bool isInitialized = false;

    private void Awake()
    {
        // Prevent multiple instances
        if (FindObjectsByType<GameOverUI>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Initialize and show the game over screen
    /// </summary>
    public void ShowGameOver()
    {
        if (isInitialized) return;

        CreateGameOverCanvas();
        StartCoroutine(FadeInCanvas());
        isInitialized = true;
    }

    /// <summary>
    /// Create the game over canvas with black background and restart button
    /// </summary>
    private void CreateGameOverCanvas()
    {
        // Get the camera to position the canvas
        Transform cameraTransform = Camera.main.transform;
        if (cameraTransform == null)
        {
            Debug.LogError("Main camera not found!");
            return;
        }

        // Create canvas GameObject
        canvasObject = new GameObject("GameOverCanvas");
        canvasObject.transform.SetParent(cameraTransform, false);
        canvasObject.transform.localPosition = new Vector3(0, 0, canvasDistance);
        canvasObject.transform.localScale = new Vector3(canvasScale, canvasScale, canvasScale);
        canvasObject.transform.localRotation = Quaternion.identity;

        // Add Canvas component
        gameOverCanvas = canvasObject.AddComponent<Canvas>();
        gameOverCanvas.renderMode = RenderMode.WorldSpace;
        gameOverCanvas.sortingOrder = 999;

        // Create black background image
        GameObject fadeImageGO = new GameObject("FadeImage");
        fadeImageGO.transform.SetParent(canvasObject.transform, false);
        
        fadeImage = fadeImageGO.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0); // Start transparent
        fadeImage.raycastTarget = false;

        RectTransform fadeRect = fadeImageGO.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;
        fadeRect.sizeDelta = new Vector2(1920, 1080);

        // Create "Game Over" text
        GameObject gameOverTextGO = new GameObject("GameOverText");
        gameOverTextGO.transform.SetParent(canvasObject.transform, false);
        
        TextMeshProUGUI gameOverText = gameOverTextGO.AddComponent<TextMeshProUGUI>();
        gameOverText.text = "GAME OVER";
        gameOverText.fontSize = 72;
        gameOverText.alignment = TextAlignmentOptions.Center;
        gameOverText.color = Color.white;

        RectTransform textRect = gameOverTextGO.GetComponent<RectTransform>();
        textRect.anchoredPosition = new Vector2(0, 200);
        textRect.sizeDelta = new Vector2(1920, 200);

        // Create Restart button
        GameObject buttonGO = new GameObject("RestartButton");
        buttonGO.transform.SetParent(canvasObject.transform, false);

        // Add button background image
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f); // Dark gray

        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchoredPosition = new Vector2(0, -100);
        buttonRect.sizeDelta = new Vector2(400, 120);

        // Add Button component
        restartButton = buttonGO.AddComponent<Button>();
        restartButton.onClick.AddListener(OnRestartClicked);
        
        // Add button text
        GameObject buttonTextGO = new GameObject("Text");
        buttonTextGO.transform.SetParent(buttonGO.transform, false);

        TextMeshProUGUI buttonText = buttonTextGO.AddComponent<TextMeshProUGUI>();
        buttonText.text = "RESTART";
        buttonText.fontSize = 40;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;

        RectTransform buttonTextRect = buttonTextGO.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        // Add color transition for button feedback
        ColorBlock colors = restartButton.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f);
        restartButton.colors = colors;

        Debug.Log("GameOver Canvas created successfully");
    }

    /// <summary>
    /// Fade in the game over canvas
    /// </summary>
    private System.Collections.IEnumerator FadeInCanvas()
    {
        float timer = 0f;
        
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, timer / fadeInDuration);
            
            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
            
            yield return null;
        }

        // Ensure fully opaque
        Color finalColor = fadeImage.color;
        finalColor.a = 1f;
        fadeImage.color = finalColor;
    }

    /// <summary>
    /// Handle restart button click
    /// </summary>
    private void OnRestartClicked()
    {
        Debug.Log("Restart button clicked");
        Time.timeScale = 1f; // Make sure time is running
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Hide the game over screen
    /// </summary>
    public void Hide()
    {
        if (canvasObject != null)
        {
            Destroy(canvasObject);
        }
        isInitialized = false;
    }
}

