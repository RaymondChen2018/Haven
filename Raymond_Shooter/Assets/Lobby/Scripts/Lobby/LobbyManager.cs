using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using UnityEngine.Networking.Match;
using System.Collections;
using System.IO;


namespace Prototype.NetworkLobby
{
    public class LobbyManager : NetworkLobbyManager
    {
        static short MsgKicked = MsgType.Highest + 1;
        static short MsgHotJoinDisabled = MsgType.Highest + 2;//ruiming defined, delete if no use
        static short MsgVersionDifferent = MsgType.Highest + 3;//ruiming defined, delete if no use
        static short MsgServerFull = MsgType.Highest + 4;//ruiming defined, delete if no use
        static public LobbyManager s_Singleton;

        public bool dedicated_build = false;
        [SerializeField] private GameObject player_prefab_bot;
        [SerializeField] private GameObject[] player_prefab_human;
        [SerializeField] private GameObject player_prefab_zombie;
        [SerializeField] private GameObject player_prefab_observer;
        public GameObject Cvar_list_panel;//For dedicated server debugging
        [SerializeField] private GameObject Cvar_template;
        private int spawn_index = 0;
        public string[] maps;
        public string cvar_path = "Dedicated_cvar.txt";
        public int file_read_interval = 2;



        [Header("Unity UI Lobby")]
        [Tooltip("Time in second between all players ready & match start")]
        public float prematchCountdown = 5.0f;

        [Space]
        [Header("UI Reference")]
        public LobbyTopPanel topPanel;

        public RectTransform mainMenuPanel;
        public RectTransform BackgroundPanel;
        public RectTransform lobbyPanel;

        //Info panels
        public RectTransform matchname_panel;
        public LobbyInfoPanel infoPanel;
        public LobbyCountdownPanel countdownPanel;
        public GameObject addPlayerButton;

        protected RectTransform currentPanel;

        public UnityEngine.UI.Button backButton;

        public Text statusInfo;
        public Text hostInfo;


        //Client numPlayers from NetworkManager is always 0, so we count (throught connect/destroy in LobbyPlayer) the number
        //of players, so that even client know how many player there is.
        [HideInInspector]
        public int _playerNumber = 0;
        public Server_watcher cvar_watcher;

        //used to disconnect a client properly when exiting the matchmaker
        [HideInInspector]
        public bool _isMatchmaking = false;

        protected bool _disconnectServer = false;
        
        protected ulong _currentMatchID;

        protected LobbyHook _lobbyHooks;

        

        public void reset_var()
        {
            spawn_index = 0;
        }

        public void spawn_cvar()
        {
            if(Server_watcher.Singleton != null)
            {
                return;
            }
            GameObject obj = Instantiate(Cvar_template);
            NetworkServer.Spawn(obj);
        }

        void Start()
        {
            s_Singleton = this;
            _lobbyHooks = GetComponent<Prototype.NetworkLobby.LobbyHook>();
            currentPanel = mainMenuPanel;

            backButton.gameObject.SetActive(false);
            GetComponent<Canvas>().enabled = true;

            DontDestroyOnLoad(gameObject);

            SetServerInfo("Offline", "None");

            if (dedicated_build)
            {
                mainMenuPanel.GetComponent<LobbyMainMenu>().OnClickDedicated();
                lobbyPanel.gameObject.SetActive(true);
                //Cvar_list_panel.SetActive(true);
                Camera.main.enabled = false;
                //access_cvar();
                //write_to_log("Server started============\n", true);
            }
        }
        


        public override void OnLobbyClientSceneChanged(NetworkConnection conn)
        {
            if (SceneManager.GetSceneAt(0).name == lobbyScene)
            {
                if (topPanel.isInGame)
                {
                    ChangeTo(lobbyPanel);
                    if (_isMatchmaking)
                    {
                        if (conn.playerControllers[0].unetView.isServer)
                        {
                            backDelegate = StopHostClbk;
                        }
                        else
                        {
                            backDelegate = StopClientClbk;
                        }
                    }
                    else
                    {
                        if (conn.playerControllers[0].unetView.isClient)
                        {
                            backDelegate = StopHostClbk;
                        }
                        else
                        {
                            backDelegate = StopClientClbk;
                        }
                    }
                }
                else
                {
                    ChangeTo(mainMenuPanel);
                }

                //topPanel.ToggleVisibility(true);
                topPanel.isInGame = false;
            }
            else
            {
                ChangeTo(null);

                Destroy(GameObject.Find("MainMenuUI(Clone)"));

                //backDelegate = StopGameClbk;
                topPanel.isInGame = true;
                topPanel.ToggleVisibility(false);
            }
        }




        





