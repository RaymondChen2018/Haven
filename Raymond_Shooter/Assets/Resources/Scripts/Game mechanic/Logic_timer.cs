using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class Logic_timer : Entity_generic {
    public bool StartDisabled = false;
    public bool UseRandomTime = false;
    public float MinimumRandomInterval = 0;
    public float MaximumRandomInterval = 0;
    public float RefireInterval = 0;

    float time_prev = 0;
    float timeLeft = 1;
    bool mapSpawned = false;
    [SerializeField]
    public List<CONSTANTS.IO> I_O;
    // Use this for initialization
    void Start () {
        Server_watcher.Singleton.onClientReady.Add(OnClientReady);
        if (StartDisabled)
        {
            enabled = false;
        }
	}
	
	// Update is called once per frame
	void Update () {
        if (!mapSpawned)
        {
            return;
        }

        timeLeft -= Time.time - time_prev;
        time_prev = Time.time;
        //
        if(timeLeft <= 0)
        {
            //Fire
            OnTimer();
            refillTimer();
        }
	}
    void refillTimer()
    {
        if (UseRandomTime)
        {
            timeLeft = UnityEngine.Random.Range(MinimumRandomInterval, MaximumRandomInterval);
        }
        else
        {
            
            timeLeft = RefireInterval;
        }
    }

    public void OnClientReady()
    {
        mapSpawned = true;
    }
    //Inputs
    public void Enable()
    {
        time_prev = Time.time;
        refillTimer();
        enabled = true;
    }
    public void Disable()
    {
        enabled = false;
    }
    public void RefireTime(float timeInterval)
    {
        RefireInterval = timeInterval;
    }
    public void ResetTimer()
    {
        refillTimer();
    }
    public void LowerRandomBound(float timeMin)
    {
        MinimumRandomInterval = timeMin;
    }
    public void UpperRandomBound(float timeMax)
    {
        MaximumRandomInterval = timeMax;
    }
    

    //Outputs
    public void OnTimer()
    {
        CONSTANTS.invokeOutput(CONSTANTS.OUTPUT_NAME.OnTimer, I_O);
    }


}
