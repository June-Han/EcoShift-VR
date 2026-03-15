using UnityEngine;

public class LitterClickable : MonoBehaviour
{
    public void OnClicked()
    {
        DistortionManager.Instance.ApplyDistortion();
        gameObject.SetActive(false);
    }
}