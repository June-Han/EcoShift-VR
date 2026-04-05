using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Score Settings")]
    public int maxScore = 10;
    private int currentScore;

    /// <summary>
    /// Fired immediately after the score changes.
    /// Parameter is the new score value.
    /// </summary>
    public event System.Action<int> onScoreChanged;

    [Header("UI")]
    public TextMeshProUGUI scoreText;

    public DialogueTrigger gameCompleteDialogue;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        TryBindScoreText();
        UpdateScoreUI();
    }

    /// <summary>
    /// Attempts to find the score TextMeshProUGUI if not already bound.
    /// Tries multiple strategies:
    ///   1. Inspector reference (already assigned)
    ///   2. GameObject tagged "Score"
    ///   3. HandSwapper.scoreUIPrefab's child TextMeshProUGUI
    ///      (covers the case where the text object isn't tagged)
    /// </summary>
    private void TryBindScoreText()
    {
        if (scoreText != null) return;

        // Strategy 1: Find by tag
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

        // Strategy 2: Find via HandSwapper's scoreUIPrefab
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

        // Strategy 3: Last resort — find any active TextMeshProUGUI whose text looks like a score
        Debug.LogWarning("ScoreManager: Could not find score text by tag or HandSwapper. Score UI will not update until a tagged 'Score' object is available.");
    }

    public void AddScore()
    {
        currentScore = Mathf.Min(currentScore + 1, maxScore);
        UpdateScoreUI();
        onScoreChanged?.Invoke(currentScore);
        if (currentScore >= maxScore) OnGameComplete();
    }

    public void RemoveScore(int amount)
    {
        Debug.Log($"ScoreManager: RemoveScore({amount}) called. Score before: {currentScore}");
        currentScore = Mathf.Max(currentScore - amount, 0);
        UpdateScoreUI();
        onScoreChanged?.Invoke(currentScore);
        Debug.Log($"ScoreManager: Score after RemoveScore: {currentScore}");
    }

    void UpdateScoreUI()
    {
        // Lazy re-bind: the score UI prefab may have been activated after Start()
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
        // trigger win state here
    }
}
