using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using XUUtils;

public class UGLMultiScreenObject : MonoBehaviour
{
    bool[] _intersectingGameViews = new bool[8];
    public Renderer mainRenderer;

    Bounds worldBounds => mainRenderer != null ? mainRenderer.bounds : new Bounds(this.transform.position, Vector3.one);
        
    
    private void Update()
    {
        refreshVisibilityInfo();
    }

    void refreshVisibilityInfo()
    {
        for(int i = 0; i < _intersectingGameViews.Length; i++)
        {
            _intersectingGameViews[0] = false;
        }

        foreach(var cam in UGLMultiScreen.Current.Cameras)
        {

            _intersectingGameViews[cam.cameraNumber] = cam.IsInView(worldBounds);
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
