using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(sendInterval = 0.05f)]
public class AI_generic : NetworkBehaviour {
    public enum AI_personality {


    }

    public float lookaround_min_interval = 1;
    public float lookaround_max_interval = 10;
    static float hearing_interval = 2;
    static float hearing_forget_interval = 10;
    [HideInInspector] public bool tuned_in = false;
    /// <summary>
    /// This overrides view radius and focus on the current closest enemy, to prevent detection on further enemies and waste calculation cpu
    /// </summary>
    float eye_sight;
    /// <summary>
    /// This saves the closest enemy and prevent real-time call on every frame
    /// </summary>
    GameObject closest_enemy_cache;
    float detection_interval = 0.15f;
    float time_to_detect = 0;
    //Orient
    public float orient_force = 225;
    public float look_force = 350;
    //Attack
    public float reaction_time = 1;
    public float time_response = 0;
    public float melee_range = 1;
    //Dodge
    public float dodge_dist = 100;
    //Use
    public GameObject[] Weapon_choices;
    [SyncVar(hook = "Hook_equip")] public GameObject equiped_item;
    //public ushort inventory_size = 1000;
    //[HideInInspector] public ushort initial_inventory_size = 1000;
    [SyncVar] public ushort inventory_weight = 0;
    private Gun_generic gun;
    public GameObject ammo_drop_template;
    /// <summary>
    /// Must be signed, if unsigned the value will warp around if drop below 0
    /// </summary>
    public int ammo_drop_count = 0;
    //Movement
    //public float stress_resistent;
    private Navigation_manual navigation;
    private Sound_watcher sound_watcher;
    public float nav_update_interval;
    public float nav_update_interval_urgent;
    
    //Navigation
    /// <summary>
    /// This is the time when npc gets nav update regularly
    /// </summary>
    private float time_to_updateNav = 0;
    /// <summary>
    /// This is the time when ai is allowed to update nav when: unable to track path, target go out of range
    /// </summary>
    private float time_to_updateNav_urgent = 0;
    [HideInInspector] public List<Navigation_manual._Node> path = new List<Navigation_manual._Node>();

    //Network
    bool moving = false;
    Vector2 movetopos;
    Vector2 lookatpos;
    GameObject movetoobj;
    GameObject lookatobj;
    GameObject dodgefrom;
    public Vector2 navtopos;
    public GameObject navtoobj;
    byte dec_case;
    float time_to_respond = 0;
    /// <summary>
    /// After this time stamp, npc revert to reaction timer system; If an AI is not relax, it will immediately attack enemy without reaction time
    /// </summary>
    float time_to_relax = 0;
    bool damageDistracted = false;
    Vector2 damage_dir;
    static int damageDistraction_time = 1;
    static int damage_detection_interval = 4;
    float time_to_resume_from_dmg = 0;
    float time_to_detect_damage = 0;
    public Vector2 sound_source = CONSTANTS.VEC_NULL;
    Vector2 look_corner_pos = CONSTANTS.VEC_NULL;
    Vector2 move_patrol_pos = CONSTANTS.VEC_NULL;
    float time_to_hear_again = 0;
    float time_to_forget_sound = 0;
    float time_to_lookback = 0;
    static float lookback_interval = 2;
    [HideInInspector] public bool lookback_scheduled = false;

    [SyncVar] public bool sync_moving = false;
    //[SyncVar]
    public Vector2 sync_movetopos;
    [SyncVar] public short sync_movetopos_x;
    [SyncVar] public short sync_movetopos_y;
    //[SyncVar]
    /// <summary>
    /// Actual coordinate, no multiplier
    /// </summary>
    public Vector2 sync_lookatpos;
    [SyncVar] public short sync_lookatpos_x;
    [SyncVar] public short sync_lookatpos_y;
    [SyncVar] public GameObject sync_movetobj = null;
    [SyncVar] public GameObject sync_lookatobj = null;
    [SyncVar] public GameObject sync_dodgefrom = null;
    [SyncVar] public byte sync_dec_case;
    [SyncVar(hook = "hook_pos_update_x")] public short sync_x;
    [SyncVar(hook = "hook_pos_update_y")] public short sync_y;
    private float time_to_update_pos = 0;
    float pos_update_interval = 0.3f;
    

    public enum AI_condition { IDLE, FEAR, AGGRESSIVE, PASSIVE_AGGRESSIVE, SCRIPT}
    [HideInInspector] public bool suppressed = false;




    public AI_condition cond = AI_condition.IDLE;//0 idle; 1 fear, avoiding; 2 agressive, chasing and shooting; 3 passive aggressive, avoiding and shooting

    public LayerMask detectionFilter;
    public LayerMask detectionAllyFilter;
    //public float reload_multiplier = 1.0f;
    /// <summary>
    /// Animation bugs, robot animation plays slower than human animation, on AI only
    /// </summary>
    public float reload_anim_duration = 1f;
    public bool reloading = false;
    private float time_reload_finish = 0;
    public List<GameObject> memory_enemies = new List<GameObject>();
    static int max_enemies_memory = 3;
    private float time_to_lookaround = 0;
    

    private Rigidbody2D zombieRB;

    [HideInInspector] public Body_generic body;
    private Animator anim_upper;
    private Animator anim_lower;
    Server_watcher cvar_watcher;
    Interpolator_generic interpolater;
    public int path_index = 0;

    Client_watcher client_watcher;
    float time_prev = 0;
    float delta_time = 1;
    float time_to_build_base = 0;


    bool isDedicatedServer()
    {
        return isServer && !isClient;
    }
    public override void OnStartClient()
    {
        if (equiped_item != null && equiped_item.GetComponent<Equipable_generic>().parented == false)
        {
            Hook_equip(equiped_item);
            equiped_item.GetComponent<Equipable_generic>().Hook_mdl_equip(gameObject);
        }
        
    }
    // Use this for initialization
    void Awake () {
        GetComponent<Body_generic>().OnDamaged = OnDamaged;
        zombieRB = GetComponent<Rigidbody2D>();
        body = GetComponent<Body_generic>();
        interpolater = GetComponent<Interpolator_generic>();
        anim_upper = body.anim_upper;
        anim_lower = body.anim_lower;
        navigation = FindObjectOfType<Navigation_manual>();//GameObject.Find("Navigation").GetComponent<Navigation_manual>();
        sound_watcher = FindObjectOfType<Sound_watcher>();//GameObject.Find("Sound_watcher").GetComponent<Sound_watcher>();
        cvar_watcher = Server_watcher.Singleton;
        if (isServer)
        {
            personality_form();
            
        }

        
    }

    void Start()
    {
        if (!isServer)
        {
            Team_watcher[] teams = FindObjectsOfType<Team_watcher>();
            for (int i = 0; i < teams.Length; i++)
            {
                if (teams[i].race == GetComponent<Body_generic>().character_type)
                {
                    body.team = teams[i];
                }
            }
        }
        else
        {
            Destroy(GetComponent<Interpolator_generic>());
        }
    }

