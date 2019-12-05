using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Logic_auto : Entity_generic {


    [SerializeField]
    public List<CONSTANTS.IO> I_O;

	// Use this for initialization
	void Start () {
        Server_watcher.Singleton.onClientReady.Add(OnMapSpawn);
        
	}
	
	// Update is called once per frame
	void Update () {
		
	}


    //Outputs
    [ServerCallback]
    public void OnMapSpawn()
    {
        CONSTANTS.invokeOutput(CONSTANTS.OUTPUT_NAME.OnMapSpawn, I_O);
    }
}
