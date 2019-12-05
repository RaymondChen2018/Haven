using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
public class Player_generic : NetworkBehaviour {
    Action state_update;
    [HideInInspector] public bool cl_game_ready = false;
    
    public KeyCode toggleChat;
    public KeyCode toggleScoreBoard;
    //Network
    int request_sendtime = 0;
    int request_sendtime_sv = 0;
    [SyncVar(hook = "Hook_name")] public string character_name = "Player";
    /// <summary>
    /// Local version of latency, only available for local client
    /// </summary>
    [HideInInspector] public short latency = 0;
    /// <summary>
    /// Global version of latency, available for every clients
    /// </summary>
    [SyncVar(hook = "Hook_latency_sv")] public short latency_sv = 0;

    [SyncVar(hook = "Hook_kill1")] public short kill1 = 0;

    [SyncVar(hook = "Hook_kill2")] public short kill2 = 0;

    [SyncVar(hook = "Hook_kill3")] public short kill3 = 0;

    [SyncVar(hook = "Hook_death")] public short deaths = 0;

    float time_to_test_latency = 0;
    bool packet_received = true;
    public Body_generic.Character_type character_type = Body_generic.Character_type.Nothing;
    Server_watcher cvar_watcher;
    Message_board_watcher msg_watcher;
    public GameObject GUI_template;
    [HideInInspector] public GUI_manager GUI;
    public ScoreToken_generic GUI_token;
    // Use this for initialization
    void Start () {
        cvar_watcher = Server_watcher.Singleton;
        msg_watcher = cvar_watcher.GetComponent<Message_board_watcher>();
        
        if (!isLocalPlayer)
        {
            state_update = Update_inGame;
            return;
        }

        cvar_watcher.local_player = this;
        GUI = Instantiate(GUI_template).GetComponent<GUI_manager>();
        GUI.cvar_watcher = cvar_watcher;
        GUI_template = null;
        if (character_type == Body_generic.Character_type.Observer)
        {
            GUI.observer_healthbar.gameObject.SetActive(true);
            GetComponent<Observer_controller>().health_bar = GUI.observer_healthbar;
            GetComponent<Observer_controller>().debug_info = GUI.debug_info;
        }
        else
        {
            GetComponent<Player_controller>().debug_info = GUI.debug_info;
        }
        cvar_watcher.onClientReady.Add(GUI.OnClientReady);
        state_update = Update_waitingReady;

    }

