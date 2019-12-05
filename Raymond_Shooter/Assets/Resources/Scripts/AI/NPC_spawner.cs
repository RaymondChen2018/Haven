using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NPC_spawner : NetworkBehaviour {
    public List<KeyValuePair<GameObject, int>> npc_template = new List<KeyValuePair<GameObject, int>>();
    public List<KeyValuePair<GameObject, int>> equip_template = new List<KeyValuePair<GameObject, int>>();
    public float spawn_limit = 50;
    public float spawn_interval = 5;
    public float ammo_drop_chance = 0.2f;
    public AI_generic.AI_condition npc_condition = AI_generic.AI_condition.IDLE;
    public int spawn_count = 0;
    private float time_to_spawn = 0;
    // Use this for initialization
    

    void Start () {
        //if (!isServer)
        //{
        //    Destroy(gameObject);
        //}
        time_to_spawn = spawn_interval ;
        
	}
	
	// Update is called once per frame
    [ServerCallback]
	void Update () {
		if(spawn_count < spawn_limit && Time.time > time_to_spawn && npc_template.Count > 0)
        {
            time_to_spawn = Time.time + spawn_interval;

            //Randomize npc type
            int seed_npc = Random.Range(0, 100);
            int index = 0;
            for(int i = 0; i < npc_template.Count; i++)
            {
                if (seed_npc > npc_template[i].Value)
                {
                    seed_npc -= npc_template[i].Value;
                }
                else
                {
                    index = i;
                    break;
                }
            }
            //Spawn npc
            GameObject npc = Instantiate(npc_template[index].Key, transform.position, transform.rotation);
            spawn_count++;
            //npc.GetComponent<Body_generic>().OnDeath = decrement;
            NetworkServer.Spawn(npc);

            //Spawn weapon
            npc.GetComponent<Body_generic>().OnRespawn = npc.GetComponent<AI_generic>().shopping;
            npc.GetComponent<AI_generic>().shopping();
            //assign_random_weapon(npc);
            npc.GetComponent<AI_generic>().set_ai_condition(npc_condition);
        }
	}
    
    public void assign_random_weapon(GameObject npc)
    {
        int index = 0;
        AI_generic _npc = npc.GetComponent<AI_generic>();
        _npc.ammo_drop_count = 0;
        if (equip_template != null && equip_template.Count > 0)
        {
            //Randomize weapon type
            int seed_equip = Random.Range(0, 100);
            for (int i = 0; i < equip_template.Count; i++)
            {
                if (seed_equip > equip_template[i].Value)
                {
                    seed_equip -= equip_template[i].Value;
                }
                else
                {
                    index = i;
                    break;
                }
            }
            /*
            //Spawn weapon
            Transform weapon_bone = npc.GetComponent<Body_generic>().weapon_bone.transform;
            GameObject equip = Instantiate(equip_template[index].Key, weapon_bone.position, weapon_bone.rotation);
            NetworkServer.Spawn(equip);
            Gun_generic gun = equip.GetComponent<Gun_generic>();
            //Give ammo type
            if (gun.ammo_template != null)
            {
                int seed = Random.Range(0, 100);
                if (seed < CONSTANTS.NPC_DROP_CHANCE * 100)
                {
                    _npc.ammo_drop_count = gun.ammo_template.GetComponent<Ammo_generic>().amount * 2;
                    
                }
                _npc.ammo_drop_template = gun.ammo_template;
            }
            //Npc pick up
            _npc.Pickup_item(equip);
            */
            //_npc.request_weapon(equip_template[index].Key);
        }
    }
    public void decrement()
    {
        spawn_count--;
    }


}
