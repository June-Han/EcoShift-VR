using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // Add this if using XR Toolkit 3.0+

[RequireComponent(typeof(XRSimpleInteractable))]
public class VRHoverOutline : MonoBehaviour
{
    [Tooltip("Drag the child object with the Outline script into this slot")]
    public Behaviour outlineComponent; 

    private XRBaseInteractable interactable;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        
        // Ensure the outline is turned off when the insect first spawns
        if (outlineComponent != null)
        {
            outlineComponent.enabled = false;
        }
    }

    void OnEnable()
    {
        // Subscribe to the VR raycast hover events
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
    }

    void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        interactable.hoverEntered.RemoveListener(OnHoverEnter);
        interactable.hoverExited.RemoveListener(OnHoverExit);
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        // Turn the outline ON when the laser touches it
        if (outlineComponent != null)
        {
            outlineComponent.enabled = true;
        }
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        // Turn the outline OFF when the laser leaves
        if (outlineComponent != null)
        {
            outlineComponent.enabled = false;
        }
    }
}