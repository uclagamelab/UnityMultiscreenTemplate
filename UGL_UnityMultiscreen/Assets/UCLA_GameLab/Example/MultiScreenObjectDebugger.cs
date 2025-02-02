using UnityEngine;
using TMPro;
public class MultiScreenObjectDebugger : MonoBehaviour
{
    [SerializeField] UGLMultiScreenObject _mso;
    [SerializeField] TextMeshPro _textMeshPro;
    void Awake()
    {
    }


    void Update()
    {
        string text = "";
        foreach (var cam in _mso.GetAllIntersectingCameras()) 
        {
            if (!string.IsNullOrEmpty(text))
            {
                text += ", ";
            }
            text += $"{cam.cameraNumber}";   
        }
        _textMeshPro.text = text;
    }
}
