using UnityEngine;
using TMPro;
using System;
public class MultiScreenObjectScriptExample : MonoBehaviour
{
    [SerializeField] UGLMultiScreenObject _mso;
    [SerializeField] TextMeshPro _textMeshPro;

    void Awake()
    {
        _mso.OnEnterCameraChange += OnCameraChange;
    }

    private void OnCameraChange(UGLMultiScreenObject.EnterChangeInfo info)
    {
        if (info.entered) 
        {
            Debug.Log("entered camera " + info.camera.cameraNumber, this);
        }
        else //exited
        {
            Debug.Log("exited camera " + info.camera.cameraNumber, this);
        }
    }

    void Update()
    {
        string text = "";
        foreach (var cam in _mso.getAllIntersectingCameras())
        {
            if (!string.IsNullOrEmpty(text))
            {
                text += ", ";
            }
            text += $"{cam.cameraNumber}";
        }
        _textMeshPro.text = $"all visible: ({text})";
        _textMeshPro.text += $"\nlast entered:{_mso.lastEnteredCamera.arrangementLocation}";
    }
}
