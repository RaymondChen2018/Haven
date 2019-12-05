using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
public class Gun_generic : MonoBehaviour, IEquiptable
{
    public ushort ammo;
    public ushort capacity;
    public float rate_of_fire = 0;
    public int burst_shots = 3;
    public bool isSemiAuto = true;
    public bool isFullyAuto = false;
    /// <summary>
    /// Means if the bullet path should be precalculated before sending through the network; If true, bullet projectiles wont be spawn by the gun_generic
    /// </summary>
    public bool Local_precomputation = false;
    public FireMode firemode = FireMode.Semi_auto;
    public GunType guntype;
    public Ammo_generic.AmmoType ammotype;
    
    public float blt_speed;             
    public float blt_speed_min;
    public float blt_speed_damp;
    public float blt_mass;
    public Gradient blt_color;
    public Texture blt_texture;
    public bool blt_custom;
    public float thermal = 0;               //(*C), laser temperature, flamer temperature
    public float effective_dist = 0;        //Laser range, AI minimum shooting range
    public float maximum_dist = 100;        //AI maximum shooting range
    public float noise_dist = 100;          //Sound radius to attract npc
    public float power;                     //Explosive power, plasma power
    public float radius;                    //Explosive radius;
    public float shake_extent = 1;
    public float accuracy;  
    public float precise;   
    public float recoil;
    public ushort weapon_weight;
    public ushort weapon_size;
    public bool reload_all = true;
    public float reload_time;
    
    public float bias_factor = 20;
    public float rate_of_readjust;
    public float readjust_factor = 20;
    public AudioClip fire_sound;
    public GameObject muzzleflash;
    public GameObject spark;                //Bullet spark, Explosive Fx
    public GameObject bullet;
    public GameObject trail = null;
    public GameObject ammo_template;
    public Transform fire_point;



    [HideInInspector]
    public float time_to_fire = 0;
    [HideInInspector]
    public float time_to_readjust = 0;
    [HideInInspector]
    private int firemode_index = 0;
    //[HideInInspector]
    public float firecone_angle = 0;
    public enum FireMode { Semi_auto, Fully_auto };
    public enum GunType { M92fs, R870, M134, M4a1, Mp5, Sv10, HS50, Fnfal, DG_Laser101, RPG7, M2A1_7, AWP, LAW, DE, Mac10, AA12,
    Glock17, M249, X1, DG_Laser375, Ar15, Schmidt, Ak47};
    private List<FireMode> modes = new List<FireMode>();
    private Sound_watcher sound_watcher;
    private Server_watcher cvar_watcher;
    Equipable_generic equip;

