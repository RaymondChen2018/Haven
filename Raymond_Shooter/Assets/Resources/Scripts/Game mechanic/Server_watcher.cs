using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System;

/*
    In this networked game, a server_watcher is a singleton gameobject("object", in Unity's terms) that performs 
    a variety of network relays to synchronize functionalities between the server and its clients.

    Instead of implementing these functionalities as some dedicated gameobjects themselves, I collected many of 
    them and shove them into this network view in order to minimize the number of networked objects in the game.

    At the stage of any match instance, there is a server_watcher and a client_watcher.They both exist on the
    server and on the client and server as network views. Difference is: The client_watcher only lives as long
    as a match last.It is created along with other gameobjects when a match scene is loaded and destroyed
    like others when the match ends; Server_watcher, on the other hand, lives since when server/client enters 
    a lobby and over-sees the match along the way. 

    Having a larger "scope", server_watcher allows for supplying data to gameobjects that require server parameters 
    upon match initialization, especially on over-seeing the initial loading process where everyone already has
    a synchronized server_watcher but not else. Client-watcher could not guarantee that since instances of it
    at different end can be created at different time frame and cause disagreement.
    before other gameobjects.
*/
public class Server_watcher : NetworkBehaviour {
    //Singleton
    static public Server_watcher Singleton;
    public SpawnPoint_manager comp_spawnPoint_manager;
    Message_board_watcher message_watcher;

    //Match Status
    public enum MATCH_STATE
    {
        /// <summary>
        /// When in lobby
        /// </summary>
        LOBBY,
        /// <summary>
        /// When lobby is counting down to start game
        /// </summary>
        LOBBY_COUNTDOWN,
        /// <summary>
        /// When local instance is initializing
        /// </summary>
        GAME_INITIALIZING,
        /// <summary>
        /// When local instance (server) is waiting for clients
        /// </summary>
        GAME_WAIT,
        /// <summary>
        /// When the game is in-session
        /// </summary>
        GAME_INSESSION,
        /// <summary>
        /// When the match is concluded but yet transition back to lobby
        /// </summary>
        GAME_PREVAIL
    }
    [SyncVar] public MATCH_STATE syn_match_state = MATCH_STATE.LOBBY;

    //State flags
    /// <summary>
    /// Between clients ready and scene change. Client-accurate.
    /// </summary>
    public bool cl_preroundStarted = false;

    //Match Parameters
    [SyncVar(hook = "Hook_HumanRespawns")] public short tickets_human = 500;
    [SyncVar(hook = "Hook_RobotRespawns")] public short tickets_robot = 200;
    [SyncVar(hook = "Hook_ZombieRespawns")] public short tickets_zombie = 0;
    [SyncVar(hook = "Hook_HumanLimit")] public short team_num_human = 10;
    [SyncVar(hook = "Hook_RobotLimit")] public short team_num_robot = 3;
    [SyncVar(hook = "Hook_ZombieLimit")] public short team_num_zombie = 20;
    [SyncVar(hook = "Hook_FriendlyFire")] public bool allyBulletPassThru = false;
    [SyncVar(hook = "Hook_LOSEnable")] public bool losVision = true;
    [SyncVar(hook = "Hook_MaxSkillDeath")] public byte maxSkillSpentPerDeath = 3;
    [SyncVar(hook = "Hook_RespawnTime")] public int respawn_time = 10;
    [SyncVar(hook = "Hook_TeamTransparent")] public bool team_transparent = false;
    [SyncVar(hook = "Hook_StartMoney")] public int insurance_money = 150;
    [SyncVar(hook = "Hook_Map")] public int map = 0;
    [SyncVar] public CONSTANTS.MAP_TYPE map_type = CONSTANTS.MAP_TYPE.PVP;
    [HideInInspector] public short team_num_human_player = 0;
    [HideInInspector] public short team_num_robot_player = 0;
    [HideInInspector] public short team_num_zombie_player = 0;
    public GameObject[] purchases_human;
    public GameObject[] purchases_robot;
    public GameObject[] purchases_zombie;
    public CONSTANTS.UPGRADE_TYPE[] upgrades_human;
    public CONSTANTS.UPGRADE_TYPE[] upgrades_robot;
    public CONSTANTS.UPGRADE_TYPE[] upgrades_zombie;
    [HideInInspector] public Team_watcher team_human;
    [HideInInspector] public Team_watcher team_robot;
    [HideInInspector] public Team_watcher team_zombie;
    [HideInInspector] public List<GameObject> players = new List<GameObject>();



    /// <summary>
    /// Server record
    /// </summary>
    List<GameObject> entity_pool = new List<GameObject>();

