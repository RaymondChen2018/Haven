using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreToken_generic : MonoBehaviour {
    public Text Name;
    public Text Kill1;
    public Text Kill2;
    public Text Kill3;
    public Text Deaths;
    public Text Latency;
    public Player_generic associated_client;
	// Use this for initialization
	void Start () {
        /*
        Name.text = "Player";
        Kill1.text = "0";
        Kill2.text = "0";
        Kill3.text = "0";
        Deaths.text = "0";
        Latency.text = "0";
        */
    }
}
