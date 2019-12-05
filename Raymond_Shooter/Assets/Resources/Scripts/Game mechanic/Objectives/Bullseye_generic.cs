using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;

public class Bullseye_generic : NetworkBehaviour {
    public struct argument
    {
        public GameObject activator;
        public GameObject bullseye;
    };
    public float health;
    public Action<argument> action = null;


    public void damage(GameObject activator, float dmg_physics = 0, float dmg_thermal = 0)
    {
        if (activator != null && !activator.GetComponent<Body_generic>().dmg_tags.Contains(gameObject.tag))//the activator is not hostile against the bullseye, disallow damage
        {
            return;
        }
        //damage calculation
        if (dmg_physics != 0)
        {
            health -= dmg_physics;
        }
        if(dmg_thermal != 0)
        {
            health -= dmg_thermal / 100;
        }
        //death
        if(health <= 0)
        {
            Die(activator);
        }
    }
    
    public void Die(GameObject activator)
    {
        if (action != null)
        {
            argument arg;
            arg.activator = activator;
            arg.bullseye = gameObject;
            action.Invoke(arg);
        }
        NetworkServer.Destroy(gameObject);
    }
}
