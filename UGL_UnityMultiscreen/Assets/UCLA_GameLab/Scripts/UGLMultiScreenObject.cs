using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using XUUtils;
using UnityEngine.Events;

public class UGLMultiScreenObject : MonoBehaviour
{
    //bool[] _intersectingGameViewsPrev = new bool[8];
    bool[] _intersectingGameViews = new bool[8];
    //int lastEnteredCam = -1;
    public bool isVisibleToAnyCamera { get; private set; } = false;
    public Renderer mainRenderer;
    List<int> _visibilityStack = new List<int>(8);
    int _lastCalcTime = -1;

    //public delegate void OnEnterCameraChangeDelegate(UGLSubCamera camera, bool entered);
    public delegate void OnAnyEnterCameraChangeDelegate(UGLSubCamera camera, UGLMultiScreenObject obj, bool entered);
    public static event OnAnyEnterCameraChangeDelegate OnAnyEnterCameraChange = (Camera, obj, enterer) => { };
    public event OnAnyEnterCameraChangeDelegate OnEnterCameraChange = (Camera, obj, enterer) => { };


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

        _lastCalcTime = Time.frameCount;
        isVisibleToAnyCamera = false;
        foreach(var cam in UGLMultiScreen.Current.Cameras)
        {
            var camNumber = cam.cameraNumber;
            var prevVisible =_intersectingGameViews[camNumber];
            var nowVisible = cam.IsInView(worldBounds);
            
            isVisibleToAnyCamera |= nowVisible;

            if (prevVisible != nowVisible) 
            {
           
                _intersectingGameViews[camNumber] = nowVisible;

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
                    this.OnEnterCameraChange(cam, this, nowVisible);
                    OnAnyEnterCameraChange(cam, this, nowVisible);
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
        return _intersectingGameViews.GetOrDefault(cameraNumber, false);
    }

    public IEnumerable<UGLSubCamera> getAllIntersectingCameras()
    {
        for (int i = 0; i < _intersectingGameViews.Length; i++)
        {
            if (_intersectingGameViews[i])
            {
                yield return UGLMultiScreen.Current.GetCamera(i);
            }
        }
    }
}
