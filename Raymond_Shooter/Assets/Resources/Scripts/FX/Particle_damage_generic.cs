using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle_damage_generic : MonoBehaviour {
    public float dmg_physics = 0;
    public float dmg_thermal = 0;
    public GameObject activator = null;
	// Use this for initialization
	void Start () {
        
	}
    void OnTriggerStay2D(Collider2D collision)
    {
        //if ( (layer_value & 1<<collision.gameObject.layer) != 0)
        //{
        if (collision.GetComponent<Body_hitbox_generic>()!=null)
        {
            collision.GetComponent<Body_hitbox_generic>().body.damage(activator, Vector2.zero, dmg_physics, dmg_thermal);
        }
        else if (collision.GetComponent<Bullseye_generic>()!=null)
        {
            collision.GetComponent<Bullseye_generic>().damage(activator, dmg_physics, dmg_thermal);
        }
        //}
    }
}
