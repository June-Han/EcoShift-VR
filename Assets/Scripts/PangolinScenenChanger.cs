using System;
using UnityEngine;

public class PangolinScenenChanger : MonoBehaviour
{
    private SceneChanger sceneChanger;
    private void Start()
    {
        sceneChanger = FindObjectOfType<SceneChanger>();
        if (sceneChanger == null)
        {
            Debug.LogError("SceneChanger not found in the scene. Please make sure there is a SceneChanger component in the scene.");
        }
    }

    public void ChangeScene()
    {
        sceneChanger.LoadTerrestrialThreatScene();
    }
}
