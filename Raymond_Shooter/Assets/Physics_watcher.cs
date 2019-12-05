using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Physics_watcher : NetworkBehaviour {

    static public Physics_watcher Singleton;
    
    List<Prop_physics_multiplayer> props_active = new List<Prop_physics_multiplayer>();
    List<Prop_physics_multiplayer> props_inactive = new List<Prop_physics_multiplayer>();

    // Use this for initialization
    void Start () {
	    Singleton = this;
	}
	/*
	// Update is called once per frame
	void Update () {
		for(int i = 0; i < props_inactive.Count; i++)
        {
            if (!props_inactive[i].propRB.IsSleeping())
            {
                props_active.Add(props_inactive[i]);
                props_inactive.RemoveAt(i);
                i++;
            }  
        }
        for (int i = 0; i < props_active.Count; i++)
        {
            if (props_active[i].propRB.IsSleeping())
            {
                props_active.RemoveAt(i);
                i++;
                continue;
            }


            //Server end
            if (isServer)
            {
                //Sync
                sync_prop(props_active[i]);
            }
            else
            {

            }
            

        }
        
    }
    */
    void sync_prop(Prop_physics_multiplayer prop)
    {

    }
    public void register_prop(Prop_physics_multiplayer prop)
    {
        props_active.Add(prop);
    }
    public void Kill(Prop_physics_multiplayer prop)
    {
        props_active.Remove(prop);
    }

}
