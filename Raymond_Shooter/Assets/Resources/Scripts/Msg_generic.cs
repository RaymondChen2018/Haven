using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Msg_generic : MonoBehaviour {
    public int visiable_interval = 5;
    public int fade_interval = 1;
    float time_to_fade = 0;
    float time_to_hide = 0;
    Text text;
    Color alpha = Color.white;
    bool stayOn = false;
	// Use this for initialization
	void Start () {
		time_to_fade = Time.time + visiable_interval;
        time_to_hide = Time.time + visiable_interval + fade_interval;
        text = GetComponent<Text>();
        alpha = text.color;
	}
    /// <summary>
    /// wake up without fading
    /// </summary>
	public void wake()
    {
        enabled = true;
        stayOn = true;
        alpha.a = 1;
        text.color = alpha;
    }
    public void off()
    {
        stayOn = false;

    }
	// Update is called once per frame
	void Update () {
        if (stayOn)
        {
            return;
        }
		if(Time.time > time_to_hide)
        {
            alpha.a = 0;
            text.color = alpha;
            enabled = false;
        }
        else if(Time.time > time_to_fade)
        {
            alpha.a = Mathf.Lerp(1, 0, (Time.time - time_to_fade)/fade_interval);
            text.color = alpha;
        }
	}
}