    void Update () {
        delta_time = Time.deltaTime;
        if (body.isDead || (body.character_cond != Body_generic.Character_condition.FREE && body.character_cond != Body_generic.Character_condition.SCRIPTED))
        {
            return;
        }

        //AI analyzing actions
        if (isServer)
        { 
            //aggressive
            if (cond == AI_condition.AGGRESSIVE && body.character_cond == Body_generic.Character_condition.FREE)
            {
                //Detect closest
                
                GameObject closest = get_closest_seen_enemy();
                GameObject remembered_object = i_remember();
                if(remembered_object == null && cvar_watcher.map_type == CONSTANTS.MAP_TYPE.PVP)
                {
                    remembered_object = body.team.request_enemy_base();
                }
                bool relaxed = true;//When AI is relaxed, its reflex slows down and will have react interval before attacking
                if(closest != null)
                {
                    relaxed = false;
                    if(remembered_object != null && Vector2.Distance(transform.position, remembered_object.transform.position) < Vector2.Distance(zombieRB.position, closest.transform.position) && navigation.LOS(zombieRB.position, remembered_object.transform.position, -1, navigation.LOS_block))
                    {
                        closest = remembered_object;
                    }
                }
                else if(remembered_object != null)//If npc currently remembers a target but not facing toward it while looking at path, regard it as seen
                {
                    if(Vector2.Distance(zombieRB.position, remembered_object.transform.position) < body.viewRadius && navigation.LOS(zombieRB.position, remembered_object.transform.position, -1, navigation.LOS_block))
                    {
                        closest = remembered_object;
                    }
                }


                //Reset moving = true
                moving = true;
                //No fire power, drop item and engage melee
                if (equiped_item != null && gun.ammo <= 0 && ammo_drop_count <= 0)
                {
                    Drop_item(equiped_item, Vector2.zero);
                }
                //If spot
                if (closest != null)
                {
                    //1. cant shoot(gun user), dodge
                    if (!canShoot())
                    {
                        reload_gun();
                        //If find hide location
                        Vector2 hidepos = get_nearest_cover_from(closest);
                        if (hidepos != CONSTANTS.VEC_NULL)
                        {
                            //Make dodgefrom = closest
                            dodgefrom = closest;
                            //Make navtopos = hidepos
                            navtopos = hidepos;
                            //Make navtoobj = null
                            navtoobj = null;
                            //Make lookatobj = closest
                            lookatobj = closest;
                            //case 14
                            dec_case = 14;

                        }
                        //If cant find hide
                        else
                        {
                            //Make dodgefrom = closest
                            dodgefrom = closest;
                            //Make lookatobj = closest
                            lookatobj = closest;
                            //Make moving = false
                            moving = false;
                            //Make navtopos = null
                            navtopos = CONSTANTS.VEC_NULL;
                            //Make navtoobj = null
                            navtoobj = null;
                            //case 0
                            dec_case = 0;

                        }
                    }
                    //2. New target; Reacting if doesnt remember this object and when AI is in relax state
                    else if (!remember(closest) && Time.time > time_to_relax)
                    {
                        //Make lookatobj = closest
                        lookatobj = closest;
                        //Make moving = false
                        moving = false;
                        //Make navtopos = null
                        navtopos = CONSTANTS.VEC_NULL;
                        //Make navtoobj = null
                        navtoobj = null;
                        //Store to memory
                        memory_store(closest);
                        //Reaction time
                        time_to_respond = Time.time + reaction_time;
                        //case 3
                        dec_case = 3;

                    }
                    //3. Old target, Remember || reaction time passes
                    else if (Time.time > time_to_respond)
                    {
                        //If within minimum shooting range
                        if (is_effective_shooting_range(closest))
                        {
                            //Make moving = false
                            moving = false;
                            //Make lookatobj = closest
                            lookatobj = closest;
                            //navtopos dont change

                            //navtoobj dont change

                            //case 1
                            dec_case = 1;

                        }
                        //If outside moving shooting range
                        else
                        {
                            //Make navtoobj = closest
                            navtoobj = closest;
                            //Make lookatobj = closest
                            lookatobj = closest;
                            //case 2
                            dec_case = 2;

                        }

                    }

                    if (!relaxed)
                    {
                        time_to_relax = Time.time + CONSTANTS.AI_relax_time;
                    }
                }
                //If non spotted
                else
                {
                    if (Time.time > time_to_build_base)
                    {
                        time_to_build_base = Time.time + CONSTANTS.AI_FIND_ZONE_INTERVAL;
                        body.build_base();
                    }
                    if (equiped_item != null && (gun.ammo < gun.capacity * 0.55f || (gun.isShotgun() && gun.ammo < gun.capacity)))
                    {
                        reload_gun();
                    }
                    //1. Track damage source
                    //If were damaged
                    if (under_damage_distraction())
                    {
                        //If No-ammo reloading
                        if (!canShoot())
                        {
                            //Make lookatpos = damage source
                            lookatpos = zombieRB.position + damage_dir;
                            //Make moving = false
                            moving = false;
                            //Make navtopos = null
                            navtopos = CONSTANTS.VEC_NULL;
                            //Make navtoobj = null
                            navtoobj = null;
                            //Make lookatobj = null
                            lookatobj = null;
                            //case 4
                            dec_case = 4;

                        }
                        //If has ammo
                        else
                        {
                            
                            //if has original target, 
                            if (is_pursuing_target())
                            {
                                //Make lookatpos = damage source
                                lookatpos = zombieRB.position + damage_dir;
                                //navtopos dont change

                                //navtoobj dont change

                                //Make lookatobj = null
                                lookatobj = null;
                                //case 5
                                dec_case = 5;

                            }

                            //If no original target
                            else
                            {
                                //Make lookatpos = damage source
                                lookatpos = zombieRB.position + damage_dir;
                                //Make moving = false;
                                moving = false;
                                //Make navtopos = null
                                navtopos = CONSTANTS.VEC_NULL;
                                //Make navtoobj = null
                                navtoobj = null;
                                //Make lookatobj = null
                                lookatobj = null;
                                //case 7
                                dec_case = 7;

                            }
                            //Debug.DrawLine(transform.position, lookatpos);
                        }

                    }

                    //2. Track memory
                    //If remember
                    else if (remembered_object != null)
                    {
                        //If No-ammo reloading
                        if (!canShoot())
                        {
                            //Make moving = false
                            moving = false;
                            //Lookatpos dont change

                            //Make navtopos = null
                            navtopos = CONSTANTS.VEC_NULL;
                            //Make navtoobj = null
                            navtoobj = null;
                            //Make lookatobj = null
                            lookatobj = null;
                            //case 8
                            dec_case = 8;
                        }
                        //If has ammo
                        else
                        {
                            moving = true;
                            //Make navtoobj = remembered
                            navtoobj = remembered_object;
                            //Make lookatpos = path node(reference later)

                            //Make lookatobj = null
                            lookatobj = null;
                            //case 9
                            dec_case = 9;
                        }
                    }

                    //3. Track sound
                    //If hear
                    else if (sound_source != CONSTANTS.VEC_NULL && Time.time < time_to_forget_sound)//If has sound to investigate
                    {
                        //When its time to hear again
                        if(Time.time > time_to_hear_again)
                        {
                            sound_watcher.tune_in_ai(this);
                        }
                        else if (tuned_in)//Before hearing again or forget sound, make sure sound doesnt update
                        {
                            sound_watcher.tune_out_ai(this);
                        }

                        //If reach
                        if (arrive_at(sound_source))
                        {
                            sound_watcher.tune_in_ai(this);
                            sound_source = CONSTANTS.VEC_NULL;
                        }
                        //If No-ammo reloading
                        else if (!canShoot())
                        {
                            //Make moving = false
                            moving = false;
                            //Make lookatpos = sound source
                            lookatpos = sound_source;
                            //Make navtopos = null
                            navtopos = CONSTANTS.VEC_NULL;
                            //Make navtoobj = null
                            navtoobj = null;
                            //Make lookatobj = null
                            lookatobj = null;
                            //case 10
                            dec_case = 10;

                        }
                        //If has ammo
                        else
                        {
                            //Make navtopos = sound source
                            navtopos = sound_source;
                            //Make lookatpos = sound source
                            lookatpos = sound_source;
                            //Make navtoobj = null
                            navtoobj = null;
                            //Make lookatobj = null
                            lookatobj = null;
                            //case 11
                            dec_case = 11;

                        }

                    }

                    //4. AlertPatrol
                    else
                    {
                        //keep ear up for sound
                        if (!tuned_in)
                        {
                            sound_watcher.tune_in_ai(this);
                        }
                        
                        //If have not arrive patrol position
                        if (Vector2.Distance(zombieRB.position, move_patrol_pos) <= body.size * 2 || move_patrol_pos == CONSTANTS.VEC_NULL)
                        {
                            move_patrol_pos = get_patrol_location();
                        }
                        //If time to look around, toggle patrol_lookpath to lookpath later
                        if (Time.time > time_to_lookaround && Time.time < time_to_lookback)
                        {
                            
                            //If corner point is null || corner point can no longer be seen
                            if (look_corner_pos == CONSTANTS.VEC_NULL || !navigation.LOS(look_corner_pos, zombieRB.position, -1, navigation.LOS_block))
                            {
                                look_corner_pos = get_corner_pos();
                            }
                            lookback_scheduled = false;
                            //Make lookatpos = corner point
                            lookatpos = look_corner_pos;
                            //Make navtopos = random pos
                            navtopos = move_patrol_pos;
                            //Make navtoobj = null
                            navtoobj = null;
                            //Make lookatobj = null
                            lookatobj = null;
                            //case 12
                            dec_case = 12;

                        }
                        //If time to resume look && is moving
                        else
                        {
                            if (!lookback_scheduled)
                            {
                                lookback_scheduled = true;
                                time_to_lookaround = Time.time + UnityEngine.Random.Range(lookaround_min_interval, lookaround_max_interval);
                                time_to_lookback = time_to_lookaround + lookback_interval;
                            }
                            
                            //Make lookatpos = path node(reference later)
                            
                            //Make navtopos = random pos
                            navtopos = move_patrol_pos;
                            //Make navtoobj = null
                            navtoobj = null;
                            //Make lookatobj = null
                            lookatobj = null;
                            //case 13 
                            dec_case = 13;

                        }
                    }
                }
            }
            else if(body.character_cond == Body_generic.Character_condition.SCRIPTED)
            {
                if(body.scriptedSequence.state == Scripted_sequence.STATE.Movement)
                {
                    moving = true; 
                }
                else
                {
                    moving = false;
                }
            }

            


            //Navigation=====================================
            Vector2 path_node_pos = Vector2.zero;
            int nav_case = 9;
            //If moving = true
            if (moving == true)
            {
                //if navtoobj != null
                if (navtoobj != null)
                {
                    //navtopos = navtoobj.position
                    
                    navtopos = navtoobj.transform.position;
                }


                //Needs navigation
                //if (!navigation.LOS(transform.position, navtopos, body.size, navigation.Path_block))
                if (!navigation.LOS(zombieRB.position, navtopos, -1, navigation.Path_block))
                {

                    bool path_valid = false;//If ai has no path OR the path is no longer useable
                    //If path available
                    if (path != null && path.Count > 0)
                    {
                        
                        if (path_index >= path.Count)//End of the line and cant find target
                        {
                            path_index = 0;
                            path.Clear();
                        }
                        //1. Try navigate to the next node
                        else if (path_index + 1 < path.Count && navigation.LOS(zombieRB.position, path[path_index + 1].position, -1, navigation.Path_block))
                        {
                            //Make path node 2
                            path_index++;
                            path_valid = true;
                        }
                        //2. Try navigate to the current node
                        else if ((path_index < path.Count && navigation.LOS(zombieRB.position, path[path_index].position, -1, navigation.Path_block)))
                        {
                            path_valid = true;
                        }
                        else//Fail to track current node, attempt to loop through path for valid node
                        {
                            for(int i = path.Count - 1; i >= 0; i--)
                            {
                                //Debug.LogError("finding");
                                if (navigation.LOS(zombieRB.position, path[i].position, -1, navigation.Path_block))
                                {
                                    path_valid = true;
                                    path_index = i;
                                    break;
                                }
                            }

                            if (!path_valid)
                            {
                                path_index = 0;
                                path.Clear();
                            }
                        } 
                    }
                    

                    
                    if(path_valid)
                    {
                        //nav_case 0
                        nav_case = 0;
                        path_node_pos = path[path_index].position;
                    }
                    else
                    {
                        //moving = false
                        moving = false;
                        //nav_case 1
                        nav_case = 1;
                    }
                    
                    //request navigation
                    if (Time.time > time_to_updateNav || (!path_valid && Time.time > time_to_updateNav_urgent))//request a nav if nav is out-dated
                    {
                        time_to_updateNav = Time.time + nav_update_interval;
                        time_to_updateNav_urgent = Time.time + nav_update_interval_urgent;
                        navigation.RequestPath(gameObject);
                        
                    }
                }
                //target can be directed to
                else
                {
                    //if navtoobj != null
                    if (navtoobj != null)
                    {
                        //Make movetoobj = navtoobj
                        movetoobj = navtoobj;
                        //nav_case 2
                        nav_case = 2;
                        
                    }

                    //if navtoobj == null
                    else
                    {
                        //Make movetopos = navtopos
                        movetopos = navtopos;
                        //nav_case 3
                        nav_case = 3;


                    }
                }
            }
            

            //Case analysis==================================

            //Passing nav_case to case to reduce sync size
            //sync_case = case * 10 + nav_case
            byte case_encoded = (byte)(dec_case * 10 + nav_case);
            sync_dec_case = case_encoded;
            //sync_moving = moving
            sync_moving = moving;


            //case 14: Reloading in the middle of encounter and fleeing to hiding spot
            if(dec_case == 14)
            {
                //sync_lookatobj = lookatobj
                sync_lookatobj = lookatobj;
                //sync_dodgefrom = dodgefrom
                sync_dodgefrom = dodgefrom;
                //if nav_case 0: need to navigate
                if(nav_case == 0)
                {
                    //sync_movetopos = path_node_pos
                    sync_movetopos = path_node_pos;
                }
                //if nav_case 3: tracking point
                else if(nav_case == 3)
                {
                    //sync_movetopos = movetopos
                    sync_movetopos = movetopos;
                }
            }
            

            //case 0: Reloading in the middle of encounter but not where to hide
            else if(dec_case == 0)
            {
                //sync_lookatobj = lookatobj
                sync_lookatobj = lookatobj;
                //sync_dodgefrom = dodgefrom
                sync_dodgefrom = dodgefrom;
            }
            

            //case 1: Engage in attack within mimimum shooting range
            else if(dec_case == 1)
            {
                //sync_lookatobj = lookatobj
                sync_lookatobj = lookatobj;
            }
            

            //case 2: Engage in attack outside of mimimum shooting range
            else if(dec_case == 2)
            {
                //sync_lookatobj = lookatobj
                sync_lookatobj = lookatobj;
                //if nav_case 0: need navigation
                if(nav_case == 0)
                {
                    //sync_movetopos = path_node_pos
                    sync_movetopos = path_node_pos;
                }
                    
                //if nav_case 2: tracking object
                else if(nav_case == 2)
                {
                    //sync_movetoobj = movetoobj
                    sync_movetobj = movetoobj;
                }
                    
                //if nav_case 3: tracking point
                else if(nav_case == 3)
                {
                    //sync_movetopos = movetopos
                    sync_movetopos = movetopos;
                }
                    
            }
            

            //case 3: See a new target and surprise
            else if(dec_case == 3)
            {
                //sync_lookatobj = lookatobj
                sync_lookatobj = lookatobj;
            }
            

            //case 4: Being shot when reloading, face to damage source but hesitate to move
            else if(dec_case == 4)
            {
                //sync_lookatpos = lookatpos
                sync_lookatpos = lookatpos;
            }
            

            //case 5: Being shot when pursuing enemy, face to damage source but continue moving to original target
            else if(dec_case == 5)
            {
                //sync_lookatpos = lookatpos
                sync_lookatpos = lookatpos;
            }
            

            //case 6: Being shot when pursuing enemy, resume to face back to path
            else if(dec_case == 6)
            {
                //sync_lookatpos = pathnode
                sync_lookatpos = path_node_pos;
            }
            

            //case 7: Being shot when patroling, stop look at damage source
            else if(dec_case == 7)
            {
                //sync_lookatpos = lookatpos
                sync_lookatpos = lookatpos;
            }
            

            //case 8: Remember an enemy but reloading, so cant moves

            //case 9: Remember an enemy when able to attack, move to remembered enemy
            else if(dec_case == 9)
            {
                //sync_lookatpos = pathnode
                sync_lookatpos = path_node_pos;
                
                //if nav_case 0: Need navigation
                if(nav_case == 0)
                {
                    //sync_moveto = pathnode
                    sync_movetopos = path_node_pos;
                }
                //if nav_case 2: tracking object
                else if(nav_case == 2)
                {
                    //sync_movetoobj = movetoobj
                    sync_movetobj = movetoobj;
                    //sync_lookatobj = movetoobj
                    sync_lookatobj = movetoobj;
                }
                //if nav_case 3: tracking point
                else if (nav_case == 3)
                {
                    //sync_movetopos = movetopos
                    sync_movetopos = movetopos;
                }
            }
            

            //case 10: Hear sound when reloading, look at sound source but dont move
            else if(dec_case == 10)
            {
                //sync_lookatpos = lookatpos
                sync_lookatpos = lookatpos;
            }
            

            //case 11: Head sound when able to attack, go to sound source and look at sound source
            else if(dec_case == 11)
            {
                //sync_lookatpos = lookatpos
                sync_lookatpos = lookatpos;
                //if nav_case 0: Need navigation
                if(nav_case == 0)
                {
                    //sync_movetopos = pathnode
                    sync_movetopos = path_node_pos;
                }
                    
                //if nav_case 3: Tracking point
                else if(nav_case == 3)
                {
                    //sync_movetopos = movetopos
                    sync_movetopos = movetopos;
                }
                    
            }
            

            //case 12: patrol and is looking away
            else if(dec_case == 12)
            {
                //sync_lookatpos = lookatpos
                sync_lookatpos = lookatpos;
                //if nav_case 0: Need navigation
                if(nav_case == 0)
                {
                    //sync_movetopos = pathnode
                    sync_movetopos = path_node_pos;
                }
                    
                //if nav_case 3: Tracking point
                else if(nav_case == 3)
                {
                    //sync_movetopos = movetopos
                    sync_movetopos = movetopos;
                }
                    
            }
            //case 13: patrol and look back at the path
            else if(dec_case == 13)
            {
                
                //if nav_case 0: Need navigation
                if(nav_case == 0)
                {
                    //sync_movetopos = pathnode
                    sync_movetopos = path_node_pos;
                }
                    
                //if nav_case 3: Tracking point
                if(nav_case == 3)
                {
                    //sync_movetopos = movetopos
                    sync_movetopos = movetopos;
                }    
                //sync_lookatpos = pathnode
                sync_lookatpos = sync_movetopos;
            }
            sync_movetopos_x =  (short)(sync_movetopos.x * CONSTANTS.SYNC_POS_MUTIPLIER);
            sync_movetopos_y = (short)(sync_movetopos.y * CONSTANTS.SYNC_POS_MUTIPLIER);
            sync_lookatpos_x = (short)(sync_lookatpos.x * CONSTANTS.SYNC_POS_MUTIPLIER);
            sync_lookatpos_y = (short)(sync_lookatpos.y * CONSTANTS.SYNC_POS_MUTIPLIER);

            if (Time.time > time_to_update_pos)
            {
                sync_x = (short)(zombieRB.position.x * CONSTANTS.SYNC_POS_MUTIPLIER);
                sync_y = (short)(zombieRB.position.y * CONSTANTS.SYNC_POS_MUTIPLIER);
                //sync_pos = transform.position;
                time_to_update_pos += pos_update_interval;
            }
            //NPC shooting
            //if decide to attack
            //if aimed
            //shoot


            //Gun user
            if(equiped_item != null)
            {
                //Reloading finish
                if (body.reloading && Time.time > time_reload_finish)
                {
                    body.reloading = false;
                    
                    equiped_item.GetComponent<Equipable_generic>().loaded = true;

                    
                    if (gun.reload_all)
                    {
                        int amount_to_reload = Mathf.Min((gun.capacity - gun.ammo), ammo_drop_count);
                        gun.ammo += (ushort)amount_to_reload;
                        ammo_drop_count -= amount_to_reload;
                    }
                    else
                    {
                        ammo_drop_count--;
                        gun.ammo += 1;
                    }
                    ammo_drop_count = Mathf.Max(0, ammo_drop_count);
                    inventory_weight = (ushort)(gun.weapon_weight + ammo_drop_template.GetComponent<Ammo_generic>().bullet_weight * ammo_drop_count);
                    if (anim_upper != null)
                    {
                        body.set_animation_state(-2);
                    }
                }

                //Decide to attack
                if (dec_case == 1 || dec_case == 2)
                {
                    float size = 1;
                    float dist = Vector2.Distance(zombieRB.position, lookatobj.transform.position);
                    float dist_canShoot = Vector2.Distance(zombieRB.position, gun.fire_point.position);
                    if (lookatobj.GetComponent<Body_generic>() != null)
                    {
                        size = lookatobj.GetComponent<Body_generic>().size * 2;
                    }
                    else
                    {
                        size = 1f;
                    }
                    //Enemy within reachability
                    if (dist >= dist_canShoot)
                    {
                        bool aim_lock = false;
                        bool viable_range = false;
                        Vector2 aim_vec = (Vector2)lookatobj.transform.position - zombieRB.position;
                        float aim_at = Mathf.Atan2(aim_vec.y, aim_vec.x) * 180 / 3.14f;//zombieRB.rotation;

                        if(Mathf.DeltaAngle(aim_at, zombieRB.rotation) < CONSTANTS.AI_AIM_TOLERANCE)
                        {
                            aim_lock = true;
                        }

                        if (gun.firecone_angle > (double)Mathf.Atan2(size, dist) * 180 / Mathf.PI)
                        {
                            aim_lock = false;
                        }
                        if (dist <= gun.maximum_dist)
                        {
                            viable_range = true;
                        }

                        if (aim_lock && viable_range)
                        {
                            body.set_animation_state(-2);
                            gun.Pull_trigger(body);
                        }
                    }
                    //Enemy standing between gun point and this AI, cannot shoot
                    else
                    {
                        body.melee();
                    }
                }
            }
            //Melee user
            else
            {
                //Decide to attack
                if (dec_case == 1 || dec_case == 2)
                {
                    //If within melee range
                    //Attack
                    if(Vector2.Distance(zombieRB.position,lookatobj.transform.position) < body.size * 3)
                    {
                        body.melee();
                    }
                }
            }
        }


        ai_motion();
    }

