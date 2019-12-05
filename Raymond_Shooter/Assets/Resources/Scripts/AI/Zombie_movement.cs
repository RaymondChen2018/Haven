using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zombie_movement : MonoBehaviour {

    public float player_speed_walk;
    public float player_speed_sprint;
    
    public KeyCode left;
    public KeyCode right;
    public KeyCode upward;
    public KeyCode downward;
    public KeyCode Sprint;
    
    private Rigidbody2D theRB;
    private float mouseangle;

    // Use this for initialization
    void Start () {
        theRB = GetComponent<Rigidbody2D>();
    }
	
	// Update is called once per frame
	void Update () {
        float player_speed = player_speed_walk;
        if (Input.GetKey(Sprint))
        {
            player_speed += player_speed_sprint;
        }
        if (Input.GetKey(left))
        {
            theRB.velocity = new Vector2(-player_speed, theRB.velocity.y);
        }
        else if (Input.GetKey(right))
        {
            theRB.velocity = new Vector2(player_speed, theRB.velocity.y);
        }
        else
        {
            theRB.velocity = new Vector2(0, theRB.velocity.y);
        }
        if (Input.GetKey(upward))
        {
            theRB.velocity = new Vector2(theRB.velocity.x, player_speed);
        }
        else if (Input.GetKey(downward))
        {
            theRB.velocity = new Vector2(theRB.velocity.x, -player_speed);
        }
        else
        {
            theRB.velocity = new Vector2(theRB.velocity.x, 0);
        }
        if ((theRB.velocity.x != 0) && (theRB.velocity.y != 0))
        {
            theRB.velocity = new Vector2(theRB.velocity.x / Mathf.Sqrt(2), theRB.velocity.y / Mathf.Sqrt(2));
        }
    }
}
