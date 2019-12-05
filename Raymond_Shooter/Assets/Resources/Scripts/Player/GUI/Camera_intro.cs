using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.ImageEffects;
using UnityEngine.Networking;
public class Camera_intro : NetworkBehaviour {
    public float fade_intensity;
    public float fade_duration;
    private float fade_end_time;
    private float intro_viewsize;
    private float intro_viewsize_end;
    // Use this for initialization
    void Start () {
        if (!isLocalPlayer)
        {
            return;
        }
        Camera.main.GetComponent<ScreenOverlay>().enabled = true;
        Camera.main.GetComponent<ScreenOverlay>().intensity = fade_intensity;
        fade_end_time = Time.time + fade_duration;
        intro_viewsize = Camera.main.orthographicSize / 2;
        intro_viewsize_end = Camera.main.orthographicSize;
        Camera.main.orthographicSize = intro_viewsize;
    }
	
	// Update is called once per frame
	void Update () {
        if (!isLocalPlayer)
        {
            return;
        }
        if (Time.time < fade_end_time)
        {
            Camera.main.GetComponent<ScreenOverlay>().intensity = ((fade_end_time - Time.time) / fade_duration) * fade_intensity;
            Camera.main.orthographicSize = intro_viewsize_end - (fade_end_time - Time.time) / fade_duration * (intro_viewsize_end - intro_viewsize);
        }
        else
        {
            Camera.main.GetComponent<ScreenOverlay>().intensity = 0;
            Camera.main.orthographicSize = intro_viewsize_end;
            Destroy(this);
        }
    }
}