    //This function uses syncvars to simulate client and server sides' npc movement
    void ai_motion()
    {
        zombieRB.mass = body.initial_mass + (float)(inventory_weight / 500f);
        int _case = sync_dec_case / 10;
        int _nav_case = sync_dec_case % 10;

        //Client & server sides npc movement
        //Movement
        if (sync_moving)
        {
            //Fetch move direction from synchronized data
            Vector2 move_dir = Vector2.zero;
            if ( ( (_case >= 11 && _case <= 14) || (_case == 9) || (_case == 2) || (_case == 5)) && (_nav_case == 0 || _nav_case == 3) )
            {
                Vector2 sync_movetopos_opt = new Vector2((float)sync_movetopos_x / CONSTANTS.SYNC_POS_MUTIPLIER, (float)sync_movetopos_y / CONSTANTS.SYNC_POS_MUTIPLIER);
                move_dir = (sync_movetopos_opt - zombieRB.position).normalized;
            }
            else if (_nav_case == 2 && (_case == 2 || _case == 9 || _case == 5))
            {
                if (sync_movetobj != null)//Found null on sync_movetobj
                {
                    move_dir = ((Vector2)sync_movetobj.transform.position - zombieRB.position).normalized;
                }
            }


            //Steer away from obstacles
            float result_angle = Mathf.Atan2(move_dir.y, move_dir.x);
            float left_angle = (result_angle - CONSTANTS.AI_DETECT_ANGLE * Mathf.PI / 180) ;
            float right_angle = (result_angle + CONSTANTS.AI_DETECT_ANGLE * Mathf.PI / 180);
            Vector2 left_vec = new Vector2(Mathf.Cos(left_angle), Mathf.Sin(left_angle)).normalized * (body.size + CONSTANTS.AI_DETECT_DIST);
            Vector2 right_vec = new Vector2(Mathf.Cos(right_angle), Mathf.Sin(right_angle)).normalized * (body.size + CONSTANTS.AI_DETECT_DIST);
            RaycastHit2D detect_left = Physics2D.Raycast(left_vec + zombieRB.position, -left_vec, body.size * CONSTANTS.AI_DETECT_DIST, detectionAllyFilter);
            RaycastHit2D detect_right = Physics2D.Raycast(right_vec + zombieRB.position, -right_vec, body.size * CONSTANTS.AI_DETECT_DIST, detectionAllyFilter);
            if (detect_left.collider != null)
            {
                Body_hitbox_generic body_detect_left = detect_left.collider.GetComponent<Body_hitbox_generic>();
                if (body_detect_left == null || body_detect_left.body.gameObject != gameObject)
                {
                    Vector2 left_n = new Vector2(-move_dir.y, move_dir.x) / Mathf.Sqrt(Mathf.Pow(move_dir.x, 2) + Mathf.Pow(move_dir.y, 2));
                    move_dir += left_n.normalized * CONSTANTS.AI_DETECT_STEER_FACT;
                    move_dir.Normalize();
                }
            }
            if (detect_right.collider != null)
            {
                Body_hitbox_generic body_detect_right = detect_right.collider.GetComponent<Body_hitbox_generic>();
                if (body_detect_right == null || body_detect_right.body.gameObject != gameObject)
                {
                    Vector2 right_n = new Vector2(-move_dir.y, move_dir.x) / Mathf.Sqrt(Mathf.Pow(move_dir.x, 2) + Mathf.Pow(move_dir.y, 2));
                    move_dir -= right_n.normalized * CONSTANTS.AI_DETECT_STEER_FACT;
                    move_dir.Normalize();
                }
            }

            //Dodge from object
            if (_case == 14 || _case == 0)
            {
                if(sync_dodgefrom != null)//Happens on local occasionally when base destroyed server-sided and sync_dodgefrom becomes null; code however wasnt up to sync for some reason
                {
                    float dist = Vector2.Distance(sync_dodgefrom.transform.position, zombieRB.position);
                    move_dir += 2 * (zombieRB.position - (Vector2)sync_dodgefrom.transform.position).normalized * (1 - dist / dodge_dist);
                }
            }

            //Backward movement
            float client_move_speed = body.speed_run;
            float motion_diff = Mathf.Abs(Mathf.DeltaAngle(zombieRB.rotation, Mathf.Atan2(move_dir.y, move_dir.x) * 180 / Mathf.PI));
            if(motion_diff > 90)
            {
                client_move_speed *= (1 - (motion_diff - 90) / 360);//the larger the last integer is, the larger backward walking speed is
            }
            zombieRB.AddForce(move_dir.normalized * delta_time * client_move_speed / (50 * Time.fixedDeltaTime));
        }

        //Orient
        Vector2 syn_lookatpos_opt = new Vector2((float)sync_lookatpos_x / CONSTANTS.SYNC_POS_MUTIPLIER, (float)sync_lookatpos_y / CONSTANTS.SYNC_POS_MUTIPLIER);
        Vector2 client_orient_to = syn_lookatpos_opt - zombieRB.position;
        if ((_case == 14 ||( _case >= 0 && _case <= 3) || (_case == 9 && _nav_case == 2)) && sync_lookatobj != null)//Tracking object
        {
            //Tracking object location
            client_orient_to = (Vector2)sync_lookatobj.transform.position - zombieRB.position;
        }

        Vector2 client_look_to = client_orient_to;
        if (_case == 10)//Sound source, look at but not turn to
        {
            client_look_to = syn_lookatpos_opt - zombieRB.position;
        }


        //Set orientation
        orient_to(client_orient_to);
        //Face orientation
        look_to(client_look_to);
    }
    float cross_product(Vector2 relative_trg_pos, float current_rotation)
    {
        return Mathf.Cos(current_rotation * Mathf.PI / 180) * relative_trg_pos.y - relative_trg_pos.x * Mathf.Sin(current_rotation * Mathf.PI / 180);
    }
    [ServerCallback]
    public void set_pos_n_orient(Vector2 position, float angle)
    {
        sync_lookatpos.x = (Mathf.Cos(angle * 3.14f / 180) * 5 + zombieRB.position.x);
        sync_lookatpos.y = (Mathf.Sin(angle * 3.14f / 180) * 5 + zombieRB.position.y);
        
        zombieRB.position = position;
    }
    void orient_to(Vector2 relative_trg_pos)
    {
        float cp_orient = cross_product(relative_trg_pos, zombieRB.rotation);
        float angle_orient = Mathf.Atan2(relative_trg_pos.y, relative_trg_pos.x) * 180 / Mathf.PI;
        if (Mathf.Abs(Mathf.DeltaAngle(angle_orient, zombieRB.rotation)) < orient_force * delta_time)//jump to target angle
        {
            zombieRB.MoveRotation(angle_orient);
        }
        else if (cp_orient > 0)//Left turn
        {
            zombieRB.MoveRotation(zombieRB.rotation + orient_force * delta_time);
        }
        else if (cp_orient < 0)//Right turn
        {
            zombieRB.MoveRotation(zombieRB.rotation - orient_force * delta_time);
        }
    }
    void look_to(Vector2 relative_trg_pos)
    {
        float cp_look = cross_product(relative_trg_pos, body.headshot_box.transform.eulerAngles.z);
        float angle_look = Mathf.Atan2(relative_trg_pos.y, relative_trg_pos.x) * 180 / Mathf.PI;

        float offset = Mathf.Abs(Mathf.DeltaAngle(body.headshot_box.transform.eulerAngles.z, angle_look));
        float current_tilt = body.anim_upper.GetFloat("Head_tilt");
        if (cp_look < 0)//right
        {
            body.anim_upper.SetFloat("Head_tilt", Mathf.Min(90, current_tilt + Mathf.Min(look_force * delta_time, offset)));
        }
        else if(cp_look > 0)//left
        {
            body.anim_upper.SetFloat("Head_tilt", Mathf.Max(-90, current_tilt - Mathf.Min(look_force * delta_time, offset)));
        }
    }
    public void alert_new_sound(Vector2 source)
    {
        //heardSomething = true;
        //sound_trace_pos = source;
        //previous_hearing_time = Time.time;
        sound_source = source;
        time_to_forget_sound = Time.time + hearing_forget_interval;
        time_to_hear_again = Time.time + hearing_interval;
    }

