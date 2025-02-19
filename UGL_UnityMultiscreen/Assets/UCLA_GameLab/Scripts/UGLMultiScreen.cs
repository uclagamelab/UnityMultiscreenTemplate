using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using XUUtils;
using Unity.VisualScripting;
using NaughtyAttributes;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using NaughtyAttributes.Editor;
#endif

[ExecuteInEditMode]
public class UGLMultiScreen : MonoBehaviour
{
    static UGLMultiScreen _I;
    public static UGLMultiScreen Current
    {
        get
        {
            if (_I == null) _I = GameObject.FindFirstObjectByType<UGLMultiScreen>();
            return _I;
        }
    }

    const int N_MONITORS = 6;

    //const float SUBSCREEN_H_O_W = 3f / 4f; //Height over width (aspect ratio of monitors)
    public Vector2 aspectRatio = new(16, 9);
    float SUBSCREEN_H_O_W => aspectRatio.y / aspectRatio.x; //Height over width (aspect ratio of monitors)

    public List<UGLSubCamera> Cameras
    {
        get => _Cameras;
    }

    public bool[] arrangementGrid
    {
        get
        {
            if (_arrangementGrid == null || _arrangementGrid.Length != N_MONITORS * N_MONITORS)
            {
                _arrangementGrid = new bool[N_MONITORS * N_MONITORS];
                int nCols = Mathf.CeilToInt(Mathf.Sqrt(N_MONITORS));
                int nPlaced = 0;
                for (int i = 0; i < N_MONITORS * N_MONITORS; i++)
                {
                    int xi = i % N_MONITORS;
                    int yi = i / N_MONITORS;
                    _arrangementGrid[i] = xi < nCols && nPlaced < N_MONITORS;
                    nPlaced += _arrangementGrid[i] ? 1 : 0;
                }
            }
            return _arrangementGrid;
        }
    }


    [ShowIf("false")]
    public bool autoRefreshSimulationView = false;

    public enum CameraArrangementStyle
    {
        SimpleGrid,
        SeamlessOrthographic,
        PerspectiveFrankenCam,
    }
    [ShowIf("false")]
    public CameraArrangementStyle cameraArrangementStyle = CameraArrangementStyle.SimpleGrid;

    //bool simpleGridMode => cameraArrangementStyle == CameraArrangementStyle.SimpleGrid;

    #region Camera Positioning Settings
    [ShowIf("false")]
    public Vector2 simpleGridCameraSpacing = new(2,2);
    [ShowIf("false")]
    public float frankenPerspectiveFOV = 60;
    [ShowIf("false")]
    public Vector2 frankenPerspectivePadding = new Vector2(0, 0);
    [ShowIf("false")]
    public float frankenOrthographicSize = 5;
    [ShowIf("false")]
    public Vector2 frankenOrthographicPadding = Vector2.zero;
    #endregion

    #region Serialized, but don't modify Directly
    [Space(15)]
    [Header("--- Don't Edit Below ---")]
    [FormerlySerializedAs("Cameras")]
    [SerializeField] List<UGLSubCamera> _Cameras = new();

    [SerializeField] bool[] _arrangementGrid = null;
    public bool inSimulationMode = false;
    #endregion
    void Start()
    {
        for (int i = 0; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate(); //Enable the display
        }

        this.RefreshCameraSettings();
    }

    private void OnDestroy()
    {
        if (_I == this) _I = null;
    }

