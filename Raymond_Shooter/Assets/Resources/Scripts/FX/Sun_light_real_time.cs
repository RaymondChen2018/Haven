using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sun_light_real_time : MonoBehaviour {
    public bool rotate_x = true;
    public bool rotate_y = false;
    public float update_interval = 1;
    public Gradient sun_gradient;
    public Gradient ambient_gradient;
    public Client_watcher client_watcher;
    private float time_to_update;
    public float sun_rotation_x;
    public float sun_rotation_y;
    public float sun_rotation_z;
	// Use this for initialization
	void Start () {
        time_to_update = 0;
        if (Server_watcher.Singleton.isServer)
        {
            update_sun_networked();
        }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
    }
	
	// Update is called once per frame
    
	void Update () {
        if (Server_watcher.Singleton.isServer && Time.time > time_to_update)
        {
            time_to_update += update_interval;
            update_sun_networked();
        }
	}

    public void update_sun_networked()
    {
        int time = System.DateTime.Now.Second + System.DateTime.Now.Minute * 60 + System.DateTime.Now.Hour * 3600;
        client_watcher.setSunTime((float)time / (float)86400);
    }
    public void update_sun(float ratio)
    {
        //Vector3 rot = transform.rotation.eulerAngles;
        Vector3 rot = Vector3.zero;
        if (rotate_x)
        {
            rot.x = ratio * 360;
            transform.eulerAngles = new Vector3(rot.x, sun_rotation_y, sun_rotation_z);
            //transform.rotation = Quaternion.Euler(rot.x, rotation_y, rotation_z);
        }
        else if (rotate_y)
        {
            rot.y = ratio * 360;
            transform.eulerAngles = new Vector3(sun_rotation_x, rot.y, sun_rotation_z);
            //transform.rotation = Quaternion.Euler(rot.x, rotation_y, rotation_z);
        }
        
        //Ambient calculation
        if (ratio <= ambient_gradient.colorKeys[0].time)
        {
            RenderSettings.ambientLight = ambient_gradient.colorKeys[0].color;
        }
        else if (ratio >= ambient_gradient.colorKeys[ambient_gradient.colorKeys.Length - 1].time)
        {
            RenderSettings.ambientLight = ambient_gradient.colorKeys[ambient_gradient.colorKeys.Length - 1].color;
        }
        else
        {

            for (int i = 0; i < ambient_gradient.colorKeys.Length; i++)
            {

                if (ratio == ambient_gradient.colorKeys[i].time)
                {
                    RenderSettings.ambientLight = ambient_gradient.colorKeys[i].color;
                    break;
                }
                else if (ratio < ambient_gradient.colorKeys[i].time)
                {
                    float lerp_ratio = (ratio - ambient_gradient.colorKeys[i - 1].time) / (ambient_gradient.colorKeys[i].time - ambient_gradient.colorKeys[i - 1].time);
                    RenderSettings.ambientLight = Color.Lerp(ambient_gradient.colorKeys[i - 1].color, ambient_gradient.colorKeys[i].color, lerp_ratio);
                    break;
                }
            }
        }
        //Sun light
        if (ratio <= sun_gradient.colorKeys[0].time)
        {
            GetComponent<Light>().color = sun_gradient.colorKeys[0].color;
        }
        else if (ratio >= sun_gradient.colorKeys[sun_gradient.colorKeys.Length - 1].time)
        {
            GetComponent<Light>().color = sun_gradient.colorKeys[sun_gradient.colorKeys.Length-1].color;
        }
        else
        {
            
            for (int i = 0; i < sun_gradient.colorKeys.Length; i++)
            {
                
                if (ratio == sun_gradient.colorKeys[i].time)
                {
                    GetComponent<Light>().color = sun_gradient.colorKeys[i].color;
                    break;
                }
                else if (ratio < sun_gradient.colorKeys[i].time)
                {
                    float lerp_ratio = (ratio - sun_gradient.colorKeys[i - 1].time) / (sun_gradient.colorKeys[i].time - sun_gradient.colorKeys[i - 1].time);
                    GetComponent<Light>().color = Color.Lerp(sun_gradient.colorKeys[i - 1].color, sun_gradient.colorKeys[i].color, lerp_ratio);
                    break;
                }
            }
        }
        

    }
}