        public void ChangeTo(RectTransform newPanel)
        {
            if (currentPanel != null)
            {
                currentPanel.gameObject.SetActive(false);
            }

            if (newPanel != null)
            {
                newPanel.gameObject.SetActive(true);
            }

            currentPanel = newPanel;

            if (currentPanel != mainMenuPanel)
            {
                backButton.gameObject.SetActive(true);
            }
            else
            {
                backButton.gameObject.SetActive(false);
                SetServerInfo("Offline", "None");
                _isMatchmaking = false;
            }
            
            if(newPanel == null)//In game
            {
                //BackgroundPanel.gameObject.SetActive(false);
            }
            else//In menu
            {
                //BackgroundPanel.gameObject.SetActive(true);
            }
        }

        public void DisplayIsConnecting()
        {
            var _this = this;

            infoPanel.Display("Connecting...", "Cancel", () => { _this.backDelegate(); });
        }

        public void SetServerInfo(string status, string host)
        {
            statusInfo.text = status;
            hostInfo.text = host;
        }


        public delegate void BackButtonDelegate();
        /// <summary>
        /// Back button destine function
        /// </summary>
        public BackButtonDelegate backDelegate;
        public void GoBackButton()
        {
            backDelegate();
			topPanel.isInGame = false;
            Cursor.visible = true;
        }

        // ----------------- Server management

        public void AddLocalPlayer()
        {
            TryToAddPlayer();
        }

        public void RemovePlayer(LobbyPlayer player)
        {
            player.RemovePlayer();
        }

        public void SimpleBackClbk()
        {
            ChangeTo(mainMenuPanel);
        }
                 
        public void StopHostClbk()
        {
            if (_isMatchmaking)
            {
				matchMaker.DestroyMatch((NetworkID)_currentMatchID, 0, OnDestroyMatch);
				_disconnectServer = true;
            }
            else
            {
                StopHost();
            }

            
            ChangeTo(mainMenuPanel);
        }

        public void StopClientClbk()
        {
            StopClient();
            if (_isMatchmaking)
            {
                StopMatchMaker();
                
            }

            ChangeTo(mainMenuPanel);
            if(cvar_watcher != null)
            {
                cvar_watcher.gameObject.SetActive(true);
            }
            
        }

        public void StopServerClbk()
        {
            StopServer();
            ChangeTo(mainMenuPanel);
        }

        class KickMsg : MessageBase { }
        public void KickPlayer(NetworkConnection conn)
        {
            conn.Send(MsgKicked, new KickMsg());
        }
        public void KickVersionDifferent(NetworkConnection conn)
        {
            conn.Send(MsgVersionDifferent, new KickMsg());
            cvar_watcher.sv_sendMessage(null, "A join attempt is denied; Reason: Version Mismatch", true);
        }




        public void KickedMessageHandler(NetworkMessage netMsg)
        {
            infoPanel.Display("Kicked by Server", "Close", null);
            netMsg.conn.Disconnect();
        }
        public void KickedHotJoinDisabled(NetworkMessage netMsg)
        {
            infoPanel.Display("Match is in session", "Close", null);
            netMsg.conn.Disconnect();
        }
        public void KickedVersionDifferent(NetworkMessage netMsg)
        {
            infoPanel.Display("Version mismatch", "Close", null);
            netMsg.conn.Disconnect();
        }
        public void KickedServerFull(NetworkMessage netMsg)
        {
            infoPanel.Display("Server full", "Close", null);
            netMsg.conn.Disconnect();
        }

        //===================

        public override void OnStartHost()
        {
            base.OnStartHost();

            ChangeTo(lobbyPanel);
            backDelegate = StopHostClbk;
            SetServerInfo("Hosting", networkAddress);
        }

		public override void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
		{
			base.OnMatchCreate(success, extendedInfo, matchInfo);
            _currentMatchID = (System.UInt64)matchInfo.networkId;
		}

		public override void OnDestroyMatch(bool success, string extendedInfo)
		{
			base.OnDestroyMatch(success, extendedInfo);
			if (_disconnectServer)
            {
                StopMatchMaker();
                StopHost();
            }
        }

