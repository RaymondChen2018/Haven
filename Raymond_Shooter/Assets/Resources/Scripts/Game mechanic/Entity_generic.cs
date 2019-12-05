using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Entity_generic : NetworkBehaviour {
    [SerializeField] private string classname;
    public string targetname;

    protected Server_watcher cvar_watcher;


	// Use this for initialization
	void Start () {
		cvar_watcher = Server_watcher.Singleton;
        cvar_watcher.add_entity(gameObject);
        if(targetname != "")
        {
            name = targetname;
        }
	}
    [ServerCallback]
    public void Kill()
    {
        NetworkServer.Destroy(gameObject);
    }
}
