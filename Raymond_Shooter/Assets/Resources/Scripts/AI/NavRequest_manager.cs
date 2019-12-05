using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;

public class NavRequest_manager : MonoBehaviour {

    public Queue<GameObject> pathRequestQueue = new Queue<GameObject>();
    public int count;
    //PathRequest currentPathRequest;
    private void Update()
    {
        count = pathRequestQueue.Count;
    }
    public void RequestPath(GameObject zombie)//, GameObject player, float z_size, float p_size)
    {   
        if (!pathRequestQueue.Contains(zombie))
        {
            pathRequestQueue.Enqueue(zombie);
        }
    }
}