        //allow to handle the (+) button to add/remove player
        public void OnPlayersNumberModified(int count)
        {
            _playerNumber += count;

            int localPlayerCount = 0;
            foreach (PlayerController p in ClientScene.localPlayers)
                localPlayerCount += (p == null || p.playerControllerId == -1) ? 0 : 1;

            addPlayerButton.SetActive(localPlayerCount < maxPlayersPerConnection && _playerNumber < maxPlayers);
            
        }

        void OnFailedToConnect(NetworkConnectionError error)
        {
            Debug.Log("Could not connect to server: " + error);
        }

        // ----------------- Server callbacks ------------------

        //we want to disable the button JOIN if we don't have enough player
        //But OnLobbyClientConnect isn't called on hosting player. So we override the lobbyPlayer creation
        public override GameObject OnLobbyServerCreateLobbyPlayer(NetworkConnection conn, short playerControllerId)
        {
            GameObject obj = Instantiate(lobbyPlayerPrefab.gameObject) as GameObject;

            LobbyPlayer newPlayer = obj.GetComponent<LobbyPlayer>();
            newPlayer.ToggleJoinButton(numPlayers + 1 >= minPlayers);


            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                LobbyPlayer p = lobbySlots[i] as LobbyPlayer;

                if (p != null)
                {
                    p.RpcUpdateRemoveButton();
                    p.ToggleJoinButton(numPlayers + 1 >= minPlayers);
                }
            }

            return obj;
        }


        void calculate_player_race()
        {
            for (int i = 0; i < lobbySlots.Length; i++)
            {
                if (lobbySlots[i] == null)
                {
                    continue;
                }
                if ((lobbySlots[i] as LobbyPlayer).playerCharacter == LobbyPlayer.CharacterType.Human)
                {
                    cvar_watcher.team_num_human_player++;
                }
                else if ((lobbySlots[i] as LobbyPlayer).playerCharacter == LobbyPlayer.CharacterType.Robot)
                {
                    cvar_watcher.team_num_robot_player++;
                }
                else if ((lobbySlots[i] as LobbyPlayer).playerCharacter == LobbyPlayer.CharacterType.Zombie)
                {
                    cvar_watcher.team_num_zombie_player++;
                }
            }
        }
        public int get_lobbyPlayers_index(NetworkConnection conn)
        {
            for (int i = 0; i < lobbySlots.Length; i++)
            {
                LobbyPlayer lobbyPlayer = lobbySlots[i] as LobbyPlayer;

                if (lobbyPlayer.connectionToClient == null)
                {
                    Debug.LogError("connectionclient null");
                }
                if(conn == null)
                {
                    Debug.LogError("conn null");
                }
                if (lobbyPlayer != null && lobbyPlayer.connectionToClient.connectionId == conn.connectionId)
                {
                    return i;
                }
            }
            return -1;
        }

        public override GameObject OnLobbyServerCreateGamePlayer(NetworkConnection conn, short playerControllerId)
        {
            Vector2 v = Vector2.zero;
            GameObject obj = null;
            
            
            //lobbyPlayer.conn
            LobbyPlayer.CharacterType playerCharacter = LobbyPlayer.CharacterType.Human;

            for (int i = 0; i < lobbySlots.Length; i++)
            {
                LobbyPlayer lobbyPlayer = lobbySlots[i] as LobbyPlayer;
                if (lobbyPlayer != null && lobbyPlayer.connectionToClient.connectionId == conn.connectionId)
                {
                    playerCharacter = lobbyPlayer.playerCharacter;
                }
            }
            spawn_index++;
            if(playerCharacter == LobbyPlayer.CharacterType.Robot)
            {
                Info_robot_spawn[] spawns = FindObjectsOfType<Info_robot_spawn>();
                if (spawns != null && spawns.Length > 0)
                {
                    v = spawns[Random.Range(0, spawns.Length)].transform.position;
                }
                obj = Instantiate(player_prefab_bot, v, Quaternion.identity);
            }
            else if (playerCharacter == LobbyPlayer.CharacterType.Human)
            {
                Info_human_spawn[] spawns = FindObjectsOfType<Info_human_spawn>();
                if (spawns != null && spawns.Length > 0)
                {
                    v = spawns[Random.Range(0, spawns.Length)].transform.position;
                }
                obj = Instantiate(player_prefab_human[UnityEngine.Random.Range(0, player_prefab_human.Length)], v, Quaternion.identity);
            }
            else if (playerCharacter == LobbyPlayer.CharacterType.Zombie)
            {
                Info_zombie_spawn[] spawns = FindObjectsOfType<Info_zombie_spawn>();
                if (spawns != null && spawns.Length > 0)
                {
                    v = spawns[Random.Range(0, spawns.Length)].transform.position;
                }
                obj = Instantiate(player_prefab_zombie, v, Quaternion.identity);
            }
            else if (playerCharacter == LobbyPlayer.CharacterType.Observer)
            {
                obj = Instantiate(player_prefab_observer, Vector2.zero, Quaternion.identity);
            }

            cvar_watcher.players.Add(obj);
            return obj;
        }