    Player_controller[] all_players;
    AI_generic[] all_ais;
    Body_generic[] all_bodies;
    [HideInInspector] public Body_generic.Character_type local_character_type = Body_generic.Character_type.Observer;
    public Player_generic local_player = null;
    public List<Action> onClientReady;
    

    //Map Spawn Time
    [SyncVar] public float serverMapSpawnTime = 0;
    public float localMapSpawnTime = 0;

    float time_to_readfile = 0;

    public Prototype.NetworkLobby.LobbyManager lobbyManager;

    /// <summary>
    /// Is this a dedicated server instance?
    /// </summary>
    /// <returns></returns>
    public bool isDedicated()
    {
        return isServer && !isClient;
    }

    /*Unity functions*/
    //Initialization
    void Start()
    {
        Singleton = this;
        message_watcher = GetComponent<Message_board_watcher>();
        lobbyManager = FindObjectOfType<Prototype.NetworkLobby.LobbyManager>();
        onClientReady = new List<Action>();
        lobbyManager.cvar_watcher = this;
        DontDestroyOnLoad(gameObject);
    }
    //Frame update
    void Update()
    {
        if (isServer)
        {
            if (atState(MATCH_STATE.GAME_INITIALIZING, MATCH_STATE.GAME_PREVAIL))
            {
                map_update();
            }
            else
            {
                lobby_update();
            }
        }
    }
    void OnLevelWasLoaded(int level)
    {
        //Debug.LogError("cp0");
        if (level == 0)
        {
            reset_var();
            lobbyManager.reset_var();
        }
    }

    /// <summary>
    /// Determines if the game is in certain stage.
    /// Example: Match initialization period: begin = game_initializing, end = game_waiting
    /// </summary>
    /// <param name="begin">When this starts</param>
    /// <param name="end">When this ends (inclusive)</param>
    /// <returns></returns>
    public bool atState(MATCH_STATE begin, MATCH_STATE end)
    {
        return syn_match_state >= begin && syn_match_state <= end;
    }


