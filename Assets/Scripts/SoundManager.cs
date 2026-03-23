using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Ambient Sound")]
    [Tooltip("The AudioSource on the sound-emitting object in the scene.")]
    public AudioSource ambientSource;

    [Tooltip("Fade out duration in seconds when stopping ambient audio.")]
    public float fadeOutDuration = 1.0f;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PlayAmbient()
    {
        if (ambientSource != null && !ambientSource.isPlaying)
            ambientSource.Play();
    }

    public void StopAmbient(bool fade = true)
    {
        if (ambientSource == null) return;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        if (fade)
            fadeCoroutine = StartCoroutine(FadeOut(ambientSource, fadeOutDuration));
        else
            ambientSource.Stop();
    }

    private System.Collections.IEnumerator FadeOut(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        source.Stop();
        source.volume = startVolume; // restore for potential replay
    }

    // Call this between dialogue sequences to swap to a different clip
    public void SwapClip(AudioClip newClip, bool playImmediately = true)
    {
        if (ambientSource == null) return;
        ambientSource.Stop();
        ambientSource.clip = newClip;
        if (playImmediately) ambientSource.Play();
    }
}