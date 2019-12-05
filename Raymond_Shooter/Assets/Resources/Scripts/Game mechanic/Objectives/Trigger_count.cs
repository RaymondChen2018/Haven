using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class Trigger_count :  NetworkBehaviour {
    public LayerMask detect;
    public Action action = null;

	[ServerCallback]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (action != null)
        {
            action.Invoke();
        }
    }
}
