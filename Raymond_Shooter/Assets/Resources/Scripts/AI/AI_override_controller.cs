using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AI_override_controller : NetworkBehaviour {
    public GameObject goal;
    public Transform face_to;
    public bool cam_follow = false;
    public Transform sprite_orient;
    private float walkforce;
    private Rigidbody2D playerRB;


	// Use this for initialization
	void Start () {
        playerRB = GetComponent<Rigidbody2D>();

        
        if(isLocalPlayer)
        {
            walkforce = GetComponent<Body_generic>().speed_run;
        }
        else
        {
            enabled = false;
        }
	}
	
	// Update is called once per frame
	void Update () {

        if(goal != null)
        {
            playerRB.AddForce((goal.transform.position - transform.position).normalized * walkforce);
            if (Vector2.Distance(goal.transform.position, transform.position) < 0.1f)
            {
                goal = goal.GetComponent<Node_track>().next;
            }
        }
        if(face_to != null)
        {
            float face_angle;
            Vector2 face_vec = face_to.position - transform.position;
            face_angle = Mathf.Atan2(face_vec.y, face_vec.x) * 180 / Mathf.PI;
            sprite_orient.transform.eulerAngles = new Vector3(playerRB.transform.eulerAngles.x, playerRB.transform.eulerAngles.y, face_angle);
        }
        else if(face_to == null && goal != null)
        {
            float face_angle;
            Vector2 face_vec = goal.transform.position - transform.position;
            face_angle = Mathf.Atan2(face_vec.y, face_vec.x) * 180 / Mathf.PI;
            sprite_orient.transform.eulerAngles = new Vector3(playerRB.transform.eulerAngles.x, playerRB.transform.eulerAngles.y, face_angle);
        }
        if (cam_follow)
        {
            Camera.main.transform.position = new Vector3(playerRB.position.x, playerRB.position.y, Camera.main.transform.position.z);
        }
    }
    [ClientRpc]
    public void Rpc_face_to(GameObject target)
    {
        if(target != null)
        {
            face_to = target.transform;
        }
        else
        {
            face_to = null;
        }
    }
    [ClientRpc]
    public void Rpc_direct(GameObject target)
    {
        goal = target;
    }
}
