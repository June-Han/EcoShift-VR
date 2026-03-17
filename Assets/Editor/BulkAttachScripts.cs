// Assets/Editor/BulkAttachScripts.cs
using UnityEngine;
using UnityEditor;


public class BulkAttachScripts : EditorWindow
{
    [MenuItem("Tools/Attach Clickable Scripts to Environment")]
    public static void AttachScripts()
    {
        // Attach InsectClickable to all children of Insects
        GameObject insects = GameObject.Find("Insects");
        if (insects != null)
        {
            foreach (Transform child in insects.GetComponentsInChildren<Transform>())
            {
            if (child.gameObject == insects) continue; // skip the parent itself
            if (child.GetComponent<InsectClickable>() == null)
                child.gameObject.AddComponent<InsectClickable>();
            
            UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable = child.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            if (interactable == null)
                interactable = child.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
    
            // Wire up the SelectEntered event
            interactable.selectEntered.AddListener((args) => child.GetComponent<InsectClickable>().OnClicked());
            }
            

            Debug.Log($"Attached InsectClickable to {insects.transform.childCount} insects");
        }
        else Debug.LogWarning("Could not find GameObject named 'Insects'");

        // Attach LitterClickable to all children of Trash
        GameObject trash = GameObject.Find("Trash");
        if (trash != null)
        {
            foreach (Transform child in trash.transform)
            {
                if (child.GetComponent<LitterClickable>() == null)
                    child.gameObject.AddComponent<LitterClickable>();
            }

            foreach (Transform child in trash.GetComponentsInChildren<Transform>())
            {
            if (child.gameObject == trash) continue; // skip the parent itself
            if (child.GetComponent<LitterClickable>() == null)
                child.gameObject.AddComponent<LitterClickable>();
            
            UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable = child.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
            if (interactable == null)
                interactable = child.gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
    
            // Wire up the SelectEntered event
            interactable.selectEntered.AddListener((args) => child.GetComponent<LitterClickable>().OnClicked());
            }
            Debug.Log($"Attached LitterClickable to {trash.transform.childCount} trash objects");
        }
        else Debug.LogWarning("Could not find GameObject named 'Trash'");
    }
}