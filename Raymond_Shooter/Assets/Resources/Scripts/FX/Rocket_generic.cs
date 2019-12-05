using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Rocket_generic : NetworkBehaviour, Iprojectile
{
    public float power;
    public float radius;
    public float mass;
    public LayerMask hit_fltr;
    public GameObject smokeTrail;
    public GameObject explosion;
    public float life = 3;

    [HideInInspector] public float aim_angle;
    [HideInInspector] public float speed;
    [HideInInspector] public Vector2 aimdir;
    [HideInInspector] public bool local = false;
    [HideInInspector] public GameObject activator = null;
    //private Rigidbody2D rocketRB;
    private GameObject smoke = null;
    private bool fade = false;
    private float fade_time = 0.1f;
    private float spawn_time = 0;
    private float time_to_destroy = 0;
    private Rigidbody2D rocketRB;
    float delta_factor = 1;
    float delta_time = 1;
    float time_prev = 0;

    // Use this for initialization
    void Start () {
        //time_prev = Time.realtimeSinceStartup;
        //rocketRB = GetComponent<Rigidbody2D>();
        GetComponent<TrailRenderer>().sortingLayerName = "Flame";
        spawn_time = Time.time;
        rocketRB = GetComponent<Rigidbody2D>();
        if (smokeTrail != null && !isDedicated())
        {
            smoke = Instantiate(smokeTrail, Vector3.zero, Quaternion.identity);
            smoke.GetComponent<Fluid_generic>().PS.gameObject.transform.parent = transform;
            smoke.GetComponent<Fluid_generic>().PS.gameObject.transform.position = transform.position;
        }
    }
    bool isDedicated()
    {
        return isServer && !isClient;
    }
    [ClientRpc]
	void Rpc_spawn_explosion(Vector2 position)
    {
        spawn_explosion(position);
    }
    void spawn_explosion(Vector2 position)
    {
        GameObject exp = Instantiate(explosion, Vector3.zero, Quaternion.identity);
        Explosion_generic exp_generic = exp.GetComponent<Explosion_generic>();
        Vector3 exp_pos = new Vector3(position.x, position.y, transform.position.z);
        exp_generic.PS.transform.position = exp_pos;
        exp_generic.activator = activator;
        exp_generic.power = power;
        exp_generic.radius = radius;
    }
    [ClientRpc]
    void Rpc_unlink_smoke()
    {
        if (smoke != null)
        {
            smoke.GetComponent<Fluid_generic>().remove();
        }
    }
    public void emit()
    {

    }
    //This function define how the projectile travel
    public void travel()
    {
        //If hit something
        if (local)
        {
            detect();
        }
        //If rocket flew for too long, fade
        if (Time.time > spawn_time + life && !fade)
        {
            if (isServer)
            {
                Rpc_unlink_smoke();
            }
            fade = true;
            speed = 0;
            Destroy(GetComponent<SpriteRenderer>());
            time_to_destroy = Time.time + fade_time;
        }
        //If server, run step-by-step path tracing to prevent missing target
        if (local)
        {
            rocketRB.MovePosition(rocketRB.position + aimdir.normalized * speed  * delta_time * CONSTANTS.PROJECTILE_SPEED_MULTI);
        }
    }
    //This function define how the projectile predicts
    public void detect()
    {
        RaycastHit2D hit = Physics2D.Raycast(rocketRB.position, aimdir, speed * delta_time * CONSTANTS.PROJECTILE_SPEED_MULTI, hit_fltr);
        
        if (hit)
        {
            //Spawn explosion
            if (explosion != null)
            {
                Vector2 exp_pos = hit.point - (hit.point - rocketRB.position).normalized * CONSTANTS.EXPLOSION_OFFSET_WALL;
                Rpc_spawn_explosion(exp_pos);
                if (isDedicated())
                {
                    spawn_explosion(exp_pos);
                }
            }
            

            //Exert force & damage
            Body_hitbox_generic hitbox = hit.collider.GetComponent<Body_hitbox_generic>();
            if (hitbox != null)
            {
                impact_character(hitbox, hit.point);
            }
            else//physic objects
            {
                impact_object(hit.collider.gameObject, hit.point);

            }
            Rpc_unlink_smoke();

            fade = true;
            rocketRB.position = hit.point - aimdir.normalized * 0.1f;//keep explosion occuring outside of wall
            Destroy(GetComponent<SpriteRenderer>());
            time_to_destroy = Time.time + fade_time;
            speed = 0;
        }
    }
    //This function will detect character collision and calculate damage
    //Return 1: keep the projectile going
    //Return 0: this object shouldn't be impacted
    //Return -1: this projectile is stopped by character
    public int impact_character(Body_hitbox_generic hit_box, Vector2 hit_point)
    {
        Vector2 force = CONSTANTS.DAMAGE_FORCE_MULTIPLIER * aimdir * speed * mass / 2;
        Body_generic body = hit_box.body;
        if (body.isPlayer && !body.hasAuthority)//client player
        {
            body.Rpc_add_force(force);
        }
        else//host player & npc
        {
            body.GetComponent<Rigidbody2D>().AddForceAtPosition(force, hit_point);
        }
        if (activator.GetComponent<Body_generic>().dmg_tags.Contains(hit_box.tag))
        {
            body.damage(activator, force: force, dmg_physics: Damage(), headshot: false);
        }
        return -1;
    }
    //This function will detect object collision
    //projectile will stop no matter what
    public void impact_object(GameObject obj, Vector2 hit_point)
    {
        if(obj.tag == "structure")
        {
            if (activator.GetComponent<Body_generic>().dmg_tags.Contains(obj.tag) && activator.layer != obj.layer)
            {
                obj.GetComponent<Structure_generic>().health -= Damage();
            }
        }
        if (obj.GetComponent<Rigidbody2D>() != null)
        {
            Vector2 force = CONSTANTS.DAMAGE_FORCE_MULTIPLIER * aimdir * speed * mass / 2;
            obj.GetComponent<Rigidbody2D>().AddForceAtPosition(force, hit_point);
        }
    }
    float Damage()
    {
        return mass* speed *speed / 10000;
    }
    //This function define how the projectile behave after stopping impact & removing
    public void remove()
    {
        if (isServer)
        {
            NetworkServer.Destroy(gameObject);
        }
    }
    // Update is called once per frame
    void Update () {
        if (!fade)
        {
            //delta_factor = Time.deltaTime * CONSTANTS.MAX_FPS;
            delta_time = Time.deltaTime;//Time.realtimeSinceStartup - time_prev;
            //time_prev = Time.realtimeSinceStartup;
            travel();
        }
        //remove this rocket
        else if(Time.time > time_to_destroy)
        {
            remove();
        }
    }
}
