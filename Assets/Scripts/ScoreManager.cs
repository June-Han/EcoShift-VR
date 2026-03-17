using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    
    [Header("Score Settings")]
    public int maxScore = 10;
    private int currentScore = 0;

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
        // Finds it by name anywhere in the scene, including on XR Origin
        GameObject scoreUIText = GameObject.Find("ScoreText"); // use your TMP object's name
        if (scoreUIText != null)
            scoreText = scoreUIText.GetComponent<TextMeshProUGUI>();
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