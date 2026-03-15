using UnityEngine;

public class InsectClickable : MonoBehaviour
{
    // For VR: called by your ray interactor's select event
    public void OnClicked()
    {
        ScoreManager.Instance.AddScore();
        gameObject.SetActive(false); // disappear
    }
}