    void Update()
    {
        AutoRefreshSimViewIfNecessary();

        if (!Application.isPlaying) return;


        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.L))
        {
            UGLMultiScreenAdminPanel.AdminPanelsOpen = !UGLMultiScreenAdminPanel.AdminPanelsOpen;
        }
    }

    Vector2 _lastAppliedResolution;
    void AutoRefreshSimViewIfNecessary()
    {
        if (autoRefreshSimulationView && inSimulationMode)
        {
            RefreshCameraSettings(false);
        }
    }

    void PositionCameras()
    {
#if UNITY_EDITOR
        UnityEditor.Undo.RegisterFullObjectHierarchyUndo(this.gameObject, "Auto Position Cameras");
#endif
        bool seamlessOrtho = this.cameraArrangementStyle == CameraArrangementStyle.SeamlessOrthographic;

        this.GetArrangementExtents(out var arrangementSize);
        Vector3 arrangementCenter = new Vector3(arrangementSize.x - 1, arrangementSize.y - 1, 0) * .5f;
        if (this.cameraArrangementStyle == CameraArrangementStyle.SimpleGrid || seamlessOrtho)
        {
            Vector3 camSpacing = seamlessOrtho ?
                new Vector3(frankenOrthographicSize / SUBSCREEN_H_O_W + frankenOrthographicPadding.x, frankenOrthographicSize + frankenOrthographicPadding.y, 1) * 2 / arrangementSize.y
                :
                simpleGridCameraSpacing;

            foreach (var cam in Cameras)
            {
                cam.camera.orthographic = seamlessOrtho;
                if (seamlessOrtho)
                {
                    cam.camera.orthographicSize = frankenOrthographicSize / arrangementSize.y;
                }

                var i = cam.outputDisplayNumber;
                Vector3 arrangeLocation = this.getArrangementLocation(i).asXyVector3() - arrangementCenter;
                arrangeLocation.y *= -1;
                cam.transform.localPosition = Vector3.Scale(arrangeLocation, camSpacing);
                cam.transform.localRotation = Quaternion.identity;
            }
        }
        else if (this.cameraArrangementStyle == CameraArrangementStyle.PerspectiveFrankenCam)
        {
            float subFov = frankenPerspectiveFOV / Mathf.Max(arrangementSize.x, arrangementSize.y);// * SUBSCREEN_H_O_W;
            foreach (var cam in Cameras)
            {
                cam.camera.orthographic = false;
                cam.camera.fieldOfView = subFov;
                var i = cam.outputDisplayNumber;
                Vector2 arrangeLocation = this.getArrangementLocation(i);
                cam.transform.localPosition = Vector3.zero;
                Vector2 normalizedCoord = Vector2.zero;
                normalizedCoord = arrangeLocation - (Vector2)arrangementCenter;
                //rotate to align the the frustums
                cam.transform.localEulerAngles = new Vector3(
                    normalizedCoord.y * (subFov + frankenPerspectivePadding.y),
                    normalizedCoord.x * (subFov / SUBSCREEN_H_O_W + frankenPerspectivePadding.x),
                    0);
                arrangeLocation.y *= -1;
            }
        }
        RefreshCameraSettings();
    }

    //TODO: untested
    public UGLSubCamera GetCameraByOutputScreen(int screenIdx)
    {
        foreach (var cam in this.Cameras)
        {
            if (cam.outputDisplayNumber == screenIdx) return cam;
        }
        return null;
    }

    public UGLSubCamera GetCameraByNumber(int cameraNumber)
    {
        return this.Cameras[cameraNumber];
    }

    public UGLSubCamera GetCameraByArrangementLocation(Vector2Int loc) => GetCameraByArrangementLocation(loc.x, loc.y);
    public UGLSubCamera GetCameraByArrangementLocation(int x, int y)
    {
        foreach (var cam in this.Cameras)
        {
            var arrangeLoc = cam.arrangementLocation;
            if (arrangeLoc.x == x && arrangeLoc.y == y)
            {
                return cam;
            }
        }
        return null;
    }

    public UGLSubCamera GetCameraForMousePosition(Vector2 mousePosition, out Vector3 screenPosition)//, out int displayNumber)
    {
        int displayNumber = -1;
        screenPosition = mousePosition;
        if (!inSimulationMode)
        {
            Vector3 relativeMouseAndDisplay = Display.RelativeMouseAt(mousePosition);
            if (relativeMouseAndDisplay != Vector3.zero)
            {

                displayNumber = (int)relativeMouseAndDisplay.z;
                screenPosition = relativeMouseAndDisplay.withZ(0);
                return GetCameraByOutputScreen(displayNumber);
            }
        }

        UGLSubCamera chosenCam = null;
        foreach (var cam in this.Cameras)
        {
            Vector3 vpPosition = cam.camera.ScreenToViewportPoint(Input.mousePosition);
            if (XUUtil.NumberIsBetween(vpPosition.x, 0, 1) && XUUtil.NumberIsBetween(vpPosition.y, 0, 1))
            {

                chosenCam = cam;
                break;
            }
        }
        return chosenCam;
    }

    public void RefreshCameraSettings(bool force = true) => SetSingleScreenSimulationMode(inSimulationMode, force);
    void SetSingleScreenSimulationMode(bool enable, bool force = true)
    {
        inSimulationMode = enable;
        this.GetArrangementExtents(out var offset, out var size);
        Vector2 gameViewSize = new Vector2(Screen.width, Screen.height);

#if UNITY_EDITOR //in editor, Screen.Width and Screen.height don't return the rendering size
        {
            PlayModeWindow.GetRenderingResolution(out var gameViewSizeX, out var gameViewSizeY);//GetMainGameViewSize();
            gameViewSize.x = gameViewSizeX;
            gameViewSize.y = gameViewSizeY;
        }
#endif
        if (gameViewSize == _lastAppliedResolution && !force)
        {
            return;
        }
        _lastAppliedResolution = gameViewSize;

        float screenHoW = gameViewSize.y / gameViewSize.x;

        Vector2 cellSize = new Vector2(1f / size.x, 1f / size.y);
        cellSize.y = cellSize.x / screenHoW * SUBSCREEN_H_O_W;

        Vector2 centeringOffset = Vector2.zero;

        float normalizedH = size.y * cellSize.y;

        if (normalizedH > 1)
        {
            cellSize /= normalizedH;
            float normalizedW = size.x * cellSize.x;
            centeringOffset.x = (1 - normalizedW) / 2;
        }
        else
        {
            centeringOffset.y = (1 - normalizedH) / 2;
        }

        foreach (var cam in this.Cameras)
        {
            cam.setSimulationMode(inSimulationMode);

            if (!inSimulationMode)
            {
                cam.camera.rect = new Rect(0, 0, 1, 1);
            }
            else
            {
                Vector2 arrangePos = this.getArrangementLocation(cam.outputDisplayNumber);
                Vector2 cellPos = new Vector2(arrangePos.x * cellSize.x, (size.y - arrangePos.y - 1) * cellSize.y);

                float tempScale = 1;// .5f;
                cellPos += centeringOffset;
                cam.camera.rect = new Rect(cellPos * tempScale, cellSize * tempScale);
            }
        }
    }

    public void GetArrangementExtents(out Vector2Int size) => GetArrangementExtents(out var dontCare, out size);
    public void GetArrangementExtents(out Vector2Int offset, out Vector2Int size)
    {
        Vector2Int min = new(N_MONITORS + 1, N_MONITORS + 1);
        Vector2Int max = new(-1, -1);

        for (int yi = 0; yi < N_MONITORS; yi++)
        {
            for (int xi = 0; xi < N_MONITORS; xi++)
            {
                int i = yi * N_MONITORS + xi;
                if (_arrangementGrid[i])
                {
                    min.x = Mathf.Min(xi, min.x);
                    min.y = Mathf.Min(yi, min.y);
                    max.x = Mathf.Max(xi + 1, max.x);
                    max.y = Mathf.Max(yi + 1, max.y);
                }
            }
        }
        offset = min;
        size = max - min;
    }

    public Vector2Int getArrangementLocation(int displayNumber)
    {
        int nFound = 0;
        GetArrangementExtents(out var offset, out var size);

        for (int yi = 0; yi < N_MONITORS; yi++)
        {
            for (int xi = 0; xi < N_MONITORS; xi++)
            {
                int i = yi * N_MONITORS + xi;
                if (_arrangementGrid[i])
                {
                    if (displayNumber == nFound)
                    {
                        return new Vector2Int(xi - offset.x, yi - offset.y);
                    }
                    nFound++;
                }
            }
        }
        return new Vector2Int(-1, -1);
    }

    int nAssigned
    {
        get
        {
            int _nAssigned = 0;
            foreach (var b in arrangementGrid) if (b) _nAssigned++;
            return _nAssigned;
        }
    }