    void adjust_eye_sight()
    {
        if(closest_enemy_cache == null)
        {
            eye_sight = body.viewRadius;
            return;
        }
        eye_sight = Vector2.Distance(closest_enemy_cache.transform.position, zombieRB.position);
        if(eye_sight > body.viewRadius)
        {
            eye_sight = body.viewRadius;
        }
    }
    //Find the closest seen enemy based on straight distance
    //Note that this might not work for zombie as it might seek for longer target due to complexity of navigation
    GameObject get_closest_seen_enemy()
    {
        adjust_eye_sight();
        if (Time.time <= time_to_detect && closest_enemy_cache != null)//If closest enemy cache available and not the time update
        {
            return closest_enemy_cache;
        }
        time_to_detect = Time.time + detection_interval;

        float angle = body.headshot_box.transform.eulerAngles.z;
        float angle_rad = angle * Mathf.PI / 180;
        Vector2 point = zombieRB.position + new Vector2((eye_sight / 2) * Mathf.Cos(angle_rad), (eye_sight / 2) * Mathf.Sin(angle_rad));
        float box_height = Mathf.Tan((body.viewAngle/ 2) * Mathf.PI / 180) * eye_sight * 2;
        
        //Collider2D[] enemies_insight = Physics2D.OverlapCircleAll(transform.position, view_dist, detectionFilter);
        Collider2D[] enemies_insight = Physics2D.OverlapBoxAll(point, new Vector2(eye_sight, box_height), angle, detectionFilter);
        GameObject closest = null;
        float closest_dist = eye_sight;
        for (int i = 0; i < enemies_insight.Length; i++)
        {
            GameObject enemy;
            bool isStructure = false;
            if (enemies_insight[i].gameObject.tag == "structure")//Structure
            {
                isStructure = true;
                enemy = enemies_insight[i].gameObject;
            }
            else//Mobile enemies
            {
                enemy = enemies_insight[i].GetComponent<Body_hitbox_generic>().body.gameObject;
            }
            //if (body.enemy_tags.Contains(enemies_insight[i].tag))//If deemed as enemy
            //{

            
            Vector2 pos_difference = (Vector2)enemy.transform.position - zombieRB.position;
            float angle_difference = Mathf.Atan2(pos_difference.y, pos_difference.x) * 180 / Mathf.PI;
            float dist = Vector2.Distance(enemy.transform.position, zombieRB.position);
            if (Mathf.DeltaAngle(angle, angle_difference) > body.viewAngle / 2 || dist > eye_sight)//If out of field of view
            {
                continue;
            }
            if (dist <= closest_dist || isStructure)//If this enemy is even closer;
            {
                RaycastHit2D hit = Physics2D.Linecast(zombieRB.position, enemy.transform.position, navigation.LOS_block);
                if (hit)
                {
                    continue;
                }
                if (isStructure)//Store structure location
                {
                    body.team.spot_enemy_base(enemy);
                    if(dist > closest_dist)//if not the closest then seek other target instead
                    {
                        continue;
                    }
                }
                closest = enemy;
                closest_dist = Vector2.Distance(enemy.transform.position, zombieRB.position);
            }
            //}
        }

        closest_enemy_cache = closest;
        return closest;
    }

