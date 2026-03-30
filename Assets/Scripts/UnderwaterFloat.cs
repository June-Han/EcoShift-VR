using UnityEngine;

public class UnderwaterFloat : MonoBehaviour
{
    [Header("Float Settings")]
    [Tooltip("How fast the object bobs up and down.")]
    public float floatSpeed = 0.5f; 
    [Tooltip("How high and low the object travels.")]
    public float floatAmount = 0.2f; 
    [Tooltip("How fast the object gently spins.")]
    public float rotationSpeed = 10f; 

    private float startY;

    void Start()
    {
        // Record the starting height
        startY = transform.position.y;
    }

    void Update()
    {
        // Calculate new Y position using a Sine wave for smooth bobbing
        float newY = startY + (Mathf.Sin(Time.time * floatSpeed) * floatAmount);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Add a gentle rotation so it looks like it's drifting
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        transform.Rotate(Vector3.right, (rotationSpeed * 0.5f) * Time.deltaTime, Space.Self);
    }
}