#if UNITY_EDITOR

    [CustomEditor(typeof(UGLMultiScreen))]
    public class Ed : NaughtyInspector
    {
        Dictionary<string, SerializedProperty> _serializedProps = new();

        void ezDrawSerializedProperty(string name)
        {
            if (!_serializedProps.TryGetValue(name, out var prop))
            {
                prop = serializedObject.FindProperty(name);
                _serializedProps[name] = prop;
            }

            if (prop == null)
            {
                GUILayout.Label($"Couldn't find property '{name}'");
            }
            else
            {
                EditorGUILayout.PropertyField(prop);
            }
        }


        public override void OnInspectorGUI()
        {
            var script = target as UGLMultiScreen;
            bool changed = false;

            drawScreenSimulationSection(ref changed);

            GUILayout.Space(10);



            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();



            GUILayout.BeginVertical(GUILayout.Width(160));
            GUILayout.Label($"Screen Arangement ({script.nAssigned}/{N_MONITORS})");
            for (int yi = 0; yi < N_MONITORS; yi++)
            {
                GUILayout.BeginHorizontal();
                for (int xi = 0; xi < N_MONITORS; xi++)
                {
                    int idx = yi * N_MONITORS + xi;
                    var curVal = script.arrangementGrid[idx];
                    var nuVal = GUILayout.Toggle(curVal, "", GUILayout.Width(15));


                    if (nuVal != curVal)
                    {
                        bool okToTurnOn = !nuVal || script.nAssigned + 1 <= N_MONITORS;
                        if (okToTurnOn)
                        {
                            script.arrangementGrid[idx] = nuVal;
                            changed = true;
                        }
                    }
                }
                GUILayout.EndHorizontal();

            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();



            GUILayout.Space(10);

            //GUILayout.Label("Auto Arrange Cameras");
            _showArrange = EditorGUILayout.BeginFoldoutHeaderGroup(_showArrange, "Auto Arrange Cameras");
            if (_showArrange)
            {

                ezDrawSerializedProperty(nameof(UGLMultiScreen.cameraArrangementStyle));
                if (script.cameraArrangementStyle == CameraArrangementStyle.SimpleGrid)
                {
                    ezDrawSerializedProperty(nameof(UGLMultiScreen.simpleGridCameraSpacing));
                }
                else if (script.cameraArrangementStyle == CameraArrangementStyle.PerspectiveFrankenCam)
                {
                    ezDrawSerializedProperty(nameof(UGLMultiScreen.frankenPerspectiveFOV));
                    ezDrawSerializedProperty(nameof(UGLMultiScreen.frankenPerspectivePadding));
                }
                else if (script.cameraArrangementStyle == CameraArrangementStyle.SeamlessOrthographic)
                {
                    ezDrawSerializedProperty(nameof(UGLMultiScreen.frankenOrthographicSize));
                    ezDrawSerializedProperty(nameof(UGLMultiScreen.frankenOrthographicPadding));
                }

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Position World Cameras"))
                {
                    script.PositionCameras();
                    changed = true;
                }
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (changed)
            {
                EditorUtility.SetDirty(script.gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(script);
            }

            serializedObject.ApplyModifiedProperties();

            //GUILayout.Space(10);
            //_defaultInspectorFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_defaultInspectorFoldout, "Default Inspector");
            //EditorGUILayout.EndFoldoutHeaderGroup();
            //if (_defaultInspectorFoldout)
            //{
            //    base.OnInspectorGUI();
            //}
        }

        void drawScreenSimulationSection(ref bool changed)
        {

            var script = target as UGLMultiScreen;
            GUILayout.Label("   Single Screen Simulation");
            GUILayout.BeginHorizontal();
            GUI.enabled = !script.inSimulationMode;
            if (GUILayout.Button("ON"))
            {
                script.SetSingleScreenSimulationMode(true);
                changed = true;
            }
            GUI.enabled = script.inSimulationMode;
            if (GUILayout.Button("OFF"))
            {
                script.SetSingleScreenSimulationMode(false);
                changed = true;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Refresh Simulation View"))
            {
                script.SetSingleScreenSimulationMode(script.inSimulationMode);
                changed = true;
            }
            ezDrawSerializedProperty(nameof(UGLMultiScreen.autoRefreshSimulationView));
        }
        bool _defaultInspectorFoldout = false;
        bool _showArrange = false;
    }
#endif

    //private void OnGUI()
    //{
    //    if (!Application.isPlaying)
    //    {
    //        return;
    //    }

    //    var cam = UGLMultiScreen.Current.GetCameraForMousePosition(Input.mousePosition, out var screenPosFixed, out var display);
    //    var pos = screenPosFixed;// Input.mousePosition;

    //    if (display >= 0)
    //    {
    //        pos.y = Display.displays[display].renderingHeight - pos.y;
    //    }
    //    else
    //    {
    //        pos.y = Screen.height - pos.y;
    //    }
    //    GUI.Button(new Rect(pos, new Vector2(220,80)), $"\nfixed:{screenPosFixed}" +
    //        $"\ncam-disp:{(cam == null ? -1 : cam.outputDisplayNumber)}" +
    //        $"\ndisplay: {display}");
    //}
}
