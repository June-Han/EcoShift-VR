using UnityEngine;
using UnityEngine.InputSystem;

public class SwimLocomotion : MonoBehaviour
{
    [Header("Movement Settings")]
    public float swimSpeed = 3f;
    public float sprintMultiplier = 1.8f;
    public float verticalBoost = 1.5f;

    [Header("Water Physics")]
    [Tooltip("How quickly the player builds up to full speed (like pushing through water)")]
    public float acceleration = 2.5f;
    [Tooltip("How quickly the player glides to a stop (water drag)")]
    public float drag = 1.8f;
    [Tooltip("Minimum speed before snapping to zero (prevents endless micro-drift)")]
    public float stopThreshold = 0.01f;

    [Header("Swimming Feel")]
    [Tooltip("Subtle speed oscillation that mimics a swim stroke rhythm")]
    public float strokePulseStrength = 0.15f;
    public float strokePulseSpeed = 3.5f;
    [Tooltip("How much the player banks/tilts into turns")]
    public float bankAmount = 2f;
    public float bankSmoothing = 3f;
    [Tooltip("Gentle speed-based head bob to feel the water rushing")]
    public float moveBobAmount = 0.008f;
    public float moveBobSpeed = 6f;

    [Header("Audio - Underwater Ambient")]
    [Tooltip("Looping underwater ambient sound (drag in ambientsound_underwater.wav)")]
    public AudioClip underwaterAmbientClip;
    [Range(0f, 1f)]
    public float ambientVolume = 0.3f;

    [Header("Input")]
    public InputActionReference moveAction;    // Left joystick (forward/back, strafe)
    public InputActionReference swimUpAction;  // Optional: dedicated "swim up" button (e.g. right grip)
    public InputActionReference swimDownAction; // Optional: dedicated "swim down" button (e.g. left grip)
    public InputActionReference sprintAction;  // Optional: sprint button

    [Header("References")]
    public Transform headTransform; // The VR Camera / head

    private CharacterController characterController;

    // Max Water Height of the environment
    [Header("Water Settings")]
    [Tooltip("The highest Y-coordinate the player's head can go.")]
    public float maxWaterHeight = 30f; 

    // This flag lets SceneChanger turn swimming on/off
    [Header("State")]
    public bool isActive = false;

    // Internal velocity for momentum system
    private Vector3 currentVelocity;
    private float currentBank;

    // Audio
    private AudioSource ambientSource;

    // Freeze movement during cinematics without killing audio! ---
    public bool canMove = true;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Try to auto-find the head if not assigned
        if (headTransform == null)
        {
            Camera mainCam = GetComponentInChildren<Camera>();
            if (mainCam != null) headTransform = mainCam.transform;
        }

        // Enable input actions
        if (moveAction != null) moveAction.action.Enable();
        if (swimUpAction != null) swimUpAction.action.Enable();
        if (swimDownAction != null) swimDownAction.action.Enable();
        if (sprintAction != null) sprintAction.action.Enable();

