using System;
using UnityEngine;
using UnityEngine.Assertions;

public class GamePauser : MonoBehaviour
{
    private bool hasTriggered = false;
    private void Awake() 
    {
        Collider col = GetComponent<Collider>();
        Assert.IsNotNull(col, "GamePauser requires a Collider component.");
        Assert.IsTrue(col.isTrigger, "GamePauser's Collider must be set as a Trigger.");
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            HunterChaseManager.Instance.PauseGame();
        }
    }
}
