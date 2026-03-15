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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start() => UpdateScoreUI();

    public void AddScore()
    {
        currentScore = Mathf.Min(currentScore + 1, maxScore);
        UpdateScoreUI();
        if (currentScore >= maxScore) OnGameComplete();
    }

    void UpdateScoreUI() => scoreText.text = $"{currentScore}/{maxScore}";

    void OnGameComplete()
    {
        Debug.Log("Game Complete!");
        // trigger win state here
    }
}