    /*Map scene*/
    //Preround
    /// <summary>
    /// When the server scene is loaded and all clients are ready
    /// </summary>
    void sv_preround_start()
    {
        all_players = FindObjectsOfType<Player_controller>();
        all_ais = FindObjectsOfType<AI_generic>();
        all_bodies = FindObjectsOfType<Body_generic>();

        //Skin initialization
        for (int i = 0; i < all_bodies.Length; i++)
        {
            if (all_bodies[i].isPlayer)
            {
                all_bodies[i].Rpc_send_skin_color((sbyte)Array.IndexOf(CONSTANTS.PLAYERCOLORS, all_bodies[i].skin_color));
            }
            Npc_skin_generic[] skins = all_bodies[i].GetComponentsInChildren<Npc_skin_generic>();
            if (skins == null || skins.Length == 0)
            {
                continue;
            }


            //Randomize each skin part, and form a skin map to send to clients
            byte[] skin_map = new byte[skins.Length];
            for (int j = 0; j < skins.Length; j++)
            {
                byte skin_index = (byte)UnityEngine.Random.Range(0, skins[j].sprites_pool.Count);
                skin_map[skins[j].skin_id] = skin_index;
            }

            all_bodies[i].Rpc_send_skin_info(skin_map);
        }

        serverMapSpawnTime = Time.realtimeSinceStartup;
        syn_match_state = MATCH_STATE.GAME_INSESSION;
        map_match_start();
        Rpc_match_start();

        if (isDedicated())
        {
            Camera.main.enabled = false;
        }

        StartCoroutine(sv_match_start());
    }
    //Match starts
    /// <summary>
    /// Preround ends
    /// </summary>
    IEnumerator sv_match_start()
    {
        yield return new WaitForSeconds(respawn_time);

        for (int i = 0; i < all_ais.Length; i++)
        {

            all_ais[i].body.character_cond = Body_generic.Character_condition.FREE;
        }

        for (int i = 0; i < all_players.Length; i++)
        {

            all_players[i].body.character_cond = Body_generic.Character_condition.FREE;
            if (map_type == CONSTANTS.MAP_TYPE.PVP)
            {
                all_players[i].Rpc_unload_startup_menu();
            }
        }

        for (int i = 0; i < all_bodies.Length; i++)
        {
            all_bodies[i].respawnProtected = false;
        }

    }
    /// <summary>
    /// Signal the clients that all clients are ready and preround/freezeround has started
    /// </summary>
    [ClientRpc]
    public void Rpc_match_start()
    {
        if (isServer)
        {
            return;
        }

        map_match_start();
    }
    /// <summary>
    /// Signal server and clients
    /// </summary>
    void map_match_start()
    {
        cl_preroundStarted = true;
        localMapSpawnTime = Time.realtimeSinceStartup;

        //Remove "wait for players" screen
        if (isClient)//fix later, GUI = null despite client ready
        {
            Destroy(local_player.GUI.waitForPlayers);
        }

        //Weapon menu open
        if (Menu_watcher.Singleton != null)
        {
            Menu_watcher.Singleton.respawn_countDown();
        }

        //Perform operations registered to be executed when all clients are ready
        for (int i = 0; i < onClientReady.Count; i++)
        {
            onClientReady[i]();
        }
    }
    /// <summary>
    /// Check if there is a winner
    /// </summary>
    public void sv_map_check_winner()
    {

        if (team_human.enabled == false && team_zombie.enabled == false && team_robot.enabled == true)//Robot wins
        {
            sv_match_conclude(Body_generic.Character_type.Robot);
        }
        else if (team_human.enabled == true && team_zombie.enabled == false && team_robot.enabled == false)//Human wins
        {
            sv_match_conclude(Body_generic.Character_type.Human);
        }
        else if (team_human.enabled == false && team_zombie.enabled == true && team_robot.enabled == false)//Zombie wins
        {
            sv_match_conclude(Body_generic.Character_type.Zombie);
        }
        else if (team_human.enabled == false && team_zombie.enabled == false && team_robot.enabled == false)//Draw
        {
            sv_match_conclude(Body_generic.Character_type.Nothing);
        }
        else//Tell lost team they lost
        {

        }
    }
    //Match concludes
    /// <summary>
    /// Match conclude but yet to return to lobby
    /// </summary>
    void sv_match_conclude(Body_generic.Character_type race_winner)
    {
        syn_match_state = MATCH_STATE.GAME_PREVAIL;
        StartCoroutine(backToLobby());
        //Tell client to display summary
        Rpc_match_conclude((byte)race_winner);
    }
    [ClientRpc]
    public void Rpc_match_conclude(byte race_winner)
    {
        if (Menu_watcher.Singleton == null)
        {
            return;
        }

        Menu_watcher.Singleton.summary((Body_generic.Character_type)race_winner);
        if (!isServer)
        {
            StartCoroutine(backToLobby());
        }
    }
    //Match ends
    /// <summary>
    /// For server & clients;
    /// </summary>
    /// <returns></returns>
    public IEnumerator backToLobby()
    {
        yield return new WaitForSeconds(5);
        cl_preroundStarted = false;

        Cursor.visible = true;

        onClientReady.Clear();
        if (isServer)
        {
            syn_match_state = MATCH_STATE.LOBBY;
            comp_spawnPoint_manager.enabled = false;
            lobbyManager.ServerReturnToLobby();
        }
    }
    //Reset singletons
    void reset_var()
    {
        team_num_human_player = 0;
        team_num_robot_player = 0;
        team_num_zombie_player = 0;
        players.Clear();
        team_human = null;
        team_zombie = null;
        team_robot = null;
        Client_watcher.Singleton = null;
        Fx_watcher.Singleton = null;
        Navigation_manual.Singleton = null;
        Sound_watcher.Singleton = null;
        Decal_manager.Singleton = null;
        Menu_watcher.Singleton = null;
        Pool_watcher.Singleton = null;
        comp_spawnPoint_manager.clearAll();

    }
    [ServerCallback]
    void map_update()
    {
        //Initialization + waiting
        if (atState(MATCH_STATE.GAME_INITIALIZING, MATCH_STATE.GAME_WAIT))
        {
            bool allReady = true;
            //Make sure all in-game players are created locally
            int player_count = 0;
            for (int i = 0; i < lobbyManager.lobbySlots.Length; i++)
            {
                if (lobbyManager.lobbySlots[i] != null)
                {
                    player_count++;
                }
            }
            if (player_count == players.Count)
            {
                //Wait for all clients to be ready
                for (int i = 0; i < players.Count; ++i)
                {
                    if (players[i] == null)
                    {
                        continue;
                    }
                    if (!players[i].GetComponent<Player_generic>().cl_game_ready)
                    {
                        allReady = false;
                    }
                }
            }
            else//Not all in-game players are created
            {
                allReady = false;
            }
            if (allReady)
            {
                sv_preround_start();
            }

        }
        //Freeze round + match
        else
        {
            comp_spawnPoint_manager.enabled = true;

            //More operations during match go here...

        }
    }

