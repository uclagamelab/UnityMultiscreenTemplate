using System.Collections;
using UnityEngine;
using XUUtils;

public class MoveToCameraRandomly : MonoBehaviour,  MultiScreenMouseExample.IClickable
{


    void Start()
    {
        this.StartCoroutine(MoveAroundRandomlyLoop());
    }

    IEnumerator MoveAroundRandomlyLoop()
    {
        int randomCamIdx = 0;
        while (true)
        {
            randomCamIdx = randomCamIdx + Random.Range(1, UGLMultiScreen.Current.Cameras.Count);
            randomCamIdx %= UGLMultiScreen.Current.Cameras.Count;
            Vector3 start = this.transform.position;
            var randomCam = UGLMultiScreen.Current.Cameras[randomCamIdx].transform;
            var randomDest = randomCam.position + randomCam.forward;
            randomDest *= 10 / randomCam.forward.z;


            randomDest += Random.insideUnitCircle.asXyVector3().scaled(2, 0.75f / randomCam.forward.z, 0);

            yield return this.xuTween((t) =>
            {
                this.transform.position = Vector3.Lerp(start, randomDest, t);
            }, Random.Range(.5f, 1.5f));

            yield return new WaitForSeconds(Random.Range(.5f, .75f));
        }
    }

    //Respond to clicks in MultiScreenMouseExample.cs
    public void Click() => UGLMultiScreen.Current.StartCoroutine(ClickRoutine());
    public IEnumerator ClickRoutine()
    {
        //stop moving around randomly coroutine
        this.StopAllCoroutines();

        //disable collider, to as to ignore multiple clicks
        this.GetComponent<Collider>().enabled = false;

        //animate scaling up (cheapo explosion)
        yield return this.xuTween((t) => { 
            this.transform.localScale = Vector3.one * Mathf.Lerp(1, 3, t);
        }, .15f);
        
        //when done, hide the object, and wait
        this.gameObject.SetActive(false);
        yield return new WaitForSeconds(5);

        //restore the default scale and unhide the object
        this.transform.localScale = Vector3.one;
        this.gameObject.SetActive(true);
        this.GetComponent<Collider>().enabled = true;

        //restart moving around randomly coroutine
        this.StartCoroutine(MoveAroundRandomlyLoop());

    }
}
