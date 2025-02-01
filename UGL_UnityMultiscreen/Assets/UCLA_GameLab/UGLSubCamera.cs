using System;
using UnityEngine;

public class UGLSubCamera : MonoBehaviour
{
    int overrideScreenIdx = -1;
    public int cameraNumber => this.transform.GetSiblingIndex();
    
    public int screenNumber => overrideScreenIdx >= 0 ? overrideScreenIdx : cameraNumber;

    [SerializeField] Camera _camera;
    public Camera camera => _camera;


    void Awake()
    {
        //ResetOverrideScreenNumber();
    }

    public void ResetOverrideScreenNumber() => SetOutputScreen(-1);
    public void SetOutputScreen(int screenNumber)
    {
        if (!Application.isPlaying)
        {
            Debug.LogError($"'{nameof(SetOutputScreen)}' is playmode only");
            return;
        }
        this.overrideScreenIdx = screenNumber;
        this.camera.targetDisplay = this.screenNumber;
    }

    internal void setSimulationMode(bool inSimulationMode)
    {
        camera.targetDisplay = inSimulationMode ? 0 : screenNumber;
    }
}
