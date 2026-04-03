using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Events;
public class DialogueManager : MonoBehaviour {
    public static DialogueManager Instance { get; private set; }

    public TextMeshProUGUI uiText;
    private int currentStep = 0;

    public string[] dialogues;
    public UnityEvent onDialogueEnd;
    private UnityAction currentDialogueCallback;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    public void ShowDialogue(string[] newDialogues, UnityAction onComplete = null) {
        dialogues = newDialogues;
        currentDialogueCallback = onComplete;
        currentStep = 0;
        gameObject.SetActive(true);

        // Make sure ALL descendant UI elements are active (Popup_Panels starts disabled in scene)
        ActivateAllChildren(transform);

        // Reset all child RectTransform positions to prevent offset issues
        // (e.g. Popup_Panels scene override had anchoredPosition of ~28000,1000)
        ResetChildRectTransforms(transform);

        // Ensure Canvas renders on top of everything and children are on UI layer
        Canvas canvasComp = GetComponent<Canvas>();
        if (canvasComp != null) {
            canvasComp.sortingOrder = 100;
        }
        SetLayerRecursive(transform, 5); // 5 = UI layer

        // Make sure HeadFollowUI is enabled so it positions the Canvas in front of the player
        HeadFollowUI headFollow = GetComponent<HeadFollowUI>();
        if (headFollow != null) {
            headFollow.enabled = true;
        }

        // Ensure an EventSystem exists (Main_Scene's EventSystem gets destroyed on scene load)
        EnsureEventSystem();

        // Assign event camera so UI interactions work
        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam != null && canvasComp != null && canvasComp.renderMode == RenderMode.WorldSpace) {
            canvasComp.worldCamera = cam;
        }

        // Reset the Canvas RectTransform anchored position so it doesn't drift
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null) {
            rt.anchoredPosition = Vector2.zero;
        }

        NextSentence();
    }

    private void ActivateAllChildren(Transform parent) {
        foreach (Transform child in parent) {
            child.gameObject.SetActive(true);
            ActivateAllChildren(child);
        }
    }

    private void ResetChildRectTransforms(Transform parent) {
        foreach (Transform child in parent) {
            RectTransform childRT = child.GetComponent<RectTransform>();
            if (childRT != null) {
                childRT.anchoredPosition = Vector2.zero;
            }
            // Don't recurse deeper — only reset immediate children of the Canvas
            // (InnerPanel, Buttons, etc. should keep their layout-relative positions)
        }
    }

    private void EnsureEventSystem() {
        if (EventSystem.current != null) return;

        // No EventSystem in this scene — create one with XRUIInputModule
        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<EventSystem>();

        // Try to add XRUIInputModule for VR controller UI interaction
        System.Type xrUIType = System.Type.GetType(
            "UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit");
        if (xrUIType != null) {
            esObj.AddComponent(xrUIType);
        } else {
            // Fallback: add StandaloneInputModule (works for non-VR testing)
            esObj.AddComponent<StandaloneInputModule>();
        }

        Debug.Log("[DialogueManager] Created EventSystem — was missing from scene.");
    }

    private void SetLayerRecursive(Transform parent, int layer) {
        parent.gameObject.layer = layer;
        foreach (Transform child in parent) {
            SetLayerRecursive(child, layer);
        }
    }

    public void NextSentence() {
        if (currentStep < dialogues.Length) {
            uiText.text = dialogues[currentStep];
            currentStep++;
        } else {
            if (onDialogueEnd != null) onDialogueEnd.Invoke();

            if (currentDialogueCallback != null) {
                currentDialogueCallback.Invoke();
                currentDialogueCallback = null;
            }

            gameObject.SetActive(false);
        }
    }
}
