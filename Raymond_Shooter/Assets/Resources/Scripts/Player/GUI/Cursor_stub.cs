using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Cursor_stub : NetworkBehaviour {
    [SyncVar] public float cursor_size = 0;

    // Use this for initialization
    void Start () {
        
    }
	
	// Update is called once per frame
	void Update () {
        /**
        if (cursor_size == -1)
        {
            transform.localScale = new Vector3(0, 0, 0);
            return;
        }
        transform.localScale = new Vector3(1, 1, 1);
        transform.position = new Vector3(Camera.main.ScreenToWorldPoint(Input.mousePosition).x, Camera.main.ScreenToWorldPoint(Input.mousePosition).y, 0);
        float cam_scale = GameObject.Find("Main Camera").GetComponent<Camera>().orthographicSize / 10;
        transform.FindChild("cursor_l").localScale = new Vector3(0.3f * cam_scale, 0.03f * cam_scale, 1);
        transform.FindChild("cursor_r").localScale = new Vector3(0.3f * cam_scale, 0.03f * cam_scale, 1);
        transform.FindChild("cursor_u").localScale = new Vector3(0.3f * cam_scale, 0.03f * cam_scale, 1);
        transform.FindChild("cursor_d").localScale = new Vector3(0.3f * cam_scale, 0.03f * cam_scale, 1);
        transform.FindChild("cursor_l").localPosition = new Vector3(- cursor_size - 0.3f, 0, 0);
        transform.FindChild("cursor_r").localPosition = new Vector3(cursor_size + 0.3f, 0, 0);
        transform.FindChild("cursor_u").localPosition = new Vector3(0, cursor_size + 0.3f, 0);
        transform.FindChild("cursor_d").localPosition = new Vector3(0, - cursor_size - 0.3f, 0);
    */
    }
}
