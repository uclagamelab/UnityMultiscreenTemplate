using XUUtils;
using UnityEngine;
using UnityEngine.Events;
using NaughtyAttributes;

public class UGLMultiScreenObjectEvents : MonoBehaviour
{

    [Header("Camera Enter/Exit Events")]
    public EventGroup Camera0 = new();
    public EventGroup Camera1 = new();
    public EventGroup Camera2 = new();
    public EventGroup Camera3 = new();
    public EventGroup Camera4 = new();
    public EventGroup Camera5 = new();
    public EventGroup AnyCamera = new();

    [Space(5)]
    [Foldout("Visibility Change Events")]
    public UnityEvent OnBecomeVisible = new();
    [Foldout("Visibility Change Events")]
    public UnityEvent OnBecomeInvisible = new();

    [Foldout("Advanced Options")]
    public bool enterDefaultSimpleCallback = true;
    [Foldout("Advanced Options")]
    public bool exitDefaultSimpleCallback = true;
    [Foldout("Advanced Options")]
    public bool enterOnGainExclusiveView = false;
    [Foldout("Advanced Options")]
    public bool exitOnLoseExclusiveView = false;

    EventGroup[] _OnEnterEvents;

    [System.Serializable]
    public class EventGroup
    {
        bool entered = false;
        public UnityEvent onEnter = new();
        public UnityEvent onExit = new();
        public EventGroup()
        {

        }
        public EventGroup(UnityEvent onEnter, UnityEvent onExit)
        {
            this.onEnter = onEnter;
            this.onExit = onExit;
        }

        public void SetEnteredLatched(bool entered)
        {
            if (this.entered != entered)
            {
                this.entered = entered;
                (entered ? onEnter : onExit).SafeInvoke();
            }
        }
    }

    bool visibleToAny = false;

    void Start()
    {
        var mso = this.GetComponent<UGLMultiScreenObject>();
        
        if (mso == null) 
            mso = this.GetComponentInParent<UGLMultiScreenObject>();
        
        mso.OnEnterCameraChange += onEnterCameraChange;
        _OnEnterEvents = new EventGroup[] {Camera0, Camera1, Camera2, Camera3, Camera4, Camera5};
    }


    private void onEnterCameraChange(UGLMultiScreenObject.EnterChangeInfo info)
    {
        bool entered = info.entered;
        bool previouslyVisible = visibleToAny;
        visibleToAny = info.obj.isVisibleToAnyCamera;


        //if (enterOnGainExclusiveView)
        //{
        //    if (info.previousExclusiveCamera != info.obj.exclusiveCamera && info.obj.exclusiveCamera != null)
        //    {
        //        var camGroup = _OnEnterEvents[info.obj.exclusiveCamera.cameraNumber];
        //        camGroup.onEnter.Invoke();
        //    }
        //}

        //if (exitOnLoseExclusiveView)
        //{
        //    if ((info.previousExclusiveCamera != info.obj.exclusiveCamera && info.previousExclusiveCamera != null))
        //    {
        //        var camGroup = _OnEnterEvents[info.previousExclusiveCamera.cameraNumber];
        //        camGroup.onExit.Invoke();
        //    }
        //    if (!enterOnGainExclusiveView && !entered && info.previousExclusiveCamera == null && info.visibleToCameraPreviously(info.camera.cameraNumber))
        //    {
        //        var camGroup = _OnEnterEvents[info.camera.cameraNumber];
        //        camGroup.onExit.Invoke();
        //    }
        //    if (!enterOnGainExclusiveView && !entered && info.previousExclusiveCamera == null && info.obj.exclusiveCamera != null)// && info.visibleToCameraPreviously(info.camera.cameraNumber))
        //    {
        //        var camGroup = _OnEnterEvents[info.camera.cameraNumber];
        //        camGroup.onExit.Invoke();
        //    }
        //}


   
        if (info.entered && enterDefaultSimpleCallback || !info.entered && exitDefaultSimpleCallback)
        {
            var camGroup = _OnEnterEvents[info.camera.cameraNumber];
            camGroup.SetEnteredLatched(info.entered);

            (entered ? camGroup.onEnter : camGroup.onExit).SafeInvoke();
            (entered ? AnyCamera.onEnter : AnyCamera.onExit).SafeInvoke();
        }

        if (info.previousExclusiveCamera != info.obj.exclusiveCamera)
        {
            if (enterOnGainExclusiveView && info.obj.exclusiveCamera != null)
            {
                _OnEnterEvents[info.obj.exclusiveCamera.cameraNumber].SetEnteredLatched(true);
                AnyCamera.onEnter.SafeInvoke();
            }

            if (exitOnLoseExclusiveView && info.previousExclusiveCamera != null)
            {
                _OnEnterEvents[info.previousExclusiveCamera.cameraNumber].SetEnteredLatched(false);
                AnyCamera.onExit.SafeInvoke();
            }
        }

        if (entered && !previouslyVisible)
        {
            OnBecomeVisible.SafeInvoke();
        }
        else if (!entered && previouslyVisible)
        {
            OnBecomeInvisible.Invoke();
        }


    }
}
