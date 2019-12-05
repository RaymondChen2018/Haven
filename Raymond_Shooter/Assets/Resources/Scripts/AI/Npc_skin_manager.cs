using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Npc_skin_manager : MonoBehaviour {
    Object[] _skin_list;
    public Sprite[] skin_list;
	// Use this for initialization
	void Awake () {

        _skin_list = Resources.LoadAll("Sprites/Human parts/Heads/");
        skin_list = new Sprite[_skin_list.Length];
        for(int i = 0; i < _skin_list.Length; i++)
        {
            skin_list[i] = _skin_list[i] as Sprite;
        }

    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
