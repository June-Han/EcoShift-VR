using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    
    [Header("Score Settings")]
    public int maxScore = 10;
    private int currentScore;

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
        if (scoreText == null)
        {
            // Auto-bind to TextMeshProUGUI with "Score" tag
            GameObject scoreUIObject = GameObject.FindGameObjectWithTag("Score");
            if (scoreUIObject != null)
            {
                scoreText = scoreUIObject.GetComponent<TextMeshProUGUI>();
                if (scoreText == null)
                {
                    Debug.LogError("ScoreManager: Found object with 'Score' tag but it doesn't have TextMeshProUGUI component!");
                }
            }
            else
            {
                Debug.LogError("ScoreManager: No GameObject found with 'Score' tag!");
            }
        }
        UpdateScoreUI();
    }

    public void AddScore()
    {
        currentScore = Mathf.Min(currentScore + 1, maxScore);
        UpdateScoreUI();
        if (currentScore >= maxScore) OnGameComplete();
    }

    void UpdateScoreUI() => scoreText.text = $"{currentScore}/{maxScore}";

    void OnGameComplete()
    {
        if (gameCompleteDialogue != null)
            gameCompleteDialogue.TriggerDialogue();
        Debug.Log("Game Complete!");
        // trigger win state here
    }
}