    public void Pull_trigger()
    {
        if (gun == null)
        {
            return;
        }
        gun.Pull_trigger(body);
    }


    public void Script_to_move(GameObject target, float speed)
    {
        body.script_mod_speed(speed);
        //Mimicing remembered object movement pattern
        dec_case = 9;
        moving = true;
        navtoobj = target;
    }
    public void Script_cease_control()
    {
        body.script_resume_speed();
        dec_case = 0;
        moving = false;
        navtoobj = null;
    }
    Vector2 get_corner_pos()
    {
        List<Navigation_manual._Node> nodes = navigation.surround_nodes(zombieRB.position, -1, body.size * 2);
        int length = nodes.Count;
        if(nodes.Count == 0)
        {
            return CONSTANTS.VEC_NULL;
        }
        int random = Random.Range(0, length-1);
        return nodes[random].position;
    }
    /// <summary>
    /// This user has a gun but is gun has no bullet
    /// </summary>
    /// <returns></returns>
    bool canShoot()
    {
        if (equiped_item != null && gun.ammo <= 0)
        {
            return false;
        }
        return true;
    }
    Vector2 get_patrol_location()
    {
        int patrol_index = UnityEngine.Random.Range(0, navigation.patrol_areas.Length);
        BoxCollider2D area = navigation.patrol_areas[patrol_index];
        float patrol_x = UnityEngine.Random.Range((area.transform.position.x - area.size.x / 2), (area.transform.position.x + area.size.x / 2));
        float patrol_y = UnityEngine.Random.Range((area.transform.position.y - area.size.y / 2), (area.transform.position.y + area.size.y / 2));
        return new Vector2(patrol_x, patrol_y);
    }
    Vector2 get_nearest_cover_from(GameObject enemy)
    {
        return navigation.nearest_cover(zombieRB.position, enemy.transform.position);
    }
    /// <summary>
    /// This user has a gun and is within effective shooting range
    /// </summary>
    /// <param name="enemy"></param>
    /// <returns></returns>
    bool is_effective_shooting_range(GameObject enemy)
    {
        if(equiped_item != null)//stop by when ideal shooting range
        {
            return Vector2.Distance(enemy.transform.position, zombieRB.position) < gun.effective_dist * 4 / 5;
        }
        else//If melee, keep pushing toward target
        {
            return false;
        }
    }
    bool under_damage_distraction()
    {
        return Time.time < time_to_resume_from_dmg;
    }
    bool is_pursuing_target()
    {
        return navtoobj != null;//|| navtopos != CONSTANTS.VEC_NULL; This is excluded because sound source shouldnt counted as urgent target
    }
    bool arrive_at(Vector2 position)
    {
        return Vector2.Distance(zombieRB.position, position) < body.size * 2;
    }
    void hook_pos_update_x(short pos_x)
    {
        sync_x = pos_x;
        //transform.position = new Vector2(pos_x/ CONSTANTS.SYNC_POS_MUTIPLIER, transform.position.y);
        if (!isServer)
        {
            interpolater.interpolate_x(pos_x / CONSTANTS.SYNC_POS_MUTIPLIER);
        }
        
    }
    void hook_pos_update_y(short pos_y)
    {
        sync_y = pos_y;
        //transform.position = new Vector2(transform.position.x, pos_y/ CONSTANTS.SYNC_POS_MUTIPLIER);
        if (!isServer)
        {
            interpolater.interpolate_y(pos_y / CONSTANTS.SYNC_POS_MUTIPLIER);
        }
        
    }
    [ServerCallback]
    void OnDamaged(float damage, Vector2 dmg_dir)
    {
        if(Time.time > time_to_detect_damage && dmg_dir != CONSTANTS.VEC_NULL)
        {
            damage_dir = -dmg_dir.normalized * 100;
            
            time_to_resume_from_dmg = Time.time + damageDistraction_time;
            time_to_detect_damage = Time.time + damage_detection_interval;
        }
        
    }

