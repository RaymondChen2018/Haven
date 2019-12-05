using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Button : MonoBehaviour {
    public struct argument
    {
        public GameObject activator;
        public GameObject button;
    };
    public Action<argument> action = null;
    public GameObject owner;
	public void use(GameObject activator)
    {
        if(owner != null && owner != activator)
        {
            return;
        }
        argument arg;
        arg.activator = activator;
        arg.button = gameObject;
        action.Invoke(arg);
    }
}
