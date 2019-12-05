using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gunfx_remover : MonoBehaviour {
    private ParticleSystem particle;
    private Light light;
    public SpriteRenderer glow;
    public float life = 1;
    public bool isParticle = false;
    public bool isAudio = false;
    public bool count_life = false;
    public bool hasLight = false;
    float time_to_destroy = 0;
    float spawn_time = 0;
	// Use this for initialization
	void Start () {
        spawn_time = Time.time;
        if (isParticle)
        {
            particle = GetComponent<ParticleSystem>();
        }
        if (hasLight)
        {
            light = GetComponent<Light>();
        }
        if (count_life)
        {
            time_to_destroy = Time.time + life;
        }
        else if (isAudio)
        {
            AudioSource audio = GetComponent<AudioSource>();
            audio.pitch = Time.timeScale;
            if(audio.clip == null)
            {
                isAudio = false;
            }
            else{
                time_to_destroy = Time.time + GetComponent<AudioSource>().clip.length;
            }
        }
        
    }

    // Update is called once per frame
    void Update () {
        if(glow != null)
        {
            Color temp = glow.color;
            temp.a *= particle.time;
            glow.color = temp;
        }
        if(hasLight)
        {
            light.intensity = Mathf.Max(0, 1 - (Time.time - spawn_time) / particle.main.startLifetime.constantMax);
            //Debug.Log("time: "+Time.time+"; spawn: "+spawn_time);
        }
        if (count_life || isAudio)
        {
            if (Time.time > time_to_destroy)//timeLeft <= 0)
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
        else if (isParticle && !particle.IsAlive())
        {
            Destroy(gameObject);
        }
        
        

	}
}
