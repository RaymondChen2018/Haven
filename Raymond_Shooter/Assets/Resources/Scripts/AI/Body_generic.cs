using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

[NetworkSettings(sendInterval = 0.1f)]
public class Body_generic : NetworkBehaviour, IDamageActivator, IDamageVictim
{
    public enum Character_type { Human, Robot, Zombie, Observer, Nothing }
    public Character_type character_type = Character_type.Human;
    public enum Character_condition { FREE, FROZEN, DEAD, SCRIPTED }
    [SyncVar] public Character_condition character_cond = Character_condition.FROZEN;
    [SyncVar] public bool staticRB = false;
    /// <summary>
    /// Health is synchronized across the network
    /// </summary>
    [SyncVar] public float health = 100;
    /// <summary>
    /// reload multiplier affect both server and client animation effect, so must be sync
    /// </summary>
    [SyncVar] public float reload_multiplier = 1.0f;
    /// <summary>
    /// The higher value this is, movement penalty is lower
    /// </summary>
    [SyncVar] public float stress_resistent;
    /// <summary>
    /// Melee damage
    /// </summary>
    [SyncVar] public float strength = 10;
    /// <summary>
    /// Aim suppression
    /// </summary>
    [SyncVar] public float aim_suppress = 1;
    /// <summary>
    /// Movement speed
    /// </summary>
    [SyncVar] public float speed_run = 1000;
    /// <summary>
    /// Total space of the inventory
    /// </summary>
    [SyncVar] public ushort inventory_size;

    /// <summary>
    /// Animation update for quick events: reload
    /// -1: reload; -2: unreload
    /// </summary>
    [SyncVar(hook = "Hook_anim_state")] public sbyte animation_state;
    [SyncVar(hook = "Hook_anim_scripted")] public sbyte animation_script;
    [SyncVar(hook = "Hook_update_name")] public string character_name = "npc";
    [HideInInspector] public Scripted_sequence scriptedSequence;


    public float viewRadius;
    [Range(20, 270)]
    public float viewAngle;


    public GameObject melee_detector;
    bool isMeleeing = false;
    Vector2 melee_previous_pos;
    Vector2 melee_box;
    List<GameObject> meleeList = new List<GameObject>();
    public LayerMask melee_filter;
    public float melee_cooldown = 1.5f;
    float time_to_melee = 0;
    float melee_period = 1;


    [HideInInspector] public bool reloading = false;
    /// <summary>
    /// Experience is server-sided
    /// </summary>
    public float experience = 0;
    /// <summary>
    /// money is server-sided
    /// </summary>
    public int money = 0;
    /// <summary>
    /// skill points is server-sided
    /// </summary>
    public byte skill_points;
    /// <summary>
    /// The base amount killer earns when killing this character
    /// </summary>
    public int character_base_worth = 10;
    /// <summary>
    /// If experience exceeds this value will add to skill points
    /// </summary>
    public float next_upgrade_xp = 20;
    public float temperature_tolerance = 20;
    public float temperature_recover_rate = 1f;
    public float body_temperature = 28;
    public float ignite_temperature = 200;
    public float current_temperature;
    private float time_to_tick = 0;
    private float time_to_regen = 0;
    public float tissue_dense;//bullet-stoping power
    public float physical_resilience;
    public float size;
    public bool isPlayer = false;
    public int dead_explode_threshold = -100;

    [HideInInspector] public float initial_mass = 0;



    //Battle


    /// <summary>
    /// list of obj it can deal damage; Human/Zombie team characters are assumed as the enemy teams in objective map and CAN damage entities tagged as "Player"
    /// </summary>
    public List<string> dmg_tags;
    public GameObject weapon_bone;
    /// <summary>
    /// Provides reference for visual; eg. leg movement angle measuring
    /// </summary>
    public SpriteRenderer sprite;
    public GameObject headshot_fx;
    public GameObject[] bleed_fx;
    public GameObject bleed_fx_hs;
    public GameObject explode_splash_decal;
    public float explode_splash_decal_size = 2.5f;
    public GameObject blood_mist_fx;
    public GameObject ash_fx;
    public Collider2D headshot_box;
    public Collider2D hitbox_main;
    int start_layer_idx;
    //Condition
    public Animator anim_upper;
    public Animator anim_lower;
    [SyncVar(hook = "Hook_ignite")] public bool isIgnited = false;
    private Fx_watcher fluid_watcher;
    [HideInInspector] public Server_watcher cvar_watcher;
    [HideInInspector] [SyncVar] public float max_health;
    public bool isDead = false;
    public bool canExplode = true;
    static float respawn_time;
    private float time_to_destroy = 0;

    //public int isAimed;
    [HideInInspector] public Action<KeyValuePair<GameObject, float>> OnDeathSubmitScore;
    [HideInInspector] public Action DeathAction = null;
    [HideInInspector] public Action OnRespawn = null;
    
    //public Collider2D hitbox_collider;
    [HideInInspector] public Rigidbody2D bodyRB;
    Collider2D bodyCLDR;
    RigidbodyType2D initial_rb_type = RigidbodyType2D.Dynamic;
    public enum suppression { burnt, blinded, electricuted}
    public Player_controller player_controller;
    private AI_generic ai_generic;
    private float previous_health = 100;
    
    float initial_health;
    float initial_resiliance;
    float initial_tissue_dense;
    float initial_speed_run;
    float initial_strength;
    float initial_stress;
    float prescript_speed_run;//Temp store speed before scripted_sequence changes this
    ushort initial_inventory_size = 0;
    Vector3 initial_scale = Vector3.one;
    float initial_size = 1;
    public Color skin_color;
    public Colorscheme_generic objColorCode;
    int levels = 0;
    public Action<float, Vector2> OnDamaged;
    [HideInInspector] public Vector2 latest_dmg_dir;
    [HideInInspector] public CONSTANTS.UPGRADE_TYPE[] upgrades = null;
    public Team_watcher team;
    public GameObject base_structure;
    public int base_structure_price = 3000;
    public LayerMask base_structure_unavailable_fltr;
    public Material mat_low;
    public Material mat_high;
    float time_to_gainSkill;
    [HideInInspector] public bool respawnProtected = true;


    /// <summary>
    /// When this character dies, it instantiate a dead corpse; And when this character is further destroy and form blood mist, it uses this link to remove the instantiated corpse.
    /// </summary>
    GameObject ragdoll = null;
    public GameObject ragdoll_prefab = null;
    public GameObject flame_prefab = null;
    ParticleSystem flame_entity = null;
    private Vector2 previous_position;
    /// <summary>
    /// Rigidbody velocity messed up by moveposition and remains at zero, so its not reliable
    /// </summary>
    private float body_speed = 0;
    private Vector2 body_velocity = Vector2.zero;
    public float test_float = 0;
    float delta_time = 1;
    [HideInInspector] public bool has_built_base = false;
    [SerializeField]
    public List<CONSTANTS.IO> I_O;





