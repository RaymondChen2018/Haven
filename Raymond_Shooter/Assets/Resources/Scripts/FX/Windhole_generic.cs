using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Windhole_generic : MonoBehaviour {
    public GameObject windhole;
    public float windhole_life;
    public float emission_rate;
    public float emission_force;
    public float emission_angle_var;
    private float time_to_emit = 0;

    // Use this for initialization
    void Start () {

	}
	
	// Update is called once per frame
	void Update () {
        if(Time.time > time_to_emit)
        {
            time_to_emit = Time.time + 1 / emission_rate;
            GameObject a_windhole = Instantiate(windhole, transform.position, Quaternion.identity);
            a_windhole.GetComponent<Life>().life = windhole_life;
            float local_angle = transform.eulerAngles.z + Random.Range(-emission_angle_var, emission_angle_var) * Mathf.PI / 180;
            a_windhole.GetComponent<Rigidbody2D>().AddForce(new Vector2(Mathf.Cos(local_angle), Mathf.Sin(local_angle)).normalized * emission_force);
        }
    }
}