    public void set_ai_condition(AI_condition the_condition)
    {
        cond = the_condition;
    }
    public void set_ai_condition(int the_condition)
    {
        cond = (AI_condition)the_condition;
    }
    
    /// <summary>
    /// Remove existing memories
    /// </summary>
    public void memory_clean()
    {
        if(memory_enemies != null && memory_enemies.Count > 0)
        {
            memory_enemies.Clear();
        }
    }
    /// <summary>
    /// Remove dead enemies from memories
    /// </summary>
    void forget_thedead()
    {
        for (int i = 0; i < memory_enemies.Count; i++)
        {
            //Debug.Log("body: "+memory_enemies[i]);
            if (memory_enemies[i] == null || memory_enemies[i].GetComponent<Body_generic>().isDead)
            {
                memory_enemies.RemoveAt(i);
                i--;
            }
        }
    }

    /// <summary>
    /// Does this AI remember this enemy?
    /// </summary>
    /// <param name="enemy"></param>
    /// <returns></returns>
    bool remember(GameObject enemy)
    {
        forget_thedead();
        if (memory_enemies.Contains(enemy))
        {
            return true;
        }
        return false;
    }
    /// <summary>
    /// Remember a new enemy
    /// </summary>
    /// <param name="enemy"></param>
    void memory_store(GameObject enemy)
    {
        if(enemy.tag == "structure")
        {
            return;
        }
        forget_thedead();
        memory_enemies.Add(enemy);
        while (memory_enemies.Count > max_enemies_memory)
        {
            memory_enemies.RemoveAt(0);
        }
    }
    /// <summary>
    /// Get the most recently seen enemy
    /// </summary>
    /// <returns></returns>
    GameObject i_remember()
    {
        forget_thedead();
        if(memory_enemies.Count > 0)
        {
            return memory_enemies[memory_enemies.Count - 1];
        }
        return null;
    }
    //Server only
    public void reload_gun()
    {
        if (body.reloading || equiped_item == null || gun.ammo >= gun.capacity || ammo_drop_count <= 0)
        {
            return;
        }

        body.reloading = true;
        time_reload_finish = Time.time + gun.reload_time * body.reload_multiplier;
        body.set_animation_state(-1);

    }
    
