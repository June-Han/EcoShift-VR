using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GrassScatter : MonoBehaviour
{
    [Header("Grass Prefabs")]
    public GameObject grassPrefab1;
    public GameObject grassPrefab2;

    [Header("Scatter Settings")]
    public int density = 500;
    public float groundWidth = 40f;
    public float groundDepth = 60f;
    public float raycastHeight = 10f;

    [Header("Ground Layer")]
    [Tooltip("Set this to the layer your ground object is on so grass only spawns on the ground.")]
    public LayerMask groundLayer = ~0; // defaults to Everything

    [Header("Scale Variance")]
    public float minScale = 0.8f;
    public float maxScale = 1.2f;

    [HideInInspector]
    public GameObject scatterParent;

    public void Scatter()
    {
        // Safety check — prevent scattering if attached directly to the ground mesh
        if (GetComponent<MeshFilter>() != null || GetComponent<MeshRenderer>() != null)
        {
            Debug.LogError("GrassScatter: Do NOT attach this to your ground object! " +
                           "Create a separate empty GameObject and attach it there instead.");
            return;
        }

        // Clean up previous scatter
        if (scatterParent != null)
        {
            DestroyImmediate(scatterParent);
        }

        // Validate prefabs
        if (grassPrefab1 == null && grassPrefab2 == null)
        {
            Debug.LogError("GrassScatter: Please assign at least one grass prefab.");
            return;
        }

        // Create a parent object to keep the hierarchy clean
        scatterParent = new GameObject("Scattered Grass");
        scatterParent.transform.SetParent(transform);
        scatterParent.transform.localPosition = Vector3.zero;

        Vector3 center = transform.position;
        int placed = 0;

        for (int i = 0; i < density; i++)
        {
            // Random position within the ground bounds
            float randomX = Random.Range(-groundWidth / 2f, groundWidth / 2f);
            float randomZ = Random.Range(-groundDepth / 2f, groundDepth / 2f);
            Vector3 rayOrigin = new Vector3(center.x + randomX, center.y + raycastHeight, center.z + randomZ);

            // Raycast downward — only hits the selected ground layer
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayer))
            {
                // Pick a random grass prefab
                GameObject prefabToPlace = PickRandomPrefab();
                if (prefabToPlace == null) continue;

                // Instantiate at hit point
                #if UNITY_EDITOR
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabToPlace, scatterParent.transform);
                #else
                GameObject instance = Instantiate(prefabToPlace, hit.point, Quaternion.identity, scatterParent.transform);
                #endif
                instance.transform.position = hit.point;

                // Random Y rotation
                instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                // Random uniform scale
                float scale = Random.Range(minScale, maxScale);
                instance.transform.localScale = Vector3.one * scale;

                placed++;
            }
        }

        Debug.Log($"GrassScatter: Placed {placed} grass objects out of {density} attempts.");
    }

    public void Clear()
    {
        if (scatterParent != null)
        {
            DestroyImmediate(scatterParent);
            scatterParent = null;
            Debug.Log("GrassScatter: Cleared all scattered grass.");
        }
    }

    private GameObject PickRandomPrefab()
    {
        List<GameObject> available = new List<GameObject>();
        if (grassPrefab1 != null) available.Add(grassPrefab1);
        if (grassPrefab2 != null) available.Add(grassPrefab2);
        if (available.Count == 0) return null;
        return available[Random.Range(0, available.Count)];
    }
}


// -------------------------------------------------------
// Custom Editor — draws the Scatter and Clear buttons
// -------------------------------------------------------
#if UNITY_EDITOR
[CustomEditor(typeof(GrassScatter))]
public class GrassScatterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GrassScatter scatterScript = (GrassScatter)target;

        EditorGUILayout.Space(10);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("🌿  SCATTER GRASS", GUILayout.Height(40)))
        {
            Undo.RegisterFullObjectHierarchyUndo(scatterScript.gameObject, "Scatter Grass");
            scatterScript.Scatter();
        }

        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
        if (GUILayout.Button("🗑  CLEAR GRASS", GUILayout.Height(30)))
        {
            Undo.RegisterFullObjectHierarchyUndo(scatterScript.gameObject, "Clear Grass");
            scatterScript.Clear();
        }

        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);

        // Warning if accidentally attached to a mesh object
        if (scatterScript.GetComponent<MeshFilter>() != null || scatterScript.GetComponent<MeshRenderer>() != null)
        {
            EditorGUILayout.HelpBox(
                "⚠️ This component is on a mesh object! Move it to a separate empty GameObject to avoid deleting your ground.",
                MessageType.Error
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Set Ground Layer to your ground's layer so grass only spawns on the ground, not on rocks, trees, etc.",
                MessageType.Info
            );
        }
    }
}
#endif