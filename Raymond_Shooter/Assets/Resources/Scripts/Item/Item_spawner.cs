using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Item_spawner : NetworkBehaviour {
    public string spawned_item;
    public bool spawn_at_start = true;
    public bool isAttachment = true;
    public int spawn_amount = 1;

    private Transform spawn_point;
    // Use this for initialization
    void Start () {
        if (spawn_at_start)
        {
            if (isServer)
            {
                if (!isAttachment)
                {
                    spawn_point = GetComponent<Transform>();
                    GameObject item_spawn = Instantiate(Resources.Load("Prefab/" + spawned_item) as GameObject, spawn_point.position, Quaternion.identity);
                    NetworkServer.Spawn(item_spawn);
                    NetworkServer.Destroy(gameObject);
                    return;
                }
            }
            Destroy(this);
        }
    }
    //Used for individual spawner
    [ServerCallback]
    void spawn()
    {
        if (spawn_amount == 0){
            NetworkServer.Destroy(gameObject);
            return;
        }
        spawn_point = GetComponent<Transform>();
        GameObject item_spawn = Instantiate(Resources.Load("Prefab/" + spawned_item) as GameObject, spawn_point.position, Quaternion.identity);
        NetworkServer.Spawn(item_spawn);
        spawn_amount--;
    }
}
