using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Logic_relay : Entity_generic {

    [SerializeField]
    public List<CONSTANTS.IO> I_O;
    // Use this for initialization
    void Start () {

        if (isServer)
        {
            enabled = false;
            return;
        }

        //

        Server_watcher.Singleton.onClientReady.Add(OnClientReady);
        
	}
	public void OnClientReady()
    {
        OnSpawn();

    }
    [ClientRpc]
    public void Rpc_trigger()
    {
        if (isServer)
        {
            return;
        }
        OnTrigger();
    } 

    //Inputs
    public void Trigger(bool includeClient = false)
    {
        OnTrigger();
        if (includeClient)
        {
            Rpc_trigger();
        }
        
    }
    //Outputs
    public void OnSpawn()
    {
        CONSTANTS.invokeOutput(CONSTANTS.OUTPUT_NAME.OnSpawn, I_O);
    }
    public void OnTrigger()
    {
        CONSTANTS.invokeOutput(CONSTANTS.OUTPUT_NAME.OnTrigger, I_O);
    }
}
