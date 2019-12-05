using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Nav_dynamic : MonoBehaviour {
    public float update_interval = 0.5f;
    public float node_tolerance = 0.5f;
    private Navigation_manual nav;
    private float time_to_update = 0;

	// Use this for initialization
	void Awake () {
        nav = GetComponent<Navigation_manual>();
	}
	
	// Update is called once per frame
	void Update () {
        if(Time.time > time_to_update)
        {
            time_to_update = Time.time + update_interval;
            //update dynamic nodes' positions
            for(int i = 0; i < nav.nodes_dyn.Count; i++)
            {
                nav.nodes_dyn[i].position = nav.nodes_dyn[i].reference.transform.position;
            }
            //update neighboor
            for (int i = 0; i < nav.nodes.Length; i++)
            {
                nav.nodes[i].neighboor.Clear();
                nav.nodes[i].neighboor = nav.surround_nodes(nav.nodes[i].position, node_tolerance, 0.1f);
                
            }
        }
	}
}
