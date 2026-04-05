using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VRFader : MonoBehaviour
{
    public float fadeDuration = 1.0f;
    private Image fadeImage;

    void Start()
    {
        // Automatically builds the black canvas right in front of the camera
        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = new Vector3(0, 0, 0.5f);
        canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0); // Start completely transparent
        fadeImage.rectTransform.sizeDelta = new Vector2(2000, 2000); 

        // Automatically fade in when the scene starts
        StartCoroutine(FadeRoutine(1, 0));
    }


    public void FadeOutToNewScene()
    {
        StartCoroutine(FadeRoutine(0, 1));
    }

    IEnumerator FadeRoutine(float alphaIn, float alphaOut)
    {
        float timer = 0;
        while (timer <= fadeDuration)
        {
            Color newColor = fadeImage.color;
            newColor.a = Mathf.Lerp(alphaIn, alphaOut, timer / fadeDuration);
            fadeImage.color = newColor;
            
            timer += Time.deltaTime;
            yield return null;
        }
    }
}
