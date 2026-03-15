using UnityEngine;

public class FloatingArrow : MonoBehaviour
{
    [Header("Floating Arrow Config")]
    public float rotationSpeed = 100f; 
    public float floatAmplitude = 0.5f; 
    public float floatFrequency = 1f;  
    public bool isActive = false;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        gameObject.SetActive(isActive);
    }

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        
        Vector3 tempPos = startPos;
        tempPos.y += Mathf.Sin(Time.fixedTime * Mathf.PI * floatFrequency) * floatAmplitude;
        
        transform.position = tempPos;
    }
    
    public void ActiveArrow()
    {
        isActive = true;
        gameObject.SetActive(isActive);
    }

    public void DeactiveArrow()
    {
        isActive = false;
        gameObject.SetActive(isActive);
    }
}