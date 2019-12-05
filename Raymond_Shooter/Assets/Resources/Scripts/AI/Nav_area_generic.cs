using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Nav_area_generic : MonoBehaviour {
    public int area_code = 0;
    public List<Navigation_manual._Node> area_nodes = new List<Navigation_manual._Node>();
    public List<Nav_area_generic> adjacent_areas;
    void Start()
    {
        Destroy(GetComponent<SpriteRenderer>());
    }
    /*
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<AI_generic>())
        {
            collision.GetComponent<AI_generic>().area_code = area_code;
        }
        else if(collision.tag == "Player")
        {
            collision.GetComponent<Player_controller>().area_code = area_code;
        }
    }
    */
}
