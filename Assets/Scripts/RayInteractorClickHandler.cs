using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.InputSystem;


public class RayInteractorClickHandler : MonoBehaviour
{
    [Header("Input Settings")]
    [Tooltip("Bind to 'XRI RightHand Interaction/Activate Value' or similar trigger action")]
    public InputActionReference triggerActionReference;

    [Header("References")]
    [Tooltip("XRRayInteractor component on this controller")]
    private XRRayInteractor rayInteractor;

    private InputAction triggerAction;
    private float triggerThreshold = 0.5f;
    private bool wasTriggerPressed;

    private void OnEnable()
    {
        rayInteractor = GetComponent<XRRayInteractor>();
        
        if (rayInteractor == null)
        {
            Debug.LogError("RayInteractorClickHandler: Cannot find XRRayInteractor component!");
            return;
        }
        
        // Get InputAction from reference
        if (triggerActionReference != null)
        {
            triggerAction = triggerActionReference.action;
            if (triggerAction != null)
            {
                triggerAction.Enable();
                Debug.Log("RayInteractorClickHandler: Trigger action enabled successfully");
            }
            else
            {
                Debug.LogError("RayInteractorClickHandler: Input Action Reference is null!");
            }
        }
        else
        {
            Debug.LogError("RayInteractorClickHandler: Trigger Action Reference not assigned in Inspector!");
        }
    }

    private void OnDisable()
    {
        if (triggerAction != null)
        {
            triggerAction.Disable();
        }
    }

    private void Update()
    {
        if (triggerAction == null || rayInteractor == null)
            return;
        
        float triggerValue = triggerAction.ReadValue<float>();
        bool isTriggerPressed = triggerValue >= triggerThreshold;
        
        // Only trigger on button press (transition from unpressed to pressed)
        if (isTriggerPressed && !wasTriggerPressed)
        {
            Debug.Log($"Trigger pressed! Value: {triggerValue}");
            HandleTriggerPress();
        }
        
        wasTriggerPressed = isTriggerPressed;
    }
    
    private void HandleTriggerPress()
    {
        Vector3 rayOrigin = rayInteractor.rayOriginTransform.position;
        Vector3 rayDirection = rayInteractor.rayOriginTransform.forward;
        float rayDistance = rayInteractor.maxRaycastDistance;
        
        Debug.Log($"Raycast from: {rayOrigin}, direction: {rayDirection}, distance: {rayDistance}");
        
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance))
        {
            Debug.Log($"Hit: {hit.collider.gameObject.name}");
            
            // Try to get LitterClickable component
            LitterClickable litter = hit.collider.GetComponent<LitterClickable>();
            if (litter != null)
            {
                Debug.Log("Litter clicked!");
                litter.OnClicked();
                return;
            }

            // Try to get InsectClickable component
            InsectClickable insect = hit.collider.GetComponent<InsectClickable>();
            if (insect != null)
            {
                Debug.Log("Insect clicked!");
                insect.OnClicked();
                return;
            }

            // Try to get FishClickable component (marine scene)
            FishClickable fish = hit.collider.GetComponent<FishClickable>();
            if (fish != null)
            {
                Debug.Log("Fish clicked!");
                fish.OnClicked();
                return;
            }

            // Try to get DebrisClickable component (marine scene)
            DebrisClickable debris = hit.collider.GetComponent<DebrisClickable>();
            if (debris != null)
            {
                Debug.Log("Debris clicked!");
                debris.OnClicked();
                return;
            }

            Debug.LogWarning($"Hit object {hit.collider.gameObject.name} has no clickable component!");
        }
        else
        {
            Debug.Log("Raycast did not hit anything");
        }
    }
}

