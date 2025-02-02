using System;
using UnityEngine;
using UnityEditor;

public class UGLSubCamera : MonoBehaviour
{
    int overrideScreenIdx = -1;
    public int cameraNumber => this.transform.GetSiblingIndex();
    
    public int screenNumber
    {
        get
        {
            if (overrideScreenIdx < 0)
            {
                overrideScreenIdx = cameraNumber;
            }
            return overrideScreenIdx;
        }
        private set
        {
            overrideScreenIdx = value;
        }
    }

    [SerializeField] Camera _camera;
    public Camera camera => _camera;


    void Awake()
    {
        //ResetOverrideScreenNumber();
        if (UGLMultiScreenPrefs.Data.screenRemappingOk())
        {
            this.SetOutputScreen(UGLMultiScreenPrefs.Data.screenRemapping[this.cameraNumber], false);
        }
    }

    public void ResetOverrideScreenNumber() => SetOutputScreen(-1);
    public void SetOutputScreen(int screenNumber, bool writeToSaveData = true)
    {
        if (!Application.isPlaying)
        {
            Debug.LogError($"'{nameof(SetOutputScreen)}' is playmode only");
            return;
        }

        this.screenNumber = screenNumber;
        this.camera.targetDisplay = UGLMultiScreen.I.inSimulationMode ? 0 : this.screenNumber;


        if (writeToSaveData) 
        try
        {
            UGLMultiScreenPrefs.Data.screenRemapping[this.cameraNumber] = screenNumber;
            UGLMultiScreenPrefs.SaveAll();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    internal void setSimulationMode(bool inSimulationMode)
    {
        camera.targetDisplay = inSimulationMode ? 0 : screenNumber;
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(UGLSubCamera))]
    class Ed : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var script = target as UGLSubCamera;
            GUILayout.Label($"SCREEN: {script.screenNumber}");
            //GUILayout.Label($"GAME: {script.}");
        }
    }
#endif
}
