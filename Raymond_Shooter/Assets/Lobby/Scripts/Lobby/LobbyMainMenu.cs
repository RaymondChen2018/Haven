using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Prototype.NetworkLobby
{
    //Main menu, mainly only a bunch of callback called by the UI (setup throught the Inspector)
    public class LobbyMainMenu : MonoBehaviour 
    {
        public LobbyManager lobbyManager;

        public RectTransform lobbyServerList;
        public RectTransform lobbyPanel;

        public InputField ipInput;
        public InputField matchNameInput;

        public void OnEnable()
        {
            lobbyManager.topPanel.ToggleVisibility(false);

            ipInput.onEndEdit.RemoveAllListeners();
            ipInput.onEndEdit.AddListener(onEndEditIP);

            matchNameInput.onEndEdit.RemoveAllListeners();
            matchNameInput.onEndEdit.AddListener(onEndEditGameName);
        }


        public void OnClick_display_match_custom()
        {
            lobbyManager.ChangeTo(lobbyManager.matchname_panel);
            //matchname_panel.SetActive(true);
        }
        public void OnClick_close_match_custom()
        {
            lobbyManager.ChangeTo(GetComponent<RectTransform>());
            //matchname_panel.SetActive(false);
        }


        public void OnClickExit()
        {
            Application.Quit();
        }


        public void OnClickHost()
        {
            lobbyManager.StartHost();
        }

        public void OnClickHotJoin()
        {

            lobbyManager.ChangeTo(lobbyPanel);


            lobbyManager.networkAddress = ipInput.text;

            Network.Connect(lobbyManager.networkAddress, 7777);

            lobbyManager.backDelegate = lobbyManager.StopClientClbk;
            lobbyManager.DisplayIsConnecting();

            lobbyManager.SetServerInfo("Connecting...", lobbyManager.networkAddress);
            //Debug.LogError("goto: " + lobbyManager.networkAddress);
        }
        public void OnClickJoin()
        {
            connect_to(ipInput.text);
        }
        public void OnClickJoinDefault()
        {
            connect_to("52.53.114.166");
        }
        public void OnClickJoinLocal()
        {
            connect_to("127.0.0.1");
        }
        void connect_to(string address = "")
        {
            lobbyManager.ChangeTo(lobbyPanel);


            lobbyManager.networkAddress = address;//ipInput.text;
            
            lobbyManager.StartClient();

            lobbyManager.backDelegate = lobbyManager.StopClientClbk;
            lobbyManager.DisplayIsConnecting();

            lobbyManager.SetServerInfo("Connecting...", lobbyManager.networkAddress);
            //Debug.LogError("goto: " + lobbyManager.networkAddress);
        }

        public void OnClickDedicated()
        {
            lobbyManager.ChangeTo(null);
            lobbyManager.StartServer();
            
            lobbyManager.backDelegate = lobbyManager.StopServerClbk;

            lobbyManager.SetServerInfo("Dedicated Server", lobbyManager.networkAddress);
            lobbyManager.spawn_cvar();
        }

        public void OnClickCreateMatchmakingGame()
        {
            lobbyManager.StartMatchMaker();
            
            string match_name = "RS Match";
            if(matchNameInput.text != "")
            {
                match_name = matchNameInput.text;
            }
            lobbyManager.matchMaker.CreateMatch(
                match_name,
                (uint)lobbyManager.maxPlayers,
                true,
				"", "", "", 0, 0,
				lobbyManager.OnMatchCreate);

            lobbyManager.backDelegate = lobbyManager.StopHost;
            lobbyManager._isMatchmaking = true;
            lobbyManager.DisplayIsConnecting();

            lobbyManager.SetServerInfo("Matchmaker Host", lobbyManager.matchHost);

            //lobbyManager.spawn_dummy();
        }

        public void OnClickOpenServerList()
        {
            lobbyManager.StartMatchMaker();
            lobbyManager.backDelegate = lobbyManager.SimpleBackClbk;
            lobbyManager.ChangeTo(lobbyServerList);
        }

        void onEndEditIP(string text)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                OnClickJoin();
            }
        }

        void onEndEditGameName(string text)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                OnClickCreateMatchmakingGame();
            }
        }

    }
}
