using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Editor utility that creates a "Lives Canvas" prefab with 3 heart Image children
/// and a LivesUI component. The prefab is saved to Assets/Miscel_Assets/.
///
/// BEFORE running: import your heart sprite sheet into Assets/Miscel_Assets/,
/// set Sprite Mode to "Multiple", and slice it into "heart_full" and "heart_empty"
/// using the Sprite Editor (Grid By Cell Count: 1 column, 2 rows).
///
/// Usage: Unity menu → Tools → EcoShift → Create Lives Canvas Prefab
/// </summary>
public class CreateLivesCanvasPrefab : MonoBehaviour
{
    [MenuItem("Tools/EcoShift/Create Lives Canvas Prefab")]
    public static void CreatePrefab()
    {
        string spriteDir = "Assets/Miscel_Assets";

        if (!AssetDatabase.IsValidFolder(spriteDir))
            AssetDatabase.CreateFolder("Assets", "Miscel_Assets");

        // --- Look for the sliced heart sprites ---
        // Search for any sub-sprite named "heart_full" or containing "full"/"empty"
        Sprite fullSprite  = FindSpriteByName("heart_full");
        Sprite emptySprite = FindSpriteByName("heart_empty");

        if (fullSprite == null || emptySprite == null)
        {
            // Fallback: search all sprites in Miscel_Assets for sprites from a 2-row sheet
            string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { spriteDir });
            foreach (string guid in spriteGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (Object obj in subAssets)
                {
                    if (obj is Sprite s)
                    {
                        string lower = s.name.ToLower();
                        if (fullSprite == null && (lower.Contains("full") || lower.EndsWith("_0")))
                            fullSprite = s;
                        if (emptySprite == null && (lower.Contains("empty") || lower.EndsWith("_1")))
                            emptySprite = s;
                    }
                }
            }
        }

        if (fullSprite == null)
        {
            bool proceed = EditorUtility.DisplayDialog("Heart Sprites Not Found",
                "Could not find 'heart_full' and 'heart_empty' sprites.\n\n" +
                "Make sure you:\n" +
                "1. Imported the heart sprite sheet into Assets/Miscel_Assets/\n" +
                "2. Set Sprite Mode to 'Multiple'\n" +
                "3. Sliced it in the Sprite Editor (1 col, 2 rows)\n" +
                "4. Named the sub-sprites 'heart_full' (top) and 'heart_empty' (bottom)\n\n" +
                "Create prefab with procedural fallback instead?",
                "Use Fallback", "Cancel");

            if (!proceed) return;
        }

        // --- Build the Canvas hierarchy ---
        GameObject canvasGO = new GameObject("Lives Canvas");
        canvasGO.layer = 6;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.referencePixelsPerUnit = 100;

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(800f, 300f);
        canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        canvasRect.localPosition = new Vector3(0f, 0.05f, 0.15f);

        // --- Create 3 heart Images ---
        int totalHearts = 3;
        float heartSize = 80f;
        float heartGap  = 10f;
        float totalWidth = (totalHearts * heartSize) + ((totalHearts - 1) * heartGap);
        float rightEdge = 350f;
        float startX = rightEdge - totalWidth;

        Image[] heartImages = new Image[totalHearts];

        for (int i = 0; i < totalHearts; i++)
        {
            GameObject heartGO = new GameObject($"Heart_{i}");
            heartGO.transform.SetParent(canvasGO.transform, false);
            heartGO.layer = 6;

            RectTransform rect = heartGO.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(
                startX + (i * (heartSize + heartGap)) + (heartSize / 2f), 0f);
            rect.sizeDelta = new Vector2(heartSize, heartSize);

            Image img = heartGO.AddComponent<Image>();
            if (fullSprite != null)
                img.sprite = fullSprite;
            img.color = Color.white;
            img.raycastTarget = false;
            img.preserveAspect = true;

            heartImages[i] = img;
        }

        // --- Add LivesUI and wire up references ---
        LivesUI livesUI = canvasGO.AddComponent<LivesUI>();
        livesUI.totalHearts = totalHearts;
        livesUI.heartImages = heartImages;
        livesUI.fullHeartSprite  = fullSprite;
        livesUI.emptyHeartSprite = emptySprite;

        // --- Save as prefab ---
        string prefabPath = spriteDir + "/LivesCanvas.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);
        DestroyImmediate(canvasGO);

        // Select it in the Project window
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        string spriteStatus = fullSprite != null
            ? $"Full sprite: {fullSprite.name}\nEmpty sprite: {(emptySprite != null ? emptySprite.name : "NOT FOUND — will grey out instead")}"
            : "Using procedural fallback (no sprites found)";

        Debug.Log($"<color=green>[EcoShift]</color> Lives Canvas prefab created at: {prefabPath}");
        EditorUtility.DisplayDialog("Lives Canvas Prefab Created",
            $"Prefab saved to:\n{prefabPath}\n\n" +
            $"{spriteStatus}\n\n" +
            "To use it:\n" +
            "1. Drag it into XR Origin → Camera Offset → Right Controller\n" +
            "2. Drag it into HandSwapper's 'Lives UI Prefab' field\n" +
            "3. Reposition the hearts however you like in the Scene view",
            "OK");
    }

    /// <summary>
    /// Searches all sprites in the project for one matching the given name.
    /// </summary>
    private static Sprite FindSpriteByName(string name)
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object obj in subAssets)
            {
                if (obj is Sprite s && s.name == name)
                    return s;
            }
        }
        return null;
    }
}