        public override void OnLobbyServerPlayerRemoved(NetworkConnection conn, short playerControllerId)
        {
            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                LobbyPlayer p = lobbySlots[i] as LobbyPlayer;

                if (p != null)
                {
                    p.RpcUpdateRemoveButton();
                    p.ToggleJoinButton(numPlayers + 1 >= minPlayers);
                }
            }
        }

        public override void OnLobbyServerDisconnect(NetworkConnection conn)
        {
            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                LobbyPlayer p = lobbySlots[i] as LobbyPlayer;

                if (p != null)
                {
                    p.RpcUpdateRemoveButton();
                    p.ToggleJoinButton(numPlayers >= minPlayers);
                }
            }
        }

        public override bool OnLobbyServerSceneLoadedForPlayer(GameObject lobbyPlayer, GameObject gamePlayer)
        {
            //This hook allows you to apply state data from the lobby-player to the game-player
            //just subclass "LobbyHook" and add it to the lobby object.

            //If in-game players
            if (gamePlayer.GetComponent<Body_generic>() != null)
            {
                gamePlayer.GetComponent<Body_generic>().set_player_name(lobbyPlayer.GetComponent<LobbyPlayer>().playerName);
                gamePlayer.GetComponent<Body_generic>().set_player_color(lobbyPlayer.GetComponent<LobbyPlayer>().playerColor);
            }
            

            if (_lobbyHooks)
                _lobbyHooks.OnLobbyServerSceneLoadedForPlayer(this, lobbyPlayer, gamePlayer);

            return true;
        }
        
        // --- Countdown management

        public override void OnLobbyServerPlayersReady()
        {
			bool allready = true;
			for(int i = 0; i < lobbySlots.Length; ++i)
			{
				if(lobbySlots[i] != null)
					allready &= lobbySlots[i].readyToBegin;
			}

			if(allready)
				StartCoroutine(ServerCountdownCoroutine());
        }

        public IEnumerator ServerCountdownCoroutine()
        {
            float remainingTime = prematchCountdown;
            int floorTime = Mathf.FloorToInt(remainingTime);
            cvar_watcher.syn_match_state = Server_watcher.MATCH_STATE.LOBBY_COUNTDOWN;
            while (remainingTime > 0)
            {
                yield return null;

                remainingTime -= Time.deltaTime;
                int newFloorTime = Mathf.FloorToInt(remainingTime);

                if (newFloorTime != floorTime)
                {//to avoid flooding the network of message, we only send a notice to client when the number of plain seconds change.
                    floorTime = newFloorTime;

                    for (int i = 0; i < lobbySlots.Length; ++i)
                    {
                        if (lobbySlots[i] != null)
                        {//there is maxPlayer slots, so some could be == null, need to test it before accessing!
                            (lobbySlots[i] as LobbyPlayer).RpcUpdateCountdown(floorTime);
                        }
                    }
                }
            }

            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                if (lobbySlots[i] != null)
                {
                    (lobbySlots[i] as LobbyPlayer).RpcUpdateCountdown(0);
                }
            }



            
            calculate_player_race();
            flag_all_nonReady();
            if(maps != null && cvar_watcher.map >= 0 && cvar_watcher.map < maps.Length)
            {
                playScene = maps[cvar_watcher.map];
            }
            if(playScene.Substring(0, 4).ToLower() == "pvp_")
            {
                cvar_watcher.map_type = CONSTANTS.MAP_TYPE.PVP;
            }
            else if (playScene.Substring(0, 4).ToLower() == "obj_")
            {
                cvar_watcher.map_type = CONSTANTS.MAP_TYPE.Objective;
            }
            else if (playScene.Substring(0, 4).ToLower() == "zs_")
            {
                cvar_watcher.map_type = CONSTANTS.MAP_TYPE.ZSurvival;
            }
            else
            {
                cvar_watcher.map_type = CONSTANTS.MAP_TYPE.Custom;
            }
            ServerChangeScene(playScene);
            cvar_watcher.syn_match_state = Server_watcher.MATCH_STATE.GAME_INITIALIZING;
        }
        /// <summary>
        /// Assuring each client is ready by waiting for their reply
        /// </summary>
        void flag_all_nonReady()
        {
            for (int i = 0; i < lobbySlots.Length; ++i)
            {
                if (lobbySlots[i] != null)
                {
                    lobbySlots[i].readyToBegin = false;
                }
                    
            }
        }

        // ----------------- Client callbacks ------------------


        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);

            infoPanel.gameObject.SetActive(false);

            conn.RegisterHandler(MsgKicked, KickedMessageHandler);
            conn.RegisterHandler(MsgHotJoinDisabled, KickedHotJoinDisabled);
            conn.RegisterHandler(MsgVersionDifferent, KickedVersionDifferent);
            conn.RegisterHandler(MsgServerFull, KickedServerFull);

            if (!NetworkServer.active)
            {//only to do on pure client (not self hosting client)
                ChangeTo(lobbyPanel);
                backDelegate = StopClientClbk;
                SetServerInfo("Client", networkAddress);
            }
            
        }


        public override void OnClientDisconnect(NetworkConnection conn)
        {
            
            base.OnClientDisconnect(conn);
            ChangeTo(mainMenuPanel);
        }
        public override void OnServerConnect(NetworkConnection conn)
        {

            //base.OnServerConnect(conn);
            if (cvar_watcher != null && numPlayers >= maxPlayers)
            {
                conn.Send(MsgServerFull, new KickMsg());
                cvar_watcher.sv_sendMessage(null, "A join attempt is denied; Reason: Server Full", true);
                //conn.Disconnect();
                return;
            }

            // cannot join game in progress
            //string loadedSceneName = SceneManager.GetSceneAt(0).name;
            //if (loadedSceneName != lobbyScene)
            //{
            if (cvar_watcher != null && !(cvar_watcher.syn_match_state == Server_watcher.MATCH_STATE.LOBBY))
            {
                conn.Send(MsgHotJoinDisabled, new KickMsg());
                cvar_watcher.sv_sendMessage(null, "A join attempt is denied; Reason: Match is already in session", true);
                //conn.Disconnect();
                return;
            }

            if (cvar_watcher != null)
            {
                cvar_watcher.sv_sendMessage(null, "A new player has joined the game", true);
            }
        }
        public override void OnServerDisconnect(NetworkConnection conn)
        {
            string player_name = null;
            for (int i = 0; i < lobbySlots.Length; i++)
            {

                LobbyPlayer lobbyPlayer = lobbySlots[i] as LobbyPlayer;

                if (lobbyPlayer != null && lobbyPlayer.connectionToClient.connectionId == conn.connectionId)
                {
                    player_name = lobbyPlayer.playerName;
                    break;
                }
            }
            cvar_watcher.sv_sendMessage(null, "Player '" + player_name + "' disconnected", true);
            
            
            base.OnServerDisconnect(conn);

            int connected_count = 0;
            for (int i = 0; i < lobbySlots.Length; i++)
            {
                LobbyPlayer lobbyPlayer = lobbySlots[i] as LobbyPlayer;
                if (lobbyPlayer != null)
                {
                    connected_count++;
                }
            }
            if (cvar_watcher.syn_match_state == Server_watcher.MATCH_STATE.GAME_INSESSION && (connected_count < 1))
            {
                cvar_watcher.syn_match_state = Server_watcher.MATCH_STATE.GAME_PREVAIL;
                StartCoroutine(cvar_watcher.backToLobby());
            }
        }

        public override void OnClientError(NetworkConnection conn, int errorCode)
        {
            ChangeTo(mainMenuPanel);
            infoPanel.Display("Cient error : " + (errorCode == 6 ? "timeout" : errorCode.ToString()), "Close", null);
        }
    }
}