    void Awake()
    {
        //Register to cvar watcher on awake, onstart has a low chance to glitch the gun
        cvar_watcher = FindObjectOfType<Server_watcher>();
        sound_watcher = FindObjectOfType<Sound_watcher>();
    }
    // Use this for initialization
    void Start()
    {
        cvar_watcher = FindObjectOfType<Server_watcher>();
        equip = GetComponent<Equipable_generic>();
        gameObject.tag = "pickup_gun";
        if(bullet != null && bullet.tag == "bullet_flame")
        {
            GameObject blt = Instantiate(bullet, Vector2.zero, Quaternion.identity);
            blt.GetComponent<Fluid_generic>().PS.transform.parent = transform;
            blt.GetComponent<Fluid_generic>().PS.transform.position = fire_point.position;
            blt.GetComponent<Fluid_generic>().PS.transform.rotation = Quaternion.Euler(0, 0, transform.rotation.z - blt.GetComponent<Fluid_generic>().PS.shape.arc / 2);
            blt.GetComponent<Fluid_generic>().PS.transform.localScale = new Vector3(1,1,1);
            bullet = blt.GetComponent<Fluid_generic>().PS.gameObject;
            blt.GetComponent<Fluid_generic>().local = GetComponent<Equipable_generic>().isServer;
            blt.GetComponent<Fluid_generic>().DmgParticle_start_dmg *= thermal;
            blt.GetComponent<Fluid_generic>().DmgParticle_end_dmg *= thermal;
        }
        if (isSemiAuto)
        {
            modes.Add(FireMode.Semi_auto);
        }
        if (isFullyAuto)
        {
            modes.Add(FireMode.Fully_auto);
        }
        for(int i = 0; i < modes.Count; i++)
        {
            if(modes[i] == firemode)
            {
                firemode_index = i;
                break;
            }
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        //Adjust aim
        if ((firecone_angle > 0) && (Time.time > time_to_readjust))
        {
            time_to_readjust = Time.time + 1 / rate_of_readjust;
            firecone_angle -= accuracy / readjust_factor;
        }else if (firecone_angle < 0) 
        {
            firecone_angle = 0;
        }
    }



    public bool isShotgun()
    {
        return burst_shots > 1;
    }


    //Shooting mechanic: Local player shoots -> server shoots & test hit -> third-party clients shoot

    //Initialize bullet paths; Simulate local-wise; Ask server to simlulate on other clients
    public void Pull_trigger_player(Player_controller client_player)
    {
        if ((ammo <= 0) || (Time.time <= time_to_fire || is_shoot_inside_wall()))
        {
            return;
        }
        client_player.reloading = false;
        client_player.body.anim_reload(false);
        time_to_fire = Time.time + 1 / rate_of_fire;
        //Obtain bullet initial position and direction
        short firepoint_x = (short)(fire_point.position.x * CONSTANTS.SYNC_POS_MUTIPLIER);
        short firepoint_y = (short)(fire_point.position.y * CONSTANTS.SYNC_POS_MUTIPLIER);
        short aim_angle_short = get_aim_dir_short();
        float aim_angle_float = get_aim_dir_float();

        //Calculate additional bullet output due to low fps
        int fps_stack = (int)Mathf.Clamp(Time.deltaTime/ (1.0f / rate_of_fire), 1, CONSTANTS.MAX_ROF_FRAMERATE_OVERLOAD);
        fps_stack = Mathf.Min(fps_stack, ammo);
        ammo -= (ushort)fps_stack;
        client_player.GetComponent<Rigidbody2D>().AddForce(-get_aim_vec().normalized * recoil * fps_stack);
        //client_player.shake_screen(shake_extent, transform.rotation.eulerAngles.z+180);





        //Initialize bullet paths
        //Simulate bullet effects
        if (burst_shots == 1)
        {
            if(fps_stack <=1)
            {
                short aim_dir_bias = get_bullet_seed_single(aim_angle_float, firecone_angle);
                //Local shooting
                shoot(client_player.gameObject, firepoint_x, firepoint_y, aim_dir_bias, null);
                //Others shooting
                if (client_player.isServer && !Local_precomputation)
                {
                    server_shoot(client_player.gameObject, firepoint_x, firepoint_y, aim_dir_bias, null, ammo <= 0);
                }
                else if (!Local_precomputation)
                {
                    client_player.Cmd_request_shoot_optimized_single(firepoint_x, firepoint_y, aim_dir_bias, ammo <= 0);
                }
            }
            else
            {
                sbyte[] blt_dir = get_bullet_seed_single_incremental(firecone_angle, client_player.body.aim_suppress, fps_stack);
                //Local shooting
                shoot(client_player.gameObject, firepoint_x, firepoint_y, aim_angle_short, blt_dir);
                //Others shooting
                if (client_player.isServer && !Local_precomputation)
                {
                    server_shoot(client_player.gameObject, firepoint_x, firepoint_y, aim_angle_short, blt_dir, ammo <= 0);
                }
                else if (!Local_precomputation)
                {
                    client_player.Cmd_request_shoot_optimized(firepoint_x, firepoint_y, aim_angle_short, blt_dir, ammo <= 0);
                }
            }
        }
        else if (burst_shots >= 2)
        {
            sbyte[] blt_dir = get_bullet_seed(firecone_angle, fps_stack);
            //Local shooting
            shoot(client_player.gameObject, firepoint_x, firepoint_y, aim_angle_short, blt_dir);
            //Others shooting
            if (client_player.isServer && !Local_precomputation)
            {
                server_shoot(client_player.gameObject, firepoint_x, firepoint_y, aim_angle_short, blt_dir, ammo <= 0);
            }
            else if(!Local_precomputation)
            {
                client_player.Cmd_request_shoot_optimized(firepoint_x, firepoint_y, aim_angle_short, blt_dir, ammo <= 0);
            }
        }
        
        firecone_angle = Mathf.Clamp(firecone_angle + fps_stack * (client_player.body.aim_suppress) * accuracy / bias_factor,0, accuracy - accuracy / bias_factor);
        /*
        firecone_angle += fps_stack * (client_player.body.aim_suppress) * accuracy / bias_factor;
        if(firecone_angle > accuracy - accuracy / bias_factor)
        {
            firecone_angle = accuracy;
        }
        */
        /*
        if ()
        {
            firecone_angle += (client_player.body.aim_suppress) * accuracy / bias_factor;
        }
        else
        {
            
        }
        */
    }

    //Server pull trigger
    public void Pull_trigger(Body_generic body)//AI_generic ai)
    {
        if ((ammo <= 0) || (Time.time <= time_to_fire || (body.isPlayer && is_shoot_inside_wall())))
        {
            return;
        }
        body.reloading = false;
        body.anim_reload(false);
        time_to_fire = Time.time + 1 / rate_of_fire;
        //Obtain bullet initial position and direction
        short firepoint_x = (short)(fire_point.position.x * CONSTANTS.SYNC_POS_MUTIPLIER);
        short firepoint_y = (short)(fire_point.position.y * CONSTANTS.SYNC_POS_MUTIPLIER);
        short aim_angle_short = get_aim_dir_short();
        float aim_angle_float = get_aim_dir_float();

        //Calculate additional bullet output due to low fps
        int fps_stack = (int)Mathf.Clamp(Time.deltaTime / (1.0f / rate_of_fire), 1, CONSTANTS.MAX_ROF_FRAMERATE_OVERLOAD);
        fps_stack = Mathf.Min(fps_stack, ammo);
        ammo -= (ushort)fps_stack;
        body.bodyRB.AddForce(-get_aim_vec().normalized * recoil * fps_stack);
        //client_player.shake_screen(shake_extent, transform.rotation.eulerAngles.z+180);

        /*
        if ((ammo <= 0) || (Time.time <= time_to_fire))
        {
            return;
        }
        

        short firepoint_x = (short)(fire_point.position.x * CONSTANTS.SYNC_POS_MUTIPLIER);
        short firepoint_y = (short)(fire_point.position.y * CONSTANTS.SYNC_POS_MUTIPLIER);
        short aim_angle_short = get_aim_dir_short();
        float aim_angle_float = get_aim_dir_float();

        ammo -= 1;
        time_to_fire = Time.time + 1 / rate_of_fire;
        body.bodyRB.AddForce(-get_aim_vec().normalized * recoil);//ai.GetComponent<Rigidbody2D>().AddForce(-get_aim_vec().normalized * recoil);
        body.reloading = false;//ai.reloading = false;
        */


        if (burst_shots == 1)
        {
            if (fps_stack <= 1)
            {
                short aim_dir_bias = get_bullet_seed_single(aim_angle_float, firecone_angle);
                //Local shooting
                shoot(body.gameObject, firepoint_x, firepoint_y, aim_dir_bias, null);
                //Others shooting
                if (body.isServer && !Local_precomputation)
                {
                    server_shoot(body.gameObject, firepoint_x, firepoint_y, aim_dir_bias, null, ammo <= 0);
                }
                else if (body.isPlayer && !Local_precomputation)
                {
                    body.GetComponent<Player_controller>().Cmd_request_shoot_optimized_single(firepoint_x, firepoint_y, aim_dir_bias, ammo <= 0);
                }
            }
            else
            {
                sbyte[] blt_dir = get_bullet_seed_single_incremental(firecone_angle, body.aim_suppress, fps_stack);
                //Local shooting
                shoot(body.gameObject, firepoint_x, firepoint_y, aim_angle_short, blt_dir);
                //Others shooting
                if (body.isServer && !Local_precomputation)
                {
                    server_shoot(body.gameObject, firepoint_x, firepoint_y, aim_angle_short, blt_dir, ammo <= 0);
                }
                else if (!Local_precomputation)
                {
                    body.GetComponent<Player_controller>().Cmd_request_shoot_optimized(firepoint_x, firepoint_y, aim_angle_short, blt_dir, ammo <= 0);
                }
            }
            /*
            short aim_dir_bias = get_bullet_seed_single(get_aim_dir_float(), firecone_angle);
            //Local shooting
            shoot(ai.gameObject, firepoint_x, firepoint_y, aim_dir_bias, null);
            //Others shooting
            if (!Local_precomputation)
            {
                server_shoot(ai.gameObject, firepoint_x, firepoint_y, aim_dir_bias, null, ammo <= 0);
            }
            */
        }
        else if (burst_shots >= 2)
        {
            sbyte[] blt_dir = get_bullet_seed(firecone_angle, fps_stack);
            //Local shooting
            shoot(body.gameObject, firepoint_x, firepoint_y, aim_angle_short, blt_dir);
            //Others shooting
            if (body.isServer && !Local_precomputation)
            {
                server_shoot(body.gameObject, firepoint_x, firepoint_y, aim_angle_short, blt_dir, ammo <= 0);
            }
            else if (!Local_precomputation)
            {
                body.GetComponent<Player_controller>().Cmd_request_shoot_optimized(firepoint_x, firepoint_y, aim_angle_short, blt_dir, ammo <= 0);
            }
            /*
            sbyte[] blt_dir = get_bullet_seed(firecone_angle);
            //Local shooting
            shoot(ai.gameObject, firepoint_x, firepoint_y, aim_angle_short, blt_dir);
            //Others shooting
            if (!Local_precomputation)
            {
                server_shoot(ai.gameObject, firepoint_x, firepoint_y, aim_angle_short, blt_dir, ammo <= 0);
            }
            */
        }

        //---------------------------------------------------
        firecone_angle = Mathf.Clamp(firecone_angle + fps_stack * (body.aim_suppress) * accuracy / bias_factor, 0, accuracy);

        
    }


    public bool is_shoot_inside_wall()
    {
        LayerMask layer = new LayerMask();
        layer.value = 2048;//Level layer
        RaycastHit2D hit = Physics2D.Linecast(transform.position, fire_point.position, layer);
        return hit.collider != null;
    }

    public short get_aim_dir_short()
    {
        Vector2 aimdir = get_aim_vec();
        return CONSTANTS.seed_float_to_short((Mathf.Atan2(aimdir.y, aimdir.x) * 180 / Mathf.PI), 360);
    }
    public float get_aim_dir_float()
    {
        Vector2 aimdir = get_aim_vec();
        return (Mathf.Atan2(aimdir.y, aimdir.x) * 180 / Mathf.PI);
    }
    public Vector2 get_aim_vec()
    {
        return fire_point.transform.position - transform.position;
    }
    //Computed in local machine, server/local client;
    public sbyte[] get_bullet_seed(float firecone, int frame_stack = 1)
    {
        sbyte[] blt_dir = new sbyte[burst_shots * frame_stack];
        for (int i = 0; i < burst_shots*frame_stack; i++)
        {
            blt_dir[i] = CONSTANTS.seed_float_to_sbyte(Random.Range(-precise - firecone, precise + firecone), precise + accuracy); //CONSTANTS.MAX_GUN_BIAS);// precise + firecone_angle);
        }
        return blt_dir;
    }
    //Fps stack purpose
    public sbyte[] get_bullet_seed_single_incremental(float firecone, float aim_suppress, int frame_stack)
    {
        sbyte[] blt_dir = new sbyte[frame_stack];
        float bias = aim_suppress * accuracy / bias_factor;
        for (int i = 0; i < frame_stack; i++)
        {
            blt_dir[i] = CONSTANTS.seed_float_to_sbyte(Random.Range(-precise - firecone, precise + firecone), precise+accuracy); //CONSTANTS.MAX_GUN_BIAS);// precise + (firecone + bias * i));
            firecone = Mathf.Clamp(firecone + bias, 0, accuracy - accuracy / bias_factor);
        }
        return blt_dir;
    }
    public short get_bullet_seed_single(float aim_dir, float firecone)
    {
        return CONSTANTS.seed_float_to_short(aim_dir + Random.Range(-precise - firecone, precise + firecone), 360);
    }
    
    //This function causes event on server and then ask everyone to spawn the effect
    public void server_shoot(GameObject gun_user, short fire_point_x, short fire_point_y, short aim_dir_short, sbyte[] blt_dir, bool dry_gun)
    {
        //GameObject user = GetComponent<Equipable_generic>().user;
        if(equip == null)
        {
            equip = GetComponent<Equipable_generic>();
        }
        if(equip.loaded != !dry_gun)//If called everytime, hook will update regardless if value changes
        {
            equip.loaded = !dry_gun;
        }
        
        //attract npc
        sound_watcher.summon_listener(transform.position, noise_dist, gun_user.layer);
        //If precomputed, dont spawn projectiles because they have been spawned via other means.
        if (Local_precomputation)
        {
            return;
        }
        //Spawn projectiles effects across all clients
        if (gun_user.GetComponent<Body_generic>().isPlayer)
        {
            if(blt_dir == null)// burst_shots == 1)
            {
                gun_user.GetComponent<Player_controller>().Rpc_client_shoot_optimized_single(fire_point_x, fire_point_y, aim_dir_short);
            }
            else// if(burst_shots >= 2)
            {
                gun_user.GetComponent<Player_controller>().Rpc_client_shoot_optimized(fire_point_x, fire_point_y, aim_dir_short, blt_dir);
            }


            //Simulate server-sided projectile on dedicated server
            if (cvar_watcher.isDedicated())
            {
                shoot(gun_user, fire_point_x, fire_point_y, aim_dir_short, blt_dir);
            }
        }
        else
        {
            if(blt_dir == null)//burst_shots == 1)
            {
                gun_user.GetComponent<AI_generic>().Rpc_client_shoot_optimized_single(fire_point_x, fire_point_y, aim_dir_short);
            }
            else// if(burst_shots >= 2)
            {
                gun_user.GetComponent<AI_generic>().Rpc_client_shoot_optimized(fire_point_x, fire_point_y, aim_dir_short, blt_dir);
            }
        }

    }

    //This function simulate bullets locally
    public void shoot(GameObject gun_user, short fire_point_x, short fire_point_y, short aim_dir, sbyte[] aim_dir_offset, float lag_prediction = 0)
    {
        Vector2 fire_point = new Vector2(fire_point_x / CONSTANTS.SYNC_POS_MUTIPLIER, fire_point_y / CONSTANTS.SYNC_POS_MUTIPLIER);

        Body_generic user_body = gun_user.GetComponent<Body_generic>();
        if (equip == null)
        {
            equip = GetComponent<Equipable_generic>();
        }
        bool hasAuthority = equip.isServer;

        if (!cvar_watcher.isDedicated() && !Local_precomputation) { spawn_muzzle(); }//Only spawn muzzle on clients
        
        float aim_dir_float = CONSTANTS.seed_short_to_float(aim_dir, 360);

        float firecone_maxrange = accuracy + precise;
        if (bullet.tag == "bullet")//Shooting bullet is local authoritative, meaning shooter decides if he hits
        {
            if (aim_dir_offset == null)
            {
                fire_bullet(gun_user, fire_point, aim_dir_float, local_player_protocol(user_body), lag_prediction);
            }
            else
            {
                for (int i = 0; i < aim_dir_offset.Length; i++)
                {
                    float blt_dir = aim_dir_float + CONSTANTS.seed_sbyte_to_float(aim_dir_offset[i], firecone_maxrange);
                    fire_bullet(gun_user, fire_point, blt_dir, local_player_protocol(user_body), lag_prediction);
                }
            }
        }
        else if (bullet.tag == "bullet_laser")//Shooting laser is local authoritative, meaning shooter decides if he hits
        {
            if (aim_dir_offset == null)
            {
                fire_laser(gun_user, fire_point, aim_dir_float, local_player_protocol(user_body));
            }
            else
            {
                for (int i = 0; i < aim_dir_offset.Length; i++)
                {
                    float blt_dir = aim_dir_float + CONSTANTS.seed_sbyte_to_float(aim_dir_offset[i], firecone_maxrange);
                    fire_laser(gun_user, fire_point, blt_dir, local_player_protocol(user_body));
                }
            }

        }
        else if (bullet.tag == "bullet_rocket")//Shooting rocket is server authoritative, meaning server decides if he hits
        {
            if (hasAuthority)
            {
                if (aim_dir_offset == null)
                {
                    fire_rocket(gun_user, fire_point, aim_dir_float, hasAuthority);
                }
                else
                {
                    for (int i = 0; i < aim_dir_offset.Length; i++)
                    {
                        float blt_dir = aim_dir_float + CONSTANTS.seed_sbyte_to_float(aim_dir_offset[i], firecone_maxrange);
                        fire_rocket(gun_user, fire_point, blt_dir, hasAuthority);
                    }
                }

            }
        }
        if (bullet.tag == "bullet_tesla")//This function only runs on local. local simulate path, broadcast path and damage to network
        {
            fire_tesla(gun_user, fire_point, aim_dir_float, local_player_protocol(user_body), null, lag_prediction);
            
        }
        else if (bullet.tag == "bullet_flame")//Shooting bullet is server authoritative, meaning server decides if he hits
        {
            float blt_dir = aim_dir_float + CONSTANTS.seed_sbyte_to_float(aim_dir_offset[0], firecone_maxrange);
            bullet.transform.rotation = Quaternion.Euler(0, 0, blt_dir - bullet.GetComponent<ParticleSystem>().shape.arc / 2);
            bullet.GetComponent<ParticleSystem>().Emit(burst_shots);
        }
    }

    /// <summary>
    /// Check if bullet has authority: NPC & server player only authoritative on server; Client only authoritative on local
    /// </summary>
    /// <param name="user_body"></param>
    /// <returns></returns>
    bool local_player_protocol(Body_generic user_body)
    {
        return user_body.isLocalPlayer || (user_body.isServer && !user_body.isPlayer);
    }
    public void spawn_muzzle()
    {
        if (muzzleflash != null)
        {
            Vector3 fire_pos = fire_point.position;
            fire_pos.z = muzzleflash.transform.position.z;
            AudioSource muzzle = Instantiate(muzzleflash, fire_pos, transform.rotation).GetComponent<AudioSource>();
            muzzle.maxDistance = noise_dist * 1.5f;
            muzzle.pitch = Time.timeScale;
            muzzle.priority = (int)noise_dist;
            muzzle.clip = fire_sound;
            muzzle.Play();
            //muzzle.PlayOneShot(fire_sound);

        }
    }
    void fire_bullet(GameObject activator, Vector2 fire_point, float blt_dir, bool hasAuthority, float lag_prediction = 0)
    {
        //Withdraw from pool
        Bullet_generic the_bullet = Pool_watcher.Singleton.request_blt();
        //Get new ones
        if (the_bullet == null)
        {
            the_bullet = Instantiate(this.bullet, fire_point, Quaternion.identity).GetComponent<Bullet_generic>();
            the_bullet.pool_watcher = Pool_watcher.Singleton;
            the_bullet.initial_hit_fltr = the_bullet.hit_fltr;
            the_bullet.default_gradient = the_bullet.GetComponent<TrailRenderer>().colorGradient;
            the_bullet.default_texture = the_bullet.GetComponent<TrailRenderer>().material.mainTexture;
        }
        else
        {
            the_bullet.GetComponent<TrailRenderer>().Clear();
            the_bullet.transform.position = fire_point;

        }
        the_bullet.aimdir = (new Vector2(Mathf.Cos(blt_dir * Mathf.PI / 180), Mathf.Sin(blt_dir * Mathf.PI / 180))).normalized;
        the_bullet.GetComponent<TrailRenderer>().widthMultiplier = blt_mass / 20;
        the_bullet.mass = blt_mass;
        the_bullet.speed = blt_speed;
        the_bullet.speed_muzzle = blt_speed;
        the_bullet.speed_damp = blt_speed_damp;
        the_bullet.speed_min = blt_speed_min;
        the_bullet.spark = spark;
        the_bullet.activator = activator;
        the_bullet.local = hasAuthority;
        the_bullet.isDedicated = cvar_watcher.isDedicated();
        the_bullet.lag_comp = lag_prediction;
        //Remove hit collision for ally if server flag is off
        if (cvar_watcher.allyBulletPassThru)
        {
            the_bullet.hit_fltr.value = the_bullet.initial_hit_fltr.value - (1 << activator.GetComponent<Body_generic>().hitbox_main.gameObject.layer);
        }

        //Customize color
        if (blt_custom)
        {
            the_bullet.GetComponent<TrailRenderer>().colorGradient = blt_color;
            if (blt_texture != null)
            {
                the_bullet.GetComponent<TrailRenderer>().material.mainTexture = blt_texture;
            }
        }
        //the_bullet.lag_sim(lag_prediction);
    }
    void fire_laser(GameObject activator, Vector2 fire_point, float blt_dir, bool hasAuthority)
    {
        bool new_laser = false;
        //Withdraw from pool
        Laser_generic laser = Pool_watcher.Singleton.request_lsr();
        //Get new ones
        if (laser == null)
        {
            new_laser = true;
            laser = Instantiate(bullet, fire_point, Quaternion.identity).GetComponent<Laser_generic>();
            laser.pool_watcher = Pool_watcher.Singleton;
            laser.initial_hit_fltr = laser.hit_fltr;
            laser.default_gradient = laser.GetComponent<LineRenderer>().colorGradient;
            laser.default_texture = laser.GetComponent<LineRenderer>().material.mainTexture;
        }
        else
        {
            laser.transform.position = fire_point;
        }


        
        
        //GameObject blt = Instantiate(bullet, fire_point, Quaternion.identity);
        //Laser_generic laser = blt.GetComponent<Laser_generic>();
        laser.aimdir = (new Vector2(Mathf.Cos(blt_dir * Mathf.PI / 180), Mathf.Sin(blt_dir * Mathf.PI / 180))).normalized;
        laser.start = fire_point;
        laser.temperature = thermal;
        laser.initial_width = CONSTANTS.LASER_TEMP_RAMP * thermal / 100 + CONSTANTS.LASER_BASE_WIDTH;
        laser.distance = effective_dist;

        laser.isDedicated = cvar_watcher.isDedicated();
        laser.local = hasAuthority;
        laser.activator = activator;

        //Remove hit collision for ally if server flag is off
        if (cvar_watcher.allyBulletPassThru)
        {
            laser.hit_fltr.value = laser.initial_hit_fltr.value - (1 << activator.GetComponent<Body_generic>().hitbox_main.gameObject.layer);
            //laser.hit_fltr.value -= 1 << activator.GetComponent<Body_generic>().hitbox_main.gameObject.layer;
        }

        //Customize color
        if (blt_custom)
        {
            laser.GetComponent<LineRenderer>().colorGradient = blt_color;
            if (blt_texture != null)
            {
                laser.GetComponent<LineRenderer>().material.mainTexture = blt_texture;
            }
        }
        if (!new_laser)//If new laser, start function will initialize things then call emit; if pooled, call emit() directly as reset had done the job
        {
            laser.emit();
        }
        
    }
    /// <summary>
    /// Gurranteed to be local
    /// </summary>
    /// <param name="activator"></param>
    /// <param name="fire_point"></param>
    /// <param name="blt_dir"></param>
    /// <param name="hasAuthority"></param>
    /// <param name="lag_prediction"></param>
    GameObject[] fire_tesla_init(GameObject activator, Vector2 fire_point, float blt_dir, float max_bounce_range, LayerMask hit_fltr, LayerMask obstacle_fltr)
    {
        Vector2 spread_origin = fire_point;
        Vector2 aimdir = (new Vector2(Mathf.Cos(blt_dir * Mathf.PI / 180), Mathf.Sin(blt_dir * Mathf.PI / 180))).normalized;
        Body_generic activator_body = activator.GetComponent<Body_generic>();
        Player_controller activator_controller = activator.GetComponent<Player_controller>();
        Body_generic victim_body= null;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, aimdir, blt_speed * CONSTANTS.VOLT_DIST_RATIO / 2, hit_fltr + obstacle_fltr);
        List<GameObject> collidedlist = new List<GameObject>();
        Body_hitbox_generic hitbox;
        float volts_left = blt_speed;
        int number_streams = 0;
        if (hit)
        {
            hitbox = hit.collider.GetComponent<Body_hitbox_generic>();
            if (hitbox != null) //If hit body, Damage and spread
            {
                volts_left = (1 - (Vector2.Distance(hit.point, transform.position) / (blt_speed * CONSTANTS.VOLT_DIST_RATIO))) * blt_speed;
                collidedlist.Add(hitbox.body.gameObject);
                number_streams = Mathf.Max(1,(int)(volts_left / blt_speed_min));
                spread_origin = hit.point;
            }

        }

        if(number_streams == 0)
        {
            //Send trail paths
            spawn_muzzle();
            if (activator_controller != null)
            {
                activator_body.Cmd_spawn_tesla_muzzle(gameObject);
            }
            else
            {
                activator_body.Rpc_spawn_tesla_muzzle(gameObject);
            }
            return null;
        }

        List<GameObject>[] stream_path = new List<GameObject>[number_streams];
        List<Tesla_generic.teslaNode> spread_list = new List<Tesla_generic.teslaNode>();
        Tesla_generic.teslaNode initial_node = new Tesla_generic.teslaNode();
        initial_node.target = hit.collider.gameObject;
        initial_node.volts_left = volts_left;
        initial_node.stream_idxes = new int[number_streams];
        collidedlist.Add(hit.collider.gameObject);
        for (int i = 0; i < number_streams; i++)
        {
            initial_node.stream_idxes[i] = i;
        }
        //Debug.LogError("distance: "+ Vector2.Distance(hit.point, transform.position) + "; volt left: "+volts_left+ "; number streams: "+number_streams);
        spread_list.Add(initial_node);






        int path_size = 0;
        //while there are stem that is branching out
        while (spread_list.Count > 0)//Each step
        {
            //Debug.LogError("step: "+spread_list.Count);
            List<Tesla_generic.teslaNode> spread_temp = new List<Tesla_generic.teslaNode>();
            //For each of the stem
            for (int i = 0; i < spread_list.Count; i++)
            {
                List<Tesla_generic.teslaNode> current_spread_temp = new List<Tesla_generic.teslaNode>();
                int j;
                Tesla_generic.teslaNode node = spread_list[i];
                victim_body = node.target.GetComponent<Body_generic>();
                if (victim_body == null)//structure doesnt take damage from tesla
                {
                    continue;
                }
                if (activator_body.isServer)
                {
                    if(node.target.layer != activator.layer)//Only if damaging opposite team will bleed
                    {
                        victim_body.request_bleed(victim_body.transform.position, 0, false);
                    }
                    victim_body.damage(activator, Vector2.zero, dmg_electric: node.volts_left / 30);
                }
                else
                {
                    activator_controller.add_to_shot_list(victim_body.gameObject, node.volts_left / 30, victim_body.transform.position, 0, 0, false, 2);
                }
                if (activator_body.isLocalPlayer)
                {
                    activator_controller.hit_mark();
                }
                spread_origin = node.target.transform.position;
                //Debug.LogError("From node: "+node.target + "; volt: "+node.volts_left);
                //Mark all the stream with its index
                for (j = 0; j < node.stream_idxes.Length; j++)
                {
                    if (stream_path[node.stream_idxes[j]] == null)
                    {
                        stream_path[node.stream_idxes[j]] = new List<GameObject>();
                    }
                    stream_path[node.stream_idxes[j]].Add(node.target);
                    path_size++;
                    //Debug.LogError("add: " + node.target);
                }
                //overlap circle

                Collider2D[] victims = Physics2D.OverlapCircleAll(spread_origin, Mathf.Min(node.volts_left * CONSTANTS.VOLT_DIST_RATIO, radius), hit_fltr);
                //Debug.LogError("hit count: " + victims.Length);
                if (victims.Length == 0)
                {
                    continue;
                }

                //Sort circle
                victims = victims.OrderBy(o => Vector2.Distance(o.transform.position, spread_origin)).ToArray();
                //Pointer
                j = 0;
                //Compute distance for the next stem, put on dist_to_parent
                float volt = node.volts_left;
                float total_volt_to_parent = 0;
                Tesla_generic.teslaNode next_node = new Tesla_generic.teslaNode();
                next_node.target = victims[j].gameObject;
                next_node.volt_to_parent = Vector2.Distance(victims[j].transform.position, spread_origin) / CONSTANTS.VOLT_DIST_RATIO;
                //While this stem has voltage && can reach next stem
                int hits = 0;
                while (volt > blt_speed_min + next_node.volt_to_parent && hits <= CONSTANTS.TESLA_MAX_SPLIT)
                {
                    //If the next stemmed object isnt on collided list
                    if (!collidedlist.Contains(next_node.target) && !Physics2D.Linecast(spread_origin, next_node.target.transform.position, obstacle_fltr))
                    {

                        //subtract copy of dist/volt_ratio\
                        volt -= next_node.volt_to_parent + blt_speed_min;
                        total_volt_to_parent += next_node.volt_to_parent;

                        //Copy the stem onto a spread_temp list
                        spread_temp.Add(next_node);
                        current_spread_temp.Add(next_node);
                        hits++;
                        //Put the stemmed object on collided list
                        collidedlist.Add(next_node.target);
                        //Debug.LogError("spread to: " + next_node.target + "; volt distance: " + next_node.volt_to_parent);
                    }

                    //Increment pointer
                    j++;

                    if (j >= victims.Length)
                    {
                        break;
                    }
                    next_node = new Tesla_generic.teslaNode();
                    //take the next stem on the sorted list
                    next_node.target = victims[j].gameObject;
                    //Compute distance for the next stem
                    next_node.volt_to_parent = Vector2.Distance(victims[j].transform.position, spread_origin) / CONSTANTS.VOLT_DIST_RATIO;

                }
                //Pointer current_idx = 0
                int current_idx = 0;
                //For each on the spread_temp list
                for (int k = 0; k < current_spread_temp.Count; k++)
                {
                    next_node = current_spread_temp[k];
                    //stem volt = (1 - dist_to_parent / volt_of_current_stem) * volt_of_current_stem
                    if (current_spread_temp.Count == 1)
                    {
                        next_node.volts_left = volt;
                    }
                    else
                    {
                        next_node.volts_left = ((total_volt_to_parent - next_node.volt_to_parent) / (current_spread_temp.Count - 1)) * volt / total_volt_to_parent;
                    }

                    //Debug.LogError("spread to: " + next_node.target + "; volt distance: " + next_node.volt_to_parent);
                    //Debug.LogError("volts total: "+node.volts_left+"; total parent: "+ total_volt_to_parent + "; this: "+ next_node.volts_left+ "; volt to parent: "+ next_node.volt_to_parent);
                    //Initialize volt / minimum for the stream idxes
                    if(next_node.volts_left <= 0)
                    {
                        current_spread_temp.RemoveAt(k);
                        k--;
                        continue;
                    }
                    next_node.stream_idxes = new int[Mathf.Max(1, (int)(next_node.volts_left / blt_speed_min))];//Must be at least one stream
                                                                                                                   //For volt / minimum
                    
                    for (int y = 0; y < next_node.stream_idxes.Length; y++)
                    {
                        //stream_idxes[] = current_stream_idxes[k]
                        next_node.stream_idxes[y] = node.stream_idxes[current_idx];
                        /*
                        try
                        {
                            
                        }
                        catch
                        {
                            Debug.LogError("bug!");
                            Debug.LogError("next node volt: " + next_node.volts_left);
                            Debug.LogError("spread count: " + current_spread_temp.Count + "; number rays: " + next_node.stream_idxes.Length + "; number ray current: " + node.stream_idxes.Length + "; currect:" + current_idx);
                        }
                        */
                        current_idx++;
                    }
                }
            }
            spread_list.Clear();
            spread_list = spread_temp;
        }

        if (stream_path.Length == 0)
        {
            return null;
        }
        GameObject[] serialized_path = new GameObject[path_size + stream_path.Length];
        int serialized_index = 0;
        for (int i = 0; i < stream_path.Length; i++)
        {
            for (int j = 0; j < stream_path[i].Count; j++)
            {
                serialized_path[serialized_index] = stream_path[i][j];
                serialized_index++;
            }
            serialized_index++;
        }

        //Send trail paths
        if (activator_body.isPlayer && activator_body.hasAuthority)
        {
            activator_body.Cmd_send_tesla_path(serialized_path, gameObject);
        }
        else
        {
            activator_body.Rpc_send_tesla_path(serialized_path, gameObject);
        }
        return serialized_path;
    }
    public void fire_tesla(GameObject activator, Vector2 fire_point, float blt_dir, bool hasAuthority, GameObject[] serialized_path, float lag_prediction = 0)
    {
        //Withdraw from pool
        Tesla_generic tesla = Pool_watcher.Singleton.request_tsla();
        //Get new ones
        if (tesla == null)
        {
            tesla = Instantiate(this.bullet, fire_point, Quaternion.identity).GetComponent<Tesla_generic>();
            tesla.pool_watcher = Pool_watcher.Singleton;
            tesla.initial_hit_fltr = tesla.hit_fltr;
        }
        else
        {
            tesla.transform.position = fire_point;

        }

        tesla.aimdir = (new Vector2(Mathf.Cos(blt_dir * Mathf.PI / 180), Mathf.Sin(blt_dir * Mathf.PI / 180))).normalized;
        //tesla.GetComponent<TrailRenderer>().widthMultiplier = GetComponent<Gun_generic>().blt_mass / 20;

        tesla.local = hasAuthority;
        tesla.activator = activator;

        //Remove hit collision for ally if server flag is off
        if (cvar_watcher.allyBulletPassThru)
        {
            tesla.hit_fltr.value = tesla.initial_hit_fltr.value - (1 << activator.GetComponent<Body_generic>().hitbox_main.gameObject.layer);
        }



        
        //If local, initialize the tesla path and use it, otherwise have network download the path from the local client
        if (hasAuthority && serialized_path == null)
        {
            serialized_path = fire_tesla_init(activator, fire_point, blt_dir, tesla.max_bounce_range, tesla.hit_fltr, tesla.obstacle_fltr);
        }
        
        //Spawn effect
        if (serialized_path == null)
        {
            return;
        }
        int num_trails = 0;

        for (int i = 0; i < serialized_path.Length; i++)
        {
            if(serialized_path[i] == null)
            {
                num_trails++;
            }
        }
        tesla.stream_path = new List<GameObject>[num_trails];
        int j = 0;
        for (int i = 0; i < serialized_path.Length; i++)
        {
            if (serialized_path[i] == null)
            {
                j++;
            }
            else
            {
                if(tesla.stream_path[j] == null)
                {
                    tesla.stream_path[j] = new List<GameObject>();
                }
                tesla.stream_path[j].Add(serialized_path[i]);
            }
        }
        
        tesla.emit();
    }
    void fire_rocket(GameObject activator, Vector2 fire_point, float blt_dir, bool hasAuthority)
    {
        GameObject rkt = Instantiate(bullet, fire_point, Quaternion.identity);
        NetworkServer.Spawn(rkt);
        Rocket_generic rocket = rkt.GetComponent<Rocket_generic>();         
        rocket.aimdir = (new Vector2(Mathf.Cos(blt_dir * Mathf.PI / 180), Mathf.Sin(blt_dir * Mathf.PI / 180))).normalized;
        rkt.transform.rotation = transform.rotation;
        rocket.speed = blt_speed;
        rocket.transform.rotation = Quaternion.Euler(0, 0, blt_dir);
        rocket.activator = activator;
        rocket.local = true;
        //Remove hit collision for ally if server flag is off
        if (cvar_watcher.allyBulletPassThru)
        {
            rocket.hit_fltr.value -= 1 << activator.GetComponent<Body_generic>().hitbox_main.gameObject.layer;
        }

        //
    }
    
    
    
    /*
    sbyte seed_float_to_sbyte(float seed_angle_float, float range)
    {
        return (sbyte)(127f * (seed_angle_float / range));
    }
    float seed_sbyte_to_float(sbyte seed_angle_sbyte, float range)
    {
        return range * ((float)seed_angle_sbyte / 127f);
    }
    short seed_float_to_short(float seed_angle_float, float range)
    {
        return (short)(32767f * (seed_angle_float / range));
    }
    float seed_short_to_float(short seed_angle_short, float range)
    {
        return range * ((float)seed_angle_short / 32767f);
    }
    */
    //Server
    public void switch_mode()
    {
        if(firemode_index < modes.Count - 1)
        {
            firemode_index++;
        }else
        {
            firemode_index = 0;
        }
        firemode = modes[firemode_index];
    }


    public Equipable_generic.ITEM_TYPE getType()
    {
        return Equipable_generic.ITEM_TYPE.gun;
    }

    public ushort getWeight()
    {
        return weapon_weight;
    }

    public ushort getSize()
    {
        return weapon_size;
    }

}



