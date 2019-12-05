using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.IO;
using System;
public class Player_controller : NetworkBehaviour {
    
    //UI
    public KeyCode toggleChat;
    Message_board_watcher msg_watcher;
    Player_generic player_view;
    //Movement
    public KeyCode MoveLeft;
    public KeyCode MoveRight;
    public KeyCode MoveUp;
    public KeyCode MoveDown;
    public KeyCode Walk;
    //public float stress_resistent;
    [SyncVar] public bool freeze_movement = true;


    //Inventory
    public KeyCode Use;
    public KeyCode Drop;
    public KeyCode Switch_primary;
    public KeyCode Item1;
    public KeyCode Item2;
    public KeyCode Item3;
    public KeyCode Item4;
    public KeyCode Item5;
    public KeyCode Item6;
    public KeyCode Next_weapon;
    public KeyCode Previous_weapon;
    public KeyCode maglite_toggle;
    public KeyCode build_base;

    public bool maglite_local = false;
    public Light maglite;
    public NetworkTransformChild maglite_nettransform;
    public int character_subtype = 0;

    //Camera
    public KeyCode zoomout;
    public KeyCode zoomin;
    public KeyCode rotate_cam;
    public float max_view;
    public float min_view;
    private float cam_angle = 0;
    private Vector2 cam_vec = Vector2.zero;
    private Transform cam3D;
    Vector2 shake_vec;

    //Firearm
    [SyncVar] public GameObject equiped_item;
    public KeyCode Primary_fire;
    public KeyCode Reload;
    public KeyCode ToggleLaserAim;
    //public float reload_multiplier = 1.0f;
    public Transform weapon_bone;
    [HideInInspector] public bool reloading = false;
    private float time_reload_finish = 0;

    //Melee
    public KeyCode Melee;
    public LayerMask melee_hit_fltr;
    public KeyCode Shove;
    /// <summary>
    /// Base damage
    /// </summary>
    public float melee_dmg_physical;
    /// <summary>
    /// Base force
    /// </summary>
    public float melee_dmg_force;
    /// <summary>
    /// Player-charge-forward base speed
    /// </summary>
    public float melee_charge_force;
    /// <summary>
    /// Maximum charge time
    /// </summary>
    public float melee_charge_maxtime;
    /// <summary>
    /// Multiplier for each second charged
    /// </summary>
    public float melee_charge_multiplier;
    public float melee_cooldown_time;
    private Vector2 melee_aimdir;
    private float melee_multiplier = 0;
    private bool isChargeMelee = false;
    private bool isChargingForward = false;
    private float time_charge = 0;
    private float time_to_melee = 0;

    
    //Sprite Segments
    public Transform sprite_orient;


    //Privates

    private Rigidbody2D playerRB;
    private Player_inventory inventory;
    private Sound_watcher sound_watcher;
    [HideInInspector] public Vector2 mousepos;
    [HideInInspector] public Vector2 aimvec;
    [HideInInspector] public Body_generic body;
    private bool throwing_grenade = false;

    

    Server_watcher cvar_watcher;

    //Debug
    [HideInInspector] public Text debug_info;
    public List<string> debug_info_list = new List<string>();
    public LineRenderer debug_line;
    public KeyCode debug_clear;
    public KeyCode debug_toggle;
    bool debug_on = false;
    int debug_max_lines = 7;
    [HideInInspector] public bool cl_sceneLoaded = false;

    //damage list
    List<GameObject> dmgls_victims;
    List<float> dmgls_damages;
    List<Vector2> dmgls_hit_points;
    List<float> dmgls_forces;
    List<short> dmgls_force_dirs;
    List<bool> dmgls_isheadshots;
    List<byte> dmgls_damagetypes;//0 = physics, 1 = thermal
    float shot_send_interval = 0.07f;
    float time_to_send_shotdamage = 0;
    [HideInInspector] public List<Team_watcher> waiting_for_teams = new List<Team_watcher>();
    Action state_update;
    Camera mainCam;
    float time_prev = 0;
    float delta_time = 1;
    [HideInInspector] public Player_HUD hud;
    void debug_add_log(string line)
    {
        if (!debug_on)
        {
            return;
        }
        debug_info_list.Add(line);
        if(debug_info_list.Count > 7)
        {
            debug_info_list.RemoveAt(0);
        }
        debug_info.text = "";
        for (int i = 0; i < debug_info_list.Count; i++)
        {
            debug_info.text += debug_info_list[i];
        }
    }
    bool isDedicatedServer()
    {
        return !isClient && isServer;
    }
    // Use this for initialization
    void Start () {
        shake_vec = Vector2.zero;
        dmgls_victims = new List<GameObject>();
        dmgls_damages = new List<float>();
        dmgls_hit_points = new List<Vector2>();
        dmgls_forces = new List<float>();
        dmgls_force_dirs = new List<short>();
        dmgls_isheadshots = new List<bool>();
        dmgls_damagetypes = new List<byte>();
        playerRB = GetComponent<Rigidbody2D>();
        inventory = GetComponent<Player_inventory>();
        player_view = GetComponent<Player_generic>();
        body = GetComponent<Body_generic>();
        hud = GetComponent<Player_HUD>();
        mainCam = Camera.main;
        GameObject cam3D_obj = GameObject.Find("Background3D_Camera");
        
        if(cam3D_obj != null)
        {
            cam3D = cam3D_obj.transform;
        }

        sound_watcher = FindObjectOfType<Sound_watcher>();


        cvar_watcher = FindObjectOfType<Server_watcher>();
        msg_watcher = cvar_watcher.GetComponent<Message_board_watcher>();
        

        if(cvar_watcher.map_type == CONSTANTS.MAP_TYPE.Objective)
        {
            tag = CONSTANTS.TAG_PLAYER;
        }
    }

    

