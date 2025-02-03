using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.Mathematics;
using XUUtils;
using Unity.VisualScripting;
using NaughtyAttributes;
using NaughtyAttributes.Editor;

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

    static readonly Vector2 SUBSCREEN_ASPECT_RATIO_VEC = new Vector2(4, 3);
    const float SUBSCREEN_H_O_W = 3f / 4f; //Height over width (aspect ratio of monitors)

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
    public float simpleGridCameraSpacing = 2;
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
    public List<UGLSubCamera> Cameras = new();
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
        if (Application.isPlaying)
        {
            EditModeUpdate();
            return;
        }

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.L))
        {
            UGLMultiScreenAdminPanel.AdminPanelsOpen = !UGLMultiScreenAdminPanel.AdminPanelsOpen;
        }
    }

    Vector2Int _lastAppliedResolution;
    void EditModeUpdate()
    {

    }

    void PositionCameras()
    {
        bool seamlessOrtho = this.cameraArrangementStyle == CameraArrangementStyle.SeamlessOrthographic;

        this.GetArrangementExtents(out var arrangementSize);
        Vector3 arrangementCenter = new Vector3(arrangementSize.x - 1, arrangementSize.y - 1, 0) * .5f;
        if (this.cameraArrangementStyle == CameraArrangementStyle.SimpleGrid || seamlessOrtho)
        {
            Vector3 camSpacing = seamlessOrtho ?
                new Vector3(frankenOrthographicSize / SUBSCREEN_H_O_W + frankenOrthographicPadding.x, frankenOrthographicSize + frankenOrthographicPadding.y, 1) * 2 / arrangementSize.y
                :
                (new Vector3(1, SUBSCREEN_H_O_W) * simpleGridCameraSpacing);

            foreach (var cam in Cameras)
            {
                cam.camera.orthographic = seamlessOrtho;
                if (seamlessOrtho)
                {
                    cam.camera.orthographicSize = frankenOrthographicSize / arrangementSize.y;
                }

                var i = cam.screenNumber;
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
                var i = cam.screenNumber;
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

    public UGLSubCamera GetCameraByScreen(int screenIdx)
    {
        foreach (var cam in this.Cameras)
        {
            if (cam.screenNumber == screenIdx) return cam;
        }
        return null;
    }
    public UGLSubCamera GetCamera(int cameraIdx)
    {
        return this.Cameras[cameraIdx];
    }


    public void RefreshCameraSettings() => SetSingleScreenSimulationMode(inSimulationMode);
    void SetSingleScreenSimulationMode(bool enable)
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
                Vector2 arrangePos = this.getArrangementLocation(cam.screenNumber);
                Vector2 cellPos = new Vector2(arrangePos.x * cellSize.x, (size.y - arrangePos.y - 1) * cellSize.y);

                float tempScale = 1;// .5f;
                cellPos += centeringOffset;
                cam.camera.rect = new Rect(cellPos * tempScale, cellSize * tempScale);
                //cam.camera.pixelRect = new Rect(cellPosPix, cellSizePix);
            }
        }
    }

    public void GetArrangementExtents(out int2 size) => GetArrangementExtents(out var dontCare, out size);
    public void GetArrangementExtents(out int2 offset, out int2 size)
    {
        int2 min = new(N_MONITORS + 1, N_MONITORS + 1);
        int2 max = new(-1, -1);

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

    public Vector2 getArrangementLocation(int screenNumber)
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
                    if (screenNumber == nFound)
                    {
                        return new Vector2(xi - offset.x, yi - offset.y);
                    }
                    nFound++;
                }
            }
        }
        return new Vector2(-1, -1);
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
        Dictionary<string, SerializedProperty> _serializedProps;

        protected override void OnEnable()
        {
            base.OnEnable();
            _serializedProps = new Dictionary<string, SerializedProperty>();
            addProp(nameof(UGLMultiScreen.frankenOrthographicSize));
            addProp(nameof(UGLMultiScreen.frankenPerspectivePadding));
            addProp(nameof(UGLMultiScreen.frankenPerspectiveFOV));
            addProp(nameof(UGLMultiScreen.simpleGridCameraSpacing));
            addProp(nameof(UGLMultiScreen.cameraArrangementStyle));
            addProp(nameof(UGLMultiScreen.frankenOrthographicPadding));
        }

        void addProp(string name)
        {
            _serializedProps[name] = serializedObject.FindProperty(name);
        }

        void drawProp(string name)
        {
            var prop = _serializedProps.GetOrDefault(name, null);
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

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();


            GUILayout.Label("Screen Arangement");


            GUILayout.Label($"{script.nAssigned}/{N_MONITORS}");

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
                        script.arrangementGrid[idx] = nuVal;
                        changed = true;
                    }
                }
                GUILayout.EndHorizontal();

            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
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

            GUILayout.FlexibleSpace();



            GUILayout.FlexibleSpace();

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            //GUILayout.Label("Auto Arrange Cameras");
            _showArrange = EditorGUILayout.BeginFoldoutHeaderGroup(_showArrange, "Auto Arrange Cameras");
            if (_showArrange)
            {

                drawProp(nameof(UGLMultiScreen.cameraArrangementStyle));
                if (script.cameraArrangementStyle == CameraArrangementStyle.SimpleGrid)
                {
                    drawProp(nameof(UGLMultiScreen.simpleGridCameraSpacing));
                }
                else if (script.cameraArrangementStyle == CameraArrangementStyle.PerspectiveFrankenCam)
                {
                    drawProp(nameof(UGLMultiScreen.frankenPerspectiveFOV));
                    drawProp(nameof(UGLMultiScreen.frankenPerspectivePadding));
                }
                else if (script.cameraArrangementStyle == CameraArrangementStyle.SeamlessOrthographic)
                {
                    drawProp(nameof(UGLMultiScreen.frankenOrthographicSize));
                    drawProp(nameof(UGLMultiScreen.frankenOrthographicPadding));
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
        bool _defaultInspectorFoldout = false;
        bool _showArrange = false;
    }
#endif
}
