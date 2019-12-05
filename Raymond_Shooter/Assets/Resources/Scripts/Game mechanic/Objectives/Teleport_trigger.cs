using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleport_trigger : MonoBehaviour
{
    public GameObject destination;
    
    
    public void OnTriggerEnter2D(Collider2D other)
    {
        if(destination != null)
        {
            other.transform.position = destination.transform.position;
        }
    }
}
