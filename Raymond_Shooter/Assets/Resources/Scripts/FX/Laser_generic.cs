using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser_generic : MonoBehaviour, Iprojectile {
    public Vector2 start;
    public Vector2 aimdir;
    public float distance;
    public float temperature;
    public LayerMask hit_fltr;



    [HideInInspector]
    public bool local = false;
    [HideInInspector]
    public GameObject activator = null;
    [HideInInspector]
    public List<int> collidedlist;
    private Vector2 end;
    private float life = 0.2f;
    private float time_to_destroy = 0;
    [HideInInspector] public float initial_width = 0;
    private Color start_color;
    private Color end_color;
    [HideInInspector] public LayerMask initial_hit_fltr;
    [HideInInspector] public Pool_watcher pool_watcher;
    [HideInInspector] public Gradient default_gradient;
    [HideInInspector] public Texture default_texture;
    [HideInInspector] public bool isDedicated = false;
    LineRenderer line_renderer;


    public void Start()
    {
        line_renderer = GetComponent<LineRenderer>();
        collidedlist = new List<int>();
        time_to_destroy = Time.time + life;
        //width = CONSTANTS.LASER_TEMP_RAMP * temperature / 100 + CONSTANTS.LASER_BASE_WIDTH;
        
        
        emit();
    }
    public void reset()
    {
        collidedlist.Clear();
        local = false;
        isDedicated = false;
        time_to_destroy = Time.time + life;
        //width = CONSTANTS.LASER_TEMP_RAMP * temperature / 100 + CONSTANTS.LASER_BASE_WIDTH;
        line_renderer.colorGradient = default_gradient;
        line_renderer.material.mainTexture = default_texture;
        //start_color = line_renderer.startColor;
        //end_color = line_renderer.endColor;
        //Debug.LogError("end color: "+end_color);
        //GetComponent<LineRenderer>().colorGradient = default_gradient;
        //GetComponent<LineRenderer>().material.mainTexture = default_texture;
    }
    void OnDestroy()
    {
        Destroy(GetComponent<LineRenderer>().material);
    }
    

    public void emit()
    {
        start_color = line_renderer.startColor;
        end_color = line_renderer.endColor;
        detect();
    }
    //This function define how the projectile travel
    public void travel()
    {

    }
    //This function define how the projectile predicts
    public void detect()
    {
        RaycastHit2D hit = Physics2D.Raycast(start, aimdir, distance, hit_fltr);
        if (hit)
        {
            //Debug.LogError("index: "+hit_index);
            //fx calculation
            end = hit.point;
            line_renderer.SetPositions(new Vector3[] { start, end });
            line_renderer.startWidth = initial_width;
            line_renderer.endWidth = initial_width - (Vector2.Distance(start, end) / distance) * initial_width;
            line_renderer.endColor = Color.Lerp(start_color, end_color, Vector2.Distance(start, end) / distance);

            if (hit.collider.GetComponent<Body_hitbox_generic>() != null)//body object
            {
                impact_character(hit.collider.GetComponent<Body_hitbox_generic>(), hit.point);
            }
            else//any physic object
            {
                impact_object(hit.collider.gameObject, hit.point);
            }
        }
        else
        {
            end = start + aimdir.normalized * distance;
            line_renderer.SetPositions(new Vector3[] { start, end });
            line_renderer.startWidth = initial_width;
            line_renderer.endWidth = 0;
        }
    }
    //This function will detect character collision and calculate damage
    //Return 1: keep the projectile going
    //Return 0: this object shouldn't be impacted
    //Return -1: this projectile is stopped by character
    public int impact_character(Body_hitbox_generic hit_box, Vector2 hit_point)
    {
        Body_generic body = hit_box.body;
        if (body.gameObject == activator)
        {
            return -1;
        }
        if (local)
        {
            float heat_dmg = 0;
            float angle = Mathf.Atan2(aimdir.y, aimdir.x) * 180 / Mathf.PI;
            if (activator.GetComponent<Body_generic>().dmg_tags.Contains(body.tag))
            {
                heat_dmg = Damage();
            }
            Vector2 force = CONSTANTS.DAMAGE_FORCE_MULTIPLIER * aimdir.normalized * heat_dmg / 20;
            
            //If Client authoritates laser
            if (activator.GetComponent<Body_generic>().isPlayer && !activator.GetComponent<Body_generic>().isServer)
            {
                activator.GetComponent<Player_controller>().add_to_shot_list(body.gameObject, heat_dmg, hit_point, force.magnitude, CONSTANTS.seed_float_to_short(angle, 360), false, 1);
                body.GetComponent<Rigidbody2D>().AddForceAtPosition(force, hit_point);
            }
            else//Server client || npc authoritates laser
            {
                if (heat_dmg > 0)
                {
                    body.damage(activator, force, dmg_physics: CONSTANTS.heat_to_physics(heat_dmg), dmg_thermal: heat_dmg, headshot: false);
                    if (body.isPlayer && !body.hasAuthority)//Non-server client
                    {
                        body.request_bleed(hit_point, angle, false);
                        body.Rpc_add_force(force);
                    }
                    else//Host or npc
                    {
                        body.request_bleed(hit_point, angle, false);
                        body.GetComponent<Rigidbody2D>().AddForceAtPosition(force, hit_point);
                    }
                }
                //Friendly fire, just force
                else
                {
                    if (body.isPlayer && !body.hasAuthority)//Non-server client
                    {
                        body.Rpc_add_force(force);
                    }
                    else//Host or npc
                    {
                        body.GetComponent<Rigidbody2D>().AddForceAtPosition(force, hit_point);
                    }
                }
            }
        }
        return -1;
    }
    //This function will detect object collision
    //projectile will stop no matter what
    public void impact_object(GameObject obj, Vector2 hit_point)
    {
        float heat = Damage();

        if (obj.tag == "structure")//Structure 
        {
            if(local && heat > 0)
            {
                Body_generic activator_body = activator.GetComponent<Body_generic>();
                if (activator_body.dmg_tags.Contains(obj.tag) && activator.layer != obj.layer)
                {
                    float angle = Mathf.Atan2(aimdir.y, aimdir.x) * 180 / Mathf.PI;
                    //If Client authoritates laser
                    if (activator_body.isPlayer && !activator_body.isServer)
                    {
                        activator_body.player_controller.add_to_shot_list(obj, heat, hit_point, 0, CONSTANTS.seed_float_to_short(angle, 360), false, 1);
                    }
                    else//Server client || npc authoritates laser
                    {
                        obj.GetComponent<Structure_generic>().health -= CONSTANTS.heat_to_physics(heat);
                    }
                    if (activator_body.isLocalPlayer)
                    {
                        activator_body.player_controller.hit_mark();
                    }
                }
            }
        }
        if (obj.GetComponent<Rigidbody2D>() != null)
        {
            Vector2 force = CONSTANTS.DAMAGE_FORCE_MULTIPLIER * (end - start).normalized * heat / 20;
            obj.GetComponent<Rigidbody2D>().AddForceAtPosition(force, hit_point);
        }
        
    }
    float Damage()
    {
        //Debug.LogError((1 - Vector2.Distance(start, end) / distance) * temperature);
        return (1 - Vector2.Distance(start, end) / distance) * temperature;
    }
    //This function define how the projectile behave after stopping impact & removing
    public void remove()
    {
        pool_watcher.recycle_lsr(this);
    }
    // Update is called once per frame
    void Update () {
        if(Time.time > time_to_destroy)
        {
            remove();
        }
        //Color red = Color.red;
        //red.a = 0;
        GetComponent<LineRenderer>().startColor = Color.Lerp(end_color, start_color, (time_to_destroy - Time.time) / life);
        GetComponent<LineRenderer>().endColor = Color.Lerp(end_color, end_color, (time_to_destroy - Time.time) / life);
    }
}

