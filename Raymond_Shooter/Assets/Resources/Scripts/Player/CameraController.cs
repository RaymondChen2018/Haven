using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

    private GameObject objToFollow = null;
    private float interpolationRatio = -1;

    Vector3 posVect;
    bool isInterpolating = false;
	// Use this for initialization
	void Start () {

    }
	
	// Update is called once per frame
	void Update () {
		if(objToFollow != null)
        {
            // Interpolate to object
            if (isInterpolating)
            {
                // Lerp position
                posVect = Vector3.Lerp(transform.position, objToFollow.transform.position, interpolationRatio);
                posVect.z = transform.position.z;
                transform.position = posVect;

                // Check if Interpolation Completes
                posVect = objToFollow.transform.position - transform.position;
                posVect.z = 0.0f;
                if (posVect.sqrMagnitude < 0.01f)
                {
                    isInterpolating = false;
                }
            }
            // Snap to object
            else
            {
                posVect = objToFollow.transform.position;
                posVect.z = transform.position.z;
                transform.position = posVect;
            }
        }
	}

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"> The Object to follow </param>
    /// <param name="interpolateSpeed"> How fast to interpolate to this object's position (fps-independant); 0.0f-1.0f;</param>
    public void SetObjectToFollow(GameObject obj, float interpolateRatio)
    {
        objToFollow = obj;
        if (interpolateRatio > 0 && interpolateRatio < 1)
        {
            isInterpolating = true;
            interpolationRatio = interpolateRatio;
        }
    }
}
