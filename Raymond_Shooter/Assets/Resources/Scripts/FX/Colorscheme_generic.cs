using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Colorscheme_generic : MonoBehaviour {
    public enum scheme_type
    {
        Light,
        Sprite
    }
    public Gradient colorScheme;
    public float timeSpan = 1;
    public bool unscaledTime = false;
    /// <summary>
    /// Initiate at the same time
    /// </summary>
    public bool serverAccurateSpawnTime = false;
    public scheme_type schemeType = scheme_type.Sprite; 

    float serverTimeOffset = 0;
    SpriteRenderer sprite;
    Light lt;
    float start_time = 0;
    Server_watcher cvar_watcher;
	// Use this for initialization
	void Start () {
		if(schemeType == scheme_type.Sprite)
        {
            sprite = GetComponent<SpriteRenderer>();
        }
        else if(schemeType == scheme_type.Light)
        {
            lt = GetComponent<Light>();
        }
        if (serverAccurateSpawnTime)
        {
            cvar_watcher = Server_watcher.Singleton;
        }
	}
	
	// Update is called once per frame
	void Update () {
        float time = 0;

        //Adjust according to server time
        if (serverAccurateSpawnTime)
        {
            time += cvar_watcher.serverMapSpawnTime - cvar_watcher.localMapSpawnTime;
        }

        //Propagate time
        if (unscaledTime)
        {
            time = (Time.realtimeSinceStartup % timeSpan) / timeSpan;
        }
        else
        {
            time = (Time.time % timeSpan) / timeSpan;
        }

        //Update color
		if(schemeType == scheme_type.Sprite)
        {
            sprite.color = colorScheme.Evaluate(time);
        }
        else if(schemeType == scheme_type.Light)
        {
            lt.color = colorScheme.Evaluate(time);
        }
	}
}
