using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("Music Config")]
    public AudioSource audioSource;
    public AudioClip[] footstepSounds; // Plug in multiple footstep sounds for variety

    [Header("Footstep Timing")]
    public float walkStepInterval = 0.5f; // seconds between footsteps when walking
    private float stepTimer;

    private CharacterController characterController;
    
    [Header("Input Reference")]
    public InputActionReference moveAction; 

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (moveAction != null)
            moveAction.action.Enable();
    }

    void Update()
    {
        Vector2 moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        bool isMoving = moveInput.magnitude > 0.1f;

        if (isMoving)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0)
            {
                PlayFootstep();
                stepTimer = walkStepInterval;
            }
        }
        else
        {
            stepTimer = walkStepInterval;
        }
    }

    void PlayFootstep()
    {
        if (footstepSounds.Length == 0) return;
        
        int index = Random.Range(0, footstepSounds.Length);
        AudioClip clip = footstepSounds[index];
        
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(clip);
    }
}