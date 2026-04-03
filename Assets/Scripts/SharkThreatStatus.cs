using UnityEngine;

public class SharkThreatStatus : MonoBehaviour
{
    [Header("Player Health")]
    public int maxLives = 3;
    private int currentLives;

    void Start()
    {
        currentLives = maxLives;
    }

    public void GetTangled()
    {
        currentLives--;
        Debug.LogWarning($"Blue Shark TANGLED! Lives remaining: {currentLives}");

        if (DistortionManager.Instance != null)
        {
            DistortionManager.Instance.ApplyDistortion();
        }

        if (currentLives <= 0)
        {
            GameOver();
        }
    }

    private void GameOver()
    {
        Debug.LogError("THE WHALE HAS BEEN CAPTURED! GAME OVER.");
        // TODO: Game Over UI scene!
    }
}