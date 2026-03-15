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

    [Header("Marine: Fins")]
    public GameObject leftFinPrefab;
    public GameObject rightFinPrefab;

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
    }


    public void EquipPawsWithScoreUI()
    {
        EquipPaws(); 
        if (scoreUIPrefab != null) scoreUIPrefab.SetActive(true);
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

    }
}