using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Interpolator_generic : MonoBehaviour {

    /// <summary>
    /// Distance closer than this will snap to the interpolate position
    /// </summary>
    public float snap_tolerance = 0.1f;
    /// <summary>
    /// Distance further than this will teleport instead of interpolate
    /// </summary>
    public float teleport_range = 10;
    Vector2 interpolate_to = Vector2.zero;
    public bool interpolateRotation = true;
    public float snap_rot_tolerance = 1.0f;
    public float interpolate_to_rot = 0;
    public float interpolate_ratio = 0.1f;
    public float interpolaterot_ratio = 0.1f;


    //public float interpolate_force = 5;

    public bool interpolated = true;
    public bool interpolated_rot = true;
    Rigidbody2D thisRB;

    /*
    [HideInInspector]
    public Vector2 interpolate_point;
    [HideInInspector]
    public Vector2 interpolate_from_point;
    private Rigidbody2D RB;
    */
    void Start()
    {
        thisRB = GetComponent<Rigidbody2D>();    
    }
    public void interpolate_x(float x)
    {
        interpolated = false;
        interpolate_to.x = x - transform.position.x;
    }
    public void interpolate_y(float y)
    {
        interpolated = false;
        interpolate_to.y = y - transform.position.y;
    }
    public void interpolate_rot(float rot)
    {
        interpolated_rot = false;
        interpolate_to_rot = rot;
    }
    public void interpolate(Vector2 position)
    {
        interpolated = false;
        interpolate_to = position - (Vector2)transform.position;
    }
    
	void Update () {

        if(interpolated)
        {
            
        }
        else if(interpolate_to.magnitude <= snap_tolerance || interpolate_to.magnitude > teleport_range)
        {
            interpolated = true;
            if (thisRB == null)
            {
                transform.position += (Vector3)interpolate_to;
            }
            else
            {
                thisRB.position += interpolate_to;
                transform.position = thisRB.position;
            }
            interpolate_to = Vector2.zero;
        }
        else//snap to
        {

            if (thisRB == null)
            {
                transform.position += (Vector3)interpolate_to * interpolate_ratio;
            }
            else
            {

                thisRB.position = thisRB.position + interpolate_to * interpolate_ratio;
                transform.position = thisRB.position;

            }
            interpolate_to *= (1 - interpolate_ratio);
            
        }

        //Interpolate rotation
        if (interpolateRotation && !interpolated_rot)
        {
            if (Mathf.DeltaAngle(thisRB.rotation, interpolate_to_rot) <= snap_rot_tolerance)
            {
                interpolated_rot = true;
                thisRB.MoveRotation(interpolate_to_rot);
            }
            else
            {
                thisRB.MoveRotation(Mathf.LerpAngle(thisRB.rotation, interpolate_to_rot, interpolaterot_ratio));
            }
        }

        /*
        if (interpolated)
        {
            float from = Vector2.Distance(interpolate_point, transform.position);
            float dist = Vector2.Distance(interpolate_point, interpolate_from_point);
            if (from > 0.01)
            {
                RB.velocity = (interpolate_point - (Vector2)transform.position).normalized * from / dist * interpolate_force;
            }
            else
            {
                RB.velocity = Vector2.zero;
                transform.position = interpolate_point;
                interpolated = false;
            }
        }
        */
    }

   
}