    bool isDedicatedServer()
    {
        return !isClient && isServer;
    }
    [ClientRpc]
    public void Rpc_send_skin_color(sbyte color_index)
    {
        skin_color = CONSTANTS.PLAYERCOLORS[color_index];
        Skin_color_generic[] skins_color = GetComponentsInChildren<Skin_color_generic>();
        for (int i = 0; i < skins_color.Length; i++)
        {
            if (cvar_watcher.local_character_type != Character_type.Observer && ( cvar_watcher.team_transparent && character_type != cvar_watcher.local_character_type))
            {
                Destroy(skins_color[i].GetComponent<SpriteRenderer>().gameObject);
            }
            else
            {
                skins_color[i].GetComponent<SpriteRenderer>().color = CONSTANTS.PLAYERCOLORS[color_index];
            }
            Destroy(skins_color[i]);
        }
    }
    [ClientRpc]
    public void Rpc_send_skin_info(byte[] skin_map)
    {
        Npc_skin_generic[] skins = GetComponentsInChildren<Npc_skin_generic>();
        
        for (int i = 0; i < skins.Length; i++)
        {
            skins[i].skin_part.sprite = skins[i].sprites_pool[skin_map[skins[i].skin_id]];
            if(skins[i].skin_part_backcull != null)
            {
                skins[i].skin_part_backcull.sprite = skins[i].sprites_pool[skin_map[skins[i].skin_id]];
            }
            Destroy(skins[i]);
        }
        

    }
    
    
    public override void OnStartClient()
    {
        /*
        //Skin select
        Npc_skin_generic skin_pool = GetComponent<Npc_skin_generic>();
        skin_pool.skin_part.sprite = skin_pool.sprites_pool[skin_index];
        if (!isServer)
        {
            Destroy(GetComponent<Npc_skin_generic>());
        }
        */
        /*
        //Skin select
        Npc_skin_generic skin_pool = GetComponent<Npc_skin_generic>();
        if (skin_pool != null)
        {
            if (isServer)
            {
                skin_index = (short)UnityEngine.Random.Range(0, skin_pool.sprites_pool.Count);
            }
            skin_pool.skin_part.sprite = skin_pool.sprites_pool[skin_index];
            Destroy(GetComponent<Npc_skin_generic>());
        }
        */
        /*
        GameObject client_watcher_obj = GameObject.Find("Client_watcher");
        if(client_watcher_obj != null)
        {
            client_watcher = client_watcher_obj.GetComponent<Client_watcher>();
        }
        else
        {
            StartCoroutine(find_client_watcher());
        }
        */
    }
    /*
    public IEnumerator find_client_watcher()
    {
        yield return new WaitForSeconds(0.1f);
        client_watcher = GameObject.Find("Client_watcher").GetComponent<Client_watcher>();
    }
    */
    void Start()
    {
        bodyRB = GetComponent<Rigidbody2D>();
        bodyCLDR = GetComponent<Collider2D>();
        initial_rb_type = bodyRB.bodyType;
        if (isPlayer)
        {
            player_controller = GetComponent<Player_controller>();
        }
        else
        {
            ai_generic = GetComponent<AI_generic>();
            ai_naming();
        }

        cvar_watcher = Server_watcher.Singleton;
        if (isLocalPlayer)
        {
            cvar_watcher.local_character_type = character_type;
        }
        if (isHuman())
        {
            upgrades = cvar_watcher.upgrades_human;
        }
        else if (isBot())
        {
            upgrades = cvar_watcher.upgrades_robot;
        }
        else if (isZombie())
        {
            upgrades = cvar_watcher.upgrades_zombie;
        }
        //Objective color code
        if(objColorCode != null && cvar_watcher.map_type == CONSTANTS.MAP_TYPE.Objective)
        {
            objColorCode.gameObject.SetActive(true);
            Color chosen_color = Color.clear;
            if (isLocalPlayer)
            {
                chosen_color = CONSTANTS.COLOR_PLAYERLOCAL;
            }
            else if (isPlayer)
            {
                chosen_color = CONSTANTS.COLOR_PLAYERALLYOTHER;
            }
            else if(gameObject.layer == LayerMask.NameToLayer("Bot"))
            {
                chosen_color = CONSTANTS.COLOR_ALLY;
            }
            objColorCode.colorScheme.SetKeys(new GradientColorKey[] { new GradientColorKey(chosen_color, 0), new GradientColorKey(chosen_color, 1) }, objColorCode.colorScheme.alphaKeys);
        }
        start_layer_idx = hitbox_main.gameObject.layer;
        respawn_time = cvar_watcher.respawn_time;
        base_structure_price += cvar_watcher.insurance_money;
        max_health = health;
        previous_health = health;
        initial_health = health;
        initial_resiliance = physical_resilience;
        initial_tissue_dense = tissue_dense;
        initial_strength = strength;
        initial_stress = stress_resistent;
        initial_speed_run = speed_run;
        initial_mass = bodyRB.mass;
        initial_inventory_size = inventory_size;
        initial_scale = transform.lossyScale;
        initial_size = size;
        previous_position = bodyRB.position;
        melee_box = new Vector2(size, size);
        current_temperature = body_temperature;
        
        
        fluid_watcher = FindObjectOfType<Fx_watcher>();
        
        //Set quality
        quality_setting(gameObject);
        
        /*
        client_watcher = FindObjectOfType<Client_watcher>();
        if (client_watcher_obj != null)
        {
            client_watcher = client_watcher_obj.GetComponent<Client_watcher>();
        }
        else
        {
            StartCoroutine(find_client_watcher());
        }
        */
        
    }
    
    [ClientRpc]
    public void Rpc_updatePosition(Vector2 position, float angle)
    {
        if (isServer)
        {
            return;
        }
        bodyRB.position = position;
        bodyRB.MoveRotation(angle);
    }
    [Command]
    public void Cmd_send_tesla_path(GameObject[] path, GameObject gun)
    {
        Rpc_send_tesla_path(path, gun);
    }
    [ClientRpc]
    public void Rpc_send_tesla_path(GameObject[] path, GameObject gun)
    {
        if (isLocalPlayer || (isServer && !isPlayer))
        {
            return;
        }
        Gun_generic _gun = gun.GetComponent<Gun_generic>();
        _gun.fire_tesla(gameObject, _gun.fire_point.position, 0, false, path);
    }
    [Command]
    public void Cmd_spawn_tesla_muzzle(GameObject gun)
    {
        Rpc_spawn_tesla_muzzle(gun);
    }
    [ClientRpc]
    public void Rpc_spawn_tesla_muzzle(GameObject gun)
    {
        if (isLocalPlayer || (isServer && !isPlayer))
        {
            return;
        }
        Gun_generic _gun = gun.GetComponent<Gun_generic>();
        _gun.spawn_muzzle();
    }

