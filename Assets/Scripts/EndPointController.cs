using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class EndPointController : MonoBehaviour
{
    [Header("Settings")]
    public ScreenFader screenFader;     
    public AudioClip gunShotClip;        
    public DialogueTrigger dialogue;
    public HunterPatrol hunter;          

    private bool hasReached = false;     

    private void OnTriggerEnter(Collider other)
    {
        if (!hasReached && other.CompareTag("Player"))
        {
            hasReached = true;
            StartCoroutine(EndSequence());
        }
    }

    IEnumerator EndSequence()
    {
        // Stop hunter movement
        if (hunter != null)
        {
            hunter.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
        }

        // Fade to black (2 seconds)
        if (screenFader != null)
        {
            screenFader.StartFadeToBlack(1.0f);
        }

        yield return new WaitForSeconds(1.0f);

        // Trigger dialogue
        if (dialogue != null)
        {
            dialogue.TriggerDialogue();
        }

        yield return new WaitForSeconds(2.0f);

        // Play gunshot
        if (gunShotClip != null)
        {
            AudioSource.PlayClipAtPoint(gunShotClip, transform.position);
        }
        
    }
    
    public void LoadMainMenu()
    {
        // Load Main_Scene with single mode (unload all current objects)
        SceneManager.LoadScene("Main_Scene", LoadSceneMode.Single);
    }
}