    [Command]
    public void Cmd_send_message(string message)
    {
        cvar_watcher.sv_sendMessage(gameObject, message, false);
    }
    [Command]
    public void Cmd_sendReady()
    {
        cl_game_ready = true;
    }
    /// <summary>
    /// Local scene is not ready because not all npcs/players are instantiated
    /// </summary>
    void Update_waitingReady()
    {
        //Debug.LogError("waiting_for_teams count: "+ waiting_for_teams.Count);
        if (!isLocalPlayer)
        {
            return;
        }

        

        //PVP, wait for all players/npcs ready
        if(cvar_watcher.map_type == CONSTANTS.MAP_TYPE.PVP)
        {
            int total_players = cvar_watcher.team_num_human + cvar_watcher.team_num_robot + cvar_watcher.team_num_zombie;
            if (total_players > FindObjectsOfType<Body_generic>().Length)
            {
                return;
            }
        }
        //Objective, wait for all players ready
        else if(cvar_watcher.map_type == CONSTANTS.MAP_TYPE.Objective)
        {
            int player_count = 0;
            for (int i = 0; i < cvar_watcher.lobbyManager.lobbySlots.Length; i++)
            {
                if (cvar_watcher.lobbyManager.lobbySlots[i] != null)
                {
                    player_count++;
                }
            }
            if(player_count > FindObjectsOfType<Player_generic>().Length)
            {
                return;
            }
        }
        
        //All team spawn ready
        state_update = Update_waitingOthers;
        cvar_watcher.onClientReady.Add(OnClientReady);
        Cmd_sendReady();
    }
    void OnClientReady()
    {
        state_update = Update_inGame;
    }
    /// <summary>
    /// Limbo state when the local player scene is ready but other remote clients are not
    /// </summary>
    void Update_waitingOthers()
    {

    }
    /// <summary>
    /// Started when all clients are ready, in game state
    /// </summary>
    void Update_inGame()
    {
        if (isLocalPlayer)
        {
            //Client interface
            latency_test();

            //Chat
            if (Input.GetKeyDown(toggleChat))
            {
                msg_watcher = FindObjectOfType<Message_board_watcher>();
                if (msg_watcher != null)//Start editing
                {
                    msg_watcher.Turnon_chat();
                }
            }

            if (Input.GetKeyDown(toggleScoreBoard))
            {
                GUI.scoreBoard.SetActive(true);
                GUI.scoreBoardUpdate();
            }
            else if (Input.GetKeyUp(toggleScoreBoard))
            {
                GUI.scoreBoard.SetActive(false);
            }
        }
    }
    // Update is called once per frame
    void Update () {
        state_update();
    }
    public void Hook_name(string value)
    {
        character_name = value;
        if (GUI_token != null && GUI_token.gameObject.activeInHierarchy)
        {
            GUI_token.Name.text = value.ToString();
        }
    }
    public void Hook_latency_sv(short value)
    {
        latency_sv = value;
        if(GUI_token != null && GUI_token.gameObject.activeInHierarchy)
        {
            
            GUI_token.Latency.text = value.ToString();
        }
    }
    public void Hook_kill1(short value)
    {
        kill1 = value;
        if (GUI_token != null && GUI_token.gameObject.activeInHierarchy)
        {
            GUI_token.Kill1.text = value.ToString();
        }

    }
    public void Hook_kill2(short value)
    {
        kill2 = value;
        if (GUI_token != null && GUI_token.gameObject.activeInHierarchy)
        {
            GUI_token.Kill2.text = value.ToString();
        }

    }
    public void Hook_kill3(short value)
    {
        kill3 = value;
        if (GUI_token != null && GUI_token.gameObject.activeInHierarchy)
        {
            GUI_token.Kill3.text = value.ToString();
        }

    }
    public void Hook_death(short value)
    {
        deaths = value;
        if (GUI_token != null && GUI_token.gameObject.activeInHierarchy)
        {
            GUI_token.Deaths.text = value.ToString();
        }

    }
    
    //Latency
    void latency_test()
    {
        if (isServer)
        {
            return;
        }
        if (Time.time > time_to_test_latency && packet_received)
        {
            time_to_test_latency = Time.time + CONSTANTS.LATENCY_TEST_INTERVAL;
            packet_received = false;
            request_sendtime = get_latency_time();
            Cmd_Latency();
        }
    }
    [Command]
    void Cmd_Latency()
    {
        
        Target_latency(connectionToClient);
        request_sendtime_sv = get_latency_time();
    }
    [Command]
    void Cmd_Latency_response()
    {
        int now_time = get_latency_time();
        if (now_time > request_sendtime_sv)
        {
            latency_sv = latency_interpolate(latency, (short)(now_time - request_sendtime_sv));
        }
        else
        {
            latency_sv = latency_interpolate(latency, (short)((59 * 1000 + 999) - request_sendtime_sv + now_time));
        }
    }
    int get_latency_time()
    {
        return System.DateTime.Now.Millisecond + System.DateTime.Now.Second * 1000;
    }
    short latency_interpolate(short old_latency, short new_latency)
    {
        return (short)(old_latency * 0.3f + new_latency * 0.7f);
    }
    [TargetRpc]
    void Target_latency(NetworkConnection client)
    {
        int now_time = get_latency_time();
        short latency_prev = latency;
        if (now_time > request_sendtime)
        {
            latency = latency_interpolate(latency, (short)(now_time - request_sendtime));
        }
        else
        {
            latency = latency_interpolate(latency, (short)((59 * 1000 + 999) - request_sendtime + now_time));
        }
        
        Cmd_Latency_response();
        packet_received = true;
    }
}
