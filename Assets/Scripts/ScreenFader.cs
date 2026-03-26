using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    private Image fadeImage; 

    void Awake()
    {
        fadeImage = GetComponentInChildren<Image>();
    }

    public void StartFadeToBlack(float duration)
    {
        StopCoroutine("FadeToBlackCoroutine");
        StartCoroutine(FadeToBlackCoroutine(duration));
    }

    private IEnumerator FadeToBlackCoroutine(float duration)
    {
        float timer = 0f;
        Color color = fadeImage.color;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, timer / duration);
            color.a = alpha;
            fadeImage.color = color;
            yield return null; 
        }
        
        color.a = 1f;
        fadeImage.color = color;

    }
}