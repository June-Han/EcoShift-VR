using UnityEngine;

public class TrashSpawner : MonoBehaviour
{
    [Header("Trash Assets")]
    [Tooltip("Drag your trash prefabs here. The script will pick randomly from this list.")]
    public GameObject[] trashPrefabs;
    
    [Tooltip("How many pieces of trash to spawn total.")]
    public int spawnCount = 30;

    [Header("Spawn Area")]
    [Tooltip("The size of the box/elliptical volume where trash will spawn (X, Y, Z).")]
    public Vector3 spawnAreaSize = new Vector3(115f, 30f, 190f);

    // [Header("Scale Settings")]
    // [Tooltip("The minimum size of the trash.")]
    // public float minScale = 0.5f;
    // [Tooltip("The maximum size of the trash.")]
    // public float maxScale = 1.5f;

    void Start()
    {
        ScatterTrash();
    }

    void ScatterTrash()
    {
        // Safety check to make sure you assigned your prefabs
        if (trashPrefabs.Length == 0)
        {
            Debug.LogWarning("No trash prefabs assigned to the TrashSpawner!");
            return;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            // Pick a random piece of trash from your array
            GameObject randomPrefab = trashPrefabs[Random.Range(0, trashPrefabs.Length)];

            // // Pick a random location inside our defined box
            // Vector3 randomPos = transform.position + new Vector3(
            //     Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
            //     Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2),
            //     Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
            // );

            // Get a random point inside a perfect 1x1x1 sphere
            Vector3 randomPoint = Random.insideUnitSphere;

            // Stretch that point by half of your custom dimensions (since radius is half the diameter)
            Vector3 randomPos = transform.position + new Vector3(
                randomPoint.x * (spawnAreaSize.x / 2f),
                randomPoint.y * (spawnAreaSize.y / 2f),
                randomPoint.z * (spawnAreaSize.z / 2f)
            );

            // Give it a random starting rotation so they don't look uniform
            Quaternion randomRot = Random.rotation;

            // Spawn the object and make it a "child" of the Spawner to keep the hierarchy clean
            GameObject spawnedTrash = Instantiate(randomPrefab, randomPos, randomRot, transform);

            // // Generate a random number between your min and max scale
            // float randomScale = Random.Range(minScale, maxScale);

            // // Apply that random scale directly to the trash we just spawned
            // spawnedTrash.transform.localScale = Vector3.one * randomScale;
        }
    }

    // This built-in Unity function draws a visual box in the Scene view so shown the spawn area
    private void OnDrawGizmosSelected()
    {
        // Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Semi-transparent green
        // Gizmos.DrawCube(transform.position, spawnAreaSize);
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Semi-transparent green

        // Save the normal world space matrix
        Matrix4x4 oldMatrix = Gizmos.matrix;

        // Tell Unity to scale the drawing space to match your custom X, Y, Z dimensions
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, spawnAreaSize);

        // Draw a sphere with a radius of 0.5 (which gets multiplied by your scale to equal your exact dimensions)
        Gizmos.DrawSphere(Vector3.zero, 0.5f); 

        // Return the drawing space back to normal so we don't mess up other gizmos
        Gizmos.matrix = oldMatrix;
    }
}