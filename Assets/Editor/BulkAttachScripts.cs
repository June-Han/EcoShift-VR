// Assets/Editor/BulkAttachScripts.cs
using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BulkAttachScripts : EditorWindow
{
    static AudioClip insectClickSound;
    static AudioClip litterClickSound;

    [MenuItem("Tools/Attach Clickable Scripts to Environment")]
    public static void AttachScripts()
    {
        // Attach InsectClickable to all children of Insects
        GameObject insects = GameObject.Find("Insects");
        if (insects != null)
        {
            foreach (Transform child in insects.GetComponentsInChildren<Transform>())
            {
                if (child.gameObject == insects) continue;

                // Add InsectClickable
                InsectClickable ic = child.GetComponent<InsectClickable>();
                if (ic == null) ic = child.gameObject.AddComponent<InsectClickable>();

                // Assign audio clip if provided
                if (insectClickSound != null) ic.clickSound = insectClickSound;

                // Add XRSimpleInteractable
                XRSimpleInteractable interactable = child.GetComponent<XRSimpleInteractable>();
                if (interactable == null)
                    interactable = child.gameObject.AddComponent<XRSimpleInteractable>();

                // Wire up SelectEntered event
                interactable.selectEntered.AddListener((args) =>
                    child.GetComponent<InsectClickable>().OnClicked());
            }
            Debug.Log($"Attached InsectClickable to {insects.transform.childCount} insects");
        }
        else Debug.LogWarning("Could not find GameObject named 'Insects'");

        // Attach LitterClickable to all children of Trash
        GameObject trash = GameObject.Find("Trash");
        if (trash != null)
        {
            foreach (Transform child in trash.GetComponentsInChildren<Transform>())
            {
                if (child.gameObject == trash) continue;

                // Add LitterClickable
                LitterClickable lc = child.GetComponent<LitterClickable>();
                if (lc == null) lc = child.gameObject.AddComponent<LitterClickable>();

                // Assign audio clip if provided
                if (litterClickSound != null) lc.clickSound = litterClickSound;

                // Add XRSimpleInteractable
                XRSimpleInteractable interactable = child.GetComponent<XRSimpleInteractable>();
                if (interactable == null)
                    interactable = child.gameObject.AddComponent<XRSimpleInteractable>();

                // Wire up SelectEntered event
                interactable.selectEntered.AddListener((args) =>
                    child.GetComponent<LitterClickable>().OnClicked());
            }
            Debug.Log($"Attached LitterClickable to {trash.transform.childCount} trash objects");
        }
        else Debug.LogWarning("Could not find GameObject named 'Trash'");
    }

    // Optional: open a window to assign audio clips before running
    [MenuItem("Tools/Bulk Attach Settings")]
    public static void ShowWindow()
    {
        GetWindow<BulkAttachScripts>("Bulk Attach Settings");
    }

    void OnGUI()
    {
        GUILayout.Label("Audio Clips (optional)", EditorStyles.boldLabel);
        insectClickSound = (AudioClip)EditorGUILayout.ObjectField(
            "Insect Click Sound", insectClickSound, typeof(AudioClip), false);
        litterClickSound = (AudioClip)EditorGUILayout.ObjectField(
            "Litter Click Sound", litterClickSound, typeof(AudioClip), false);

        GUILayout.Space(10);
        if (GUILayout.Button("Attach Scripts to Environment"))
            AttachScripts();
    }
}