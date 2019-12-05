using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Structure_generic : NetworkBehaviour {
    public float size = 1;
    public float health = 100;

    public Team_watcher team = null;
    public GameObject spawn;
    public int alert_level = 0;
    float time_to_flush_alert = 0;
	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
        if (isServer)
        {
            if(Time.realtimeSinceStartup > time_to_flush_alert)
            {
                time_to_flush_alert = Time.realtimeSinceStartup + CONSTANTS.STRUCTURE_ALERT_FLUSH_INTERVAL;
                alert_level = 0;
            }
            if(health <= 0)
            {
                team.lose_base(this);
                NetworkServer.Destroy(gameObject);
            }
        }
	}

}
