using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI_sound : MonoBehaviour {
    public float range = 10;

    void Start()
    {
        if (!Server_watcher.Singleton.isServer)
        {
            Destroy(gameObject);
        }
    }


    //Inputs
    public void EmitSound()
    {
        Sound_watcher.Singleton.summon_listener(transform.position, range, gameObject.layer);
    }
    //Outputs

}
