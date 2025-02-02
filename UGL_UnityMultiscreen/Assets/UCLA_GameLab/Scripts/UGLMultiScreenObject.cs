using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using XUUtils;

public class UGLMultiScreenObject : MonoBehaviour
{
    bool[] _intersectingGameViews = new bool[8];
    [SerializeField] Bounds localBounds = new Bounds(Vector3.zero, Vector3.one);
    public Renderer mainRenderer;

    Bounds worldBounds
    {
        get
        {
            if (mainRenderer != null) return mainRenderer.bounds;

            Bounds ret = new();
            //ret.Encapsulate(this.transform.rotation * localBounds.max);
            //ret.Encapsulate(this.transform.rotation * localBounds.min);
            var max = this.transform.rotation * localBounds.max;
            ret.Encapsulate(transform.rotation * localBounds.extents.scaled(1, 1, 1));
            ret.Encapsulate(transform.rotation * localBounds.extents.scaled(1, -1, 1));
            ret.Encapsulate(transform.rotation * localBounds.extents.scaled(1, -1, -1));
            ret.Encapsulate(transform.rotation * localBounds.extents.scaled(1, 1, -1));
            ret.Encapsulate(transform.rotation * localBounds.extents.scaled(-1, 1, 1));
            ret.Encapsulate(transform.rotation * localBounds.extents.scaled(-1, -1, 1));
            ret.Encapsulate(transform.rotation * localBounds.extents.scaled(-1, -1, -1));
            ret.Encapsulate(transform.rotation * localBounds.extents.scaled(-1, 1, -1));
            
            ret.center = this.transform.TransformPoint(localBounds.center);
            return ret;
        }
    }

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


#if UNITY_EDITOR
    [CustomEditor(typeof(UGLMultiScreenObject)), CanEditMultipleObjects]
    public class BoundsExampleEditor : Editor
    {
        private BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            UGLMultiScreenObject script = (UGLMultiScreenObject)target;
        }

        protected virtual void OnSceneGUI()
        {
            UGLMultiScreenObject script = (UGLMultiScreenObject)target;

            if (script.mainRenderer != null) return;
            // copy the target object's data to the handle

            var prevHandleMat = Handles.matrix;
            Handles.matrix = script.transform.localToWorldMatrix;
            m_BoundsHandle.center = script.localBounds.center;
            m_BoundsHandle.size = script.localBounds.size;

            // draw the handle
            EditorGUI.BeginChangeCheck();
            m_BoundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                // record the target object before setting new values so changes can be undone/redone
                Undo.RecordObject(script, "Change Bounds");

                // copy the handle's updated data back to the target object
                Bounds newBounds = new Bounds();
                newBounds.center = m_BoundsHandle.center;
                newBounds.size = m_BoundsHandle.size;
                script.localBounds = newBounds;
            }

            Handles.matrix = prevHandleMat;
        }
    }
#endif
}