    [ServerCallback]
    void ai_naming()
    {
        if (isBot())
        {
            character_name = "npc_bot";
        }
        else if (isHuman())
        {
            character_name = "npc_human";
        }
        else if (isZombie())
        {
            character_name = "npc_zombie";
        }
    }
    [ClientCallback]
    void quality_setting(GameObject subject)
    {
        SpriteRenderer[] sprites = subject.GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i].tag == "fx")
            {
                continue;
            }

            if (QualitySettings.GetQualityLevel() <= 1)
            {
                if (sprites[i].gameObject.name == "backface")
                {
                    Destroy(sprites[i].gameObject);
                }
                sprites[i].sharedMaterial = mat_low;
            }
            else
            {
                sprites[i].sharedMaterial = mat_high;
                sprites[i].receiveShadows = true;
            }
            
        }
    }
    void Update()
    {
        if (staticRB)
        {
            bodyRB.bodyType = RigidbodyType2D.Kinematic;
            bodyCLDR.enabled = false;//Disable main collider
            bodyRB.velocity = Vector2.zero;
            bodyRB.angularVelocity = 0;
        }
        else
        {
            bodyCLDR.enabled = true;//Enable main collider
            bodyRB.bodyType = RigidbodyType2D.Dynamic;
        }
        delta_time = Time.deltaTime;
        //Speed calculation; rigidbody velocity is not reliable due to using moveposition() (while directly changing position cause synccollidertransform overhead)
        body_velocity = bodyRB.position - previous_position;
        body_speed = body_velocity.magnitude / delta_time;
        body_velocity = body_velocity.normalized * body_speed;
        previous_position = bodyRB.position;
        //test_float = body_speed;
        //Global operations
        if(health < previous_health && OnDamaged != null)
        {
            OnDamaged(previous_health - health, latest_dmg_dir);
        }
        previous_health = health;
        melee_progress();
        //Animation movement
        /*
        if (anim_upper != null && !isDead)//&& !bodyRB.IsSleeping()
        {
            anim_upper.SetFloat("speed", body_speed * 0.8f, 1, Time.deltaTime * 10f);
            anim_lower.SetFloat("speed", body_speed * 0.8f, 1, Time.deltaTime * 10f);
            anim_upper.SetFloat("motion_cycle", Time.time);
        }
        */
        if(anim_upper != null && !isDead)
        {
            
            float num = Mathf.Atan2(body_velocity.y, body_velocity.x) * 180f / 3.14159274f;
            Vector3 eulerAngles = sprite.transform.rotation.eulerAngles;
            float num2 = (num - eulerAngles.z) % 360f;
            Vector2 vector = new Vector2(Mathf.Cos(num2 / 180f * Mathf.PI), Mathf.Sin(num2 / 180f * Mathf.PI)).normalized * body_speed;
            anim_upper.SetFloat("speed", body_speed * 0.8f, 3, delta_time * 10f);
            
            if(anim_lower != null)
            {
                anim_upper.SetFloat("motion_cycle", anim_lower.GetCurrentAnimatorStateInfo(0).normalizedTime);
                anim_lower.SetFloat("speed", body_speed * 0.8f, 3, delta_time * 10f);
                anim_lower.SetFloat("speedx", vector.x * 0.8f, 3, delta_time * 10f);
                anim_lower.SetFloat("speedy", vector.y * 0.8f, 3, delta_time * 10f);
            }
            else
            {
                anim_upper.SetFloat("motion_cycle", anim_upper.GetCurrentAnimatorStateInfo(0).normalizedTime);
                anim_upper.SetFloat("speedx", vector.x * 0.8f, 3, delta_time * 10f);
                anim_upper.SetFloat("speedy", vector.y * 0.8f, 3, delta_time * 10f);
            }
            
        }
        
        /*
        if (!isZombie())
        {
            float num = Mathf.Atan2(body_velocity.y, body_velocity.x) * 180f / 3.14159274f;
            Vector3 eulerAngles = sprite.transform.rotation.eulerAngles;
            float num2 = (num - eulerAngles.z) % 360f;
            Vector2 vector = new Vector2(Mathf.Cos(num2 / 180f * Mathf.PI), Mathf.Sin(num2 / 180f * Mathf.PI)).normalized * body_speed;
            anim_upper.SetFloat("speed", body_speed * 0.8f, 1, Time.deltaTime * 10f);
            anim_lower.SetFloat("speed", body_speed * 0.8f, 1, Time.deltaTime * 10f);
            anim_lower.SetFloat("speedx", vector.x * 0.8f, 1, Time.deltaTime * 10f);
            anim_lower.SetFloat("speedy", vector.y * 0.8f, 1, Time.deltaTime * 10f);
            
            Vector2 velocity = bodyRB.velocity;
            float y = velocity.y;
            Vector2 velocity2 = bodyRB.velocity;
            float num = Mathf.Atan2(y, velocity2.x) * 180f / 3.14159274f;
            Vector3 eulerAngles = sprite.transform.rotation.eulerAngles;
            float num2 = (num - eulerAngles.z) % 360f;
            Vector2 vector = new Vector2(Mathf.Cos(num2 / 180f * 3.14159274f), Mathf.Sin(num2 / 180f * 3.14159274f)).normalized * bodyRB.velocity.magnitude;
            //anim_upper.SetFloat("speed", body_speed * 0.8f, 1, Time.deltaTime * 10f);
            //anim_lower.SetFloat("speed", body_speed * 0.8f, 1, Time.deltaTime * 10f);
            //anim_lower.SetFloat("speedx", vector.x * 0.8f, 1, Time.deltaTime * 10f);
            //anim_lower.SetFloat("speedy", vector.y * 0.8f, 1, Time.deltaTime * 10f);
            
        }
        
        else
        {
            anim_upper.SetFloat("speed", body_speed * 0.8f, 1, Time.deltaTime * 10f);
            anim_lower.SetFloat("speed", body_speed * 0.8f, 1, Time.deltaTime * 10f);
            anim_upper.SetFloat("motion_cycle", Time.time);
        }
        */
        /*
        else if (isHuman())
        {
            anim_upper.SetFloat("speed", bodyRB.velocity.magnitude * 0.8f);
            anim_lower.SetFloat("speed", bodyRB.velocity.magnitude * 0.8f);
        }
        */



        //Server Operation======================================================
        if (!isServer)
        {
            return;
        }
        //Zombie skill auto incrementation
        if (isZombie() && Time.time > time_to_gainSkill)
        {
            skill_points++;
            time_to_gainSkill = Time.time + CONSTANTS.ZOMBIE_SKILLADD_INTERVAL;
        }
        //adjust body temperature
        if(Time.time > time_to_tick)
        {
            //Regen
            if(Server_watcher.Singleton.map_type == CONSTANTS.MAP_TYPE.Objective && isBot() && !isPlayer && health < max_health && Time.time > time_to_regen)
            {
                time_to_tick = Time.time + CONSTANTS.NETWORK_TICK_RATE;
                health = Mathf.Clamp(health + CONSTANTS.OBJ_ALLYNPC_REGEN, health, max_health);
            }
            //Adjust temperature
            if (current_temperature != body_temperature)
            {
                time_to_tick = Time.time + CONSTANTS.NETWORK_TICK_RATE;
                float temp_gap = Mathf.Abs(current_temperature - body_temperature) - temperature_tolerance;
                if (Mathf.Abs(current_temperature - body_temperature) < temperature_recover_rate)
                {
                    current_temperature = body_temperature;
                }
                else if (current_temperature > body_temperature)
                {
                    if (isIgnited && current_temperature < ignite_temperature)
                    {
                        //suppress(suppression.burnt, false);
                        isIgnited = false;
                        //Rpc_unignite();
                    }
                    current_temperature -= temperature_recover_rate;
                }
                else
                {
                    if (isIgnited && current_temperature < ignite_temperature)
                    {
                        //suppress(suppression.burnt, false);
                        isIgnited = false;
                        //Rpc_unignite();
                    }
                    current_temperature += temperature_recover_rate;
                }
                //damage burn & freeze
                if (temp_gap > 0)
                {

                    damage(null, Vector2.zero, dmg_physics: 0, dmg_thermal: temp_gap / 15, headshot: false, explodeEnabled: false, damage_mode: 1);
                }
            }

        }
        


    }



    [ServerCallback]
    public void set_player_name(string name)
    {
        if(name == "")
        {
            character_name = "Player";
        }
        else
        {
            character_name = name;
        }
        GetComponent<Player_generic>().character_name = character_name;
    }

    public void set_player_color(Color color)
    {
        skin_color =  color;
    }


    public void anim_reload(bool is_reloading)
    {
        if(anim_upper == null)
        {
            return;
        }
        Gun_generic gun;
        if (isPlayer)
        {
            if(player_controller.equiped_item == null)
            {
                return;
            }
            gun = player_controller.equiped_item.GetComponent<Gun_generic>();
        }
        else
        {
            if (ai_generic.equiped_item == null)
            {
                return;
            }
            gun = ai_generic.equiped_item.GetComponent<Gun_generic>();
        }
        //Body_generic body = GetComponent<Body_generic>();
        if (is_reloading)
        {
            anim_upper.SetBool("reload", true);
            anim_upper.SetFloat("reload_time", 1 / (gun.reload_time * reload_multiplier));
        }
        else
        {
            anim_upper.SetBool("reload", false);
        }
    }
    public void Hook_update_name(string newName)
    {
        character_name = newName;
        if (isPlayer)
        {
            GetComponent<Player_generic>().character_name = newName;
        }
    }
    public void Hook_anim_scripted(sbyte value)
    {
        animation_script = value;
        anim_upper.SetInteger("Script", value);
        //anim_upper.Play("Scripted_Anim");
    }
    /// <summary>
    /// Change animation state
    /// -1: reload
    /// -2: unreload
    /// -3: melee charge
    /// -4: melee discharge; charge forward
    /// -5: melee stop
    /// </summary>
    /// <param name="value"></param>
    public void Hook_anim_state(sbyte value)
    {
        animation_state = value;
        if(value >= 0)//Change firearm
        {
            anim_upper.SetInteger("armType", value);
            anim_upper.Play("Switch_firearm");
        }
        else if (value == -1 && !isLocalPlayer)//Reload start (only for non-local, local had already started animation)
        {
            anim_reload(true);
        }
        else if(value == -2 && !isLocalPlayer)//Reload finish (only for non-local, local had already stopped animation)
        {
            anim_reload(false);
        }
        else if(value == -3)//Melee charge
        {
            anim_upper.SetBool("keydown", true);
            anim_upper.Play("Melee_charge");
        }
        else if (value == -4)//Melee discharge, charge forward
        {
            anim_upper.SetBool("keydown", false);
            anim_upper.SetBool("meleeCharge", true);
        }
        else if(value == -5)//Melee stop
        {
            anim_upper.SetBool("meleeCharge", false);
        }
        else if(value == -6 && !isLocalPlayer)//Melee start
        {
            anim_melee_start();
        }
        else if(value == -7)//Melee bitoff
        {
            //Dont remove this condition
            //This is a place holder
        }
    }
    public IEnumerator anim_melee_bitOff()
    {
        yield return new WaitForSeconds(0.2f);
        anim_upper.SetInteger("melee", -1);
        if (isServer)
        {
            set_animation_state(-7);
        }
    }
    //Tell client to Stun flash screen
    [ClientRpc(channel = 1)]
    public void Rpc_stunned_screen(float stun_ratio)
    {
        if (!isLocalPlayer)//non-player entities & non-local players
        {
            return;
        }
        GetComponent<Player_HUD>().stunned_screen(stun_ratio);
    }

    [ServerCallback]
    public void damage(GameObject activator, Vector2 force, float dmg_physics = 0, float dmg_thermal = 0, float dmg_electric = 0, bool headshot = false, bool explodeEnabled = true, int damage_mode = 0) 
    {
        //Deal dmg only if activator allowed to dmg or activator dmg itself
        if (activator != null && !activator.GetComponent<Body_generic>().dmg_tags.Contains(gameObject.tag) && activator != gameObject)
        {
            return;
        }
        //respawn protection
        if (respawnProtected)
        {
            return;
        }


        //
        if(force.magnitude > 0)
        {
            latest_dmg_dir = force;
        }
        else
        {
            latest_dmg_dir = CONSTANTS.VEC_NULL;
        }
        //damage calculation
        time_to_regen = Time.time + CONSTANTS.OBJ_ALLYNPC_REGEN_DMGINTERVAL;
        float damage = 0;
        float experience_value = 0;
        int money_value = 0;

        
        if (dmg_physics != 0)
        {
            if (headshot)
            {
                damage += (dmg_physics) * (1 - Mathf.Pow(100, -(dmg_physics) / physical_resilience)) * 8;
            }else
            {
                damage += (dmg_physics) * (1 - Mathf.Pow(100, -(dmg_physics) / physical_resilience));
            }
             // x * (1-1.1^(-x)/100)
            //health -= dmg_physics * (1 - Mathf.Pow(10, 33.8f * dmg_physics / -Mathf.Pow(physical_resilience + 2 * dmg_physics, 1.5f)));
        }

        if (dmg_thermal != 0)
        {
            if (damage_mode == 0)//weapon damage
            {
                //Out of safe temperature zone, damage
                if (dmg_thermal > body_temperature + temperature_tolerance)
                {
                    damage += (dmg_thermal - body_temperature - temperature_tolerance) / 20000;
                }
                else if (dmg_thermal < body_temperature - temperature_tolerance)
                {
                    damage += (dmg_thermal - body_temperature - temperature_tolerance) / 20000;
                }
                //temperature adjustment
                if (dmg_thermal > current_temperature && dmg_thermal > body_temperature)
                {
                    current_temperature += (dmg_thermal - current_temperature) / 100;
                    
                }
                else if (dmg_thermal < current_temperature && dmg_thermal < body_temperature)
                {
                    current_temperature -= (current_temperature - dmg_thermal) / 100;
                }
            }
            else
            {
                damage += dmg_thermal;
            }
            
        }
        if(dmg_electric != 0)
        {
            current_temperature += Mathf.Max(dmg_electric * 10 - current_temperature, 0);
            damage += dmg_electric;
        }

        if (current_temperature > ignite_temperature && !isIgnited)
        {
            isIgnited = true;
            //suppress(suppression.burnt, true);
            //Rpc_ignite();
        }
        health -= damage;
        experience_value += damage / 100;
        //death
        if (health <= 0)
        {
            if (!isDead)//Killing blow
            {
                experience_value += damage - Mathf.Abs(health);
                money_value += (int)experience/20 + character_base_worth;
                server_die(activator, force, headshot);

            }
            if (health < dead_explode_threshold && canExplode && explodeEnabled)//If damage can explode the body, if the body has not exploded, body damaged enough to explode
            {
                canExplode = false;
                if(current_temperature > ignite_temperature * 2)
                {
                    Rpc_explode(1);
                }
                else
                {
                    Rpc_explode(0);
                }
                
            }
        }
        if(activator != null)
        {
            Body_generic activator_body = activator.GetComponent<Body_generic>();
            //Experience earned by the activator
            activator_body.add_resource(money_value, experience_value / 10);
        }
    }
    /// <summary>
    /// This function set rewards
    /// This function calculates skill points earned by the incrementation of experience
    /// </summary>
    /// <param name="money_value"></param>
    /// <param name="experience_value"></param>
    public void add_resource(int money_value, float experience_value)
    {
        if (!isPlayer)
        {
            experience_value *= CONSTANTS.AI_XP_BONUS_RATIO;
            money_value = (int)(money_value * CONSTANTS.AI_XP_BONUS_RATIO);
        }
        if (isHuman())
        {
            experience_value *= 3;
        }
        else if (isZombie())
        {
            experience_value *= 2;
        }
        money += money_value;
        experience += experience_value;
        while (experience >= next_upgrade_xp)
        {
            skill_points++;
            next_upgrade_xp *= CONSTANTS.LEVEL_XP_MULTIPLIER;
        }
    }
    public bool isBot()
    {
        return character_type == Character_type.Robot;
    }
    public bool isHuman()
    {
        return character_type == Character_type.Human;
    }
    public bool isZombie()
    {
        return character_type == Character_type.Zombie;
    }
    void server_die(GameObject activator, Vector2 force, bool headshot)
    {
        isDead = true;
        OnDeath();
        bool canRespawn = false;
        //PVP, request respawn
        if (cvar_watcher.map_type == CONSTANTS.MAP_TYPE.PVP)
        {
            team.alive--;
            canRespawn = team.requestRespawn();
            //request respawn
            if (canRespawn)
            {
                StartCoroutine(Enable_respawn());
            }
            //Activator score addition
            if (activator != null)
            {
                Player_generic activator_client = activator.GetComponent<Player_generic>();
                if (gameObject != activator && activator_client != null)//Add score, If the player didnt kill itself
                {
                    if (character_type == Character_type.Human)
                    {
                        activator_client.kill1++;
                    }
                    else if (character_type == Character_type.Robot)
                    {
                        activator_client.kill2++;
                    }
                    else if (character_type == Character_type.Zombie)
                    {
                        activator_client.kill3++;
                    }
                }
            }
        }
        

        


        //AI: drop gun & ammo, clear memory
        //Player: drop weapon thru player_controller.die(), add death count
        if (!isPlayer)
        {
            AI_generic ai = GetComponent<AI_generic>();
                
            ai.Drop_item(ai.equiped_item, force);
            ai.memory_clean();
            
            if (ai.ammo_drop_template != null && ai.ammo_drop_count > 0)
            {
                GameObject ammobox = Instantiate(ai.ammo_drop_template, bodyRB.position, transform.rotation);
                Client_watcher.Singleton.register_item(ammobox.GetComponent<IEquiptable>());
                NetworkServer.Spawn(ammobox);
                ammobox.GetComponent<Rigidbody2D>().AddForce(force);

                ammobox.GetComponent<Equipable_generic>().set_ammo((ushort)ai.ammo_drop_count);
                ammobox.GetComponent<Equipable_generic>().Rpc_set_ammo((ushort)ai.ammo_drop_count);
            }
            ai.ammo_drop_count = 0;
            Rpc_AI_die(activator, force, headshot);
            if (isDedicatedServer())
            {
                client_die(activator, force, headshot, 0, 0, 0, !canRespawn);
            }
        }
        else
        {
            player_controller.die();
            GetComponent<Player_generic>().deaths++;
            Rpc_player_die(activator, force, headshot, money, experience, skill_points, !canRespawn);
            if (isDedicatedServer())
            {
                client_die(activator, force, headshot, money, experience, skill_points, !canRespawn);
            }
        }
        StartCoroutine(make_debris());//explode not allowed any more

    }

    [ClientRpc]
    public void Rpc_AI_die(GameObject activator, Vector2 force, bool isHeadShot)
    {
        client_die(activator, force, isHeadShot, 0, 0, 0, false);
    }
    [ClientRpc]
    public void Rpc_player_die(GameObject activator, Vector2 force, bool isHeadShot, int update_money, float update_experience, byte update_skill_points, bool teamLost)
    {
        client_die(activator, force, isHeadShot, update_money, update_experience, update_skill_points, teamLost);
    }

    /// <summary>
    /// This function tell client to play dead and cease control
    /// This function also update client-side money, skill etc for purchase menu
    /// </summary>
    /// <param name="activator"></param>
    /// <param name="force"></param>
    /// <param name="isHeadShot"></param>
    /// <param name="update_money"></param>
    /// <param name="update_experience"></param>

    void client_die(GameObject activator, Vector2 force, bool isHeadShot, int update_money, float update_experience, byte update_skill_points, bool teamLost)
    {
        
        isDead = true;

        //Aesthetics
        if (!isServer) { StartCoroutine(make_debris()); }//explode not allowed any more
        bodyRB.MoveRotation(Mathf.Atan2(-force.y, -force.x) * 180 / Mathf.PI);
        if (!isDedicatedServer())
        {
            //ragdoll force
            Vector3 ragdoll_pos = bodyRB.position;
            ragdoll_pos.z = CONSTANTS.BACKGROUND_OFFSETZ - 0.05f;
            ragdoll = Instantiate(ragdoll_prefab, ragdoll_pos, sprite.transform.rotation);
            ragdoll.transform.localScale = transform.localScale;
            ragdoll.GetComponent<Rigidbody2D>().velocity = body_velocity;
            ragdoll.GetComponent<Rigidbody2D>().AddForce(force);
            quality_setting(ragdoll);
            //Bleed death headshot fx
            if (isHeadShot && headshot_fx != null)
            {
                float angle = Mathf.Atan2(force.y, force.x) * 180 / Mathf.PI;
                Vector2 _pos = bodyRB.position + force.normalized * size * 2;
                Instantiate(headshot_fx, _pos, Quaternion.Euler(0, 0, angle));
            }
        }

        //Play dead animation
        if (isHuman())
        {
            anim_upper.Play("Dead", 0);
            if(anim_lower != null)
            {
                anim_lower.Play("Dead", 0);
            }
            
        }
        else if (isBot())
        {
            anim_upper.Play("Dead", 0);
            if (anim_lower != null)
            {
                anim_lower.Play("Dead", 0);
            }
        }
        else if (isZombie())
        {
            anim_upper.Play("Dead", 0);
            if (anim_lower != null)
            {
                anim_lower.Play("Dead", 0);
            }
        }
        

        //Tell the character to play dead if it is a player
        if (isPlayer)
        {
            money = update_money;
            experience = update_experience;
            skill_points = update_skill_points;
            GetComponent<Player_controller>().die();
            //Tell the player to drop items and disable control if it is local player
            if (isLocalPlayer)
            {
                
                GetComponent<Player_HUD>().die_screen();
                GetComponent<Player_HUD>().show_kill_by(activator, teamLost);

                //GetComponent<Player_controller>().freeze_movement = true;
                character_cond = Character_condition.DEAD;
                Player_inventory inventory = GetComponent<Player_inventory>();
                for (int i = 0; i < inventory.item.Count; i++)
                {
                    GetComponent<Player_controller>().Drop_item(inventory.item[i], force);
                }
            }
        }
    }
    /// <summary>
    /// Server-sided
    /// </summary>
    /// <param name="upgrade_idx"></param>
    public void upgrade_stat(byte[] upgrade_idx, int ran_points = -1)
    {
        
        //Get upgrade list
        

        if (ran_points != -1)//Randomize the upgrade based on amount of skill points given
        {
            upgrade_idx = new byte[ran_points];
            for (int i = 0; i < ran_points; i++)
            {
                upgrade_idx[i] = (byte)UnityEngine.Random.Range(0, upgrades.Length);
            }
        }
        //Upgrade; If you add more conditions, dont forget to stretch the random range above
        for (int i = 0; i < upgrade_idx.Length; i++)
        {
            
            switch (upgrades[upgrade_idx[i]])
            {
                /*
                case CONSTANTS.UPGRADE_TYPE.Health://Upgrade health
                    max_health += initial_health / 30;
                    health += initial_health / 30;
                    break;
                case CONSTANTS.UPGRADE_TYPE.Phys_resistance://Upgrade physical resilience
                    physical_resilience += initial_resiliance / 10;
                    tissue_dense += initial_tissue_dense / 10;
                    break;
                case CONSTANTS.UPGRADE_TYPE.Reload://Upgrade reload speed
                    reload_multiplier *= 0.99f;
                    break;
                case CONSTANTS.UPGRADE_TYPE.Strength:
                    strength += initial_strength * 0.1f;
                    break;
                case CONSTANTS.UPGRADE_TYPE.Stress:
                    stress_resistent += initial_stress * 0.1f;
                    break;
                case CONSTANTS.UPGRADE_TYPE.Aim:
                    aim_suppress *= 0.98f;
                    break;
                case CONSTANTS.UPGRADE_TYPE.Movement:
                    speed_run += initial_speed_run * 0.08f;
                    break;
                */
                case CONSTANTS.UPGRADE_TYPE.Health://Upgrade health
                    max_health = upgrade_value(upgrade_idx[i], max_health, initial_health);
                    health = upgrade_value(upgrade_idx[i], health, initial_health);
                    break;
                case CONSTANTS.UPGRADE_TYPE.Phys_resistance://Upgrade physical resilience
                    physical_resilience = upgrade_value(upgrade_idx[i], physical_resilience, initial_resiliance);
                    tissue_dense = upgrade_value(upgrade_idx[i], tissue_dense, initial_tissue_dense);
                    break;
                case CONSTANTS.UPGRADE_TYPE.Reload://Upgrade reload speed
                    reload_multiplier = upgrade_value(upgrade_idx[i], reload_multiplier, 1);
                    break;
                case CONSTANTS.UPGRADE_TYPE.Strength:
                    strength = upgrade_value(upgrade_idx[i], strength, initial_strength);
                    break;
                case CONSTANTS.UPGRADE_TYPE.Stress:
                    stress_resistent = upgrade_value(upgrade_idx[i], stress_resistent, initial_stress);
                    break;
                case CONSTANTS.UPGRADE_TYPE.Aim:
                    aim_suppress = upgrade_value(upgrade_idx[i], aim_suppress, 1);
                    break;
                case CONSTANTS.UPGRADE_TYPE.Movement:
                    speed_run = upgrade_value(upgrade_idx[i], speed_run, initial_speed_run);
                    break;
                case CONSTANTS.UPGRADE_TYPE.Inventory:
                    if (!isPlayer)
                    {
                        inventory_size = (ushort)upgrade_value(upgrade_idx[i], inventory_size, initial_inventory_size);
                        break;
                    }
                    Player_inventory inventory = GetComponent<Player_inventory>();
                    inventory_size = (ushort)upgrade_value(upgrade_idx[i], inventory_size, initial_inventory_size);
                    break;
            }
        }
        if (isZombie())
        {
            levels += upgrade_idx.Length;
            float scale = 1f + (float)levels / 50;
            if (isDedicatedServer())
            {
                resize(scale);
            }
            Rpc_resize(scale);
        }
    }
    [ClientRpc]
    public void Rpc_resize(float scale)
    {
        resize(scale);
    }
    void resize(float scale)
    {
        size = initial_size * scale;
        melee_box.x = size / 2;
        melee_box.y = size / 2;
        transform.localScale = initial_scale * scale;
         
    }

    /// <summary>
    /// This function return an modified parameter
    /// </summary>
    /// <param name="upgrade_index"></param>
    /// <param name="parameter"></param>
    /// <param name="base_parameter">If negative one, assume the first parameter to use</param>
    /// <param name="negate"></param>
    /// <returns></returns>
    public float upgrade_value(byte upgrade_index, float parameter, float base_parameter = -1, bool negate = false)
    {
        CONSTANTS.UPGRADE_TYPE upgrade_type = upgrades[upgrade_index];
        
        switch (upgrade_type)
        {
            case CONSTANTS.UPGRADE_TYPE.Health://Upgrade health
                
                
                if(base_parameter < 0)//Choosing initial health as agent
                {
                    base_parameter = initial_health;
                }
                if (negate)
                {
                    return parameter - base_parameter / 30;
                }
                return parameter + base_parameter / 30;
            case CONSTANTS.UPGRADE_TYPE.Phys_resistance://Upgrade physical resilience
                if (base_parameter < 0)//Choosing initial resiliance as agent
                {
                    base_parameter = initial_resiliance;
                }
                if (negate)
                {
                    return parameter - base_parameter / 10;
                }
                return parameter + base_parameter / 10;
            case CONSTANTS.UPGRADE_TYPE.Reload://Upgrade reload speed
                if (base_parameter < 0)//Choosing 1 as agent
                {
                    base_parameter = 1;
                }
                if (negate)
                {
                    return parameter / 0.97f;
                }
                return parameter * 0.97f;
            case CONSTANTS.UPGRADE_TYPE.Strength:
                float ratio = 0.1f;
                if (isBot())
                {
                    ratio = 0.2f;
                }
                else if (isHuman())
                {
                    ratio = 0.15f;
                }
                if (base_parameter < 0)//Choosing initial strength as agent
                {
                    base_parameter = initial_strength;
                }
                if (negate)
                {
                    return parameter - base_parameter * ratio;
                }
                return parameter + base_parameter * ratio;
            case CONSTANTS.UPGRADE_TYPE.Stress:
                if (base_parameter < 0)//Choosing initial stress as agent
                {
                    base_parameter = initial_stress;
                }
                if (negate)
                {
                    return parameter - base_parameter * 0.3f;
                }
                return parameter + base_parameter * 0.3f;
            case CONSTANTS.UPGRADE_TYPE.Aim:
                if (base_parameter < 0)//Choosing 1 as agent
                {
                    base_parameter = 1;
                }
                if (negate)
                {
                    return parameter / 0.97f;
                }
                return parameter * 0.97f;
            case CONSTANTS.UPGRADE_TYPE.Movement:
                if (base_parameter < 0)//Choosing initial speed as agent
                {
                    base_parameter = initial_speed_run;
                }
                if (negate)
                {
                    return parameter - base_parameter * 0.02f;
                }
                return parameter + base_parameter * 0.02f;
            case CONSTANTS.UPGRADE_TYPE.Inventory:
                if (base_parameter < 0)//Choosing initial inventory as agent
                {
                    base_parameter = initial_inventory_size;
                }
                if (negate)
                {
                    return parameter - base_parameter * 0.02f;
                }
                return parameter + base_parameter * 0.02f;
        }
        return parameter;
    }



    GameObject get_weapon(GameObject subject)
    {
        if (subject.GetComponent<Body_generic>().isPlayer)
        {
            return subject.GetComponent<Player_controller>().equiped_item;
        }
        
        return subject.GetComponent<AI_generic>().equiped_item;
    }
    /// <summary>
    /// This function intends to sync animation
    /// </summary>
    /// <param name="index"></param>
    public void set_animation_state(sbyte index)
    {
        /*
        if(character_cond == Character_condition.SCRIPTED)
        {
            //Debug.Log("index: "+index);
            animation_script = index;
            if (isDedicatedServer())
            {
                Hook_anim_scripted(index);
            }
            return;
        }
        */
        animation_state = index;
        if (isDedicatedServer())
        {
            Hook_anim_state(index);
        }
    }
    public void set_script_state(sbyte index)
    {
        animation_script = index;
        if (isDedicatedServer())
        {
            Hook_anim_scripted(index);
        }
    }

    //Only local player OR NPC
    [Command]
    public void Cmd_melee_start()
    {
        set_animation_state(-6);
    }
    public void anim_melee_start()
    {
        anim_upper.SetInteger("melee", 0);
        StartCoroutine(anim_melee_bitOff());
    }
    public void melee()
    {
        if(!isMeleeing && Time.time > time_to_melee)
        {
            melee_start();
        }
    }
    public void melee_start()
    {
        if(isLocalPlayer)//Local player play melee animation locally
        {
            anim_melee_start();
        }

        if (isServer)
        {
            set_animation_state(-6);
        }
        else
        {
            Cmd_melee_start();
        }
        isMeleeing = true;
        time_to_melee = Time.time + melee_cooldown;
        melee_previous_pos = weapon_bone.transform.position;
        meleeList.Clear();
        
        StartCoroutine(melee_end());
    }
    
    public void melee_progress()
    {
        if (!isMeleeing || melee_detector.activeSelf == false)
        {
            return;
        }
        Vector2 trace_vec = (Vector2)melee_detector.transform.position - melee_previous_pos;
        RaycastHit2D[] melee_hits = Physics2D.BoxCastAll(melee_previous_pos, melee_box, 0, trace_vec, trace_vec.magnitude, melee_filter);//Physics2D.LinecastAll(melee_previous_pos, weapon_bone.transform.position, melee_filter);
        //Debug.DrawLine(transform.position, we);
        if (melee_hits != null && melee_hits.Length > 0)
        {
            for(int i = 0; i < melee_hits.Length; i++)
            {
                if (!dmg_tags.Contains(melee_hits[i].collider.tag))
                {
                    continue;
                }
                Body_hitbox_generic body_victim_hitbox = melee_hits[i].collider.GetComponent<Body_hitbox_generic>();
                
                if (body_victim_hitbox == null)//Damage structure
                {
                    if (melee_hits[i].collider.gameObject.tag != tag && !meleeList.Contains(melee_hits[i].collider.gameObject))
                    {
                        meleeList.Add(melee_hits[i].collider.gameObject);
                        //damage
                        float damage = strength;
                        float angle = Mathf.Atan2(trace_vec.y, trace_vec.x);
                        if (isServer)
                        {
                            melee_hits[i].collider.GetComponent<Structure_generic>().health -= strength;
                        }
                        else
                        {
                            player_controller.add_to_shot_list(melee_hits[i].collider.gameObject, damage, melee_hits[i].point, 0, CONSTANTS.seed_float_to_short(angle, 360), false, 0);
                        }
                    }
                    continue;
                }
                Body_generic body_victim = body_victim_hitbox.body;
                if (!meleeList.Contains(body_victim.gameObject))
                {
                    meleeList.Add(body_victim.gameObject);
                    //damage
                    float damage = strength;
                    float angle = Mathf.Atan2(trace_vec.y, trace_vec.x);

                    if (isServer)//Server player & NPC
                    {
                        body_victim.damage(gameObject, Vector2.zero, strength);
                        body_victim.request_bleed(melee_hits[i].point, angle, false);
                    }
                    else
                    {
                        player_controller.add_to_shot_list(body_victim.gameObject, damage, melee_hits[i].point, 0, CONSTANTS.seed_float_to_short(angle,360),false,0);
                    }
                }
            }
        }
        melee_previous_pos = weapon_bone.transform.position;
    }
    public IEnumerator melee_end()
    {
        yield return new WaitForSeconds(melee_period);
        isMeleeing = false;
    }
    public void build_base()
    {
        if (money > base_structure_price && !has_built_base)
        {
            //Examine available zone
            Collider2D wall = Physics2D.OverlapArea(bodyRB.position - 2 * Vector2.one, bodyRB.position + 2 * Vector2.one, base_structure_unavailable_fltr);
            if (wall == null)
            {
                has_built_base = true;
                GameObject base_struct = Instantiate(base_structure, bodyRB.position, Quaternion.identity);
                NetworkServer.Spawn(base_struct);
                Sound_watcher.Singleton.tune_in_structure(base_struct.GetComponent<Structure_generic>());
                base_struct.GetComponent<Structure_generic>().team = team;
                team.base_number++;
                money -= base_structure_price;
            }
        }
    }
    [ClientRpc]
    public void Rpc_explode(byte mode)
    {
        Destroy(ragdoll);
        turn_debris();
        ragdoll = null;
        if ((blood_mist_fx == null && mode == 0)||(mode == 1 && ash_fx == null))
        {
            return;
        }
        Vector3 pos = bodyRB.position;
        pos.z = CONSTANTS.FX_Z;
        //Effects
        
        if(mode == 0)
        {
            Instantiate(blood_mist_fx, pos, Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 360)));
            //Decal
            if (explode_splash_decal != null)
            {
                Decal_manager.Singleton.add_decal(explode_splash_decal, bodyRB.position, explode_splash_decal_size, 10);
            }
        }
        else if(mode == 1 && ash_fx != null)
        {
            ParticleSystem ash_particle = Instantiate(ash_fx, bodyRB.position, Quaternion.identity).GetComponent<ParticleSystem>();
            ParticleSystem.ShapeModule shape = ash_particle.shape;
            ParticleSystem.Burst burst = ash_particle.emission.GetBurst(0);
            burst.count = (int)(size * CONSTANTS.ASH_UNIT_COUNT);
            shape.radius = size;
            ash_particle.emission.SetBurst(0, burst);
            //Debug.LogError("burst: "+emission.burstCount);
            /*
            Debug.LogError("ash!");
            MeshFilter ash_flter = Instantiate(ash_fx, pos, Quaternion.identity).GetComponent<MeshFilter>();
            List<Vector2> vertices_list = new List<Vector2>();
            List<Vector3> _vertices_list = new List<Vector3>();
            List<ushort> triangles_list = new List<ushort>();
            List<int> _triangles_list = new List<int>();
            SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i].gameObject.name == "shadow" || sprites[i].gameObject.name == "color" || sprites[i].sprite == null)
                {
                    continue;
                }
                Debug.LogError("part name: "+ sprites[i] + "; number vertices: "+ sprites[i].sprite.vertices.Length);
                vertices_list.AddRange(sprites[i].sprite.vertices);
                triangles_list.AddRange(sprites[i].sprite.triangles);
                
            }
            for (int i = 0; i < vertices_list.Count; i++)
            {
                
                _vertices_list.Add(vertices_list[i]);

            }
            for (int i = 0; i < triangles_list.Count; i++)
            {

                _triangles_list.Add(triangles_list[i]);

            }
            ash_flter.mesh = new Mesh();
            ash_flter.mesh.Clear();
            ash_flter.mesh.vertices = _vertices_list.ToArray();//mesh.vertices = vertices_mesh;
            ash_flter.mesh.triangles = _triangles_list.ToArray();//mesh.triangles = triangles;
            ash_flter.mesh.RecalculateNormals();
            */
        }

    }
    /// <summary>
    /// Cant be gibbed
    /// </summary>
    /// <returns></returns>
    public IEnumerator make_debris()
    {
        yield return new WaitForSeconds(CONSTANTS.DEAD_DAMAGE_PERIOD);
        turn_debris();
    }
    void turn_debris()
    {
        hitbox_main.gameObject.layer = LayerMask.NameToLayer("Debris");
        headshot_box.gameObject.layer = LayerMask.NameToLayer("Debris");
        canExplode = false;
    }
    public IEnumerator Enable_respawn()
    {
        yield return new WaitForSeconds(respawn_time);
        if (team.requestRespawn())//Can respawn
        {

            if (isPlayer)//Let player decide when to spawn
            {

                Rpc_enable_respawn();
            }
            else//Npc spawn immediately
            {
                Server_respawn();
            }
        }
        else
        {
            //Tell client no more respawn
        }
    }
    [ClientRpc]
    public void Rpc_enable_respawn()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        //Enable respawn button
        GetComponent<Player_HUD>().menu.respawn_button.interactable = true;
    }
    public void request_respawn()
    {
        GetComponent<Player_HUD>().menu.respawn_button.interactable = false;
        Cmd_request_respawn();
    }
    [Command]
    public void Cmd_request_respawn()
    {
        if (!team.requestRespawn())
        {
            //Tell client no more respawn
            return;
        }

        Server_respawn();
    }
    public void Server_respawn()
    {
        team.alive++;

        //Server-side operations
        isDead = false;
        respawnProtected = true;
        has_built_base = false;
        isIgnited = false;
        health = max_health;
        current_temperature = body_temperature;
        canExplode = true;
        hitbox_main.gameObject.layer = start_layer_idx;
        headshot_box.gameObject.layer = LayerMask.NameToLayer("Headsquirt");

        if (!isPlayer)
        {
            OnRespawn.Invoke();
        }

        Rpc_teleport_to_spawn(teleport_to_spawn());
        if (!isPlayer)
        {
            GetComponent<AI_generic>().memory_clean();
            GetComponent<AI_generic>().lookback_scheduled = false;
        }

        //Client-side operations
        if (isDedicatedServer())
        {
            respawn();
        }
        Rpc_respawn();

        StartCoroutine(protectionOff());
    }
    IEnumerator protectionOff()
    {
        yield return new WaitForSeconds(CONSTANTS.SPAWN_PROTECTION_LENGTH);
        respawnProtected = false;
    }

    [ClientRpc]
    public void Rpc_teleport_to_spawn(Vector2 to_pos)
    {
        if (isServer)
        {
            return;
        }
        if (bodyRB == null)
        {
            bodyRB = GetComponent<Rigidbody2D>();
        }
        bodyRB.MovePosition(to_pos);
        //bodyRB.position = to_pos;

        transform.position = to_pos;
    }

    [ClientRpc]
    public void Rpc_respawn()
    {
        respawn();
    }
    void respawn()
    {
        //Switches
        isDead = false;
        has_built_base = false;
        hitbox_main.gameObject.layer = start_layer_idx;
        headshot_box.gameObject.layer = LayerMask.NameToLayer("Headsquirt");
        unignite();
        
        ragdoll = null;
        //transform.position = new Vector3(transform.position.x, transform.position.y, CONSTANTS.CHARACTER_ALIVE_Z);

        //Resume animation
        anim_upper.Play("Movement");
        if(anim_lower != null)
        {
            anim_lower.Play("Movement");
        }
        
        anim_reload(false);

        //Players
        if (isPlayer)
        {
            if (isLocalPlayer)//Local player: teleport to spawn & reset screen
            {
                GetComponent<Player_HUD>().reset_screen();
                GetComponent<Player_HUD>().hide_kill_by();
            }
            GetComponent<Player_controller>().maglite.enabled = false;
            //GetComponent<Player_controller>().freeze_movement = false;
            character_cond = Character_condition.FREE;
            GetComponent<Player_inventory>().weight = 0;
            GetComponent<Player_inventory>().capacity = 0;
        }
    }
    public Vector2 teleport_to_spawn()
    {
        Vector2 v = Vector2.zero;
        BoxCollider2D area = null;
        
        //Respawn point
        if (isBot())
        {
            area = cvar_watcher.comp_spawnPoint_manager.getSpawnAreaRobot();
        }
        else if (isHuman())
        {
            area = cvar_watcher.comp_spawnPoint_manager.getSpawnAreaHuman();
        }
        else if (isZombie())
        {
            area = cvar_watcher.comp_spawnPoint_manager.getSpawnAreaZombie();
        }
        if(area != null)
        {
            v.x = UnityEngine.Random.Range((area.transform.position.x - area.size.x / 2), (area.transform.position.x + area.size.x / 2));
            v.y = UnityEngine.Random.Range((area.transform.position.y - area.size.y / 2), (area.transform.position.y + area.size.y / 2));
        }
        //changing rigidbody could cause transform position to resume rb position back to origin
        bodyRB.MovePosition(v);
        transform.position = v;
        return v;
    }

    public void request_bleed(Vector2 hit_point, float angle, bool headshot)
    {
        Fx_watcher.Singleton.request_bleed(gameObject, hit_point, angle, headshot);
    }
    public void request_tesla(GameObject[] from, GameObject[] to, byte[] hops)
    {

    }

    public void bleed(Vector3 pos, float angle, bool isHeadShot)
    {
        pos.z = CONSTANTS.FX_Z;
        if (isDedicatedServer())
        {
            return;
        }
        if (bleed_fx != null && bleed_fx.Length > 0)
        {
            int bleed_index = UnityEngine.Random.Range(0, bleed_fx.Length);
            Instantiate(bleed_fx[bleed_index], pos, Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 360)));
        }
        
        if (isHeadShot && bleed_fx_hs != null)
        {
            Vector2 angle_vec = new Vector2(Mathf.Cos(angle * Mathf.PI / 180), Mathf.Sin(angle * Mathf.PI / 180));
            Vector3 _pos = pos + (Vector3)angle_vec.normalized * size * 2;
            Instantiate(bleed_fx_hs, _pos, Quaternion.Euler(0, 0, angle));
        }
    }
    [ClientRpc(channel = 1)]
    public void Rpc_bleed(Vector2 pos, float angle, bool isHeadShot)
    {
        bleed(pos, angle, isHeadShot);
    }
    [ClientRpc(channel = 3)]
    public void Rpc_bleed_n_force(Vector2 pos, Vector2 force, bool isHeadShot)
    {
        bleed(pos, Mathf.Atan2(force.y, force.x) * 180 / Mathf.PI, isHeadShot);
        if (isLocalPlayer)
        {
            GetComponent<Rigidbody2D>().AddForce(force);
        }
    }
    [ClientRpc(channel = 3)]
    public void Rpc_add_force(Vector2 force)
    {
        GetComponent<Rigidbody2D>().AddForce(force);
    }

    [ClientRpc(channel = 1)]
    public void Rpc_dead_headshot(Quaternion rot)
    {
        Vector2 angle_vec = new Vector2(Mathf.Cos(rot.eulerAngles.z * Mathf.PI / 180), Mathf.Sin(rot.eulerAngles.z * Mathf.PI / 180));
        Vector3 _pos = bodyRB.position + angle_vec.normalized * size * 2;
        _pos.z = CONSTANTS.FX_Z;
        Instantiate(headshot_fx, _pos, rot);
    }
    
    void Hook_ignite(bool ignited)
    {
        isIgnited = ignited;
        if (!isClient)
        {
            return;
        }
        if (ignited)
        {
            ignite();
        }
        else
        {
            unignite();
        }
    }
    void ignite()
    {
        //isIgnited = true;
        //behavior impact
        /*
        if (isPlayer)
        {

        }
        else
        {
            GetComponent<AI_generic>().suppressed = true;
        }
        */

        //flame fx
        if(flame_entity != null)
        {
            return;
        }
        flame_entity = Pool_watcher.Singleton.request_flame();
        if(flame_entity == null)
        {
            flame_entity = Instantiate(flame_prefab).GetComponent<ParticleSystem>();
            
        }
        if (!fluid_watcher.particleSystems.Contains(flame_entity))
        {
            fluid_watcher.particleSystems.Add(flame_entity);
        }
        flame_entity.transform.parent = transform;
        flame_entity.transform.position = transform.position;
        /*
        if (GetComponent<ParticleSystem>() != null)
        {
            GetComponent<ParticleSystem>().Play();
            if (!fluid_watcher.particleSystems.Contains(GetComponent<ParticleSystem>()))
            {
                if (fluid_watcher.enabled == false)
                {
                    fluid_watcher.enabled = true;
                }
                fluid_watcher.particleSystems.Add(GetComponent<ParticleSystem>());
            }
        }
        */

    }
    void unignite()
    {
        //isIgnited = false;
        /*
        //disable behavior impact
        if (isPlayer)
        {

        }
        else
        {
            GetComponent<AI_generic>().suppressed = false;
        }
        */
        
        //End flame fx

        if(flame_entity != null)
        {
            flame_entity.transform.parent = null;
            Pool_watcher.Singleton.recycle_flame(flame_entity);
            flame_entity = null;
        }
        
        /*
        if (GetComponent<ParticleSystem>() != null)
        {
            GetComponent<ParticleSystem>().Stop();
        }
        */
    }



    public void script_mod_speed(float new_speed)
    {
        prescript_speed_run = speed_run;
        speed_run = new_speed;
    }
    public void script_resume_speed()
    {
        speed_run = prescript_speed_run;
    }

    //Outputs
    [ServerCallback]
    void OnDeath()
    {
        CONSTANTS.invokeOutput(CONSTANTS.OUTPUT_NAME.OnDeath, I_O);
    }

    public void OnHitCharacter(IDamageVictim victim, float damage, Vector2 hitPoint, Vector2 force, bool isHeadShot, DamageType damageType)
    {
        if (isLocalPlayer)
        {
            player_controller.hit_mark();
        }

        //If non-host local hit
        if (isPlayer && !isServer)
        {
            short angleShort = CONSTANTS.seed_float_to_short(Mathf.Atan2(force.y, force.x) * 180 / Mathf.PI, 360);
            GetComponent<Player_controller>().add_to_shot_list(victim.getGameObject(), damage, hitPoint, force.magnitude, angleShort, isHeadShot, (byte)damageType);
        }
    }

    public void OnDamagedBy(IDamageActivator activator, float damageAmt, Vector2 hitPoint, Vector2 force, bool isHeadShot, DamageType damageType)
    {
        bodyRB.AddForceAtPosition(force, hitPoint);
        //If non-host local hit
        if (activator != null && activator.isPlayer() && !activator.isServer())
        {

        }
        else//Server client || npc authoritates bullet
        {
            //Cause damage and bleed and force
            if (damageAmt > 0)
            {
                switch (damageType)
                {
                    case DamageType.Physical:
                        damage(activator.getGameObject(), force: force, dmg_physics: damageAmt, headshot: isHeadShot);
                        break;
                    case DamageType.Thermal:
                        damage(activator.getGameObject(), force: force, dmg_thermal: damageAmt, headshot: isHeadShot);
                        break;
                    case DamageType.Energy:
                        damage(activator.getGameObject(), force: force, dmg_electric: damageAmt, headshot: isHeadShot);
                        break;
                }
                
                float angle = Mathf.Atan2(force.y, force.x) * 180 / Mathf.PI;
                request_bleed(hitPoint, angle, isHeadShot);
                if (isPlayer && !hasAuthority)//Pushing non-server client
                {
                    Rpc_add_force(force);
                }
            }
            //Friendly fire, just force
            else
            {
                if (isPlayer && !hasAuthority)//Non-server client
                {
                    Rpc_add_force(force);
                }
            }

        }
    }

    bool IDamageActivator.isPlayer()
    {
        return isPlayer;
    }

    bool IDamageActivator.isServer()
    {
        return isServer;
    }

    bool IDamageActivator.canDamage(IDamageVictim victim)
    {
        return GetComponent<Body_generic>().dmg_tags.Contains(victim.getGameObject().tag);
    }

    public GameObject getGameObject()
    {
        return gameObject;
    }
}
