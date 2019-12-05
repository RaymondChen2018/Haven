using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Life : MonoBehaviour {
    public float life;
    public Animator[] animators;
    public int anim_param = 0;
    float time_to_destroy = 0;
	// Use this for initialization
	void Start () {

        time_to_destroy = Time.realtimeSinceStartup + life;
        if (animators != null && animators.Length != 0)
        {
            for(int i = 0; i < animators.Length; i++)
            {
                animators[i].SetInteger("anim_param", anim_param);
            }
        }
        
	}
	
	// Update is called once per frame
	void Update () {
        if (Time.realtimeSinceStartup > time_to_destroy)//timeLeft <= 0)
        {
            Destroy(gameObject);
        }
        /*
        else
        {
            timeLeft -= Time.deltaTime;
        }
        */
	}
}
