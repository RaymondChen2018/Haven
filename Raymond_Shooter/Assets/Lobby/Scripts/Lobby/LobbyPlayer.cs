using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace Prototype.NetworkLobby
{
    //Player entry in the lobby. Handle selecting color/setting name & getting ready for the game
    //Any LobbyHook can then grab it and pass those value to the game player prefab (see the Pong Example in the Samples Scenes)
    public class LobbyPlayer : NetworkLobbyPlayer
    {
        public enum CharacterType
        {
            Robot,
            Human,
            Zombie,
            Observer
        }
        public KeyCode toggleChat;
        static public LobbyPlayer local_lobbyplayer;
        //public static Color[] Colors = new Color[] { Color.magenta, Color.red, Color.cyan, Color.blue, Color.green, Color.yellow, new Color(1,1,0,1), Color.white };
        static CharacterType[] Characters = new CharacterType[] { CharacterType.Human, CharacterType.Robot, CharacterType.Zombie, CharacterType.Observer};
        //used on server to avoid assigning the same color to two player
        static List<int> _colorInUse = new List<int>();
        static List<int> _characterInUse = new List<int>();
        int character_idx = 0;
        public bool hotjoin_stub = false;
        public UnityEngine.UI.Button colorButton;
        public UnityEngine.UI.Button characterButton;
        public InputField nameInput;
        public UnityEngine.UI.Button readyButton;
        public UnityEngine.UI.Button waitingPlayerButton;
        public UnityEngine.UI.Button removePlayerButton;

        public GameObject localIcone;
        public GameObject remoteIcone;

        //OnMyName function will be invoked on clients when server change the value of playerName
        [SyncVar(hook = "OnMyName")]
        public string playerName = "";
        [SyncVar(hook = "OnMyColor")]
        public Color playerColor = Color.white;
        [SyncVar(hook = "OnMyCharacter")]
        public CharacterType playerCharacter = CharacterType.Observer;//dont forget to change character index

        public Color OddRowColor = new Color(250.0f / 255.0f, 250.0f / 255.0f, 250.0f / 255.0f, 1.0f);
        public Color EvenRowColor = new Color(180.0f / 255.0f, 180.0f / 255.0f, 180.0f / 255.0f, 1.0f);

        public Color JoinColor = new Color(255.0f/255.0f, 0.0f, 101.0f/255.0f,1.0f);
        public Color NotReadyColor = new Color(34.0f / 255.0f, 44 / 255.0f, 55.0f / 255.0f, 1.0f);
        public Color ReadyColor = new Color(0.0f, 204.0f / 255.0f, 204.0f / 255.0f, 1.0f);
        public Color TransparentColor = new Color(0, 0, 0, 0);
        public Sprite Sprite_Robot;
        public Sprite Sprite_Human;
        public Sprite Sprite_Zombie;
        public Sprite Sprite_Observer;

        //static Color OddRowColor = new Color(250.0f / 255.0f, 250.0f / 255.0f, 250.0f / 255.0f, 1.0f);
        //static Color EvenRowColor = new Color(180.0f / 255.0f, 180.0f / 255.0f, 180.0f / 255.0f, 1.0f);

        [Command]
        public void Cmd_verifyVersion(string version)
        {
            
            //Debug.Log("server version: "+Application.version+ "; client version: "+version);
            if(version != Application.version)
            {
                //Debug.Log("version different");
                FindObjectOfType<LobbyManager>().KickVersionDifferent(connectionToClient);
            }
        }
        
        void Update()
        {
            if (Input.GetKeyDown(toggleChat))
            {
                
                Message_board_watcher msg_watcher = FindObjectOfType<Message_board_watcher>();
                if (msg_watcher != null )//Start editing
                {
                    msg_watcher.Turnon_chat();
                }
            }
        }
        public void reset_character()
        {
            character_idx = 0;
            playerCharacter = CharacterType.Observer;
        }
        public override void OnClientEnterLobby()
        {
            base.OnClientEnterLobby();
            
            if (LobbyManager.s_Singleton != null) LobbyManager.s_Singleton.OnPlayersNumberModified(1);

            LobbyPlayerList._instance.AddPlayer(this);
            LobbyPlayerList._instance.DisplayDirectServerWarning(isServer && LobbyManager.s_Singleton.matchMaker == null);

            if (isLocalPlayer)
            {
                SetupLocalPlayer();
            }
            else
            {
                SetupOtherPlayer();
            }

            //setup the player data on UI. The value are SyncVar so the player
            //will be created with the right value currently on server
            OnMyName(playerName);
            OnMyColor(playerColor);
            OnMyCharacter(playerCharacter);
            
        }

        public override void OnStartAuthority()
        {
            base.OnStartAuthority();

            //if we return from a game, color of text can still be the one for "Ready"
            readyButton.transform.GetChild(0).GetComponent<Text>().color = Color.white;

           SetupLocalPlayer();
        }

        void ChangeReadyButtonColor(Color c)
        {
            ColorBlock b = readyButton.colors;
            b.normalColor = c;
            b.pressedColor = c;
            b.highlightedColor = c;
            b.disabledColor = c;
            readyButton.colors = b;
            
        }

        void SetupOtherPlayer()
        {
            nameInput.interactable = false;
            removePlayerButton.interactable = NetworkServer.active;

            ChangeReadyButtonColor(NotReadyColor);

            readyButton.transform.GetChild(0).GetComponent<Text>().text = "...";
            readyButton.interactable = false;

            OnClientReady(false);
        }

        void SetupLocalPlayer()
        {
            Cmd_verifyVersion(Application.version);
            nameInput.interactable = true;
            remoteIcone.gameObject.SetActive(false);
            localIcone.gameObject.SetActive(true);

            CheckRemoveButton();

            if (playerColor == Color.white)
                CmdColorChange();

            ChangeReadyButtonColor(JoinColor);

            readyButton.transform.GetChild(0).GetComponent<Text>().text = "JOIN";
            readyButton.interactable = true;

            //have to use child count of player prefab already setup as "this.slot" is not set yet
            if (playerName == "")
                CmdNameChanged("Player" + (LobbyPlayerList._instance.playerListContentTransform.childCount-1));

            //we switch from simple name display to name input
            colorButton.interactable = true;
            nameInput.interactable = true;
            characterButton.interactable = true;

            nameInput.onEndEdit.RemoveAllListeners();
            nameInput.onEndEdit.AddListener(OnNameChanged);

            colorButton.onClick.RemoveAllListeners();
            colorButton.onClick.AddListener(OnColorClicked);

            characterButton.onClick.RemoveAllListeners();
            characterButton.onClick.AddListener(OnCharacterClicked);

            readyButton.onClick.RemoveAllListeners();
            readyButton.onClick.AddListener(OnReadyClicked);

            //when OnClientEnterLobby is called, the loval PlayerController is not yet created, so we need to redo that here to disable
            //the add button if we reach maxLocalPlayer. We pass 0, as it was already counted on OnClientEnterLobby
            if (LobbyManager.s_Singleton != null) LobbyManager.s_Singleton.OnPlayersNumberModified(0);


            if (isServer)
            {
                FindObjectOfType<LobbyManager>().spawn_cvar();
            }
        }

        //This enable/disable the remove button depending on if that is the only local player or not
        public void CheckRemoveButton()
        {
            if (!isLocalPlayer)
                return;

            int localPlayerCount = 0;
            foreach (PlayerController p in ClientScene.localPlayers)
                localPlayerCount += (p == null || p.playerControllerId == -1) ? 0 : 1;

            removePlayerButton.interactable = localPlayerCount > 1;
        }

        public override void OnClientReady(bool readyState)
        {
            if (readyState)
            {
                ChangeReadyButtonColor(TransparentColor);

                Text textComponent = readyButton.transform.GetChild(0).GetComponent<Text>();
                textComponent.text = "READY";
                textComponent.color = ReadyColor;
                readyButton.interactable = false;
                colorButton.interactable = false;
                characterButton.interactable = false;
                nameInput.interactable = false;
            }
            else
            {
                ChangeReadyButtonColor(isLocalPlayer ? JoinColor : NotReadyColor);

                Text textComponent = readyButton.transform.GetChild(0).GetComponent<Text>();
                textComponent.text = isLocalPlayer ? "JOIN" : "...";
                textComponent.color = Color.white;
                readyButton.interactable = isLocalPlayer;
                colorButton.interactable = isLocalPlayer;
                characterButton.interactable = isLocalPlayer;
                nameInput.interactable = isLocalPlayer;
            }
        }
        
        public void OnPlayerListChanged(int idx)
        { 
            GetComponent<Image>().color = (idx % 2 == 0) ? EvenRowColor : OddRowColor;
        }

        ///===== callback from sync var

        public void OnMyName(string newName)
        {
            playerName = newName;
            nameInput.text = playerName;
        }

        public void OnMyColor(Color newColor)
        {
            playerColor = newColor;
            colorButton.GetComponent<Image>().color = newColor;
        }

        public void OnMyCharacter(CharacterType newCharacter)
        {
            playerCharacter = newCharacter;
            if(newCharacter == CharacterType.Robot)
            {
                (characterButton).GetComponent<Image>().sprite = Sprite_Robot;
            }
            else if(newCharacter == CharacterType.Human)
            {
                (characterButton).GetComponent<Image>().sprite = Sprite_Human;
            }
            else if(newCharacter == CharacterType.Zombie)
            {
                (characterButton).GetComponent<Image>().sprite = Sprite_Zombie;
            }
            else if (newCharacter == CharacterType.Observer)
            {
                (characterButton).GetComponent<Image>().sprite = Sprite_Observer;
            }
        }
        //===== UI Handler

        //Note that those handler use Command function, as we need to change the value on the server not locally
        //so that all client get the new value throught syncvar
        public void OnColorClicked()
        {
            CmdColorChange();
        }
        public void OnCharacterClicked()
        {
            CmdCharacterChange();
        }
        public void OnReadyClicked()
        {
            SendReadyToBeginMessage();
        }

        public void OnNameChanged(string str)
        {
            CmdNameChanged(str);
            if(!EventSystem.current.alreadySelecting)//remove this will cause error when selecting off input field
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            
        }

        public void OnRemovePlayerClick()
        {
            if (isLocalPlayer)
            {
                RemovePlayer();
            }
            else if (isServer)
                LobbyManager.s_Singleton.KickPlayer(connectionToClient);
        }
        

        public void ToggleJoinButton(bool enabled)
        {
            readyButton.gameObject.SetActive(enabled);
            waitingPlayerButton.gameObject.SetActive(!enabled);
        }

        [ClientRpc]
        public void RpcUpdateCountdown(int countdown)
        {
            LobbyManager.s_Singleton.countdownPanel.UIText.text = "Game Starting in " + countdown;
            LobbyManager.s_Singleton.countdownPanel.gameObject.SetActive(countdown != 0);
        }

        [ClientRpc]
        public void RpcUpdateRemoveButton()
        {
            CheckRemoveButton();
        }

        //====== Server Command
        
        [Command]
        public void CmdColorChange()
        {
            int idx = System.Array.IndexOf(CONSTANTS.PLAYERCOLORS, playerColor);

            int inUseIdx = _colorInUse.IndexOf(idx);

            if (idx < 0) idx = 0;

            idx = (idx + 1) % CONSTANTS.PLAYERCOLORS.Length;

            bool alreadyInUse = false;

            do
            {
                alreadyInUse = false;
                for (int i = 0; i < _colorInUse.Count; ++i)
                {
                    if (_colorInUse[i] == idx)
                    {//that color is already in use
                        alreadyInUse = true;
                        idx = (idx + 1) % CONSTANTS.PLAYERCOLORS.Length;
                    }
                }
            }
            while (alreadyInUse);

            if (inUseIdx >= 0)
            {//if we already add an entry in the colorTabs, we change it
                _colorInUse[inUseIdx] = idx;
            }
            else
            {//else we add it
                _colorInUse.Add(idx);
            }

            playerColor = CONSTANTS.PLAYERCOLORS[idx];
        }

        [Command]
        public void CmdCharacterChange()
        {
            Server_watcher cvar_watcher = Server_watcher.Singleton;
            if(cvar_watcher == null)
            {
                return;
            }
            character_idx++;
            if(character_idx >= CONSTANTS.map_characters[cvar_watcher.map].Length)
            {
                character_idx = 0;
            }
            playerCharacter = CONSTANTS.map_characters[cvar_watcher.map][character_idx];
        }
        [Command]
        public void Cmd_send_message(string msg)
        {
            Server_watcher.Singleton.sv_sendMessage(gameObject, msg, false);
        }
        [Command]
        public void CmdNameChanged(string name)
        {
            Server_watcher cvar_watcher = Server_watcher.Singleton;
            if (cvar_watcher != null && playerName != name && playerName != "")
            {
                cvar_watcher.sv_sendMessage(gameObject, "Player '" + playerName + "' has changed the name to '" + name+"'", true);
            }
            playerName = name;
        }

        //Cleanup thing when get destroy (which happen when client kick or disconnect)
        public void OnDestroy()
        {
            LobbyPlayerList._instance.RemovePlayer(this);
            if (LobbyManager.s_Singleton != null) LobbyManager.s_Singleton.OnPlayersNumberModified(-1);

            int idx = System.Array.IndexOf(CONSTANTS.PLAYERCOLORS, playerColor);

            if (idx < 0)
                return;

            for (int i = 0; i < _colorInUse.Count; ++i)
            {
                if (_colorInUse[i] == idx)
                {//that color is already in use
                    _colorInUse.RemoveAt(i);
                    break;
                }
            }
        }
    }
}
