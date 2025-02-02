using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using XUUtils;

public class UGLMultiScreenObject : MonoBehaviour
{
    //bool[] _intersectingGameViewsPrev = new bool[8];
    bool[] _intersectingGameViews = new bool[8];
    //int lastEnteredCam = -1;
    bool visibleToAny = false;
    public Renderer mainRenderer;
    List<int> _visibilityStack = new List<int>(8);

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
        refreshVisibilityInfo();
    }

    void refreshVisibilityInfo()
    {
        //for(int i = 0; i < _intersectingGameViews.Length; i++)
        //{
        //    _intersectingGameViews[0] = false;
        //}

        visibleToAny = false;
        foreach(var cam in UGLMultiScreen.Current.Cameras)
        {
            var camNumber = cam.cameraNumber;
            var prevVisible =_intersectingGameViews[camNumber];
            var nowVisible = cam.IsInView(worldBounds);
            
            visibleToAny |= nowVisible;

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
            }
        }
        
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
    }

    public IEnumerable<UGLSubCamera> GetAllIntersectingCameras()
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
