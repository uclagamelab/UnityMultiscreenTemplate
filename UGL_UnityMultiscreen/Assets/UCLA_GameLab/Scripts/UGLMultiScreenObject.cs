using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using XUUtils;
using UnityEngine.Events;

[RequireComponent(typeof(UGLMultiScreenObjectEvents))]
public class UGLMultiScreenObject : MonoBehaviour
{
    bool[] _intersectingCamerasPrev = new bool[8];
    bool[] _intersectingCameras = new bool[8];
    //int lastEnteredCam = -1;
    public bool isVisibleToAnyCamera { get; private set; } = false;
    public Renderer mainRenderer;
    List<int> _visibilityStack = new List<int>(8);
    int _lastCalcTime = -1;

    //public delegate void OnEnterCameraChangeDelegate(UGLSubCamera camera, bool entered);
    public delegate void OnAnyEnterCameraChangeDelegate(EnterChangeInfo info);
    public static event OnAnyEnterCameraChangeDelegate OnAnyEnterCameraChange = (info) => { };
    public event OnAnyEnterCameraChangeDelegate OnEnterCameraChange = (info) => { };
    public struct EnterChangeInfo
    {
        public UGLSubCamera camera;
        public UGLMultiScreenObject obj;
        public bool entered;
        public UGLSubCamera previousExclusiveCamera;
        bool[] intersectingCamerasPrev;

        public EnterChangeInfo(UGLSubCamera camera, UGLMultiScreenObject obj, bool entered, UGLSubCamera previousExclusiveCamera, bool[] prevVisibility) : this()
        {
            this.camera = camera;
            this.obj = obj;
            this.entered = entered;
            this.previousExclusiveCamera = previousExclusiveCamera;
            this.intersectingCamerasPrev = prevVisibility;
        }
        public bool visibleToCameraPreviously(int camNumber)
        {
            return intersectingCamerasPrev.GetOrDefault(camNumber);
        }
    }

    /// <summary>
    /// The camera that the object most recently entered.
    /// </summary>
    public int lastEnteredCamera => _visibilityStack.GetOrDefault(_visibilityStack.Count - 1, lastVisibleCamera);
    public int lastVisibleCamera
    {
        get;
        private set;
    }

    Bounds worldBounds => mainRenderer != null ? mainRenderer.bounds : new Bounds(this.transform.position, Vector3.one);

    private void Start()
    {
        refreshVisibilityInfo();
    }

    private void Update()
    {
        refreshVisibilityInfo(true);
    }

    void refreshVisibilityInfo(bool doCallbacks = false)
    {
        //if (Time.frameCount == _lastCalcTime)
        //{
        //    return;
        //}
        var prevExclusiveCam = exclusiveCamera;
        for (int i = 0; i < _intersectingCameras.Length; i++)
        {

            this._intersectingCamerasPrev[i] = _intersectingCameras[i];
        }

        _lastCalcTime = Time.frameCount;
        isVisibleToAnyCamera = false;
        foreach (var cam in UGLMultiScreen.Current.Cameras)
        {
            var camNumber = cam.cameraNumber;
            var prevVisible = _intersectingCameras[camNumber];
            var nowVisible = cam.IsInView(worldBounds);

            isVisibleToAnyCamera |= nowVisible;

            if (prevVisible != nowVisible)
            {

                _intersectingCameras[camNumber] = nowVisible;

                if (_visibilityStack.Contains(camNumber))
                {
                    _visibilityStack.Remove(camNumber);
                    if (_visibilityStack.Count == 0)
                    {
                        lastVisibleCamera = camNumber;
                    }
                }

                if (nowVisible)
                {
                    lastVisibleCamera = camNumber;
                    _visibilityStack.Add(camNumber);
                }

                if (doCallbacks)
                {
                    EnterChangeInfo info = new (cam, this, nowVisible, prevExclusiveCam, _intersectingCamerasPrev);
                    this.OnEnterCameraChange(info);
                    OnAnyEnterCameraChange(info);
                }
            }
        }

    }

    //private void OnDrawGizmosSelected()
    //{
    //    Gizmos.color = Color.magenta;
    //    Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
    //}

    public bool isVisibleToCamera(UGLSubCamera cam) => isVisibleToCamera(cam.cameraNumber);
    public bool isVisibleToCamera(int cameraNumber)
    {
        return _intersectingCameras.GetOrDefault(cameraNumber, false);
    }

    public int nIntersectingCameras
    {
        get
        {
            getExclusiveCamera(out int nIntersectingCameras);
            return nIntersectingCameras;
        }
    }


    /// <summary>
    /// If the object is visible to 
    /// </summary>
    /// <returns></returns>
    public UGLSubCamera exclusiveCamera
    {
        get => getExclusiveCamera(out int dontCare);
    }
   

    UGLSubCamera getExclusiveCamera(out int nIntersectingCameras)
    {
        UGLSubCamera ret = null;
        nIntersectingCameras = 0;
        for (int i = 0; i < _intersectingCameras.Length; i++)
        {
            if (_intersectingCameras[i])
            {
                ret = UGLMultiScreen.Current.GetCameraByNumber(i);
                nIntersectingCameras++;
            }
        }
        return nIntersectingCameras == 1 ? ret : null;
    }

    public IEnumerable<UGLSubCamera> getAllIntersectingCameras()
    {
        for (int i = 0; i < _intersectingCameras.Length; i++)
        {
            if (_intersectingCameras[i])
            {
                yield return UGLMultiScreen.Current.GetCameraByNumber(i);
            }
        }
    }
}
