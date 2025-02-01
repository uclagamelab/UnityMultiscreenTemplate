using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.Mathematics;
using XUUtils;

public class UGLMultiScreen : MonoBehaviour
{
    static UGLMultiScreen _I; 
    public static UGLMultiScreen I 
    { 
        get
        {
            if(_I == null) _I = GameObject.FindFirstObjectByType<UGLMultiScreen>();
            return _I;
        }
    }

    const int N_MONITORS = 6;
    int2[] arrangement = { new int2(0, 0), };
    [SerializeField] bool[] _arrangementGrid = null;

    static readonly Vector2 SUBSCREEN_ASPECT_RATIO_VEC = new Vector2(4, 3);
    const float SUBSCREEN_H_O_W = 3f / 4f; //Height over width (aspect ratio of monitors)
    public List<UGLSubCamera> Cameras = new();

    public bool inSimulationMode = false;
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


    void Awake()
    {
        
    }

    private void OnDestroy()
    {
        if (_I == this) _I = null;
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            UGLMultiScreenAdminPanel.AdminPanelsOpen = !UGLMultiScreenAdminPanel.AdminPanelsOpen;
        }
    }

    public float cameraSpacing = 2;
    void ArrangeCameras()
    {
        this.GetArrangementExtents(out var arrangementSize);
        Vector3 offset = new Vector3(arrangementSize.x - 1, arrangementSize.y - 1, 0) * .5f;

        for (int i = 0; i < N_MONITORS; i++)
        {
            var cam = GetCamera(i);
            Vector3 arrangeLocation = this.getArrangementLocation(i+1).asXyVector3() - offset;
            arrangeLocation.y *= -1;
            cam.transform.localPosition = cameraSpacing * Vector3.Scale(arrangeLocation, new Vector3(1, SUBSCREEN_H_O_W));
        }
    }

    public UGLSubCamera GetCamera(int i)
    {
        return this.Cameras[i];
    }


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
    public class Ed : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var script = target as UGLMultiScreen;

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
                        EditorUtility.SetDirty(script);
                    }
                }
                GUILayout.EndHorizontal();

            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            if (GUILayout.Button("Toggle Sim Mode"))
            {
                script.SetSingleScreenSimulationMode(!script.inSimulationMode);
            }

            if (GUILayout.Button("Refresh Game View"))
            {
                script.SetSingleScreenSimulationMode(script.inSimulationMode);
            }

            if (GUILayout.Button("Arrange Cameras"))
            {
                script.ArrangeCameras();
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
#endif
}
