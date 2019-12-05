using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Green_arrowpath : MonoBehaviour {
    public float scroll_speed;
    public float flicker_interval = -1;
    public float flicker_alpha_min = 0;


    private Material mat;
    private Vector2 offset;
    public float time_cycle = 0;

    // Use this for initialization
    void Start () {
        mat = GetComponent<LineRenderer>().material;
        offset = new Vector2(0,0);
	}
	
	// Update is called once per frame
	void Update () {
        
        offset.x = - Time.time * scroll_speed;
        if(offset.x >= 1)
        {
            offset.x = 0;
        }
        mat.mainTextureOffset = offset;
        if(flicker_interval != -1)
        {
            if(Time.time > time_cycle + flicker_interval)
            {
                time_cycle = Time.time;
            }
            Color color = GetComponent<LineRenderer>().startColor;
            if (Time.time - time_cycle < flicker_interval / 2)
            {               //alpha_interval * 
                color.a = (1 - flicker_alpha_min) * (Time.time - time_cycle) / (flicker_interval / 2) + flicker_alpha_min;
            }
            else
            {
                color.a = (1 - flicker_alpha_min) * (1-(Time.time - time_cycle - flicker_interval / 2) / (flicker_interval / 2)) + flicker_alpha_min;
            }
            GetComponent<LineRenderer>().startColor = color;
            GetComponent<LineRenderer>().endColor = color;
        }
    }
    
}
