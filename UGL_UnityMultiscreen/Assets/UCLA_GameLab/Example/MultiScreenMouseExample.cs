using System.Collections.Generic;
using UnityEngine;
using XUUtils;
public class MultiScreenMouseExample : MonoBehaviour
{
    public float _cusorDistFromCam = 1.25f;
    public bool alignToCameraDirection = true;


    void Update()
    {
        Vector3 fixedMousePosition = Input.mousePosition;
        UGLSubCamera subCamera = UGLMultiScreen.Current.GetCameraForMousePosition(Input.mousePosition, out fixedMousePosition);

        if (subCamera != null)
        {
            //position the cursor
            var ray = subCamera.camera.ScreenPointToRay(fixedMousePosition);
            Vector3 mousePositionModifiedZ = fixedMousePosition;
            mousePositionModifiedZ.z = _cusorDistFromCam;
            var cursorWorldPos = subCamera.camera.ScreenToWorldPoint(mousePositionModifiedZ);
            this.transform.position = cursorWorldPos;

            //cameras may be facing in slightly different directions
            if (alignToCameraDirection)
            {
                this.transform.rotation = subCamera.transform.rotation;
            }

            //Do click actions on hit objects with script that implments 'IClickable'
            if (Input.GetMouseButtonDown(0))
            {
                if (Physics.Raycast(ray, out var hitInfo))
                {
                    var clickable = hitInfo.collider.GetComponent<IClickable>();
                    if (clickable != null)
                    {
                        clickable.Click();
                    }
                }
            }
        }


    }

    public interface IClickable
    {
        void Click();
    }

    //OK TO IGNORE---------
    ////Example code demonstrating accessing cameras by arrangement location
    //private void LateUpdate()
    //{
    //    UGLSubCamera subCamera = UGLMultiScreen.Current.GetCameraForScreenPoint(Input.mousePosition);
    //    if (subCamera != null)
    //    {
    //        var subCamArrangeLoc = subCamera.arrangementLocation;
    //        for (int xi = -6; xi <= 6; xi++)
    //        {
    //            for (int yi = -6; yi <= 6; yi++)
    //            {
    //                var neighboringSubCam = UGLMultiScreen.Current.GetCameraByArrangementLocation(subCamArrangeLoc.x + xi, subCamArrangeLoc.y + yi);
    //                if (neighboringSubCam != null)
    //                {
    //                    var bgColor = Color.Lerp(Color.yellow, Color.black, new Vector2(xi, yi).magnitude / 3f);
    //                    neighboringSubCam.camera.backgroundColor = bgColor;
    //                }
    //            }
    //        }
    //    }
    //}
}