    // Update is called once per frame
    void Update() {

        delta_time = Time.deltaTime;//Time.realtimeSinceStartup - time_prev;
        
        if (isLocalPlayer)
        {
            
            //Client-damage update
            local_damage_list_update();

            //Local player operation regardless of death
            mousepos = new Vector2(mainCam.ScreenToWorldPoint(Input.mousePosition).x, mainCam.ScreenToWorldPoint(Input.mousePosition).y);
            //Camera zoom-in/out, camera angle
            Transform main_camera = mainCam.transform;
            main_camera.position = new Vector3(playerRB.position.x+ shake_vec.x, playerRB.position.y+ shake_vec.y, main_camera.position.z);
            if(shake_vec.magnitude > CONSTANTS.SCREENSHAKE_RECOVER_DIST * delta_time)
            {
                shake_vec = shake_vec.normalized * Mathf.Abs(shake_vec.magnitude - CONSTANTS.SCREENSHAKE_RECOVER_DIST * delta_time);
            }
            else
            {
                shake_vec = Vector2.zero;
            }



            //Stop below functions (controller)
            if (!msg_watcher.isEditingMsg)
            {
                if (Input.GetKey(zoomout))
                {
                    mainCam.orthographicSize *= 1.03f;
                }
                if (Input.GetKey(zoomin))
                {
                    mainCam.orthographicSize /= 1.03f;
                }
            }







            if (Input.GetKeyDown(rotate_cam))
            {
                cam_vec = mousepos - playerRB.position;
            }
            else if (Input.GetKey(rotate_cam))
            {
                Vector2 offset_vec = (mousepos - playerRB.position);
                float offset = cam_angle - Mathf.Atan2((offset_vec).y, (offset_vec).x) * 180 / Mathf.PI + Mathf.Atan2((cam_vec).y, (cam_vec).x) * 180 / Mathf.PI;
                cam_angle = offset;
                main_camera.rotation = Quaternion.Euler(0, 0, offset);
            }
            if (mainCam.orthographicSize > max_view)
            {
                mainCam.orthographicSize = max_view;
            }
            else if (mainCam.orthographicSize < min_view)
            {
                mainCam.orthographicSize = min_view;
            }
            if (cam3D != null)
            {
                cam3D.localPosition = new Vector3(cam3D.localPosition.x, cam3D.localPosition.y, -Mathf.Lerp(CONSTANTS.CAM3D_MIN_Z, CONSTANTS.CAM3D_MAX_Z, (mainCam.orthographicSize - CONSTANTS.CAM_MIN_VIEW) / (CONSTANTS.CAM_MAX_VIEW - CONSTANTS.CAM_MIN_VIEW)));
            }
            

            //Local operation when player is alive

        }
        //Every client operation regardless of death
        //...


        //Every client operation when player is alive
        if (body.isDead)
        {
            return;
        }
        if (body.character_type == Body_generic.Character_type.Human || body.character_type == Body_generic.Character_type.Robot)//Player is bot or human
        {
            Bot_Human();
        }
        else//Player is zombie
        {
            Zombie();
        }
    }
    //Bot operation when ALIVE
    void Bot_Human()
    {
        //Server, Assign weapon, Melee attack damage calculation
        if (isServer)
        {
            //Is thrusting forward
            if (isChargingForward)
            {
                float size = GetComponent<Body_generic>().size * 2;
                RaycastHit2D[] melee_hits = Physics2D.BoxCastAll(transform.position, new Vector2(size, size), playerRB.rotation, melee_aimdir, melee_multiplier / 5, melee_hit_fltr);//
                float damage = melee_multiplier * melee_dmg_physical;
                Vector2 force = melee_aimdir.normalized * melee_dmg_force * melee_multiplier;
                if (melee_hits != null && melee_hits.Length > 0)
                {
                    for (int i = 0; i < melee_hits.Length; i++)
                    {
                        if(melee_hits[i].collider.GetComponent<Body_hitbox_generic>() != null)
                        {
                            Body_generic body_hit = melee_hits[i].collider.GetComponent<Body_hitbox_generic>().body;
                            body_hit.damage(gameObject, force, damage);
                            if (body.dmg_tags.Contains(melee_hits[i].collider.tag))
                            {
                                body_hit.Rpc_bleed(melee_hits[i].point, transform.rotation.eulerAngles.z, false);
                                body_hit.GetComponent<Rigidbody2D>().AddForceAtPosition(force, melee_hits[i].point);
                            }
                        }
                    }
                }
            }
            
        }
        if (!isLocalPlayer)
        {
            return;
        }




        if (reloading == true && Time.time > time_reload_finish)
        {
            Refill_gun();
        }

        //Stop below functions (controller)
        if (msg_watcher.isEditingMsg || body.character_cond != Body_generic.Character_condition.FREE)
        {
            return;
        }





        //Control inputs ==============================================================

        //Orient
        float mouseangle;
        aimvec = mousepos - playerRB.position;
        mouseangle = Mathf.Atan2(aimvec.y, aimvec.x) * 180 / Mathf.PI;
        sprite_orient.transform.eulerAngles = new Vector3(playerRB.transform.eulerAngles.x, playerRB.transform.eulerAngles.y, mouseangle);
        //Maglite
        if(maglite != null)
        {
            if (maglite_local)
            {
                if (Input.GetKeyDown(maglite_toggle))
                {
                    toggle_maglite(!maglite.enabled);
                }
            }
            else
            {
                if (maglite.enabled)
                {
                    Vector3 mag_rot = maglite.transform.eulerAngles;
                    mag_rot.y = 180 - Mathf.Atan2(aimvec.magnitude, maglite.transform.position.z) * 180 / Mathf.PI;
                    maglite.transform.localEulerAngles = new Vector3(0, mag_rot.y, 0);
                }
                if (Input.GetKeyDown(maglite_toggle))
                {
                    Cmd_toggle_maglite(!maglite.enabled);
                }
            }
        }
        


        

        //Movement
        if (!isChargeMelee && !isChargingForward)
        {
            movement();
        }
        
        //Inventory
        if (Input.GetKeyDown(Use))
        {
            Pick_up();
        }
        else if (Input.GetKeyDown(Item1))
        {
            Cmd_switch_to(0);
            //local_switch_to(0);
        }
        else if (Input.GetKeyDown(Item2))
        {
            Cmd_switch_to(1);
            //local_switch_to(1);
        }
        else if (Input.GetKeyDown(Item3))
        {
            Cmd_switch_to(2);
            //local_switch_to(2);
        }
        else if (Input.GetKeyDown(Item4))
        {
            Cmd_switch_to(3);
            //local_switch_to(3);
        }
        else if (Input.GetKeyDown(Item5))
        {
            Cmd_switch_to(4);
            //local_switch_to(4);
        }
        else if (Input.GetKeyDown(Item6))
        {
            Cmd_switch_to(5);
            //local_switch_to(5);
        }
        else if (Input.GetKeyDown(Next_weapon))
        {
            if (inventory.item.Count > 1)
            {
                //Cmd_switch_next();

                sbyte index = (sbyte)inventory.item_pointer;
                if (inventory.item_pointer == inventory.item.Count - 1)
                {
                    index = 0;
                }
                else { index++; }
                Cmd_switch_to(index);
                //local_switch_to(index);
            }
        }
        else if (Input.GetKeyDown(Previous_weapon))
        {
            if (inventory.item.Count > 1)
            {

                sbyte index = (sbyte)inventory.item_pointer;
                if (inventory.item_pointer == 0)
                {
                    index = (sbyte)(inventory.item.Count - 1);
                }
                else { index--; }
                Cmd_switch_to(index);
                //local_switch_to(index);
            }
        }
        else if (Input.GetKeyDown(Drop))
        {
            if (inventory.item.Count > 0)
            {
                Drop_item(equiped_item, (mousepos - playerRB.position).normalized * inventory.drop_force);
            }
        }

        //Firearm
        else if (equiped_item != null)//Weapon
        {
            if ((equiped_item.tag == CONSTANTS.TAG_GUN) && (equiped_item.GetComponent<Gun_generic>().bullet != null))
            {
                //key detection
                Gun_generic gun = equiped_item.GetComponent<Gun_generic>();// burst & semi-auto
                if (Input.GetKeyDown(Primary_fire))
                {
                    body.set_animation_state(-2);
                    gun.Pull_trigger(body);//gun.Pull_trigger_player(this);
                    debug_add_log("\nShooting: " + equiped_item.name);
                    debug_line.SetPosition(0, transform.position);
                    debug_line.SetPosition(1, equiped_item.transform.position);
                }
                else if (Input.GetKey(Primary_fire) && gun.firemode == Gun_generic.FireMode.Fully_auto)//full auto
                {
                    gun.Pull_trigger(body);//gun.Pull_trigger_player(this);
                }
                if (Input.GetKeyDown(ToggleLaserAim))
                {
                    Cmd_toggleLaserAim();
                }
                //aim re-adjust
                if ((gun.firecone_angle > 0) && (Time.time > gun.time_to_readjust))
                {
                    gun.time_to_readjust = Time.time + 1 / gun.rate_of_readjust;
                    gun.firecone_angle -= gun.accuracy / gun.readjust_factor;
                }
                else if (gun.firecone_angle < 0)
                {
                    gun.firecone_angle = 0;
                }
            }
            else if (equiped_item.tag == CONSTANTS.TAG_AMMO)
            {
                Ammo_generic ammobox = equiped_item.GetComponent<Ammo_generic>();
                if (Input.GetKeyDown(Primary_fire))
                {
                    debug_add_log("\nDispense: " + equiped_item.name);
                    debug_line.SetPosition(0, transform.position);
                    debug_line.SetPosition(1, equiped_item.transform.position);
                    if (ammobox.amount > ammobox.dispence_amount)//client side check
                    {
                        Dispence_ammo();
                        
                    }
                    else
                    {
                        debug_add_log("\nDispense failed: not enough to dispense");
                    }
                }
            }
            else if (equiped_item.tag == CONSTANTS.TAG_GRENADE)
            {
                Grenade_generic grenade = equiped_item.GetComponent<Grenade_generic>();
                if (Input.GetKeyDown(Primary_fire))
                {
                    Cmd_pull_string(grenade.ammo, (sbyte)inventory.item_pointer);
                    grenade.GetComponent<SpriteRenderer>().color = Color.cyan;
                }
                else if (Input.GetKeyUp(Primary_fire) && grenade.pulled)
                {

                    body.anim_upper.SetBool("keydown", false);
                    Cmd_anim_keyup(true);

                    
                    StartCoroutine(throw_grenade(grenade));
                }
            }

        }
        
        if (Input.GetKeyDown(build_base))
        {
            Cmd_build_base();
        }
        //Debug
        if (Input.GetKeyDown(debug_clear))
        {
            debug_info.text = "";
            debug_info_list.Clear();
        }
        if (Input.GetKeyDown(debug_toggle))
        {
            if (debug_on)
            {
                debug_on = false;
                debug_info.enabled = false;
                debug_line.enabled = false;
                Client_watcher.Singleton.GetComponent<FPSDisplay>().enabled = false;
            }
            else
            {
                debug_on = true;
                debug_info.enabled = true;
                debug_line.enabled = true;
                Client_watcher.Singleton.GetComponent<FPSDisplay>().enabled = true;
            }
        }
        /*
        //Melee
        if (Input.GetKeyDown(Melee) && !isChargeMelee && Time.realtimeSinceStartup > time_to_melee && !isChargingForward)//Charge
        {
            isChargeMelee = true;
            time_charge = Time.realtimeSinceStartup;
            Cmd_anim_melee_charge();
        }
        else if (Input.GetKeyUp(Melee) && isChargeMelee)//Discharge
        {
            isChargeMelee = false;
            isChargingForward = true;
            gameObject.layer = LayerMask.NameToLayer("Clip_PB");
            float multiplier = (1 + (Mathf.Clamp(Time.realtimeSinceStartup - time_charge, 0, melee_charge_maxtime) / melee_charge_maxtime) * melee_charge_multiplier);
            Cmd_melee_discharge(mousepos - playerRB.position, multiplier);
            playerRB.AddForce((mousepos - playerRB.position).normalized * melee_charge_force * multiplier);
        }
        
        else if(isChargingForward && playerRB.velocity.magnitude < 1)//resume combat state
        {
            if(body.character_type == Body_generic.Character_type.Robot)
            {
                gameObject.layer = LayerMask.NameToLayer("Bot");
            }
            else if (body.character_type == Body_generic.Character_type.Human)
            {
                gameObject.layer = LayerMask.NameToLayer("Human");
            }
            isChargingForward = false;
            Cmd_melee_stop();
            time_to_melee = Time.time + melee_cooldown_time;
        }
        */
        else if (Input.GetKeyDown(Shove))
        {
            body.melee();
        }
        
        //Reload
        if (Input.GetKeyDown(Reload) && equiped_item != null)
        {
            Gun_generic gun = equiped_item.GetComponent<Gun_generic>();
            
            if (gun != null && gun.ammo < gun.capacity)
            {
                int found = -1;
                for (int i = 0; i < inventory.item.Count; i++)
                {
                    if ((inventory.item[i].tag == CONSTANTS.TAG_AMMO) && (inventory.item[i].GetComponent<Ammo_generic>().ammotype == inventory.item[inventory.item_pointer].GetComponent<Gun_generic>().ammotype))
                    {
                        found = i;
                        debug_add_log("\nReload using ammo: " + inventory.item[i].name + ", amount: " + inventory.item[i].GetComponent<Ammo_generic>().amount);
                        break;
                    }
                }
                if (found != -1 && reloading == false)
                {
                    reloading = true;
                    body.anim_reload(true);
                    Cmd_anim_reload();
                    time_reload_finish = Time.time + gun.reload_time * body.reload_multiplier;
                    
                }
                else
                {
                    debug_add_log("\nReload failed, No corresponding ammo");
                }
            }
        }
        
        
        //Switch fire mode
        if (Input.GetKeyDown(Switch_primary))
        {
            if (equiped_item != null)
            {
                equiped_item.GetComponent<Equipable_generic>().Input_switch_firemode();
            }
        }
    }
    
