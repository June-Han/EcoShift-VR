using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility that creates two DialogueTrigger GameObjects in the Shark_Threat
/// scene hierarchy — "SurvivedDialogue" and "GameOverDialogue" — pre-populated
/// with default sentences. Also wires them into SharkThreatStatus if one is found.
///
/// Usage: Unity menu → Tools → EcoShift → Create Shark End-Game Dialogues
/// </summary>
public class CreateSharkEndGameDialogues : MonoBehaviour
{
    [MenuItem("Tools/EcoShift/Create Shark End-Game Dialogues")]
    public static void Create()
    {
        // --- Survived Dialogue ---
        GameObject survivedGO = new GameObject("SurvivedDialogue");
        DialogueTrigger survivedTrigger = survivedGO.AddComponent<DialogueTrigger>();
        survivedTrigger.triggerOnce = false;
        survivedTrigger.triggerOnStart = false;
        survivedTrigger.sentences = new string[]
        {
            "Well done! The blue shark survived the bycatch threat and escaped the fishing nets. Your awareness and quick reflexes kept the shark safe.",
            "Bycatch is the unintentional capture of non-target species in fishing gear. Blue sharks are one of the most frequently caught bycatch species worldwide, with millions caught each year.",
            "You can help reduce bycatch by choosing sustainably sourced seafood, supporting fisheries that use shark-safe gear, and spreading awareness about bycatch in your community.",
            "The experience is over. You may speak to the administrators for further instructions. Press Next to return to the main menu."
        };

        // --- Game Over Dialogue ---
        GameObject gameOverGO = new GameObject("GameOverDialogue");
        DialogueTrigger gameOverTrigger = gameOverGO.AddComponent<DialogueTrigger>();
        gameOverTrigger.triggerOnce = false;
        gameOverTrigger.triggerOnStart = false;
        gameOverTrigger.sentences = new string[]
        {
            "Unfortunately, the blue shark was caught in the fishing nets. Bycatch like this is a serious threat to blue shark populations around the world.",
            "Bycatch is the unintentional capture of non-target species in fishing gear. Blue sharks are one of the most frequently caught bycatch species worldwide, with millions caught each year.",
            "You can help reduce bycatch by choosing sustainably sourced seafood, supporting fisheries that use shark-safe gear, and spreading awareness about bycatch in your community.",
            "The experience is over. You may speak to the administrators for further instructions. Press Next to return to the main menu."
        };

        // --- Try to wire into SharkThreatStatus ---
        SharkThreatStatus status = FindFirstObjectByType<SharkThreatStatus>(FindObjectsInactive.Include);
        if (status != null)
        {
            survivedGO.transform.SetParent(status.transform, false);
            gameOverGO.transform.SetParent(status.transform, false);

            status.survivedDialogue = survivedTrigger;
            status.gameOverDialogue = gameOverTrigger;
            EditorUtility.SetDirty(status);

            Debug.Log("<color=green>[EcoShift]</color> Dialogues parented to SharkThreatStatus and auto-wired.");
        }
        else
        {
            Debug.Log("<color=yellow>[EcoShift]</color> No SharkThreatStatus found — dialogues created at scene root. Drag them into the slots manually.");
        }

        // Select the survived dialogue so user can see it
        Selection.activeGameObject = survivedGO;
        Undo.RegisterCreatedObjectUndo(survivedGO, "Create Shark End-Game Dialogues");
        Undo.RegisterCreatedObjectUndo(gameOverGO, "Create Shark End-Game Dialogues");

        EditorUtility.DisplayDialog("Shark End-Game Dialogues Created",
            "Created two DialogueTrigger objects:\n\n" +
            "• SurvivedDialogue (4 panels)\n" +
            "• GameOverDialogue (4 panels)\n\n" +
            (status != null
                ? "Auto-wired into SharkThreatStatus.\n\n"
                : "No SharkThreatStatus found in scene.\nDrag them into the Survived/GameOver Dialogue slots manually.\n\n") +
            "Edit the sentences on each DialogueTrigger in the Inspector.\n" +
            "Remember to save the scene when done!",
            "OK");
    }
}
