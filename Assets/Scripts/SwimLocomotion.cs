using UnityEngine;
using UnityEngine.InputSystem;

public class SwimLocomotion : MonoBehaviour
{
    [Header("Movement Settings")]
    public float swimSpeed = 3f;
    public float verticalBoost = 1.5f;

    [Header("Input")]
    public InputActionReference moveAction;    // Left joystick (forward/back, strafe)
    public InputActionReference swimUpAction;  // Optional: dedicated "swim up" button (e.g. right grip)
    public InputActionReference swimDownAction; // Optional: dedicated "swim down" button (e.g. left grip)

    [Header("References")]
    public Transform headTransform; // The VR Camera / head

    private CharacterController characterController;

    // This flag lets SceneChanger turn swimming on/off
    [Header("State")]
    public bool isActive = false;

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
    }

    void Update()
    {
        if (!isActive || characterController == null || headTransform == null) return;

        // Read joystick input (left stick: x = strafe, y = forward/back)
        Vector2 input = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;

        // Get the head's forward and right directions (full 3D, including pitch)
        // This means looking up + pushing forward = swim upward
        Vector3 headForward = headTransform.forward;
        Vector3 headRight = headTransform.right;

        // Build movement direction from joystick + head orientation
        Vector3 moveDirection = (headForward * input.y) + (headRight * input.x);

        // Optional dedicated up/down buttons for finer control
        float upInput = swimUpAction != null ? swimUpAction.action.ReadValue<float>() : 0f;
        float downInput = swimDownAction != null ? swimDownAction.action.ReadValue<float>() : 0f;
        moveDirection += Vector3.up * (upInput - downInput) * verticalBoost;

        // Apply movement (no gravity!)
        characterController.Move(moveDirection * swimSpeed * Time.deltaTime);
    }
}
