using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages a row of heart icons on the right fin to display remaining lives.
/// Hearts swap between a full sprite and an empty sprite when a life is lost.
/// Supports both manually assigned sprites and procedural fallback.
/// </summary>
public class LivesUI : MonoBehaviour
{
    [Header("Heart References (auto-created if empty)")]
    [Tooltip("Assign manually or leave empty for auto-creation. Index 0 = leftmost heart.")]
    public Image[] heartImages;

    [Header("Heart Sprites")]
    [Tooltip("Sprite shown for remaining lives (red/full heart).")]
    public Sprite fullHeartSprite;
    [Tooltip("Sprite shown for lost lives (empty/outline heart).")]
    public Sprite emptyHeartSprite;

    [Header("Settings")]
    public int totalHearts = 3;

    private int currentLives;
    private static Sprite proceduralSprite;  // fallback if no sprites assigned

    void Start()
    {
        currentLives = totalHearts;

        // Auto-create hearts if none were assigned in the Inspector
        if (heartImages == null || heartImages.Length == 0)
        {
            CreateHearts();
        }

        // Ensure all hearts start as full
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] != null)
            {
                if (fullHeartSprite != null)
                    heartImages[i].sprite = fullHeartSprite;
                heartImages[i].color = Color.white;
            }
        }
    }

    /// <summary>
    /// Called when the player loses a life. Swaps the rightmost alive heart to the empty sprite.
    /// </summary>
    public void LoseHeart()
    {
        if (currentLives <= 0) return;

        currentLives--;

        if (currentLives < heartImages.Length && heartImages[currentLives] != null)
        {
            if (emptyHeartSprite != null)
                heartImages[currentLives].sprite = emptyHeartSprite;
            else
                heartImages[currentLives].color = new Color(0.4f, 0.4f, 0.4f, 1f); // grey fallback
        }
    }

    /// <summary>
    /// Reset all hearts to full (e.g. on scene restart).
    /// </summary>
    public void ResetHearts()
    {
        currentLives = totalHearts;
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] != null)
            {
                if (fullHeartSprite != null)
                    heartImages[i].sprite = fullHeartSprite;
                heartImages[i].color = Color.white;
            }
        }
    }

    /// <summary>
    /// Creates heart Image children arranged in a tight horizontal row,
    /// anchored to the right side of the canvas so they sit on the fin
    /// without blocking the player's forward view.
    /// </summary>
    private void CreateHearts()
    {
        // Use assigned full sprite, or fall back to procedural generation
        Sprite spriteToUse = fullHeartSprite;
        if (spriteToUse == null)
        {
            if (proceduralSprite == null)
                proceduralSprite = GenerateHeartSprite(64);
            spriteToUse = proceduralSprite;
        }

        heartImages = new Image[totalHearts];

        float heartSize = 80f;     // small enough not to obstruct view
        float heartGap  = 10f;     // tight spacing between hearts
        float totalWidth = (totalHearts * heartSize) + ((totalHearts - 1) * heartGap);

        // Anchor the row to the right side of the canvas
        float rightEdge = 350f;
        float startX = rightEdge - totalWidth;

        for (int i = 0; i < totalHearts; i++)
        {
            GameObject heartGO = new GameObject($"Heart_{i}");
            heartGO.transform.SetParent(transform, false);
            heartGO.layer = gameObject.layer;

            RectTransform rect = heartGO.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(startX + (i * (heartSize + heartGap)) + (heartSize / 2f), 0f);
            rect.sizeDelta = new Vector2(heartSize, heartSize);

            Image img = heartGO.AddComponent<Image>();
            img.sprite = spriteToUse;
            img.color = Color.white;
            img.raycastTarget = false;
            img.preserveAspect = true;

            heartImages[i] = img;
        }
    }

    /// <summary>
    /// Procedurally generates a heart-shaped sprite at runtime.
    /// Uses the implicit heart equation: (x² + y² - 1)³ - x²·y³ ≤ 0
    /// </summary>
    private static Sprite GenerateHeartSprite(int resolution)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        Color32 white  = new Color32(255, 255, 255, 255);
        Color32 clear  = new Color32(0, 0, 0, 0);

        for (int py = 0; py < resolution; py++)
        {
            for (int px = 0; px < resolution; px++)
            {
                // Map pixel to [-1.3, 1.3] range, with Y flipped so heart points down
                float x = (px / (float)(resolution - 1)) * 2.6f - 1.3f;
                float y = (py / (float)(resolution - 1)) * 2.6f - 1.3f;

                // Shift Y so the heart is centered vertically
                y -= 0.2f;

                // Heart implicit equation: (x² + y² - 1)³ - x²·y³ ≤ 0
                float x2 = x * x;
                float y2 = y * y;
                float a  = x2 + y2 - 1f;
                float value = a * a * a - x2 * y2 * y;

                // Antialiased edge
                float edge = resolution * 0.015f; // softness
                float alpha = Mathf.Clamp01(-value / (edge * 0.1f));

                if (alpha > 0.01f)
                    tex.SetPixel(px, py, new Color(1f, 1f, 1f, alpha));
                else
                    tex.SetPixel(px, py, clear);
            }
        }

        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
                             new Vector2(0.5f, 0.5f), 100f);
    }
}