/*
 * 
 * 
 * public Vector2 start;
    public Vector2 aimdir;
    public float distance;
    public float temperature;
    public LayerMask hit_fltr;



    [HideInInspector]
    public bool local = false;
    [HideInInspector]
    public GameObject activator = null;
    [HideInInspector]
    public List<int> collidedlist;
    private Vector2 end;
    private float life = 0.2f;
    private float time_to_destroy = 0;
    private float width = 0.01f;
    private float width_ramp = 0.001f;
    private Color start_color;
    private Color end_color;
    


    public void Start()
    {
        collidedlist = new List<int>();
        time_to_destroy = Time.time + life;
        width += width_ramp * temperature / 100;
        start_color = GetComponent<LineRenderer>().startColor;
        end_color = GetComponent<LineRenderer>().endColor;

        emit();
    }
    public void emit()
    {
        detect();
    }
    //This function define how the projectile travel
    public void travel()
    {

    }
    //This function define how the projectile predicts
    public void detect()
    {
        
        RaycastHit2D[] hits = Physics2D.RaycastAll(start, aimdir, distance, hit_fltr);
        
        if (hits.Length > 0)
        {
            List<float> heat_list = new List<float>();
            for (int i = 0; i < hits.Length; i++)
            {
                heat_list.Add(temperature);//start point of sub segment
                if (hits[i].collider.GetComponent<Body_hitbox_generic>() != null)//body object
                {
                    int ret_value = impact_character(hits[i].collider.GetComponent<Body_hitbox_generic>(), hits[i].point);
                    if (ret_value == 0)
                    {
                        continue;
                    }
                    else if (ret_value == -1)
                    {
                        heat_list.Add(temperature);
                        break;
                    }
                }
                else if (hits[i].collider.GetComponent<Rigidbody2D>() != null)//any physic object
                {
                    impact_object(hits[i].collider.gameObject, hits[i].point);
                    heat_list.Add(temperature);
                    break;
                }  
                heat_list.Add(temperature);//end point of sub segment
            }
            Vector2 temp_start = start;
            Vector2 temp_end;
            float dist_stack = 0;
            GradientAlphaKey[] akeys = new GradientAlphaKey[8];
            GradientColorKey[] Ckeys = new GradientColorKey[8];
            for (int i = 0; i < hits.Length; i++)
            {
                temp_end = hits[i].point;
                dist_stack += Vector2.Distance(temp_start, temp_end);
                GetComponent<LineRenderer>().widthCurve.AddKey(dist_stack / distance, width * heat_list[i*2+1] / heat_list[0]);
                if(i < 8)
                {
                    akeys[i].time = 
                }
                temp_start = temp_end;
            }

            //width
            //color

            
            
            float ratio = 1;
            
            float width_decay = width;
            Color color_decay = start_color;
            Color temp_lerp;
            GradientAlphaKey[] akeys = new GradientAlphaKey[8];
            GradientColorKey[] Ckeys = new GradientColorKey[8];
            for (int i = 0; i < hits.Length; i++)
            {
                temp_end = hits[i].point;
                ratio = (Vector2.Distance(temp_start, temp_end) / distance);

                dist_stack += Vector2.Distance(temp_start, temp_end);
                width_decay -= ratio * width;

                GetComponent<LineRenderer>().widthCurve.AddKey(dist_stack / distance, width_decay);
                if(i < 8)
                {
                    temp_lerp = Color.Lerp(start_color, end_color, Vector2.Distance(start, temp_end) / distance);
                    akeys[i] = 
                }
                

                temp_start = temp_end;
            }
            GetComponent<LineRenderer>().colorGradient.setk
        
            //fx calculation
            end = hit.point;
            GetComponent<LineRenderer>().SetPositions(new Vector3[] { start, end });
            GetComponent<LineRenderer>().startWidth = width;
            GetComponent<LineRenderer>().endWidth = width - (Vector2.Distance(start, end) / distance) * width;
            GetComponent<LineRenderer>().endColor = Color.Lerp(start_color, end_color, Vector2.Distance(start, end) / distance);
            

        }
        



        RaycastHit2D hit = Physics2D.Raycast(start, aimdir, distance, hit_fltr);
        if (hit)
        {
            //fx calculation
            end = hit.point;
            GetComponent<LineRenderer>().SetPositions(new Vector3[] { start, end });
            GetComponent<LineRenderer>().startWidth = width;
            GetComponent<LineRenderer>().endWidth = width - (Vector2.Distance(start, end) / distance) * width;
            GetComponent<LineRenderer>().endColor = Color.Lerp(start_color, end_color, Vector2.Distance(start, end) / distance);


            if (hit.collider.GetComponent<Body_hitbox_generic>() != null)//body object
            {
                impact_character(hit.collider.GetComponent<Body_hitbox_generic>(), hit.point);
            }
            else if (hit.collider.GetComponent<Rigidbody2D>() != null)//any physic object
            {
                impact_object(hit.collider.gameObject, hit.point);
            }

        }
        else
        {
            end = start + aimdir.normalized * distance;
            GetComponent<LineRenderer>().SetPositions(new Vector3[] { start, end });
            GetComponent<LineRenderer>().startWidth = width;
            GetComponent<LineRenderer>().endWidth = 0;
        }
    }
    //This function will detect character collision and calculate damage
    //Return 1: keep the projectile going
    //Return 0: this object shouldn't be impacted
    //Return -1: this projectile is stopped by character
    public int impact_character(Body_hitbox_generic hit_box, Vector2 hit_point)
    {
        Body_generic body = hit_box.body;
        //if this box has been hit OR bullet hits the shooter, nothing happen
        if (collidedlist.Contains(body.gameObject.GetInstanceID()) || body.gameObject == activator)
        {
            return 0;
        }

        if (local)
        {
            float heat = (1 - Vector2.Distance(start, end) / distance) * temperature;
            Vector2 force = (end - start).normalized * heat / 20;
            if (body.isPlayer && !body.hasAuthority)//client player
            {
                body.Rpc_add_force(force);
            }
            else//host player & npc
            {
                body.GetComponent<Rigidbody2D>().AddForceAtPosition(force, hit_point);
            }
            body.damage(activator, Vector2.zero, heat / 200f, heat, false);
        }
        temperature -= 
        temperature -= body.tissue_dense * 200;
        collidedlist.Add(body.gameObject.GetInstanceID());
        if (temperature > 0)
        {
            return 1;
        }
        else
        {
            return -1;
        }
    }
    //This function will detect object collision
    //projectile will stop no matter what
    public void impact_object(GameObject obj, Vector2 hit_point)
    {
        float heat = (1 - Vector2.Distance(start, end) / distance) * temperature;
        Vector2 force = (end - start).normalized * heat / 20;
        obj.GetComponent<Rigidbody2D>().AddForceAtPosition(force, hit_point);
    }
    //This function define how the projectile behave after stopping impact & removing
    public void remove()
    {

    }
    // Update is called once per frame
    void Update () {
        if(Time.time > time_to_destroy)
        {
            Destroy(gameObject);
        }
        Color red = Color.red;
        red.a = 0;
        GetComponent<LineRenderer>().startColor = Color.Lerp(red, start_color, (time_to_destroy - Time.time) / life);
        GetComponent<LineRenderer>().endColor = Color.Lerp(red, end_color, (time_to_destroy - Time.time) / life);
    }

    */
