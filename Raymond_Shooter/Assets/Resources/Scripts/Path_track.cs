using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
//using UnityEditor;

public class Path_track : Entity_generic {
    public Path_track NextStopTarget;
    public float NewTrainSpeed = 0;

    [SerializeField]
    public List<CONSTANTS.IO> I_O;


    //Outputs
    public void OnPass()
    {
        CONSTANTS.invokeOutput(CONSTANTS.OUTPUT_NAME.OnPass, I_O);
    }
    

    //Only provide speed when train passes
    public void pass(Func_tracktrain train)
    {
        OnPass();
        train.setTrain(NextStopTarget, NewTrainSpeed);
    }

    /*
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Handles.Label(transform.position, gameObject.name);
        if (NextStopTarget == null)
        {
            return;
        }
        Gizmos.DrawLine(transform.position, NextStopTarget.transform.position);
    }
    */
}
