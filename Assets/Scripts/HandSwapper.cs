using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HandSwapper : MonoBehaviour
{
    [Header("VR Controllers")]
    public GameObject leftController;
    public GameObject rightController;

    [Header("Terrestrial: Paws")]
    public GameObject leftPawPrefab;
    public GameObject rightPawPrefab;

    
    [Header("Terrestrial Scene: Score UI")]
    public GameObject scoreUIPrefab; 
    public GameObject timerUiPrefab;

    [Header("Marine: Fins")]
    public GameObject leftFinPrefab;
    public GameObject rightFinPrefab;

    [Header("Marine Threat: Lives UI")]
    public GameObject livesUIPrefab;

    // Call this function for terrestrial to swap handles
    public void EquipPaws()
    {
        // Turn Paws on
        leftPawPrefab.SetActive(true);
        rightPawPrefab.SetActive(true);

        // Turn others off
        leftController.SetActive(false);
        rightController.SetActive(false);
        
        leftFinPrefab.SetActive(false);
        rightFinPrefab.SetActive(false);

        if (scoreUIPrefab != null) scoreUIPrefab.SetActive(false);
        if (timerUiPrefab != null) timerUiPrefab.SetActive(false);
        if (livesUIPrefab != null) livesUIPrefab.SetActive(false);
    }


    public void EquipPawsWithScoreUI()
    {
        EquipPaws(); 
        if (scoreUIPrefab != null) scoreUIPrefab.SetActive(true);
    }

    public void EquipPawsWithTimerUI()
    {
        EquipPaws(); 
        if (timerUiPrefab != null) timerUiPrefab.SetActive(true);
    }

    // Call this function for Marine to swap handles
    public void EquipFins()
    {
        // Turn Fins ON
        leftFinPrefab.SetActive(true);
        rightFinPrefab.SetActive(true);

        // Turn others OFF
        leftController.SetActive(false);
        rightController.SetActive(false);
        leftPawPrefab.SetActive(false);
        rightPawPrefab.SetActive(false);

        if (scoreUIPrefab != null) scoreUIPrefab.SetActive(false);
        if (livesUIPrefab != null) livesUIPrefab.SetActive(false);
    }

    // Call this for Marine FoodSearch scenes that need the score counter
    public void EquipFinsWithScoreUI()
    {
        EquipFins();
        if (scoreUIPrefab != null) scoreUIPrefab.SetActive(true);
    }

    // Call this for the Marine Threat scene that needs the hearts/lives display
    public void EquipFinsWithLivesUI()
    {
        EquipFins();
        if (livesUIPrefab != null) livesUIPrefab.SetActive(true);
    }
}

