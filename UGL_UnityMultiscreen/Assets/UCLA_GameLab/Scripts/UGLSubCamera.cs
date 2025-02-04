using System;
using UnityEngine;
using UnityEditor;

public class UGLSubCamera : MonoBehaviour
{
    int overrideScreenIdx = -1;
    public int cameraNumber => this.transform.GetSiblingIndex();

    UGLMultiScreen _parent;
    public UGLMultiScreen parent
    {
        get
        {
            if (_parent == null)
            {
                _parent = GetComponentInParent<UGLMultiScreen>();
            }
            return _parent;
        }
    }

    public int outputDisplayNumber
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

    public Vector2Int arrangementLocation
    {
        get
        {
            return parent.getArrangementLocation(this.cameraNumber);
        }
    }

    void Awake()
    {
        //ResetOverrideScreenNumber();
        if (UGLMultiScreenPrefs.Data.screenRemappingOk())
        {
            this.SetOutputDisplay(UGLMultiScreenPrefs.Data.screenRemapping[this.cameraNumber], false);
        }
    }

    public void ResetOverrideScreenNumber() => SetOutputDisplay(-1, true);

    public void SetOutputDisplay(int displayNumber, bool writeToSaveData = false)
    {
        if (!Application.isPlaying)
        {
            Debug.LogError($"'{nameof(SetOutputDisplay)}' is playmode only");
            return;
        }

        this.outputDisplayNumber = displayNumber;
        this.camera.targetDisplay = parent.inSimulationMode ? 0 : this.outputDisplayNumber;


        if (writeToSaveData) 
        try
        {
            UGLMultiScreenPrefs.Data.screenRemapping[this.cameraNumber] = displayNumber;
            UGLMultiScreenPrefs.SaveAll();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    internal void setSimulationMode(bool inSimulationMode)
    {
        camera.targetDisplay = inSimulationMode ? 0 : outputDisplayNumber;
    }

    int lastPlaneCalcFrame = -1;
    Plane[] cameraPlanes = new Plane[6];
    public bool IsInView(Renderer renderer) => IsInView(renderer.bounds);
    public bool IsInView(Bounds bounds)
    {
        CachePlanes();
        return GeometryUtility.TestPlanesAABB(cameraPlanes, bounds);
    }

    private void CachePlanes()
    {
        if (Time.frameCount == lastPlaneCalcFrame)
            return;
        lastPlaneCalcFrame = Time.frameCount;
        GeometryUtility.CalculateFrustumPlanes(this.camera, cameraPlanes);
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(UGLSubCamera))]
    class Ed : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var script = target as UGLSubCamera;
            GUILayout.Label($"SCREEN: {script.outputDisplayNumber}");
            //GUILayout.Label($"GAME: {script.}");
        }
    }
#endif
}
