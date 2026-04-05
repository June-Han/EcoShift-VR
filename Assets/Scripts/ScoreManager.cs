using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Score Settings")]
    public int maxScore = 10;
    private int currentScore;
    private bool isGameOver;

    /// <summary>
    /// Fired immediately after the score changes.
    /// Parameter is the new score value.
    /// </summary>
    public event System.Action<int> onScoreChanged;

    [Header("UI")]
    public TextMeshProUGUI scoreText;

    [Header("Dialogues")]
    public DialogueTrigger gameCompleteDialogue;

    [Tooltip("Auto-created if not assigned. Shown when score drops below 0.")]
    public DialogueTrigger gameOverDialogue;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentScore = 0;
        isGameOver = false;
        // Force fresh UI binding on every Start (including after scene restart)
        scoreText = null;
        TryBindScoreText();
        UpdateScoreUI();
        EnsureGameOverDialogue();
        Debug.Log($"ScoreManager: Start() — score reset to {currentScore}, UI text: {(scoreText != null ? scoreText.text : "NOT BOUND")}");

        // The score UI may not be active yet at Start (HandSwapper activates it later).
        // Retry binding after a short delay to catch late-activated UI.
        StartCoroutine(DelayedUIBind());
    }

    private IEnumerator DelayedUIBind()
    {
        yield return new WaitForSeconds(0.5f);
        if (scoreText == null)
        {
            TryBindScoreText();
            if (scoreText != null)
            {
                Debug.Log("ScoreManager: Late-bound score text after delay.");
                UpdateScoreUI();
            }
        }
    }

    /// <summary>
    /// Attempts to find the score TextMeshProUGUI if not already bound.
    /// </summary>
    private void TryBindScoreText()
    {
        if (scoreText != null) return;

        GameObject scoreUIObject = GameObject.FindGameObjectWithTag("Score");
        if (scoreUIObject != null)
        {
            scoreText = scoreUIObject.GetComponent<TextMeshProUGUI>();
            if (scoreText != null)
            {
                Debug.Log("ScoreManager: Bound to score text via 'Score' tag.");
                return;
            }
        }

        HandSwapper handSwapper = FindFirstObjectByType<HandSwapper>();
        if (handSwapper != null && handSwapper.scoreUIPrefab != null)
        {
            scoreText = handSwapper.scoreUIPrefab.GetComponentInChildren<TextMeshProUGUI>(true);
            if (scoreText != null)
            {
                Debug.Log("ScoreManager: Bound to score text via HandSwapper.scoreUIPrefab.");
                return;
            }
        }

        Debug.LogWarning("ScoreManager: Could not find score text by tag or HandSwapper.");
    }

    /// <summary>
    /// Creates a GameOverDialogue DialogueTrigger at runtime if one isn't assigned.
    /// Mirrors the pattern used in SharkThreatStatus for end-game dialogues.
    /// </summary>
    private void EnsureGameOverDialogue()
    {
        if (gameOverDialogue != null) return;

        // Find it the same way other dialogues are found in this project
        GameObject dialogueObj = GameObject.Find("Dialogues/GameOverDialogue");
        if (dialogueObj == null) dialogueObj = GameObject.Find("GameOverDialogue");
        if (dialogueObj != null)
        {
            gameOverDialogue = dialogueObj.GetComponent<DialogueTrigger>();
            if (gameOverDialogue != null)
            {
                Debug.Log("ScoreManager: Found GameOverDialogue in scene.");
                return;
            }
        }

        Debug.LogWarning("ScoreManager: GameOverDialogue not found in scene at Start. Will retry when needed.");
    }

    public void AddScore()
    {
        if (isGameOver) return;

        currentScore = Mathf.Min(currentScore + 1, maxScore);
        UpdateScoreUI();
        onScoreChanged?.Invoke(currentScore);
        if (currentScore >= maxScore) OnGameComplete();
    }

    public void RemoveScore(int amount)
    {
        if (isGameOver) return;

        Debug.Log($"ScoreManager: RemoveScore({amount}) called. Score before: {currentScore}");
        currentScore -= amount;
        UpdateScoreUI();
        onScoreChanged?.Invoke(currentScore);
        Debug.Log($"ScoreManager: Score after RemoveScore: {currentScore}");

        if (currentScore < 0)
        {
            OnGameOver();
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText == null)
            TryBindScoreText();

        if (scoreText != null)
        {
            scoreText.text = $"{currentScore}/{maxScore}";
        }
        else
        {
            Debug.LogWarning("ScoreManager: Tried to update score, but scoreText is missing!");
        }
    }

    public int GetPlayerScore()
    {
        return currentScore;
    }

    void OnGameComplete()
    {
        if (gameCompleteDialogue != null)
            gameCompleteDialogue.TriggerDialogue();
        Debug.Log("Game Complete!");
    }

    void OnGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log($"ScoreManager: GAME OVER — score dropped to {currentScore}");

        // Retry finding the dialogue if it wasn't found at Start
        if (gameOverDialogue == null)
            EnsureGameOverDialogue();

        // Show the game-over dialogue, then restart the scene when it finishes.
        // Call DialogueManager.ShowDialogue directly with a callback — this is
        // the same approach DialogueTrigger uses internally and guarantees the
        // restart fires when the player finishes reading.
        DialogueManager dm = DialogueManager.Instance;
        if (dm == null) dm = FindFirstObjectByType<DialogueManager>();

        if (dm != null && gameOverDialogue != null && gameOverDialogue.sentences != null)
        {
            Debug.Log("ScoreManager: Showing game-over dialogue with restart callback.");
            dm.ShowDialogue(gameOverDialogue.sentences, () =>
            {
                Debug.Log("ScoreManager: Game-over dialogue finished — restarting.");
                RestartScene();
            });
        }
        else if (dm != null)
        {
            // Fallback sentences if no GameOverDialogue was found
            Debug.Log("ScoreManager: Using fallback game-over sentences.");
            dm.ShowDialogue(new string[]
            {
                "Oh no! You ate too much pollution and got sick...",
                "Press A to try again!"
            }, () =>
            {
                RestartScene();
            });
        }
        else
        {
            Debug.LogWarning("ScoreManager: No DialogueManager found — restarting scene directly.");
            RestartScene();
        }
    }

    /// <summary>
    /// Reloads the current scene to restart gameplay.
    /// Resets the singleton so the new ScoreManager can take over.
    /// </summary>
    public void RestartScene()
    {
        Debug.Log("ScoreManager: Restarting scene...");

        // Reset score and game-over state immediately
        currentScore = 0;
        isGameOver = false;

        // Update the UI now so the score shows 0 even if Start() doesn't re-run
        // (this ScoreManager may be on a persistent object that survives scene reload)
        UpdateScoreUI();

        // Keep Instance pointing to us — we survive the reload
        Instance = this;

        // Reload the current scene
        string sceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
