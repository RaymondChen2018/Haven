using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class Beacon_trigger : NetworkBehaviour {
    public GameObject owner;
    public Action action = null;
    public bool triggered = false;

    [ServerCallback]
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == owner)
        {
            triggered = true;
            if(action != null)
            {
                action.Invoke();
            }
            //disable target's control  client
            if(other.tag == "bot")
            {
                other.GetComponent<AI_generic>().enabled = false;
                //other.GetComponent<Interpolator_generic>().Rpc_interpolate(transform.position);
            }
            else if (other.tag == "Player")
            {
                //other.GetComponent<Shaded_controller>().Rpc_freeze();
                //other.GetComponent<Interpolator_generic>().Rpc_interpolate(transform.position);
            }
            GetComponent<BoxCollider2D>().enabled = false;
        }
    }
}
