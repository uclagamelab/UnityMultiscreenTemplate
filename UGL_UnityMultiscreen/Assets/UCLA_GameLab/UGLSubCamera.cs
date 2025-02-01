using UnityEngine;

public class UGLSubCamera : MonoBehaviour
{
    int overrideScreenIdx = -1;
    public int cameraNumber => this.transform.GetSiblingIndex() + 1;
    
    public int screenNumber => overrideScreenIdx >= 1 ? overrideScreenIdx : cameraNumber;

    [SerializeField] Camera _camera;
    public Camera camera => _camera;


    void Awake()
    {
        //ResetOverrideScreenNumber();
    }

    void ResetOverrideScreenNumber() => SetOutputScreen(-1);
    public void SetOutputScreen(int screenNumber)
    {
        this.overrideScreenIdx = screenNumber;
        this.camera.targetDisplay = this.screenNumber;
    }

}
