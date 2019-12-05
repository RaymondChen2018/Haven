using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
//using UnityEditor;

public class Env_laser : Entity_generic {
    public Transform TargetOfLaser;
    public float DamagePerSecond = 0;
    [SyncVar] Vector2 targetPos;
    [SyncVar] Vector2 thisPos;

    public LayerMask blockage;
    LineRenderer linerenderer;
    Vector3[] positions;
    float time_to_damage = 0;
	// Use this for initialization
	void Start () {
        linerenderer = GetComponent<LineRenderer>();
        targetPos = TargetOfLaser.position;
        thisPos = transform.position;
        positions = new Vector3[] {transform.position, targetPos};
	}

	// Update is called once per frame
	void Update () {
        if (isServer)
        {
            if(TargetOfLaser == null)
            {
                targetPos = transform.position;
            }
            else
            {
                Vector2 aimdir = TargetOfLaser.position - transform.position;
                RaycastHit2D hit = Physics2D.Raycast(transform.position, aimdir, Vector2.Distance(TargetOfLaser.position, transform.position), blockage);
                if (hit)
                {
                    targetPos = hit.point;
                    if (DamagePerSecond > 0 && Time.time > time_to_damage)
                    {
                        Body_hitbox_generic body_hitbox = hit.collider.GetComponent<Body_hitbox_generic>();

                        if (body_hitbox != null)
                        {
                            time_to_damage = Time.time + CONSTANTS.NETWORK_TICK_RATE;
                            body_hitbox.body.damage(null, Vector2.zero, dmg_electric: DamagePerSecond * CONSTANTS.NETWORK_TICK_RATE);
                        }

                    }
                }
                else
                {
                    targetPos = TargetOfLaser.position;
                }
            }
            thisPos = transform.position;
        }
        positions[0] = transform.position;
        positions[1] = targetPos;
        linerenderer.SetPositions(positions);
        
	}
    /*
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Handles.Label(transform.position, gameObject.name);
        if (TargetOfLaser == null)
        {
            return;
        }
        Gizmos.DrawLine(transform.position, TargetOfLaser.transform.position);
    }
    */
}
