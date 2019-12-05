using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
public class Team_watcher : NetworkBehaviour{
    public Body_generic.Character_type race;
    public GameObject[] character;
    //[HideInInspector] public bool team_SpawnReady = false;

    /// <summary>
    /// How many survives, meaning when respawns are out and how many still on the field; only reduce this when dead in case of no more respawn
    /// If this reaches zero, meaning that the team loses
    /// </summary>
    public short alive = 10;
    /// <summary>
    /// How many respawns allowed; This number decides if the team can still respawn character
    /// </summary>
    [SyncVar(hook = "Hook_update_counter")] public short respawns_token = 500;
    public int base_number = 0;
    Server_watcher cvar_watcher;
    public List<GameObject> enemy_base = new List<GameObject>();
    [HideInInspector] public Text text_counter;
    //[SyncVar] int body_to_getReady = -1;
    //[HideInInspector] public int body_ready = 0;

    void Awake()
    {
        //Register to cvar watcher on awake, onstart has a low chance to glitch the gun
        cvar_watcher = FindObjectOfType<Server_watcher>();
    }
    // Use this for initialization
    void Start () {
        cvar_watcher = FindObjectOfType<Server_watcher>();
        if (race == Body_generic.Character_type.Human)
        {
            cvar_watcher.team_human = this;
        }
        else if (race == Body_generic.Character_type.Robot)
        {
            cvar_watcher.team_robot = this;
        }
        else if (race == Body_generic.Character_type.Zombie)
        {
            cvar_watcher.team_zombie = this;
        }
        //set text
        if(text_counter != null)
        {
            text_counter.text = respawns_token.ToString();
        }
        

        cvar_watcher.onClientReady.Add(OnClientReady);
        /*
        //Find the player entity and register them to corresponding teams; If this fail to find players, players are going to find the team
        //Let player/observer know of this entity, and making them check if this team members are spawn-ready
        Player_controller[] players = FindObjectsOfType<Player_controller>();
        Debug.LogError("player number: "+players.Length);
        for (int i = 0; i < players.Length; i++)
        {
            //Debug.LogError("found player: " + players[i].gameObject.name + " and his type: " + players[i].GetComponent<Body_generic>().character_type);
            if (players[i].GetComponent<Body_generic>().character_type == race)
            {
                players[i].GetComponent<Body_generic>().team = this;
                //Debug.LogError("player ready: "+ players[i].gameObject.name);
                //body_ready++;
            }
        }
        */

        //Server operations, spawn npc and assign team
        if (isServer)
        {
            alive = cvar_watcher.get_joined_character(race);
            if(alive <= 0)
            {

                enabled = false;
            }
            respawns_token = cvar_watcher.get_init_tickets(race);

            //Spawn npcs
            int number_to_spawn = alive - cvar_watcher.get_joined_player(race);
            //body_to_getReady = alive;
            //Rpc_tell_client_number(alive);

            for (int i = 0; i < number_to_spawn; i++)
            {
                int ran = UnityEngine.Random.Range(0, character.Length);
                GameObject npc = Instantiate(character[ran], transform.position, transform.rotation);
                //Debug.LogError("spawn: "+character[ran]);
                NetworkServer.Spawn(npc);
                Body_generic body_npc = npc.GetComponent<Body_generic>();
                body_npc.cvar_watcher = cvar_watcher;//Needs to initialize on server side
                npc.GetComponent<Body_generic>().bodyRB = npc.GetComponent<Rigidbody2D>();
                body_npc.OnRespawn = npc.GetComponent<AI_generic>().shopping;
                npc.GetComponent<AI_generic>().set_ai_condition(AI_generic.AI_condition.AGGRESSIVE);
                body_npc.team = this;


                //Make sure the upgrades are ready before shopping;
                if (body_npc.isHuman())
                {
                    body_npc.upgrades = cvar_watcher.upgrades_human;
                }
                else if (body_npc.isBot())
                {
                    body_npc.upgrades = cvar_watcher.upgrades_robot;
                }
                else if (body_npc.isZombie())
                {
                    body_npc.upgrades = cvar_watcher.upgrades_zombie;
                }
                npc.GetComponent<AI_generic>().body = body_npc;
                npc.GetComponent<AI_generic>().shopping();
                //npc.GetComponent<AI_generic>().freeze_movement = true;//waiting for the preround to end
                npc.GetComponent<Body_generic>().character_cond = Body_generic.Character_condition.FROZEN;
            }
        }
        
        
        
        /*
        if (isServer)
        {
            int percent = 100 / weapon.Length;
            for (int i = 0; i < spawners.Length; i++)
            {
                spawners[i].spawn_limit = team_limit;
                for(int j = 0; j < weapon.Length; j++)
                {
                    spawners[i].equip_template.Add(new KeyValuePair<GameObject, int>(weapon[j], percent));
                }
                spawners[i].npc_template.Add(new KeyValuePair<GameObject, int>(character, 100));
            }
        }
        */
    }
    public void OnClientReady()
    {
        //Find the player entity and register them to corresponding teams; If this fail to find players, players are going to find the team
        //Let player/observer know of this entity, and making them check if this team members are spawn-ready
        Player_controller[] players = FindObjectsOfType<Player_controller>();

        for (int i = 0; i < players.Length; i++)
        {
            //Debug.LogError("found player: " + players[i].gameObject.name + " and his type: " + players[i].GetComponent<Body_generic>().character_type);
            if (players[i].GetComponent<Body_generic>().character_type == race)
            {
                players[i].GetComponent<Body_generic>().team = this;
                //Debug.LogError("player ready: "+ players[i].gameObject.name);
                //body_ready++;
            }
        }
        Structure_generic[] structures = FindObjectsOfType<Structure_generic>();
        for (int i = 0; i < structures.Length; i++)
        {
            if (structures[i].gameObject.layer == gameObject.layer)
            {
                if(enabled == false)
                {
                    NetworkServer.Destroy(structures[i].gameObject);
                }
                else
                {
                    structures[i].team = this;
                    Sound_watcher.Singleton.tune_in_structure(structures[i]);
                }
            }
        }
        if(race == Body_generic.Character_type.Human)
        {
            base_number = cvar_watcher.comp_spawnPoint_manager.getSpawnPointCountHuman();
        }
        else if (race == Body_generic.Character_type.Robot)
        {
            base_number = cvar_watcher.comp_spawnPoint_manager.getSpawnPointCountRobot();
        }
        else if (race == Body_generic.Character_type.Zombie)
        {
            base_number = cvar_watcher.comp_spawnPoint_manager.getSpawnPointCountZombie();
        }
        Body_generic[] all = FindObjectsOfType<Body_generic>();
        for(int i = 0; i < all.Length; i++)
        {
            all[i].teleport_to_spawn();
        }
        
    }
    void Update()
    {
        if(isServer && (alive == 0 && respawns_token == 0)||(base_number == 0 && alive == 0))
        {
            enabled = false;
            cvar_watcher.sv_map_check_winner();
        }
    }
    /*
    void Update()
    {
        
        if (isClient)
        {
            //Debug.LogError("team: "+this+"; body_ready: "+body_ready+"; body_to_getready: "+body_to_getReady);
            if (body_ready == body_to_getReady)
            {
                team_SpawnReady = true;
            }
            
        }
        
    }
    */
    public bool requestRespawn()
    {
        if (respawns_token == 0 || base_number == 0)//Respawn refused
        {
            return false;
            /*
            alive--;
            //Debug.LogError("alive: "+alive+"; base: "+base_number);
            if (alive <= 0 && base_number == 0)//This team loses all remaining members and have no respawn, fails
            {
                enabled = false;
                cvar_watcher.game_check();
            }
            return false;
            */
        }
        respawns_token--;
        return true;
    }
    public void lose_base(Structure_generic structure)
    {
        base_number--;

    }
    public GameObject request_enemy_base()
    {
        for (int i = 0; i < enemy_base.Count; i++)
        {
            if (enemy_base[i] == null)
            {
                enemy_base.RemoveAt(i);
                i--;
            }
        }
        if (enemy_base.Count > 0)
        {
            return enemy_base[0];
        }
        return null;
    }
    public void spot_enemy_base(GameObject a_base)
    {
        
        if (enemy_base.Contains(a_base))
        {
            return;
        }
        enemy_base.Add(a_base);
    }
    public void Hook_update_counter(short value)
    {
        respawns_token = value;
        if(text_counter != null)
        {
            text_counter.text = value.ToString();
        }
        
        
    }
}
