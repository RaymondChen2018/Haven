using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Message_board_watcher : MonoBehaviour {

    public GameObject text_board;
    public GameObject Chat_box;
    public int max_lines = 7;
    [HideInInspector] public bool isEditingMsg = false;

    public InputField inputfield;
    List<Msg_generic> text_list = new List<Msg_generic>();
    Server_watcher cvar_watcher;
    public GameObject Msg_template;

    
	// Use this for initialization
	void Start () {
		cvar_watcher = GetComponent<Server_watcher>();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    public void Turnon_chat()
    {
        if(isEditingMsg || (EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() != null))
        {
            return;
        }
        isEditingMsg = true;
        Chat_box.SetActive(true);

        EventSystem.current.SetSelectedGameObject(inputfield.gameObject);
        if (inputfield != null)
        {
            PointerEventData auto_pointer = new PointerEventData(EventSystem.current);
            inputfield.OnPointerClick(auto_pointer);
        }
        for(int i = 0; i < text_list.Count; i++)
        {
            text_list[i].wake();
        }
    }

    public void OnEndEditMsg(string message)
    {
        for (int i = 0; i < text_list.Count; i++)
        {
            text_list[i].off();
        }
        if (message != "")
        {
            
            Prototype.NetworkLobby.LobbyPlayer[] lobby_players = FindObjectsOfType<Prototype.NetworkLobby.LobbyPlayer>();
            if (lobby_players != null && lobby_players.Length > 0)
            {
                for (int i = 0; i < lobby_players.Length; i++)
                {
                    if (lobby_players[i].isLocalPlayer)
                    {
                        lobby_players[i].Cmd_send_message(message);
                        break;
                    }
                }
            }
            Player_generic[] players = FindObjectsOfType<Player_generic>();
            if (players != null && players.Length > 0)
            {
                for (int i = 0; i < players.Length; i++)
                {
                    if (players[i].isLocalPlayer)
                    {
                        players[i].Cmd_send_message(message);
                        break;
                    }
                }
            }
        }

        inputfield.text = "";
        Chat_box.SetActive(false);
        if(!EventSystem.current.alreadySelecting)//remove this will cause error when selecting off input field
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        
        isEditingMsg = false;
        
    }
    public void Server_say(string message)
    {
        insertTextBoard(message, Color.yellow, FontStyle.Italic);
    }
    public void Say(GameObject player, string message)
    {

        string player_name = "Unknown";
        if(player != null)
        {
            if(player.GetComponent<Player_generic>() != null)//ingame chat
            {
                Player_generic sayer = player.GetComponent<Player_generic>();
                Body_generic.Character_type sayer_team = sayer.character_type;
                if(cvar_watcher.team_transparent && sayer_team != cvar_watcher.local_character_type)
                {
                    return;
                }
                player_name = sayer.character_name;

            }
            else
            {
                player_name = player.GetComponent<Prototype.NetworkLobby.LobbyPlayer>().playerName;
            }
        }
        insertTextBoard(player_name + ": " + message, Color.white, FontStyle.Normal);
    }
    /// <summary>
    /// Place text in the text box, dont care any formatting
    /// </summary>
    /// <param name="text"></param>
    void insertTextBoard(string text, Color color, FontStyle style)
    {
        Msg_generic Msg = Instantiate(Msg_template, text_board.transform).GetComponent<Msg_generic>();
        Msg.GetComponent<Text>().text = text;
        Msg.GetComponent<Text>().color = color;
        Msg.GetComponent<Text>().fontStyle = style;
        text_list.Add(Msg);
        //Debug.LogError("len: "+ text_list.Count);
        if (text_list.Count > max_lines)
        {
            Destroy(text_list[0].gameObject);
            text_list.RemoveAt(0);
        }
    }
}