    //Bot operation when ALIVE
    void Zombie()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        //Stop below functions (controller)
        if (msg_watcher.isEditingMsg || body.character_cond != Body_generic.Character_condition.FREE)
        {
            return;
        }


        //Control inputs ==============================================================

        //Movement
        //wasd moving
        movement();
        //Orient
        float mouseangle;
        aimvec = mousepos - playerRB.position;
        mouseangle = Mathf.Atan2(aimvec.y, aimvec.x) * 180 / Mathf.PI;
        sprite_orient.transform.eulerAngles = new Vector3(playerRB.transform.eulerAngles.x, playerRB.transform.eulerAngles.y, mouseangle);
        //Melee
        if(Input.GetKeyDown(Primary_fire))
        {
            body.melee();
        }
        //Maglite
        if (maglite != null)
        {
            if (maglite_local)
            {
                if (Input.GetKeyDown(maglite_toggle))
                {
                    toggle_maglite(!maglite.enabled);
                }
            }
            else
            {
                if (maglite.enabled)
                {
                    Vector3 mag_rot = maglite.transform.eulerAngles;
                    mag_rot.y = 180 - Mathf.Atan2(aimvec.magnitude, maglite.transform.position.z) * 180 / Mathf.PI;
                    maglite.transform.localEulerAngles = new Vector3(0, mag_rot.y, 0);
                }
                if (Input.GetKeyDown(maglite_toggle))
                {
                    Cmd_toggle_maglite(!maglite.enabled);
                }
            }
        }
    }
    //tell controller to operate dead sequence
    public void die()
    {
        isChargeMelee = false;
        isChargingForward = false;
    }
    void movement()
    {
        float move_force = body.speed_run;
        Vector2 move_dir = Vector2.zero;
        if (Input.GetKey(Walk))
        {
            move_force /= 3.5f;
        }
        playerRB.mass = body.initial_mass + inventory.weight / 500f;
        //move_force *= Mathf.Pow(1.1f, -inventory.weight / body.stress_resistent);
        float move_angle = 0;

        if (Input.GetKey(MoveUp))
        {
            move_dir.y = 1;
        }
        else if (Input.GetKey(MoveDown))
        {
            move_dir.y = -1;
        }
        else
        {
            move_dir.y = 0;
        }
        if (Input.GetKey(MoveLeft))
        {
            move_dir.x = -1;
        }
        else if (Input.GetKey(MoveRight))
        {
            move_dir.x = 1;
        }
        else
        {
            move_dir.x = 0;
        }

        if (move_dir.magnitude != 0)
        {
            move_angle = Mathf.Atan2(move_dir.y, move_dir.x) * 180 / Mathf.PI;
            move_angle += mainCam.transform.rotation.eulerAngles.z;
            move_dir = new Vector2(Mathf.Cos(move_angle * Mathf.PI / 180), Mathf.Sin(move_angle * Mathf.PI / 180));
        }
        //Map backward motion scale
        float motion_diff = Mathf.Abs(Mathf.DeltaAngle(playerRB.rotation, Mathf.Atan2(move_dir.y, move_dir.x) * 180 / Mathf.PI));
        if(motion_diff > 90)
        {
            move_force *= (1 - (motion_diff - 90) / 180);//the larger the last integer is, the larger backward walking speed is
        }
        //body.test_float = move_force;
        playerRB.AddForce(move_dir.normalized * delta_time * move_force / (50 * Time.fixedDeltaTime));
    }
    
    
    
    
    /// <summary>
    /// Called by local player
    /// </summary>
    /// <param name="item"></param>
    /// <param name="throw_force"></param>
    public void Drop_item(GameObject item, Vector2 throw_force)
    {
        //Collect client-side item info and send to server
        ushort _ammo = 0;
        float _fireangle = 0;
        Gun_generic.FireMode _firemode = Gun_generic.FireMode.Semi_auto;
        if (item.tag == CONSTANTS.TAG_GUN)
        {
            _ammo = item.GetComponent<Gun_generic>().ammo;
            _fireangle = item.GetComponent<Gun_generic>().firecone_angle;
            _firemode = item.GetComponent<Gun_generic>().firemode;
        }
        else if (item.tag == CONSTANTS.TAG_AMMO)
        {
            _ammo = item.GetComponent<Ammo_generic>().amount;
        }
        else if (item.tag == CONSTANTS.TAG_GRENADE)
        {
            _ammo = item.GetComponent<Grenade_generic>().ammo;
        }
        Cmd_drop(item, throw_force, _ammo, _fireangle, _firemode);
    }

    public IEnumerator throw_grenade(Grenade_generic grenade)
    {
        yield return new WaitForSeconds(0.15f);
        if (equiped_item == grenade.gameObject)
        {
            //Toss current one, set local ammo to 1 because prefab was created when string was pulled
            if (grenade.ammo > 1)
            {
                grenade.ammo = 1;
            }
            Drop_item(equiped_item, (mousepos - playerRB.position) * grenade.toss_force_multiplier);
        }
    }
                    
    
    
    //Finish reloading
    public void Refill_gun()
    {
        if ((equiped_item == null) || (equiped_item.tag != CONSTANTS.TAG_GUN))
        {
            return;
        }


        //Animation update
        body.anim_reload(false);
        Cmd_anim_reload_complete();

        reloading = false;
        Gun_generic gun = equiped_item.GetComponent<Gun_generic>();
        //Find ammo
        int found = -1;
        for (int i = 0; i < inventory.item.Count; i++)
        {
            if ((inventory.item[i].tag == CONSTANTS.TAG_AMMO) && (inventory.item[i].GetComponent<Ammo_generic>().ammotype == inventory.item[inventory.item_pointer].GetComponent<Gun_generic>().ammotype))
            {
                found = i;
            }
        }
        //Load ammo
        if (found != -1)
        {
            Ammo_generic ammo_container = inventory.item[found].GetComponent<Ammo_generic>();
            if (gun.reload_all && ammo_container.amount > gun.capacity - gun.ammo)
            {
                ammo_container.amount -= (ushort)(gun.capacity - gun.ammo);
                inventory.capacity -= (ushort)((gun.capacity - gun.ammo) * ammo_container.bullet_size);
                inventory.weight -= (ushort)((gun.capacity - gun.ammo) * ammo_container.bullet_weight);
                gun.ammo = gun.capacity;
            }
            else if (!gun.reload_all && ammo_container.amount > 1)
            {
                ammo_container.amount -= 1;
                inventory.capacity -= ammo_container.bullet_size;
                inventory.weight -= (ushort)ammo_container.bullet_weight;
                gun.ammo += 1;
            }
            else
            {
                gun.ammo += ammo_container.amount;
                inventory.capacity -= (ushort)(ammo_container.amount * ammo_container.bullet_size);
                inventory.weight -= (ushort)(ammo_container.amount * ammo_container.bullet_weight);
                Cmd_deplete_ammo(ammo_container.gameObject);
                inventory.item.Remove(ammo_container.gameObject);
                Destroy(ammo_container.gameObject);
            }
        }
    }
    public void Dispence_ammo()
    {
        Ammo_generic ammo_pickup = equiped_item.GetComponent<Ammo_generic>();
        if (ammo_pickup.amount > ammo_pickup.dispence_amount)
        {
            inventory.capacity -= (ushort)(ammo_pickup.dispence_amount * ammo_pickup.bullet_size);
            inventory.weight -= (ushort)(ammo_pickup.dispence_amount * ammo_pickup.bullet_weight);
            ammo_pickup.amount -= ammo_pickup.dispence_amount;
            Cmd_dispence_ammo(ammo_pickup.eject, ammo_pickup.dispence_amount, GetComponent<Body_generic>().weapon_bone.transform.position, equiped_item.transform.rotation);
        }
        debug_add_log("\nDispense successful; inventory: " +inventory.capacity+"/"+body.inventory_size);
    }



    //Animation
    /// <summary>
    /// Reload animation can't use startcorroutine because shooting may stop the animation individually and can mess up animation
    /// </summary>
    [Command]
    public void Cmd_anim_reload_complete()
    {
        body.set_animation_state(-2);
        equiped_item.GetComponent<Equipable_generic>().loaded = true;
    }
    [Command]
    public void Cmd_anim_reload()
    {
        body.set_animation_state(-1);
    }


    //Melee
    [Command]
    public void Cmd_anim_melee_charge()
    {
        body.set_animation_state(-3);
    }
    [Command]
    public void Cmd_melee_discharge(Vector2 force, float multiplier)
    {
        isChargingForward = true;
        melee_aimdir = force;
        melee_multiplier = multiplier;

        body.set_animation_state(-4);
    }
    [Command]
    public void Cmd_melee_stop()
    {
        isChargingForward = false;

        body.set_animation_state(-5);
    }

    /*
    [Command]
    public void Cmd_melee(Vector2 melee_start, Vector2 aimdir, float charge_interval)
    {
        RaycastHit2D hit = Physics2D.Raycast(melee_start, aimdir, melee_range, melee_hit_fltr);
        if (hit.collider != null)
        {
            float multiplier = (1 + (Mathf.Clamp(charge_interval, 0, melee_charge_maxtime) / melee_charge_maxtime) * melee_charge_multiplier);
            hit.collider.GetComponent<Rigidbody2D>().AddForceAtPosition(aimdir.normalized * melee_dmg_force * multiplier, hit.point);
            if (hit.collider.GetComponent<Body_generic>() != null)
            {
                hit.collider.GetComponent<Body_generic>().damage(gameObject, aimdir.normalized * melee_dmg_force * multiplier, multiplier * melee_dmg_physical);
            }
        }
    }
    */

    //When weapon is out of ammo, change model into empty
    [Command]
    public void Cmd_mdl_unload(bool isUnloaded)
    {
        equiped_item.GetComponent<Equipable_generic>().loaded = !isUnloaded;
    }

    [ClientRpc]
    public void Rpc_unload_startup_menu()
    {
        if (isLocalPlayer)
        {
            GetComponent<Player_HUD>().hide_start_up_menu();
        }
    }








    public void hit_mark()
    {

        hud.hit_marker();
    }
    /// <summary>
    /// This function add shotting scores this player had earned for later transmission
    /// </summary>
    /// <param name="victim"></param>
    /// <param name="damage"></param>
    /// <param name="hit_point"></param>
    /// <param name="force"></param>
    /// <param name="force_dir"></param>
    /// <param name="isHeadshot"></param>
    /// <param name="damagetype">0: physical; 1: thermal</param>
    public void add_to_shot_list(GameObject victim, float damage, Vector2 hit_point, float force, short force_dir, bool isHeadshot, byte damagetype)
    {
        //
        int idx = -1;
        for (int i = 0; i < dmgls_victims.Count; i++)
        {
            if(dmgls_victims[i] == victim)
            {
                idx = i;
                break;
            }
        }
        if(idx == -1)//New victim
        {
            dmgls_victims.Add(victim);
            dmgls_damages.Add(damage);
            dmgls_hit_points.Add(hit_point);
            dmgls_forces.Add(force);
            dmgls_force_dirs.Add(force_dir);
            dmgls_isheadshots.Add(isHeadshot);
            dmgls_damagetypes.Add(damagetype);
        }
        else
        {
            dmgls_victims.Insert(idx+1, null);
            dmgls_damages.Insert(idx+1, damage);
            dmgls_hit_points.Insert(idx+1, hit_point);
            dmgls_forces.Insert(idx+1, force);
            dmgls_force_dirs.Insert(idx+1, force_dir);
            dmgls_isheadshots.Insert(idx+1, isHeadshot);
            dmgls_damagetypes.Insert(idx+1, damagetype);
        }
    }
    

    /// <summary>
    /// This function evaluate damage event
    /// This function only applies on non-server clients
    /// </summary>
    void local_damage_list_update()
    {
        if (!isServer && dmgls_victims.Count > 0 && Time.realtimeSinceStartup > time_to_send_shotdamage)
        {
            time_to_send_shotdamage = Time.realtimeSinceStartup + shot_send_interval;
            Cmd_I_shot_multiple_characters(dmgls_victims.ToArray(), dmgls_damages.ToArray(), dmgls_hit_points.ToArray(), dmgls_forces.ToArray(), dmgls_force_dirs.ToArray(), dmgls_isheadshots.ToArray(), dmgls_damagetypes.ToArray());
            dmgls_victims.Clear();
            dmgls_damages.Clear();
            dmgls_hit_points.Clear();
            dmgls_forces.Clear();
            dmgls_force_dirs.Clear();
            dmgls_isheadshots.Clear();
            dmgls_damagetypes.Clear();
        }
    }

    [Command]
    void Cmd_toggleLaserAim()
    {
        if (equiped_item == null)
        {
            return;
        }
        Equipable_generic equipable = equiped_item.GetComponent<Equipable_generic>();
        if (equipable.laserAimOn)
        {
            equipable.laserAimOn = false;
        }
        else
        {
            equipable.laserAimOn = true;
        }
    }
    
    /// <summary>
    /// This function send the damage list to the server
    /// </summary>
    /// <param name="victims"></param>
    /// <param name="damage"></param>
    /// <param name="hit_point"></param>
    /// <param name="force"></param>
    /// <param name="force_dir"></param>
    /// <param name="isheadshot"></param>
    /// <param name="damagetype"></param>
    [Command]
    public void Cmd_I_shot_multiple_characters(GameObject[] victims, float[] damage, Vector2[] hit_point, float[] force, short[] force_dir, bool[] isheadshot, byte[] damagetype)
    {
        Body_generic body = null;
        Structure_generic structure = null;
        Vector2 force_vec = new Vector2();
        float angle = 0;

        for (int i = 0; i < victims.Length; i++)
        {
            if (victims[i] != null)
            {
                body = victims[i].GetComponent<Body_generic>();
                structure = victims[i].GetComponent<Structure_generic>();
            }
            else if(body == null && structure == null)//If the object is destroyed by the time the remote cmd arrives
            {
                continue;
            }

            angle = CONSTANTS.seed_short_to_float(force_dir[i], 360);
            force_vec.x = Mathf.Cos(angle * Mathf.PI / 180);
            force_vec.y = Mathf.Sin(angle * Mathf.PI / 180);
            force_vec = force_vec.normalized * force[i];
            

            //Cause damage and bleed and force
            if (damage[i] > 0)
            {
                if (body != null)//Bodily damage
                {
                    if (damagetype[i] == 0)//physical
                    {
                        body.damage(gameObject, force: force_vec, dmg_physics: damage[i], headshot: isheadshot[i]);
                    }
                    else if (damagetype[i] == 1)//thermal
                    {
                        body.damage(gameObject, force: force_vec, dmg_physics: CONSTANTS.heat_to_physics(damage[i]), dmg_thermal: damage[i], headshot: isheadshot[i]);
                    }
                    else if (damagetype[i] == 2)//electrical
                    {
                        body.damage(gameObject, force: force_vec, dmg_electric: damage[i], headshot: isheadshot[i]);
                    }

                    if (body.isPlayer && !body.hasAuthority)//Non-server client
                    {
                        //body.Rpc_bleed_n_force(hit_point, force, isHeadShot);
                        body.request_bleed(hit_point[i], angle, isheadshot[i]);
                        body.Rpc_add_force(force_vec);
                    }
                    else//Host
                    {
                        body.request_bleed(hit_point[i], angle, isheadshot[i]);
                        body.GetComponent<Rigidbody2D>().AddForceAtPosition(force_vec, hit_point[i]);
                    }
                }
                else//Structural damage
                {
                    if (damagetype[i] == 0)//physical
                    {
                        structure.health -= damage[i];
                    }
                    else if (damagetype[i] == 1)//thermal
                    {
                        structure.health -= CONSTANTS.heat_to_physics(damage[i]);
                    }
                    else if (damagetype[i] == 2)//electrical
                    {
                        structure.health -= damage[i];
                    }
                }
            }
            //Friendly fire, just force
            else
            {
                if(body != null)
                {
                    if (!body.hasAuthority)//Non-server client
                    {
                        body.Rpc_add_force(force_vec);
                    }
                    else//Host
                    {
                        body.GetComponent<Rigidbody2D>().AddForceAtPosition(force_vec, hit_point[i]);
                    }
                }
                
            }
        }
    }
    

    [Command]
    public void Cmd_request_shoot_optimized_single(short fire_point_x, short fire_point_y, short aim_dir, bool dry_gun)
    {
        Gun_generic gun = equiped_item.GetComponent<Gun_generic>();
        gun.server_shoot(gameObject, fire_point_x, fire_point_y, aim_dir , null, dry_gun);
    }
    [Command]
    public void Cmd_request_shoot_optimized(short fire_point_x, short fire_point_y, short aim_dir, sbyte[] aim_seed, bool dry_gun)
    {
        Gun_generic gun = equiped_item.GetComponent<Gun_generic>();
        gun.server_shoot(gameObject, fire_point_x, fire_point_y, aim_dir, aim_seed, dry_gun);
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
        if (isLocalPlayer)
        {
            return;
        }
        Gun_generic gun = equiped_item.GetComponent<Gun_generic>();
        //Lag simulation: the client that is viewing this bullet && the shooter's latency to the server
        gun.shoot(gameObject, fire_point_x, fire_point_y, aim_dir, aim_dir_offset, cvar_watcher.local_player.latency / 2 + player_view.latency_sv / 2);
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
        if (isLocalPlayer)
        {
            return;
        }
        Gun_generic gun = equiped_item.GetComponent<Gun_generic>();
        gun.shoot(gameObject, fire_point_x, fire_point_y, aim_dir, null, cvar_watcher.local_player.latency / 2 + player_view.latency_sv / 2);//local view latency + shooter's latency
        //body.anim_reload(false);
    }


    
    




    [Command(channel = 1)]
    public void Cmd_toggle_maglite(bool on)
    {
        Rpc_toggle_maglite(on);
        if(maglite_nettransform != null)
        {
            maglite_nettransform.enabled = on;
        }
    }
    [ClientRpc]
    public void Rpc_toggle_maglite(bool on)
    {
        toggle_maglite(on);
        if (maglite_nettransform != null)
        {
            maglite_nettransform.enabled = on;
        }
    }
    void toggle_maglite(bool on)
    {
        maglite.enabled = on;
    }

    public void shake_screen(float mul, float dir)
    {
        Vector2 rand_vec = new Vector2(Mathf.Cos(dir * 3.1415926f / 180), Mathf.Sin(dir * 3.1415926f / 180));
        rand_vec *= mul * body.aim_suppress * CONSTANTS.SCREENSHAKE_DIST;
        shake_vec += rand_vec;
    }

    public void buy_cart()
    {
        //how to toggle add/remove
        //how to spawn all items and let player pick them all up
        //GetComponent<Player_HUD>().menu
        //Cmd_buy((byte)item_idx, (ushort)(inventory.size - inventory.capacity), has_item(GetComponent<Player_HUD>().menu.purchasables[item_idx]));
    }
    /// <summary>
    /// This function requests to purchase an item
    /// This function assumes client to be able to pickup all the items.
    /// This function only spawn one instance for the same thing and pass number of each as parameter, Client modify amount for item.
    /// </summary>
    /// <param name="item_idx"></param>
    /// <param name="upgrade_idx"></param>

    [Command]
    public void Cmd_buy(byte[] item_idx, byte[] upgrade_idx)
    {
        Menu_watcher menu = GetComponent<Player_HUD>().menu;
        List<GameObject> purchased_list = new List<GameObject>();
        List<byte> purchased_list_idx = new List<byte>();
        List<byte> purchased_list_count = new List<byte>();


        GameObject[] purchasables = null;
        if(body.isHuman())
        {
            purchasables = cvar_watcher.purchases_human;
        }
        else if(body.isBot())
        {
            purchasables = cvar_watcher.purchases_robot;
        }
        else if (body.isZombie())
        {
            purchasables = cvar_watcher.purchases_zombie;
        }
        for (int i = 0; i < item_idx.Length; i++)//Instantiate object first to evaluate what to spawn later
        {
            //Spawn items
            if (!purchased_list_idx.Contains(item_idx[i]))//Dont spawn multiple instances of the same ammo, just spawn by type
            {
                GameObject purchased = Instantiate((purchasables[item_idx[i]]), CONSTANTS.SPAWN_ITEM_POSITION, Quaternion.identity);

                purchased.GetComponent<Equipable_generic>().set_user(gameObject);
                
                purchased_list.Add(purchased);
                purchased_list_idx.Add(item_idx[i]);
                purchased_list_count.Add(1);
                NetworkServer.Spawn(purchased);
            }
            else
            {
                purchased_list_count[purchased_list_idx.IndexOf(item_idx[i])] += 1;
            }
            //Cost money
            body.money -= purchasables[item_idx[i]].GetComponent<Equipable_generic>().price;
            if(body.money < 0) { body.money = 0; }
        }
        server_assign_purchases(purchased_list.ToArray(), purchased_list_count.ToArray());
        body.skill_points -= (byte)upgrade_idx.Length;
        body.upgrade_stat(upgrade_idx);
    }
    
    /// <summary>
    /// Check if local has space to pick up
    /// </summary>
    public void Pick_up()
    {
        Vector2 aimdir = mousepos - (Vector2)transform.position;
        Vector2 playerposition = transform.position;
        RaycastHit2D hit = Physics2D.BoxCast(playerposition, CONSTANTS.PICK_UP_SIZE, 0, aimdir, inventory.use_reach, inventory.pickup_fltr);
        if (!hit) { return; }

        ushort available_capacity = (ushort)(body.inventory_size - inventory.capacity);
        GameObject item = hit.collider.gameObject;
        if(!item.GetComponent<Equipable_generic>().isAvailable())
        {
            return;
        }
        if (hit.collider.tag == CONSTANTS.TAG_GUN)
        {
            if (!has_item(item) && hit.collider.GetComponent<Equipable_generic>().get_size() <= available_capacity)
            {
                Cmd_pickup(item, available_capacity, false);
            }
        }
        else if (hit.collider.tag == CONSTANTS.TAG_AMMO && hit.collider.GetComponent<Ammo_generic>().bullet_size <= available_capacity)
        {
            Cmd_pickup(item, available_capacity, has_item(item));
        }
        else if(hit.collider.tag == CONSTANTS.TAG_GRENADE && hit.collider.GetComponent<Grenade_generic>().grenade_size <= available_capacity)
        {
            Cmd_pickup(item, available_capacity, has_item(item));
        }
        debug_add_log("\nClient pickup: " +item.name + ", inventory: "+inventory.capacity + "/"+body.inventory_size);
    }
    public bool has_item(GameObject item)
    {
        //It is a gun
        if (item.tag == CONSTANTS.TAG_GUN)
        {
            Gun_generic pickup_gun = item.GetComponent<Gun_generic>();
            //Already has the gun, cant take
            for (int i = 0; i < inventory.item.Count; i++)
            {
                if ((inventory.item[i].tag == CONSTANTS.TAG_GUN) && (inventory.item[i].GetComponent<Gun_generic>().guntype == pickup_gun.guntype))
                {
                    return true;
                }
            }
        }
        //It is a ammobox
        else if (item.tag == CONSTANTS.TAG_AMMO)
        {
            Ammo_generic pickup_ammotype = item.GetComponent<Ammo_generic>();
            for (int i = 0; i < inventory.item.Count; i++)
            {
                if ((inventory.item[i].tag == CONSTANTS.TAG_AMMO) && (inventory.item[i].GetComponent<Ammo_generic>().ammotype == pickup_ammotype.ammotype))
                {
                    return true;
                }
            }
        }
        else if (item.tag == CONSTANTS.TAG_GRENADE)
        {
            Grenade_generic pickup_grenade = item.GetComponent<Grenade_generic>();
            //Already has the grenade, cant take
            for (int i = 0; i < inventory.item.Count; i++)
            {
                if ((inventory.item[i].tag == CONSTANTS.TAG_GRENADE) && (inventory.item[i].GetComponent<Grenade_generic>().grenadetype == pickup_grenade.grenadetype))
                {
                    return true;
                }
            }
        }
        return false;
    }
    [Command]
    public void Cmd_build_base()
    {
        body.build_base();
    }
    /// <summary>
    /// This function let local player request to obtain item
    /// </summary>
    /// <param name="item"></param>
    /// <param name="size"></param>
    /// <param name="capacity"></param>
    /// <param name="has_it"></param>
    [Command]
    public void Cmd_pickup(GameObject item, ushort available_capacity, bool has_it)
    {
        server_pickup(item, available_capacity, has_it);
    }
    /// <summary>
    /// This function assign multiple items to an client
    /// </summary>
    /// <param name="items">item objects to assign</param>
    /// <param name="number">how many instance of the same item to assign? To modify ammo</param>
    void server_assign_purchases(GameObject[] items, byte[] number)
    {
        Rpc_get_purchases(items, number);
        if (isDedicatedServer())
        {
            get_purchases(items, number);
        }
    }
    [ClientRpc]
    public void Rpc_get_purchases(GameObject[] items, byte[] number)
    {
        get_purchases(items, number);
    }
    void get_purchases(GameObject[] items, byte[] number)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].tag == CONSTANTS.TAG_AMMO)//Modify amount for ammo
            {
                items[i].GetComponent<Ammo_generic>().amount *= number[i];
            }
            //items[i].GetComponent<Equipable_generic>().parented = true;
            obtain(items[i], -1);
        }
    }
    /// <summary>
    /// This function initializes pickup on server-side, it evaluates if client can pick up then it tell clients to assign models
    /// This function includes client's available inventory, because it can decide how much an ammobox has left after client pick partial of it up
    /// All item stats remains authoritative on server
    /// </summary>
    /// <param name="item">item in question</param>
    /// <param name="available_capacity"> client available inventory as reference</param>
    /// <param name="has_it">does the client own the same type of the item?</param>
    void server_pickup(GameObject item, ushort available_capacity, bool has_it)
    {
        
        if (item == null || !item.GetComponent<Equipable_generic>().isAvailable() || body.isDead)
        {   
            return;
        }
        IEquiptable itemIEquip = item.GetComponent<IEquiptable>();
        if (item.tag == CONSTANTS.TAG_GUN)
        {
            Client_watcher.Singleton.deregister_item(itemIEquip);
            Gun_generic pickup_gun = item.GetComponent<Gun_generic>();
            
            if (pickup_gun.weapon_size <= available_capacity)
            {
                item.GetComponent<Equipable_generic>().set_user(gameObject);
                Rpc_obtain(item, -1);
                if (isDedicatedServer())
                {
                    obtain(item, -1);
                }
            }
        }
        else if (item.tag == CONSTANTS.TAG_AMMO)
        {
            Ammo_generic pickup_ammo = item.GetComponent<Ammo_generic>();
            //pickup whole ammobox
            if (pickup_ammo.bullet_size * pickup_ammo.amount <= available_capacity)
            {
                Client_watcher.Singleton.deregister_item(pickup_ammo);
                if (has_it)//Merging ammoboxes; no need to assign ownership, just delete
                {
                    Rpc_supply_ammo(pickup_ammo.ammotype, pickup_ammo.amount, pickup_ammo.bullet_size, pickup_ammo.bullet_weight);
                    if (isDedicatedServer())
                    {
                        supply_ammo(pickup_ammo.ammotype, pickup_ammo.amount, pickup_ammo.bullet_size, pickup_ammo.bullet_weight);
                    }
                    NetworkServer.Destroy(item);
                }
                else//Pick up as independant item
                {
                    item.GetComponent<Equipable_generic>().set_user(gameObject);
                    Rpc_obtain(item, -1);
                    if (isDedicatedServer())
                    {
                        obtain(item, -1);
                    }
                }
            }
            //pick up partial of the ammobox
            else if (pickup_ammo.bullet_size < available_capacity)
            {
                Client_watcher.Singleton.prolong_item(pickup_ammo);
                ushort rounded;
                rounded = (ushort)((available_capacity) / pickup_ammo.bullet_size);
                //pickup_ammo.amount -= rounded;
                Rpc_set_ammo(item, (ushort)(pickup_ammo.amount - rounded));//reduce original ammobox's amount
                if (isDedicatedServer())
                {
                    set_ammo(item, (ushort)(pickup_ammo.amount - rounded));
                }
                if (has_it)//Adding a partial amount to the owned ammobox
                {
                    Rpc_supply_ammo(pickup_ammo.ammotype, rounded, pickup_ammo.bullet_size, pickup_ammo.bullet_weight);
                    if (isDedicatedServer())
                    {
                        supply_ammo(pickup_ammo.ammotype, rounded, pickup_ammo.bullet_size, pickup_ammo.bullet_weight);
                    }
                }
                else//Split amount and create a new ammobox; Set new spawned ammo ownership
                {
                    
                    GameObject ammobox = Instantiate(Resources.Load("Prefab/Item/" + pickup_ammo.eject) as GameObject, transform.position, Quaternion.identity);
                    NetworkServer.Spawn(ammobox);
                    ammobox.GetComponent<Equipable_generic>().set_user(gameObject);

                    Rpc_init_ammo(ammobox, rounded, pickup_ammo.eject);//
                    Rpc_obtain(ammobox, -1);
                    if (isDedicatedServer())
                    {
                        init_ammo(ammobox, rounded, pickup_ammo.eject);
                        obtain(ammobox, -1);
                    }
                }
            }
        }
        if (item.tag == CONSTANTS.TAG_GRENADE)
        {

            Grenade_generic pickup_grenade = item.GetComponent<Grenade_generic>();
            if (pickup_grenade.grenade_size <= available_capacity && pickup_grenade.exploded == false)
            {
                if (has_it && pickup_grenade.pulled == false)//has the grenade inventory and it is not triggered
                {
                    Rpc_supply_grenade(pickup_grenade.grenadetype, pickup_grenade.grenade_size, pickup_grenade.grenade_weight);
                    if (isDedicatedServer())
                    {
                        supply_grenade(pickup_grenade.grenadetype, pickup_grenade.grenade_size, pickup_grenade.grenade_weight);
                    }
                    NetworkServer.Destroy(item);
                }
                else//dont have it & triggered
                {
                    Rpc_obtain(item, -1);
                    if (isDedicatedServer())
                    {
                        obtain(item, -1);
                    }
                    item.GetComponent<Equipable_generic>().set_user(gameObject);
                }
            }
        }
    }




    [ClientRpc]
    public void Rpc_obtain(GameObject item, sbyte insertOnly)//Insertonly: when grenade is thrown, spawn new and assign it to player
    {
        //item.GetComponent<Equipable_generic>().parented = true;
        obtain(item, insertOnly);
    }
    /// <summary>
    /// This function assigns item model to the character model
    /// </summary>
    /// <param name="item"></param>
    /// <param name="insertOnly">this parameter tells if the item can be directly inserted (to this index) without affecting weight & size</param>
    public void obtain(GameObject item, sbyte insertOnly)//Used by Rpc_obtain & onstartclient in Equitable_generic
    {
        Equipable_generic item_equip = item.GetComponent<Equipable_generic>();
        

        Body_generic body = GetComponent<Body_generic>();
        if (item.tag == CONSTANTS.TAG_GUN)
        {
            reloading = false;
            
            body.anim_upper.SetInteger("armType", item_equip.anim_equip);
            body.anim_upper.Play("Switch_firearm");
            body.anim_reload(false);
            Gun_generic pickup_gun = item.GetComponent<Gun_generic>();
            inventory.capacity += pickup_gun.weapon_size;
            inventory.weight += pickup_gun.weapon_weight;
            if (inventory.item.Count > 0)
            {
                inventory.item[inventory.item_pointer].GetComponent<Equipable_generic>().setInactive();
            }
            inventory.item.Insert(0, item);
            equiped_item = item;
            inventory.item_pointer = 0;
            if (isLocalPlayer)
            {
                debug_add_log("\nServer pickup: " +item.name + ", ammo: "+pickup_gun.ammo+"; inventory: "+inventory.capacity+"/"+body.inventory_size);
            }
        }
        else if (item.tag == CONSTANTS.TAG_AMMO)
        {
            Ammo_generic pickup_ammobox = item.GetComponent<Ammo_generic>();
            inventory.capacity += (ushort)(pickup_ammobox.amount * pickup_ammobox.bullet_size);
            inventory.weight += (ushort)(pickup_ammobox.amount * pickup_ammobox.bullet_weight);
            if (inventory.item.Count > 0)
            {
                item.GetComponent<Equipable_generic>().setInactive();
            }
            else
            {
                reloading = false;
                body.anim_upper.SetInteger("armType", item_equip.anim_equip);
                body.anim_upper.Play("Switch_firearm");
                body.anim_reload(false);

                equiped_item = item;
                inventory.item_pointer = 0;
            }
            inventory.item.Add(item);
            if (isLocalPlayer)
            {
                debug_add_log("\nServer pickup: " + item.name + ", amount: " + pickup_ammobox.amount + "; inventory: " + inventory.capacity + "/" + body.inventory_size);
            }
        }
        else if (item.tag == CONSTANTS.TAG_GRENADE)
        {
            reloading = false;

            body.anim_upper.SetInteger("armType", item_equip.anim_equip);
            body.anim_upper.Play("Switch_firearm");
            body.anim_reload(false);

            Grenade_generic pickup_grenade = item.GetComponent<Grenade_generic>();
            if (insertOnly == -1)
            {
                inventory.capacity += pickup_grenade.grenade_size;
                inventory.weight += pickup_grenade.grenade_weight;
                if (inventory.item.Count > 0)
                {
                    inventory.item[inventory.item_pointer].GetComponent<Equipable_generic>().setInactive();
                }
                inventory.item.Insert(0, item);
                equiped_item = item;
                inventory.item_pointer = 0;
                if (isLocalPlayer)
                {
                    debug_add_log("\nServer pickup: " + item.name + ", number: " + pickup_grenade.ammo + "; inventory: " + inventory.capacity + "/" + body.inventory_size);
                }
            }
            else
            {
                item.GetComponent<Equipable_generic>().setInactive();
                inventory.item.Insert(insertOnly, item);
                inventory.item_pointer++;
            }
            
        }
        
        item_equip.attach(body.weapon_bone.transform);
        /*
        item.transform.position = body.weapon_bone.transform.position;
        item.transform.parent = body.weapon_bone.transform;
        item.transform.localRotation = Quaternion.Euler(0, 0, 0);
        item.transform.localScale = item.GetComponent<Equipable_generic>().mdl_scale;
        item.GetComponent<Rigidbody2D>().simulated = false;
        item.GetComponent<Collider2D>().enabled = false;
        item.GetComponent<SpriteRenderer>().sortingLayerName = "Equiped";
        item.GetComponent<NetworkTransform>().enabled = false;
        */
    }

    /// <summary>
    /// This function modifies ammo amount thats owned by the client
    /// </summary>
    /// <param name="ammotype"></param>
    /// <param name="amount"></param>
    /// <param name="unit_size"></param>
    /// <param name="unit_weight"></param>
    [ClientRpc]
    public void Rpc_supply_ammo(Ammo_generic.AmmoType ammotype, ushort amount, int unit_size, int unit_weight)
    {
        supply_ammo(ammotype, amount, unit_size, unit_weight);
        if (isLocalPlayer)
        {
            debug_add_log("\nServer pickup: " + ammotype + ", amount: " + amount + "; inventory: " + inventory.capacity + "/" + body.inventory_size);
        }
    }
    void supply_ammo(Ammo_generic.AmmoType ammotype, ushort amount, int unit_size, int unit_weight)
    {
        for (int i = 0; i < inventory.item.Count; i++)
        {
            if ((inventory.item[i].tag == CONSTANTS.TAG_AMMO) && (inventory.item[i].GetComponent<Ammo_generic>().ammotype == ammotype))
            {
                inventory.item[i].GetComponent<Ammo_generic>().amount += amount;
                inventory.capacity += (ushort)(amount * unit_size);
                inventory.weight += (ushort)(amount * unit_weight);
            }
        }
    }
    [ClientRpc]
    public void Rpc_supply_grenade(Grenade_generic.GrenadeType grenadetype, ushort unit_size, ushort unit_weight)
    {
        supply_grenade(grenadetype, unit_size, unit_weight);
    }
    void supply_grenade(Grenade_generic.GrenadeType grenadetype, ushort unit_size, ushort unit_weight)
    {
        for (int i = 0; i < inventory.item.Count; i++)
        {
            if ((inventory.item[i].tag == CONSTANTS.TAG_GRENADE) && (inventory.item[i].GetComponent<Grenade_generic>().grenadetype == grenadetype) && inventory.item[i].GetComponent<Grenade_generic>().pulled == false)
            {
                inventory.item[i].GetComponent<Grenade_generic>().ammo += 1;
                inventory.capacity += unit_size;
                inventory.weight += unit_weight;
                break;
            }
        }
    }

    void local_switch_to(sbyte index)
    {
        switch_to(index);
        Cmd_switch_to(index);
    }
    [Command]
    public void Cmd_switch_to(sbyte index)
    {
        Rpc_switch_to(index);
        if (isDedicatedServer())
        {
            switch_to(index);
        }
    }
    [ClientRpc]
    public void Rpc_switch_to(int index)
    {
        switch_to(index);
    }
    void switch_to(int index)
    {
        if (inventory.item.Count <= index)
        {
            return;
        }
        reloading = false;
        body.anim_reload(false);

        inventory.item[inventory.item_pointer].GetComponent<Equipable_generic>().setInactive();
        equiped_item = inventory.item[index];
        inventory.item_pointer = index;
        inventory.item[index].GetComponent<Equipable_generic>().setActive();

        body.anim_upper.SetInteger("armType", inventory.item[index].GetComponent<Equipable_generic>().anim_equip);
        body.anim_upper.Play("Switch_firearm");
    }



    [Command]
    public void Cmd_pull_string(int ammo, sbyte item_index)
    {
        Grenade_generic grenade = equiped_item.GetComponent<Grenade_generic>();
        if (grenade.pulled == false)
        {
            grenade.pulled = true;
            grenade.activator = gameObject;
            grenade.time_to_explode = Time.time + grenade.exp_delay;
            grenade.GetComponent<SpriteRenderer>().color = Color.cyan;
        }

        if (ammo > 1)
        {
            GameObject dispence_clone = Instantiate(Resources.Load("Prefab/Item/" + grenade.eject) as GameObject, equiped_item.transform.position, Quaternion.identity);
            NetworkServer.Spawn(dispence_clone);
            dispence_clone.GetComponent<Grenade_generic>().Rpc_set((ushort)(ammo - 1));
            Rpc_obtain(dispence_clone, item_index);
            grenade.ammo = 1;
        }
        Rpc_anim_keydown();
    }
    [ClientRpc]
    public void Rpc_anim_keydown()
    {
        body.anim_upper.SetBool("keydown", true);
    }
    [Command]
    public void Cmd_anim_keyup(bool no_local)
    {
        Rpc_anim_keyup(no_local);
    }
    [ClientRpc]
    public void Rpc_anim_keyup(bool no_local)
    {

        if((no_local && !isLocalPlayer) || (!no_local))
        {
            body.anim_upper.SetBool("keydown", false);
        }

    }
    [ClientRpc]
    public void Rpc_anim_switch()
    {
        body.anim_upper.Play("Switch_firearm");
    }
    [Command]
    public void Cmd_drop(GameObject item, Vector2 throw_force, ushort ammo, float fireangle, Gun_generic.FireMode firemode)
    {
        if(item.GetComponent<Equipable_generic>().user == null)//Prevent dropping the same item multiple times, causing client to lose weight repeatedly
        {
            return;
        }


        Rpc_drop(item, throw_force, ammo, fireangle, firemode);
        if (isDedicatedServer())
        {
            dump_item(item, throw_force, ammo, fireangle, firemode);
        }

        item.GetComponent<Equipable_generic>().set_user(null);
        IEquiptable itemIEquip = item.GetComponent<IEquiptable>();
        if (item.tag == CONSTANTS.TAG_GRENADE)
        {
            Grenade_generic grenade = item.GetComponent<Grenade_generic>();
            if (ammo > 1)//dispence only
            {
                GameObject dispence_clone = Instantiate(Resources.Load("Prefab/Item/" + grenade.eject) as GameObject, playerRB.position, item.transform.rotation);
                NetworkServer.Spawn(dispence_clone);
                dispence_clone.GetComponent<Rigidbody2D>().AddForce(throw_force);
                item.GetComponent<Rigidbody2D>().AddTorque(inventory.drop_torque);
            }
            else if (grenade.pulled == true)//tossing
            {
                //Level detect weapon bone and fire_point
                //Cheap decal
                item.layer = LayerMask.NameToLayer("Props_IgnoreItem");
                grenade.GetComponent<SpriteRenderer>().color = Color.cyan;
            }
        }
        else if (item.tag == CONSTANTS.TAG_GUN)
        {
            Client_watcher.Singleton.register_item(itemIEquip);
        }
        else if(item.tag == CONSTANTS.TAG_AMMO)
        {
            Client_watcher.Singleton.register_item(itemIEquip);
        }
    }
    [ClientRpc]
    public void Rpc_drop(GameObject item, Vector2 throw_force, ushort ammo, float fireangle, Gun_generic.FireMode firemode) {
        //item.GetComponent<Equipable_generic>().parented = false;
        dump_item(item, throw_force, ammo, fireangle, firemode);
        if (isLocalPlayer)
        {
            debug_add_log("\ndrop: " +item+"; inventory: "+inventory.capacity+"/"+body.inventory_size);
        }
    }
    public void dump_item(GameObject item, Vector2 throw_force, ushort ammo, float fireangle, Gun_generic.FireMode firemode)
    {
        reloading = false;
        body.anim_reload(false);

        //Unparent
        if (item.tag == CONSTANTS.TAG_GUN)
        {
            //item.GetComponent<SpriteRenderer>().sprite = item.GetComponent<Gun_generic>().prop_spr;
            //item.GetComponent<SpriteRenderer>().material = item.GetComponent<Gun_generic>().prop_mat;
            item.GetComponent<Gun_generic>().ammo = ammo;
            item.GetComponent<Gun_generic>().firecone_angle = fireangle;
            item.GetComponent<Gun_generic>().firemode = firemode;
            inventory.capacity -= (ushort)item.GetComponent<Gun_generic>().weapon_size;
            inventory.weight -= (ushort)item.GetComponent<Gun_generic>().weapon_weight;
        }
        else if (item.tag == CONSTANTS.TAG_AMMO)
        {
            item.GetComponent<Ammo_generic>().amount = ammo;
            inventory.capacity -= (ushort)(item.GetComponent<Ammo_generic>().amount * item.GetComponent<Ammo_generic>().bullet_size);
            inventory.weight -= (ushort)(item.GetComponent<Ammo_generic>().amount * item.GetComponent<Ammo_generic>().bullet_weight);
        }
        else if (item.tag == CONSTANTS.TAG_GRENADE)
        {
            inventory.capacity -= item.GetComponent<Grenade_generic>().grenade_size;
            inventory.weight -= (ushort)item.GetComponent<Grenade_generic>().grenade_weight;

            if (ammo > 1)
            {
                item.GetComponent<Grenade_generic>().ammo =(ushort)( ammo - 1 );
                return;
            }

            item.GetComponent<Grenade_generic>().ammo = 1;
        }
        Equipable_generic item_equip = item.GetComponent<Equipable_generic>();
        Rigidbody2D itemRB = item.GetComponent<Rigidbody2D>();
        item_equip.detach(playerRB.position);
        //item_equip.position_buffer.x = transform.position.x;
        //item_equip.position_buffer.y = transform.position.y;
        //itemRB.position = item_equip.position_buffer;
        itemRB.velocity = playerRB.velocity;
        itemRB.AddTorque(inventory.drop_torque);
        itemRB.AddForce(throw_force);
        //itemRB.velocity = (throw_force);
        /*
        item.GetComponent<SpriteRenderer>().sortingLayerName = "Items";
        item.GetComponent<Rigidbody2D>().simulated = true;
        item.GetComponent<Rigidbody2D>().velocity = playerRB.velocity;
        item.GetComponent<Collider2D>().enabled = true;
        item.transform.parent = null;
        item.transform.position = position;
        item.transform.localScale = item.GetComponent<Equipable_generic>().mdl_scale;
        item.GetComponent<Rigidbody2D>().AddTorque(inventory.drop_torque);
        item.GetComponent<Rigidbody2D>().AddForce(throw_force);
        item.GetComponent<NetworkTransform>().enabled = true;
        */



        //Change item
        int index = inventory.item.FindIndex(x => x == item);
        inventory.item.Remove(item);
        if (index == inventory.item_pointer)
        {
            inventory.item_pointer--;
            if (inventory.item_pointer >= 0)//there are remaining items & index able to decrease
            {
                equiped_item = inventory.item[inventory.item_pointer];
                equiped_item.GetComponent<Equipable_generic>().setActive();

                body.anim_upper.SetInteger("armType", inventory.item[inventory.item_pointer].GetComponent<Equipable_generic>().anim_equip);
                body.anim_upper.Play("Switch_firearm");
            }
            else if (inventory.item_pointer < 0 && inventory.item.Count > 0)//there are remaining items & wrap around index
            {
                inventory.item_pointer = 0;
                equiped_item = inventory.item[0];
                equiped_item.GetComponent<Equipable_generic>().setActive();

                body.anim_upper.SetInteger("armType", inventory.item[0].GetComponent<Equipable_generic>().anim_equip);
                body.anim_upper.Play("Switch_firearm");

            }
            else//no more item
            {
                inventory.item_pointer = -1;
                equiped_item = null;

                body.anim_upper.SetInteger("armType", -1);
            }
        }
        else if (index < inventory.item_pointer)
        {
            inventory.item_pointer--;
        }

        if (ammo == -1 && isServer)
        {
            NetworkServer.Destroy(item);
        }
    }
    [Command]
    public void Cmd_dispence_ammo(string ammo_name, ushort amount, Vector2 position, Quaternion rotation)
    {
        GameObject dispence_clone = Instantiate(Resources.Load("Prefab/Item/" + ammo_name) as GameObject, position, rotation);
        Client_watcher.Singleton.register_item(dispence_clone.GetComponent<IEquiptable>());

        NetworkServer.Spawn(dispence_clone);

        Rpc_init_ammo(dispence_clone, amount, ammo_name);
        if (isDedicatedServer())
        {
            init_ammo(dispence_clone, amount, ammo_name);
        }
    }
    /// <summary>
    /// Initialize network spawned ammobox
    /// </summary>
    /// <param name="ammobox"></param
    /// <param name="amount"></param>
    /// <param name="eject"></param>
    [ClientRpc]
    public void Rpc_init_ammo(GameObject ammobox, ushort amount, string eject)
    {
        init_ammo(ammobox, amount, eject);
    }
    void init_ammo(GameObject ammobox, ushort amount, string eject)
    {
        ammobox.GetComponent<Ammo_generic>().amount = amount;
        ammobox.GetComponent<Ammo_generic>().eject = eject;
    }
    [ClientRpc]
    public void Rpc_set_ammo(GameObject ammobox, ushort amount)
    {
        set_ammo(ammobox, amount);
    }
    void set_ammo(GameObject ammobox, ushort amount)
    {
        ammobox.GetComponent<Ammo_generic>().amount = amount;
    }
    [Command]
    public void Cmd_deplete_ammo(GameObject obj)
    {
        Rpc_deplete_ammo(obj);
        if (isDedicatedServer())
        {
            deplete_ammo(obj);
        }
    }
    [ClientRpc]
    public void Rpc_deplete_ammo(GameObject obj)
    {
        deplete_ammo(obj);
    }
    void deplete_ammo(GameObject obj)
    {
        if (obj == null || obj.GetComponent<Equipable_generic>().user == null)
        {
            return;
        }
        Player_inventory _inventory = obj.GetComponent<Equipable_generic>().user.GetComponent<Player_inventory>();
        int find = _inventory.item.IndexOf(obj);
        if (find != -1)
        {
            _inventory.item.RemoveAt(find);
            Destroy(obj);
        }
    }
}