    /*Lobby scene*/
    [ServerCallback]
    void lobby_update()
    {
        //Dedicated server reads server parameter file
        if (isDedicated() && Time.time > time_to_readfile)
        {
            time_to_readfile = Time.time + lobbyManager.file_read_interval;
            if (File.Exists(lobbyManager.cvar_path)) { 
                lobby_access_cvar_file();
            }
            else
            {
                lobby_generate_cvar_file();
            }
        }

        //More operations at lobby scene go here...

    }
    //Server parameter file
    void lobby_access_cvar_file()
    {
        StreamReader cvar_reader = new StreamReader(lobbyManager.cvar_path);
        if (cvar_reader == null)
        {
            return;
        }

        while (!cvar_reader.EndOfStream)
        {
            string[] arg = cvar_reader.ReadLine().Split(null);
            float value = 0;
            string parameter = arg[0];

            if (arg.Length >= 2 && float.TryParse(arg[1], out value))
            {
                switch (parameter)
                {
                    case "tk_human":
                        tickets_human = (short)value;
                        break;
                    case "tk_robot":
                        tickets_robot = (short)value;
                        break;
                    case "tk_zombie":
                        tickets_zombie = (short)value;
                        break;

                    case "num_human":
                        team_num_human = (short)value;
                        break;

                    case "num_robot":
                        team_num_robot = (short)value;
                        break;

                    case "num_zombie":
                        team_num_zombie = (short)value;
                        break;

                    case "passThru":
                        if (value > 0)
                        {
                            allyBulletPassThru = true;
                        }
                        else
                        {
                            allyBulletPassThru = false;
                        }
                        break;

                    case "losVision":
                        if (value > 0)
                        {
                            losVision = true;
                        }
                        else
                        {
                            losVision = false;
                        }
                        break;

                    case "teamTransparent":
                        if (value > 0)
                        {
                            team_transparent = true;
                        }
                        else
                        {
                            team_transparent = false;
                        }
                        break;

                    case "maxSkills":
                        maxSkillSpentPerDeath = (byte)value;
                        break;

                    case "rspwnTime":
                        respawn_time = (int)value;
                        break;

                    case "startMoney":
                        insurance_money = (int)value;
                        break;

                    case "map":
                        lobby_change_map((int)value);
                        break;
                }
            }
        }

        cvar_reader.Close();
    }
    void lobby_generate_cvar_file()
    {
        StreamWriter cvar_writer = new StreamWriter(lobbyManager.cvar_path);
        if (cvar_writer == null)
        {
            return;
        }

        cvar_writer.WriteLine("tk_human " + tickets_human);
        cvar_writer.WriteLine("tk_robot " + tickets_robot);
        cvar_writer.WriteLine("tk_zombie " + tickets_zombie);
        cvar_writer.WriteLine("num_human " + team_num_human);
        cvar_writer.WriteLine("num_robot " + team_num_robot);
        cvar_writer.WriteLine("num_zombie " + team_num_zombie);
        cvar_writer.WriteLine("passThru " + CONSTANTS.bool_to_int(allyBulletPassThru));
        cvar_writer.WriteLine("losVision " + CONSTANTS.bool_to_int(losVision));
        cvar_writer.WriteLine("maxSkills " + maxSkillSpentPerDeath);
        cvar_writer.WriteLine("teamTransparent " + CONSTANTS.bool_to_int(team_transparent));
        cvar_writer.WriteLine("rspwnTime " + respawn_time);
        cvar_writer.WriteLine("startMoney " + insurance_money);
        cvar_writer.WriteLine("map " + map);

        cvar_writer.Close();
    }
    //This will reset every player's character selection to observer
    void lobby_change_map(int new_map)
    {
        if (map != new_map)
        {
            map = (int)new_map;
            for (int i = 0; i < lobbyManager.lobbySlots.Length; i++)
            {
                if (lobbyManager.lobbySlots[i] == null)
                {
                    continue;
                }
                Prototype.NetworkLobby.LobbyPlayer player = (Prototype.NetworkLobby.LobbyPlayer)lobbyManager.lobbySlots[i];
                player.reset_character();
            }
        }
    }


