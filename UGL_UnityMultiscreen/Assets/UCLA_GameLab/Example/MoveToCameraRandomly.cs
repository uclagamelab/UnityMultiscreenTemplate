using System.Collections;
using UnityEngine;
using XUUtils;

public class MoveToCameraRandomly : MonoBehaviour
{
    IEnumerator Start()
    {
        int randomCamIdx = 0;
        while (true)
        {
            randomCamIdx = randomCamIdx + Random.Range(1, UGLMultiScreen.Current.Cameras.Count);
            randomCamIdx %= UGLMultiScreen.Current.Cameras.Count;
            Vector3 start = this.transform.position;
            var randomCam = UGLMultiScreen.Current.Cameras[randomCamIdx].transform;
            var randomDest = randomCam.position + randomCam.forward;
            randomDest *= 5 / randomCam.forward.z;


            randomDest += Random.insideUnitCircle.asXyVector3().scaled(2, 0.75f / randomCam.forward.z, 0);

            yield return this.xuTween((t) => 
            {
                this.transform.position = Vector3.Lerp(start, randomDest, t);
            }, Random.Range(.5f, 1.5f));

            yield return new WaitForSeconds(Random.Range(.5f,.75f));
        }
    }
}