/*
 * public void Pull_trigger()
    {
        Gun_generic gun = equiped_item.GetComponent<Gun_generic>();
        LayerMask layer = new LayerMask();
        layer.value = 2048;//Level layer
        RaycastHit2D hit = Physics2D.Linecast(transform.position, gun.fire_point.position, layer);
        if ((gun.ammo <= 0) || (Time.time <= gun.time_to_fire) || hit.collider != null)
        {
            return;
        }
        
        gun.time_to_fire = Time.time + 1 / gun.rate_of_fire;

        Vector2 playerpos = transform.position;
        Vector2 aimdir = gun.fire_point.transform.position - weapon_bone.position;
        float aim_angle;
        if (aimdir.y >= 0)
        {
            aim_angle = Vector2.Angle(new Vector2(1, 0), aimdir);
        }
        else
        {
            aim_angle = -Vector2.Angle(new Vector2(1, 0), aimdir);
        }
        Cmd_request_shoot(gun.fire_point.position, aim_angle, gun.firecone_angle);
        spawn_muzzle(gun);//prespawn muzzle
        playerRB.AddForce(-aimdir.normalized * gun.recoil);
        if (gun.firecone_angle < gun.accuracy - gun.accuracy / gun.bias_factor)
        {
            gun.firecone_angle += gun.accuracy / gun.bias_factor;
        }
        else
        {
            gun.firecone_angle = gun.accuracy;
        }
        gun.ammo -= 1;
        if(gun.ammo == 0)
        {
            Cmd_mdl_unload(true);
        }
        
    }




    //client request shoot, server init simulation, then send across clients for shoot fx; 
    //reason for seperate function: server init requires another rpc client call
    [Command]
    public void Cmd_request_shoot(Vector2 fire_point, float aim_dir, float firecone_angle)
    {

        Gun_generic gun = equiped_item.GetComponent<Gun_generic>();
        gun.firecone_angle = firecone_angle;

        sound_watcher.summon_listener(transform.position, gun.maximum_dist, gameObject.layer);
        float[] blt_dir = new float[gun.burst_shots];
        for(int i = 0; i < gun.burst_shots; i++)
        {
            blt_dir[i] = aim_dir + Random.Range(-gun.precise - gun.firecone_angle, gun.precise + gun.firecone_angle);
        }
        Rpc_client_shoot(fire_point, blt_dir);
    }
    [ClientRpc]
    public void Rpc_client_shoot(Vector2 fire_point, float[] aim_dir)
    {
        if (equiped_item == null || equiped_item.GetComponent<Gun_generic>() == null)
        {
            return;
        }
        reloading = false;
        anim_reload(false);


        Gun_generic gun = equiped_item.GetComponent<Gun_generic>();
        if (!isLocalPlayer)//Local client doesn't need muzzle cuz they spawn one when shoot locally
        {
            spawn_muzzle(gun);
        }
        if (gun.bullet.tag == "bullet")
        {
            for (int i = 0; i < aim_dir.Length; i++)
            {
                GameObject blt = Instantiate(gun.bullet, fire_point, Quaternion.identity);
                Bullet_generic bullet = blt.GetComponent<Bullet_generic>();
                bullet.aimdir = (new Vector2(Mathf.Cos(aim_dir[i] * Mathf.PI / 180), Mathf.Sin(aim_dir[i] * Mathf.PI / 180))).normalized;
                blt.GetComponent<TrailRenderer>().widthMultiplier = gun.GetComponent<Gun_generic>().blt_mass / 20;
                bullet.mass = gun.blt_mass;
                bullet.speed = gun.blt_speed;
                bullet.speed_muzzle = gun.blt_speed;
                bullet.speed_damp = gun.blt_speed_damp;
                bullet.speed_min = gun.blt_speed_min;
                bullet.spark = gun.spark;
                if (gun.blt_custom)
                {
                    blt.GetComponent<TrailRenderer>().colorGradient = gun.blt_color;
                    if (gun.blt_texture != null)
                    {
                        blt.GetComponent<LineRenderer>().material.mainTexture = gun.blt_texture;
                    }
                }
                if (isServer)
                {
                    bullet.activator = gameObject;
                    bullet.local = true;
                }
                else
                {
                    bullet.gameObject.AddComponent<Rigidbody2D>();
                }
            }
        }
        else if (gun.bullet.tag == "bullet_rocket")
        {
            if (isServer)
            {
                for (int i = 0; i < aim_dir.Length; i++)
                {
                
                    GameObject rkt = Instantiate(gun.bullet, fire_point, Quaternion.identity);
                    NetworkServer.Spawn(rkt);
                    Rocket_generic rocket = rkt.GetComponent<Rocket_generic>();
                    rocket.aimdir = (new Vector2(Mathf.Cos(aim_dir[i] * Mathf.PI / 180), Mathf.Sin(aim_dir[i] * Mathf.PI / 180))).normalized;
                    rkt.transform.rotation = gun.transform.rotation;
                    rocket.speed = gun.blt_speed;
                    rocket.transform.rotation = Quaternion.Euler(0, 0, aim_dir[i]);
                    rocket.activator = gameObject;
                    rocket.local = true;
                }
            }
        }
        else if (gun.bullet.tag == "bullet_laser")
        {
            for (int i = 0; i < aim_dir.Length; i++)
            {
                
                GameObject blt = Instantiate(gun.bullet, fire_point, Quaternion.identity);
                Laser_generic laser = blt.GetComponent<Laser_generic>();



                
                laser.start = fire_point;
                laser.temperature = gun.thermal;
                laser.distance = gun.effective_dist;
                laser.aimdir = (new Vector2(Mathf.Cos(aim_dir[i] * Mathf.PI / 180), Mathf.Sin(aim_dir[i] * Mathf.PI / 180))).normalized;
                if (gun.blt_custom)
                {
                    blt.GetComponent<LineRenderer>().colorGradient = gun.blt_color;
                    if (gun.blt_texture != null)
                    {
                        blt.GetComponent<LineRenderer>().material.mainTexture = gun.blt_texture;
                    }
                }
                if (isServer)
                {
                    laser.activator = gameObject;
                    laser.local = true;
                }
                
            }
        }
        else if (gun.bullet.tag == "bullet_flame")
        {
            gun.bullet.transform.rotation = Quaternion.Euler(0, 0, aim_dir[0] - gun.bullet.GetComponent<ParticleSystem>().shape.arc / 2);
            gun.bullet.GetComponent<ParticleSystem>().Emit(gun.burst_shots);
        }
    }
    */