    //Messages when server parameters change 
    public void Hook_FriendlyFire(bool value)
    {
        allyBulletPassThru = value;
        string bit = "Off";
        if (allyBulletPassThru)
        {
            bit = "Off";
        }
        else
        {
            bit = "On";
        }
        message_watcher.Server_say("FriendlyFire is set to " + bit);
    }
    public void Hook_LOSEnable(bool value)
    {
        losVision = value;
        string bit = "Off";
        if (losVision)
        {
            bit = "On";
        }
        else
        {
            bit = "Off";
        }
        message_watcher.Server_say("Line-Of-Sight is set to " + bit);
    }
    public void Hook_RespawnTime(int value)
    {
        respawn_time = value;
        message_watcher.Server_say("Respawn time is set to " + value + " sec");
    }
    public void Hook_Map(int value)
    {
        map = value;
        message_watcher.Server_say("Map is set to " + lobbyManager.maps[value]);
    }
    public void Hook_HumanLimit(short value)
    {
        team_num_human = value;
        message_watcher.Server_say("Human count is set to " + value);
    }
    public void Hook_RobotLimit(short value)
    {
        team_num_robot = value;
        message_watcher.Server_say("Robot count is set to " + value);
    }
    public void Hook_ZombieLimit(short value)
    {
        team_num_zombie = value;
        message_watcher.Server_say("Zombie count is set to " + value);
    }
    public void Hook_HumanRespawns(short value)
    {
        tickets_human = value;
        message_watcher.Server_say("Human respawns is set to " + value);
    }
    public void Hook_RobotRespawns(short value)
    {
        tickets_robot = value;
        message_watcher.Server_say("Robot respawns is set to " + value);
    }
    public void Hook_ZombieRespawns(short value)
    {
        tickets_zombie = value;
        message_watcher.Server_say("Zombie respawns is set to " + value);
    }
    public void Hook_MaxSkillDeath(byte value)
    {
        maxSkillSpentPerDeath = value;
        message_watcher.Server_say("Max upgrades per death is set to " + value);
    }
    public void Hook_StartMoney(int value)
    {
        insurance_money = value;
        message_watcher.Server_say("Start money is set to " + value);
    }
    public void Hook_TeamTransparent(bool value)
    {
        team_transparent = value;
        string bit = "Off";
        if (team_transparent)
        {
            bit = "On";
        }
        else
        {
            bit = "Off";
        }
        message_watcher.Server_say("Team transparent is set to " + bit);



    }

    //Getters
    /// <summary>
    /// Get the number of respawn tickets initially
    /// </summary>
    /// <param name="race"></param>
    /// <returns></returns>
    public short get_init_tickets(Body_generic.Character_type race)
    {
        if(race == Body_generic.Character_type.Human)
        {
            return tickets_human;
        }
        else if(race == Body_generic.Character_type.Robot)
        {
            return tickets_robot;
        }
        else if(race == Body_generic.Character_type.Zombie)
        {
            return tickets_zombie;
        }
        return -1;
    }
    /// <summary>
    /// Get the total number player & AIs who joined the match
    /// </summary>
    /// <param name="race">The race of which the count is queried</param>
    /// <returns></returns>
    public short get_joined_character(Body_generic.Character_type race)
    {
        if (race == Body_generic.Character_type.Human)
        {
            return team_num_human;
        }
        else if (race == Body_generic.Character_type.Robot)
        {
            return team_num_robot;
        }
        else if (race == Body_generic.Character_type.Zombie)
        {
            return team_num_zombie;
        }
        return -1;
    }
    /// <summary>
    /// Get the total number player who joined the match
    /// </summary>
    /// <param name="race">The race of which the count is queried</param>
    /// <returns></returns>
    public short get_joined_player(Body_generic.Character_type race)
    {
        if (race == Body_generic.Character_type.Human)
        {
            return team_num_human_player;
        }
        else if (race == Body_generic.Character_type.Robot)
        {
            return team_num_robot_player;
        }
        else if (race == Body_generic.Character_type.Zombie)
        {
            return team_num_zombie_player;
        }
        return -1;
    }
    
    //Message system
    /// <summary>
    /// Server sided
    /// </summary>
    /// <param name="player">The player of whose name will be printeds</param>
    /// <param name="message">The message transimited</param>
    /// <param name="isServerMsg">If true, message delievered without the name</param>
    public void sv_sendMessage(GameObject player, string message, bool isServerMsg)
    {
        if (isServerMsg)
        {
            Rpc_sendMessage_sv(message);
        }
        else
        {
            Rpc_sendMessage(player, message);
        }
    }
    [ClientRpc]
    public void Rpc_sendMessage_sv(string message)
    {
        message_watcher.Server_say(message);
    }
    [ClientRpc]
    public void Rpc_sendMessage(GameObject player, string message)
    {
        message_watcher.Say(player, message);
    }

    //Record of entities
    public void add_entity(GameObject entity)
    {
        entity_pool.Add(entity);
    }
    public void remove_entity(GameObject entity)
    {
        entity_pool.Remove(entity);
    }
}
