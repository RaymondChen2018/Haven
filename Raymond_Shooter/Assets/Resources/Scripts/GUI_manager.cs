using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GUI_manager : MonoBehaviour {
    public Text text_counter_human;
    public Text text_counter_robot;
    public Text text_counter_zombie;
    public HealthBar_generic observer_healthbar;
    public Text debug_info;
    public GameObject waitForPlayers;
    public GameObject scoreBoard;
    public GameObject scoreToken_template;
    public List<ScoreToken_generic> scoreTokens;
    public List<Player_generic> clients;
    [HideInInspector] public Server_watcher cvar_watcher;
    // Use this for initialization
    void Start () {
        scoreTokens = new List<ScoreToken_generic>();
    }
	
    public void OnClientReady()
    {
        if(cvar_watcher.map_type == CONSTANTS.MAP_TYPE.PVP)
        {
            //Initialize respawn token count
            Team_watcher[] teams = FindObjectsOfType<Team_watcher>();
            for (int i = 0; i < teams.Length; i++)
            {
                if (teams[i].race == Body_generic.Character_type.Human)
                {
                    teams[i].text_counter = text_counter_human;
                }
                else if (teams[i].race == Body_generic.Character_type.Robot)
                {
                    teams[i].text_counter = text_counter_robot;
                }
                else if (teams[i].race == Body_generic.Character_type.Zombie)
                {
                    teams[i].text_counter = text_counter_zombie;
                }

                teams[i].text_counter.text = teams[i].respawns_token.ToString();
            }
        }
        else if(cvar_watcher.map_type == CONSTANTS.MAP_TYPE.Objective)
        {
            Destroy(text_counter_human.gameObject);
            Destroy(text_counter_robot.gameObject);
            Destroy(text_counter_zombie.gameObject);
        }
        
        clients = new List<Player_generic>(FindObjectsOfType<Player_generic>());
        for(int i = 0; i < clients.Count; i++)
        {
            clients[i].GUI_token = Instantiate(scoreToken_template, scoreBoard.transform).GetComponent<ScoreToken_generic>();
            //clients[i].GUI_token.Name.text = clients[i].character_name;
            scoreTokens.Add(clients[i].GUI_token);
        }
    }
    public void scoreBoardUpdate()
    {
        if(clients == null)
        {
            return;
        }

        for(int i = 0; i < clients.Count; i++)
        {
            scoreTokens[i].Name.text = clients[i].character_name;
            if(clients[i].character_type == Body_generic.Character_type.Human)
            {
                scoreTokens[i].Name.color = clients[i].GetComponent<Body_generic>().skin_color;
                scoreTokens[i].Kill1.enabled = false;
                scoreTokens[i].Kill2.text = clients[i].kill2.ToString();
                scoreTokens[i].Kill3.text = clients[i].kill3.ToString();
                scoreTokens[i].Deaths.text = clients[i].deaths.ToString();
                scoreTokens[i].Latency.text = clients[i].latency_sv.ToString();
            }
            else if (clients[i].character_type == Body_generic.Character_type.Robot)
            {
                scoreTokens[i].Name.color = clients[i].GetComponent<Body_generic>().skin_color;
                scoreTokens[i].Kill1.text = clients[i].kill1.ToString();
                scoreTokens[i].Kill2.enabled = false;
                scoreTokens[i].Kill3.text = clients[i].kill3.ToString();
                scoreTokens[i].Deaths.text = clients[i].deaths.ToString();
                scoreTokens[i].Latency.text = clients[i].latency_sv.ToString();
            }
            else if (clients[i].character_type == Body_generic.Character_type.Zombie)
            {
                scoreTokens[i].Name.color = clients[i].GetComponent<Body_generic>().skin_color;
                scoreTokens[i].Kill1.text = clients[i].kill1.ToString();
                scoreTokens[i].Kill2.text = clients[i].kill2.ToString();
                scoreTokens[i].Kill3.enabled = false;
                scoreTokens[i].Deaths.text = clients[i].deaths.ToString();
                scoreTokens[i].Latency.text = clients[i].latency_sv.ToString();
            }
            else
            {
                scoreTokens[i].Name.color = Color.gray;
                scoreTokens[i].Kill1.enabled = false;
                scoreTokens[i].Kill2.enabled = false;
                scoreTokens[i].Kill3.enabled = false;
                scoreTokens[i].Deaths.enabled = false;
                scoreTokens[i].Latency.enabled = false;
            }
        }
    }
	// Update is called once per frame
	void Update () {
        if(clients != null)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                if (clients[i] == null)
                {
                    Destroy(scoreTokens[i].gameObject);
                    scoreTokens.RemoveAt(i);
                    clients.RemoveAt(i);
                }
            }
        }
        
	}
}