    void personality_form()
    {
        int seed1 = Random.Range(0, 98);
        //skill_map[0] = seed1;
        int seed2 = Random.Range(seed1, 99);
        //skill_map[1] = seed2;
        int seed3 =Random.Range(seed2, 100);
        //skill_map[2] = seed3;
    }
    public void shopping()
    {
        float experience = body.experience;
        int budget = Random.Range(0, (int)(body.money * (1-CONSTANTS.AI_SAVE_PERCENT)));//not counting insurance
        byte skillpoints = (byte)Mathf.Min(cvar_watcher.maxSkillSpentPerDeath, body.skill_points);

        
        
        //Find available items to buy
        List<GameObject> can_buy_weapons = new List<GameObject>();
        /*
        GameObject[] purchasables = null;

        if(body.isBot())
        {
            purchasables = cvar_watcher.purchases_robot;
        }
        else if (body.isHuman())
        {
            purchasables = cvar_watcher.purchases_human;
        }
        else if(body.isZombie())
        {
            purchasables = cvar_watcher.purchases_zombie;
        }

        
        for (int i = 0; i < purchasables.Length; i++)
        {
            Equipable_generic item = purchasables[i].GetComponent<Equipable_generic>();
            if (item.tag == CONSTANTS.TAG_GUN && body.money + cvar_watcher.insurance_money >= item.price && body.experience >= item.required_experience && body.inventory_size >= item.GetComponent<Gun_generic>().weapon_size)
            {
                can_buy_weapons.Add(purchasables[i]);
            }
        }
        */
        for (int i = 0; i < Weapon_choices.Length; i++)
        {
            Equipable_generic item = Weapon_choices[i].GetComponent<Equipable_generic>();
            if (item.tag == CONSTANTS.TAG_GUN && body.money + cvar_watcher.insurance_money >= item.price && body.experience >= item.required_experience && body.inventory_size >= item.GetComponent<Gun_generic>().weapon_size)
            {
                can_buy_weapons.Add(Weapon_choices[i]);
            }
        }
        if (can_buy_weapons.Count > 0)
        {
            int ran_buy = UnityEngine.Random.Range(0, can_buy_weapons.Count);
            buy_checkout(can_buy_weapons[ran_buy], skillpoints);
        }
        else
        {
            buy_checkout(null, skillpoints);
        }
        
    }
    
    void buy_checkout(GameObject gun_to_buy, byte skillpoints)
    {
        if(skillpoints > 0)
        {
            body.upgrade_stat(null, skillpoints);
        }
        //AI money policy: reduce all item price by insurance money
        if(gun_to_buy != null)
        {
            int money_to_use = body.money + cvar_watcher.insurance_money;
            money_to_use -= gun_to_buy.GetComponent<Equipable_generic>().price;
            if (gun_to_buy.GetComponent<Equipable_generic>().price > cvar_watcher.insurance_money)
            {
                body.money -= gun_to_buy.GetComponent<Equipable_generic>().price - cvar_watcher.insurance_money;
            }
            
            
            //Buy ammo
            Ammo_generic ammo_template = gun_to_buy.GetComponent<Gun_generic>().ammo_template.GetComponent<Ammo_generic>();
            int ammobox_size = ammo_template.amount * ammo_template.bullet_size;
            int ammobox_canBuy = money_to_use / ammo_template.GetComponent<Equipable_generic>().price;//the amount of ammobox that can buy
            int ammobox_canCarry = Mathf.Min((body.inventory_size - gun_to_buy.GetComponent<Gun_generic>().weapon_size) / ammobox_size , ammobox_canBuy);
            give_weapon(gun_to_buy, ammobox_canCarry * ammo_template.amount);
        }
        
        body.skill_points -= skillpoints;
        if (body.money < 0) { body.money = 0; }
        


    }
    /// <summary>
    /// Spawn the weapon and give it to the ai
    /// </summary>
    /// <param name="item_to_buy"></param>
    /// <param name="ammoAmount"></param>
    public void give_weapon(GameObject item_to_buy, int ammoAmount)
    {
        //Spawn weapon
        Transform weapon_bone = body.weapon_bone.transform;
        GameObject equip = Instantiate(item_to_buy, weapon_bone.position, weapon_bone.rotation);
        NetworkServer.Spawn(equip);
        Gun_generic gun_request = equip.GetComponent<Gun_generic>();
        //Give ammo type
        if (gun_request.ammo_template != null)
        {
            /*
            int seed = Random.Range(0, 100);
            if (seed < CONSTANTS.NPC_DROP_CHANCE * 100)
            {
                ammo_drop_count = gun.ammo_template.GetComponent<Ammo_generic>().amount * 2;

            }
            */
            ammo_drop_count = ammoAmount;
            ammo_drop_template = gun_request.ammo_template;
        }
        //Assigning weight
        
        inventory_weight = (ushort)(gun_request.weapon_weight + gun_request.ammo_template.GetComponent<Ammo_generic>().bullet_weight * ammoAmount);
        //Npc pick up
        Pickup_item(equip);
    }

