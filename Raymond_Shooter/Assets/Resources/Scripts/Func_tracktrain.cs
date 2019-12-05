using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Func_tracktrain : Entity_generic {
    public float InitialSpeed = 0;
    public float MaxSpeed = 0;
    public Path_track FirstStopTarget;
    Path_track destination;
    float speed;
    float passTime = 0;
	// Use this for initialization
	void Start () {
        if (!isServer)
        {
            Destroy(this);
        }
        speed = InitialSpeed;
        transform.position = FirstStopTarget.transform.position;
        FirstStopTarget.pass(this);
	}
	
	// Update is called once per frame
	void Update () {
        if(destination == null)
        {
            return;
        }
        float distance = Vector3.Distance(transform.position, destination.transform.position);
        float distCovered = (Time.time - passTime) * speed;
        float fracJourney = distCovered / distance;

        if(fracJourney >= 1)
        {
            destination.pass(this);
            transform.position = destination.transform.position;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, destination.transform.position, fracJourney);
        }
    }

    [ServerCallback]
    public void setTrain(Path_track newDestination, float newSpeed)
    {
        speed = newSpeed;
        passTime = Time.time;
        if (speed > MaxSpeed)
        {
            speed = MaxSpeed;
        }
        destination = newDestination;
    }
}