        // Set up audio sources
        SetupAudio();
    }

    void SetupAudio()
    {
        // Ambient underwater loop (constant background)
        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.clip = underwaterAmbientClip;
        ambientSource.loop = true;
        ambientSource.volume = 0f; // Start silent, fade in when active
        ambientSource.spatialBlend = 0f; // 2D sound (surrounds the player)
        ambientSource.playOnAwake = false;

    }

    void Update()
    {
        UpdateAudio();

        if (!isActive || characterController == null || headTransform == null) return;

        float dt = Time.deltaTime;

        // --- INPUT ---
        // Vector2 input = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        // float upInput = swimUpAction != null ? swimUpAction.action.ReadValue<float>() : 0f;
        // float downInput = swimDownAction != null ? swimDownAction.action.ReadValue<float>() : 0f;
        // float sprintInput = sprintAction != null ? sprintAction.action.ReadValue<float>() : 0f;

        // --- INPUT ---
        Vector2 input = Vector2.zero;
        float upInput = 0f;
        float downInput = 0f;
        float sprintInput = 0f;

        // ONLY read the controllers if the player is allowed to move!
        // If false, they all remain 0, and the water drag will smoothly bring the shark to a halt.
        if (canMove)
        {
            input = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
            upInput = swimUpAction != null ? swimUpAction.action.ReadValue<float>() : 0f;
            downInput = swimDownAction != null ? swimDownAction.action.ReadValue<float>() : 0f;
            sprintInput = sprintAction != null ? sprintAction.action.ReadValue<float>() : 0f;
        }

        // --- DESIRED DIRECTION ---
        Vector3 headForward = headTransform.forward;
        Vector3 headRight = headTransform.right;

        Vector3 desiredDirection = (headForward * input.y) + (headRight * input.x);
        desiredDirection += Vector3.up * (upInput - downInput) * verticalBoost;

        // Sprint modifier
        float speedMult = 1f + (sprintInput * (sprintMultiplier - 1f));

        Vector3 targetVelocity = desiredDirection * swimSpeed * speedMult;

        // --- STROKE PULSE ---
        if (input.magnitude > 0.1f)
        {
            float pulse = 1f + Mathf.Sin(Time.time * strokePulseSpeed) * strokePulseStrength;
            targetVelocity *= pulse;
        }

        // --- MOMENTUM & DRAG ---
        if (targetVelocity.magnitude > stopThreshold)
        {
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, acceleration * dt);
        }
        else
        {
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, drag * dt);
            if (currentVelocity.magnitude < stopThreshold)
                currentVelocity = Vector3.zero;
        }

        // --- THE MOMENTUM KILLER ---
        // If the player is touching the surface, do not let upward momentum build up!
        if (headTransform.position.y >= maxWaterHeight && currentVelocity.y > 0f)
        {
            currentVelocity.y = 0f; 
        }

        // --- APPLY MOVEMENT ---
        characterController.Move(currentVelocity * dt);

        // --- BANKING ---
        float targetBank = -input.x * bankAmount;
        currentBank = Mathf.Lerp(currentBank, targetBank, bankSmoothing * dt);

        // --- SPEED-BASED HEAD BOB ---
        float speedRatio = currentVelocity.magnitude / (swimSpeed * sprintMultiplier);
        float moveBob = Mathf.Sin(Time.time * moveBobSpeed) * moveBobAmount * speedRatio;

        if (headTransform.parent != null)
        {
            Transform cameraOffset = headTransform.parent;
            Vector3 localPos = cameraOffset.localPosition;
            cameraOffset.localPosition = new Vector3(localPos.x, localPos.y + moveBob, localPos.z);

            Quaternion bankRotation = Quaternion.Euler(0f, 0f, currentBank);
            cameraOffset.localRotation = Quaternion.Slerp(cameraOffset.localRotation, bankRotation, bankSmoothing * dt);
        }
    }

    void UpdateAudio()
    {
        if (ambientSource == null) return;

        float dt = Time.deltaTime;
        float maxSpeed = swimSpeed * sprintMultiplier;
        float speedRatio = (maxSpeed > 0f) ? currentVelocity.magnitude / maxSpeed : 0f;

        // --- AMBIENT UNDERWATER LOOP ---
        if (isActive)
        {
            // Fade in
            if (!ambientSource.isPlaying && underwaterAmbientClip != null)
                ambientSource.Play();

            ambientSource.volume = Mathf.Lerp(ambientSource.volume, ambientVolume, 2f * dt);

            // Subtle pitch shift based on speed (sounds more dynamic when moving)
            ambientSource.pitch = Mathf.Lerp(0.95f, 1.05f, speedRatio);
        }
        else
        {
            // Fade out
            ambientSource.volume = Mathf.Lerp(ambientSource.volume, 0f, 3f * dt);
            if (ambientSource.volume < 0.01f && ambientSource.isPlaying)
                ambientSource.Stop();
        }

    }

    void OnDisable()
    {
        currentVelocity = Vector3.zero;
        currentBank = 0f;

        // Stop audio gracefully
        if (ambientSource != null && ambientSource.isPlaying)
            ambientSource.Stop();
    }
}