    /// <summary>
    /// Assign the weapon, while give the ai the ammo according amount inside the gun
    /// eg. a m4 with 9999 ammo in it, that will give the ai a m4 with 31 ammo in chamber, and 9999 - 31 = 9968 as ammunition
    /// </summary>
    /// <param name="item_to_assign"></param>
    public void give_weapon_assign_prefab(Gun_generic item_to_assign)
    {
        give_weapon(item_to_assign.gameObject, item_to_assign.capacity * 3);
    }
    /// <summary>
    /// Assign the weapon, while give the ai the ammo according amount inside the gun
    /// eg. a m4 with 9999 ammo in it, that will give the ai a m4 with 31 ammo in chamber, and 9999 - 31 = 9968 as ammunition
    /// </summary>
    /// <param name="item_to_assign"></param>
    public void give_weapon_assign(Gun_generic item_to_assign)
    {

        ammo_drop_count = item_to_assign.ammo - item_to_assign.capacity;
        ammo_drop_template = item_to_assign.ammo_template;
        item_to_assign.ammo = item_to_assign.capacity;
        Pickup_item(item_to_assign.gameObject);
    }
    
    //[ServerCallback]
    public void Pickup_item(GameObject item)
    {
        
        //If item have owner, ignore
        if (item == null || body.isDead)
        {
            return;
        }
        //Assign item ownership
        equiped_item = item;
        gun = equiped_item.GetComponent<Gun_generic>();
        if (isDedicatedServer())//Dedicated server needs to invoke individually because it won't call the hook on changing sync
        {
            Hook_equip(item);
        }
        //

        Equipable_generic item_equip = item.GetComponent<Equipable_generic>();
        if(cvar_watcher.map_type == CONSTANTS.MAP_TYPE.Objective)//NPC turns on laser aim in objective mode
        {
            item_equip.laserAimOn = true;
        }
        item_equip.set_user(gameObject);
        //item.GetComponent<Equipable_generic>().parented = true;
        //Different type of items
        if (Client_watcher.Singleton != null)
        {
            Client_watcher.Singleton.deregister_item(item.GetComponent<IEquiptable>());
        }
        


    }

    //Clients
    public void obtain(GameObject item)//Used by Rpc_obtain & onstartclient in Equipable_generic
    {
        Equipable_generic item_equip = item.GetComponent<Equipable_generic>();
        gun = item.GetComponent<Gun_generic>();
        //Character hold animation
        //anim_lower.SetBool("armed", true);
        anim_upper.SetInteger("armType", item_equip.anim_equip);
        //Assign item to character
        body = GetComponent<Body_generic>();
        item_equip.attach(body.weapon_bone.transform);
        /*
        item.transform.position = body.weapon_bone.transform.position;
        item.transform.parent = body.weapon_bone.transform;
        item.transform.localScale = item.GetComponent<Equipable_generic>().mdl_scale;
        item.transform.rotation = body.weapon_bone.transform.rotation;
        item.GetComponent<Rigidbody2D>().simulated = false;
        item.GetComponent<Collider2D>().enabled = false;
        item.GetComponent<SpriteRenderer>().sortingLayerName = "Equiped";
        item.GetComponent<NetworkTransform>().enabled = false;
        */
    }
    public void Hook_equip(GameObject update_obj)
    {
        equiped_item = update_obj;
        if(update_obj != null)
        {
            //update_obj.GetComponent<Equipable_generic>().parented = true;
            obtain(update_obj);
        }
    }

    //Server only
    [ServerCallback]
    public void Drop_item(GameObject item, Vector2 throw_force)
    {
        if(item == null)
        {
            return;
        }
        inventory_weight = 0;
        body.reloading = false;
        item.GetComponent<Equipable_generic>().set_user(null);
        
        Gun_generic gun = equiped_item.GetComponent<Gun_generic>();
        
        if(Client_watcher.Singleton != null)
        {
            Client_watcher.Singleton.register_item(equiped_item.GetComponent<IEquiptable>());
        }

        
        Rpc_drop(item, throw_force, gun.ammo, gun.firecone_angle, gun.firemode);
        if (isDedicatedServer())
        {
            dump_item(item, throw_force, gun.ammo, gun.firecone_angle, gun.firemode);
        }
    }
    [ClientRpc]
    public void Rpc_drop(GameObject item, Vector2 throw_force, ushort ammo, float fireangle, Gun_generic.FireMode firemode)
    {
        dump_item(item, throw_force, ammo, fireangle, firemode);
    }
    public void dump_item(GameObject item, Vector2 throw_force, ushort ammo, float fireangle, Gun_generic.FireMode firemode)
    {
        if(item == null)
        {
            return;
        }
        //item.GetComponent<Equipable_generic>().parented = false;
        equiped_item = null;
        if (anim_upper != null)
        {
            anim_upper.SetInteger("armType", -1);
            //anim_lower.SetBool("armed", false);
        }
        if (item.GetComponent<Equipable_generic>().item_type == Equipable_generic.ITEM_TYPE.gun)
        {
            Gun_generic item_gun = item.GetComponent<Gun_generic>();
            item_gun.ammo = ammo;
            item_gun.firecone_angle = fireangle;
            item_gun.firemode = firemode;
        }

        Equipable_generic item_equip = item.GetComponent<Equipable_generic>();
        Rigidbody2D itemRB = item.GetComponent<Rigidbody2D>();
        item_equip.detach(zombieRB.position);
        //item_equip.position_buffer.x = transform.position.x;
        //item_equip.position_buffer.y = transform.position.y;
        //itemRB.transform.position = item_equip.position_buffer;
        itemRB.velocity = zombieRB.velocity;
        itemRB.AddTorque(CONSTANTS.DROP_TORQUE);
        itemRB.AddForce(throw_force);
        /*
        item.GetComponent<SpriteRenderer>().sortingLayerName = "Items";
        item.GetComponent<Rigidbody2D>().simulated = true;
        item.GetComponent<Rigidbody2D>().velocity = zombieRB.velocity;
        item.GetComponent<Collider2D>().enabled = true;
        item.transform.parent = null;
        item.transform.localScale = item.GetComponent<Equipable_generic>().mdl_scale;
        item.GetComponent<Rigidbody2D>().AddTorque(CONSTANTS.DROP_TORQUE);
        item.GetComponent<Rigidbody2D>().AddForce(throw_force);
        item.GetComponent<NetworkTransform>().enabled = true;
        */
    }
    /// <summary>
    /// Effects only
    /// </summary>
    /// <param name="fire_point_x"></param>
    /// <param name="fire_point_y"></param>
    /// <param name="aim_dir"></param>
    /// <param name="aim_dir_offset"></param>
    [ClientRpc]
    public void Rpc_client_shoot_optimized(short fire_point_x, short fire_point_y, short aim_dir, sbyte[] aim_dir_offset)
    {
        if (isServer)//If server, already initialized, back off to avoid double damage
        {
            return;
        }
        gun.shoot(gameObject, fire_point_x, fire_point_y, aim_dir, aim_dir_offset, cvar_watcher.local_player.latency / 2);
        //body.anim_reload(false);
    }
    /// <summary>
    /// Effects only
    /// </summary>
    /// <param name="fire_point_x"></param>
    /// <param name="fire_point_y"></param>
    /// <param name="aim_dir"></param>
    [ClientRpc]
    public void Rpc_client_shoot_optimized_single(short fire_point_x, short fire_point_y, short aim_dir)
    {
        if (isServer || gun == null)//If server, already initialized, back off to avoid double damage
        {
            return;
        }
        gun.shoot(gameObject, fire_point_x, fire_point_y, aim_dir, null, cvar_watcher.local_player.latency / 2);
        //body.anim_reload(false);
    }
}








