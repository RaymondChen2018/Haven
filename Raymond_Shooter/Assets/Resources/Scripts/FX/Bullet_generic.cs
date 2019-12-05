using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Bullet_generic : MonoBehaviour, Iprojectile
{
    
    public LayerMask hit_fltr;
    public LayerMask hit_head_fltr;
    public GameObject spark = null;


    [HideInInspector]
    public float speed_damp;
    [HideInInspector]
    public float speed_min;
    [HideInInspector]
    public float speed = 0;
    [HideInInspector]
    public float aim_angle;
    [HideInInspector]
    public List<int> collidedlist;
    [HideInInspector]
    public Vector2 aimdir;
    [HideInInspector]
    public float mass;
    [HideInInspector]
    public bool local = false;
    [HideInInspector] public bool isDedicated = false;
    [HideInInspector]
    public GameObject activator = null;
    [HideInInspector] public bool noDamage = false;
    [HideInInspector]
    public float speed_muzzle;
    private float time_of_collision = 0;
    private TrailRenderer trail;
    private Rigidbody2D bulletRB;
    [HideInInspector] public LayerMask initial_hit_fltr;
    [HideInInspector] public Pool_watcher pool_watcher;
    [HideInInspector] public Gradient default_gradient;
    [HideInInspector] public Texture default_texture;
    float delta_factor = 1;
    float delta_time = 1;
    [HideInInspector] public float lag_comp = 0;
    // Use this for initialization
    void Start () {
        trail = GetComponent<TrailRenderer>();
        trail.sortingLayerName = "Bullet";

        collidedlist = new List<int>();
        //bulletRB = GetComponent<Rigidbody2D>();

        //Presimulate bullet if it was spawned from network and has latency over its course
        //Debug.Log("lag: " + lag_comp);
        
        
        
    }
    
    public void reset()
    {
        collidedlist.Clear();
        local = false;
        isDedicated = false;
        time_of_collision = 0;
        GetComponent<TrailRenderer>().colorGradient = default_gradient;
        GetComponent<TrailRenderer>().material.mainTexture = default_texture;
    }
    public void emit()
    {

    }
    //This function define how the projectile behave after stopping impact & removing
    public void remove()
    {
        if (Time.realtimeSinceStartup > time_of_collision + CONSTANTS.BULLET_FADE_TIME + trail.time)
        {
            //Destroy(gameObject);
            pool_watcher.recycle_blt(this);
        }
    }
    
    void OnDestroy()
    {
        Destroy(GetComponent<TrailRenderer>().material);
    }
    
    //This function defines how the projectile travel
    public void travel()
    {
        transform.position = (Vector2)transform.position + aimdir.normalized * speed * delta_time * CONSTANTS.PROJECTILE_SPEED_MULTI;
        speed -= delta_time * CONSTANTS.FPS_SCALE * speed_damp;
        trail.widthMultiplier = Mathf.Max(0,(speed - speed_min)) / (speed_muzzle - speed_min) * mass / 20;
        
    }
    //This function will detect character collision and calculate damage
    //Return 1: keep the projectile going
    //Return 0: this object shouldn't be impacted
    //Return -1: this projectile is stopped by character
    public int impact_character(Body_hitbox_generic hit_box, Vector2 hit_point)
    {

        Body_generic body = hit_box.body;
        //if this box has been hit OR bullet hits the shooter
        if (collidedlist.Contains(body.gameObject.GetInstanceID()) || body.gameObject == activator)
        {// if previously hit or hit self
            return 0;
        }

        //Approximate fps-independent speed in the middle hit target
        float real_speed = Mathf.Lerp(speed, (speed - delta_time * CONSTANTS.FPS_SCALE * speed_damp), Vector2.Distance(transform.position, hit_point) / (speed * delta_time * CONSTANTS.PROJECTILE_SPEED_MULTI));
        if (local)
        {

            float absorbed_speed;//For force calculation
            
            if (body.tissue_dense < real_speed)
            {
                absorbed_speed = body.tissue_dense;
            }
            else
            {
                absorbed_speed = real_speed;
            }

            if (real_speed > speed_min)
            {

                bool isHeadShot = false;
                float angle = Mathf.Atan2(aimdir.y, aimdir.x) * 180 / Mathf.PI;
                float damage = 0;
                Vector2 force = CONSTANTS.DAMAGE_FORCE_MULTIPLIER * aimdir * absorbed_speed * mass / 2;

                IDamageActivator Activator = null;
                bool allowDamage = false;
                if(activator == null)
                {
                    allowDamage = true;
                }
                else
                {
                    Activator = activator.GetComponent<IDamageActivator>();
                    if (Activator.canDamage(body))
                    {
                        allowDamage = true;
                    }
                }

                //Examine if can damage, Can hit body/headbox
                if (allowDamage)
                {
                    //headshot detection
                    if (hit_box.gameObject.tag == "headsquirt")//hitbox is headbox
                    {
                        isHeadShot = true;
                    }
                    else//hitbox is bodybox, examine further if hit headbox
                    {
                        RaycastHit2D[] hit_head = Physics2D.RaycastAll(hit_point, aimdir * real_speed, body.size * 2, hit_head_fltr);
                        foreach (RaycastHit2D hitx in hit_head)
                        {
                            //hit normal bullet impace fx
                            if (hitx.collider.tag == "headsquirt" && hitx.collider.GetComponent<Body_hitbox_generic>().body.gameObject == body.gameObject)//if sub-hitbox's parent is current body box
                            {
                                isHeadShot = true;
                                break;
                            }
                        }
                    }
                    damage = Damage();
                }

                if (Activator != null)
                {
                    Activator.OnHitCharacter(body, damage, hit_point, force, isHeadShot, DamageType.Physical);
                }
                body.OnDamagedBy(Activator, damage, hit_point, force, isHeadShot, DamageType.Physical);


                //if (activator == null || activator.GetComponent<Body_generic>().dmg_tags.Contains(body.tag))
                //{
                //    //headshot detection
                //    if (hit_box.gameObject.tag == "headsquirt")//hitbox is headbox
                //    {
                //        isHeadShot = true;
                //    }
                //    else//hitbox is bodybox, examine further if hit headbox
                //    {
                //        RaycastHit2D[] hit_head = Physics2D.RaycastAll(hit_point, aimdir * real_speed, body.size * 2, hit_head_fltr);
                //        foreach (RaycastHit2D hitx in hit_head)
                //        {
                //            //hit normal bullet impace fx
                //            if (hitx.collider.tag == "headsquirt" && hitx.collider.GetComponent<Body_hitbox_generic>().body.gameObject == body.gameObject)//if sub-hitbox's parent is current body box
                //            {
                //                isHeadShot = true;
                //                break;
                //            }
                //        }
                //    }

                //    damage = Damage();

                //}
                //if(activator == null)
                //{
                //Body_generic activator_body = activator.GetComponent<Body_generic>();
                //if (activator_body.isLocalPlayer)
                //{
                //    activator_body.player_controller.hit_mark();
                //}

                ////If non-host local hit
                //if (activator != null && activator_body.isPlayer && !activator_body.isServer)
                //{
                //    activator.GetComponent<Player_controller>().add_to_shot_list(body.gameObject, damage, hit_point, force.magnitude, CONSTANTS.seed_float_to_short(angle, 360), isHeadShot, 0);
                //    body.GetComponent<Rigidbody2D>().AddForceAtPosition(force, hit_point);
                //}
                //else//Server client || npc authoritates bullet
                //{
                //    //Cause damage and bleed and force
                //    if (damage > 0)
                //    {
                //        body.damage(activator, force: force, dmg_physics: mass * real_speed * real_speed / 10000, headshot: isHeadShot);
                //        if (body.isPlayer && !body.hasAuthority)//Pushing non-server client
                //        {
                //            //body.Rpc_bleed_n_force(hit_point, force, isHeadShot);
                //            body.request_bleed(hit_point, angle, isHeadShot);
                //            body.Rpc_add_force(force);
                //        }
                //        else//Pushing host or npc
                //        {
                //            body.request_bleed(hit_point, angle, isHeadShot);
                //            body.GetComponent<Rigidbody2D>().AddForceAtPosition(force, hit_point);
                //        }
                //    }
                //    //Friendly fire, just force
                //    else
                //    {
                //        if (body.isPlayer && !body.hasAuthority)//Non-server client
                //        {
                //            body.Rpc_add_force(force);
                //        }
                //        else//Host or npc
                //        {
                //            body.GetComponent<Rigidbody2D>().AddForceAtPosition(force, hit_point);
                //        }
                //    }

                //}
                //}
            }
        }
        speed = real_speed - body.tissue_dense;
        if (speed < speed_min)
        {
            speed = 0;
            /*
            if (local)
            {
                transform.position = hit_point;
            }
            else
            {
                bulletRB.position = hit_point;
            }
            */
            time_of_collision = Time.realtimeSinceStartup;
            return -1;
        }
        collidedlist.Add(body.gameObject.GetInstanceID());
        return 1;
    }
    //This function will detect object collision
    //projectile will stop no matter what
    public void impact_object(GameObject obj, Vector2 hit_point)
    {
        if (obj.GetComponent<Rigidbody2D>() != null)
        {
            if (local)//If rigidbody, Exert force
            {
                obj.GetComponent<Rigidbody2D>().AddForceAtPosition(aimdir * speed * mass / 2, hit_point);
            }
        }
        if(obj.tag == "structure")//Structure
        {
            Body_generic activator_body = activator.GetComponent<Body_generic>();
            if (local && (activator == null || (activator_body.dmg_tags.Contains(obj.tag) && activator.layer != obj.layer)))//
            {
                float angle = Mathf.Atan2(aimdir.y, aimdir.x) * 180 / Mathf.PI;
                //If Client authoritates bullet
                if (activator != null && activator_body.isPlayer && !activator_body.isServer)
                {
                    activator_body.player_controller.add_to_shot_list(obj, Damage(), hit_point, 0, CONSTANTS.seed_float_to_short(angle, 360), false, 0);
                }
                else
                {
                    obj.GetComponent<Structure_generic>().health -= Damage();
                }
                
            }
        }
        if (spark != null && !isDedicated)
        {
            float angle = Mathf.Atan2(aimdir.y, aimdir.x) * 180 / Mathf.PI;
            Instantiate(spark, hit_point, Quaternion.Euler(0, 0, angle));
        }
        transform.position = hit_point;
        /*
        if (local)
        {
            
        }
        else
        {
            bulletRB.position = hit_point;//Avoid synctransform call
        }
        */
        speed = 0;
        time_of_collision = Time.realtimeSinceStartup;
        
    }
    float Damage()
    {
        if (noDamage)
        {
            return 0;
        }
        return mass* speed *speed / 10000;
    }
    //This function define how the projectile predicts
    public void detect()
    {
        
        //Cheap prediction
        RaycastHit2D hit_approx = Physics2D.Raycast(transform.position, aimdir * speed, speed * delta_time * CONSTANTS.PROJECTILE_SPEED_MULTI, hit_fltr);
        if(hit_approx.collider == null)
        {
            return;
        }
        //If prediction hit, run detail raycast
        RaycastHit2D[] hit = Physics2D.RaycastAll(transform.position, aimdir * speed, speed * delta_time * CONSTANTS.PROJECTILE_SPEED_MULTI, hit_fltr);
        if (hit.Length != 0)
        {
            for (int i = 0; i < hit.Length; i++)
            {
                //If structure and tag
                Body_hitbox_generic body_hitbox = hit[i].collider.GetComponent<Body_hitbox_generic>();
                //If hit lag-ghost hitbox, link to the actual hitbox then examine damage
                //Non-server clients wont be detecting the lag-ghost as it only exist on server end
                
                if (body_hitbox != null)//Characters
                {
                    int ret_value = impact_character(body_hitbox, hit[i].point);
                    if(ret_value == 0)////if this box has been hit OR bullet hits the shooter
                    {
                        continue;
                    }
                    else if(ret_value == -1)//If bullet stopped by body
                    {
                        break;
                    }
                }
                else//Objects or level; Lag compensate will not apply on physic object
                {
                    GameObject obj = hit[i].collider.gameObject;
                    impact_object(obj, hit[i].point);
                    break;
                }
            }
        }
    }

    void Update () {
        if (speed > speed_min)//Bullet is still traveling
        {
            delta_time = Time.deltaTime;//Time.realtimeSinceStartup - time_prev;

            if (lag_comp > 0)
            {
                if (!local)
                {
                    delta_time += lag_comp * Time.timeScale / 1000f;
                }
                lag_comp = 0;
            }
            delta_factor = delta_time  * CONSTANTS.MAX_FPS;
            
            detect();
            travel();
            
        }
        else//Dead bullet hanging...
        {
            remove();
        }
    }
}
