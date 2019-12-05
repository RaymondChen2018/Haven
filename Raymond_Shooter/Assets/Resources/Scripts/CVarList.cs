using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class CVarList : MonoBehaviour {
    public Server_watcher cvar_watcher;

    public Text Text_friendlyfire;
    public Text Text_los;
    public InputField Text_human_limit;
    public InputField Text_robot_limit;
    public InputField Text_zombie_limit;
    public InputField Text_respawn_time;
    public InputField Text_human_respawns;
    public InputField Text_robot_respawns;
    public InputField Text_zombie_respawns;
    public InputField Text_max_skill_death;
    public InputField Text_start_money;
    public Text Text_teamtransparent;

    //Load cvar to text board
    public void Load_cvar()
    {
        Prototype.NetworkLobby.LobbyManager lobby_manager = FindObjectOfType<Prototype.NetworkLobby.LobbyManager>();
        cvar_watcher = lobby_manager.cvar_watcher;

        if(cvar_watcher != null)
        {
            if (cvar_watcher.isServer)
            {
                //Turn on cvar inputs
                Selectable[] cvar_selects = lobby_manager.Cvar_list_panel.GetComponentsInChildren<Selectable>();
                for(int i = 0; i < cvar_selects.Length; i++)
                {
                    cvar_selects[i].interactable = true;
                }
            }
            else
            {
                //Turn on cvar inputs
                Selectable[] cvar_selects = lobby_manager.Cvar_list_panel.GetComponentsInChildren<Selectable>();
                for (int i = 0; i < cvar_selects.Length; i++)
                {
                    cvar_selects[i].interactable = false;
                }
            }
        }
        

        Text_human_limit.text = cvar_watcher.team_num_human.ToString();
        Text_robot_limit.text = cvar_watcher.team_num_robot.ToString();
        Text_zombie_limit.text = cvar_watcher.team_num_zombie.ToString();
        Text_respawn_time.text = cvar_watcher.respawn_time.ToString();
        Text_human_respawns.text = cvar_watcher.tickets_human.ToString();
        Text_robot_respawns.text = cvar_watcher.tickets_robot.ToString();
        Text_zombie_respawns.text = cvar_watcher.tickets_zombie.ToString();
        Text_max_skill_death.text = cvar_watcher.maxSkillSpentPerDeath.ToString();
        Text_start_money.text = cvar_watcher.insurance_money.ToString();
        if (cvar_watcher.allyBulletPassThru)
        {
            Text_friendlyfire.text = "Off";
        }
        else
        {
            Text_friendlyfire.text = "On";
        }

        if (cvar_watcher.losVision)
        {
            Text_los.text = "On";
        }
        else
        {
            Text_los.text = "Off";
        }
        if (cvar_watcher.team_transparent)
        {
            Text_teamtransparent.text = "Off";
        }
        else
        {
            Text_teamtransparent.text = "On";
        }
    }

    public void FriendlyFire()
    {

        cvar_watcher.allyBulletPassThru = !cvar_watcher.allyBulletPassThru;

        if (cvar_watcher.allyBulletPassThru)
        {
            Text_friendlyfire.text = "Off";
        }
        else
        {
            Text_friendlyfire.text = "On";
        }
        //cvar_watcher.Msg_proxy(null, "FriendlyFire is set to "+ Text_friendlyfire.text, true);

    }
    public void TeamTransparent()
    {

        cvar_watcher.team_transparent = !cvar_watcher.team_transparent;

        if (cvar_watcher.team_transparent)
        {
            Text_teamtransparent.text = "Off";
        }
        else
        {
            Text_teamtransparent.text = "On";
        }
        

    }

    public void LOSEnable()
    {
        cvar_watcher.losVision = !cvar_watcher.losVision;
        
        if (cvar_watcher.losVision)
        {
            Text_los.text = "On";
        }
        else
        {
            Text_los.text = "Off";
        }

        //cvar_watcher.Msg_proxy(null, "Line-Of-Sight is set to " + Text_los.text, true);
        
    }

    public void RespawnTime(string str)
    {
        int time = cvar_watcher.respawn_time;
        if(int.TryParse(str, out time))
        {
            /*
            if(cvar_watcher.respawn_time != time)
            {
                cvar_watcher.Msg_proxy(null, "Respawn time is set to " + time + " sec", true);
            }
            */
            cvar_watcher.respawn_time = time;
        }
        else
        {
            Text_respawn_time.text = cvar_watcher.respawn_time.ToString();
        }
    }

    public void HumanLimit(string str)
    {
        short limit = cvar_watcher.team_num_human;
        if (short.TryParse(str, out limit))
        {
            /*
            if (cvar_watcher.team_num_human != limit)
            {
                cvar_watcher.Msg_proxy(null, "Human count is set to " + limit, true);
            }
            */
            cvar_watcher.team_num_human = limit;
        }
        else
        {
            Text_human_limit.text = cvar_watcher.team_num_human.ToString();
        }
    }
    public void RobotLimit(string str)
    {
        short limit = cvar_watcher.team_num_robot;
        if (short.TryParse(str, out limit))
        {
            /*
            if (cvar_watcher.team_num_robot != limit)
            {
                cvar_watcher.Msg_proxy(null, "Robot count is set to " + limit, true);
            }
            */
            cvar_watcher.team_num_robot = limit;
        }
        else
        {
            Text_robot_limit.text = cvar_watcher.team_num_robot.ToString();
        }
    }
    public void ZombieLimit(string str)
    {
        short limit = cvar_watcher.team_num_zombie;
        if (short.TryParse(str, out limit))
        {
            /*
            if (cvar_watcher.team_num_zombie != limit)
            {
                cvar_watcher.Msg_proxy(null, "Zombie count is set to " + limit, true);
            }
            */
            cvar_watcher.team_num_zombie = limit;
        }
        else
        {
            Text_zombie_limit.text = cvar_watcher.team_num_zombie.ToString();
        }
    }

    public void HumanRespawns(string str)
    {
        short respawns = cvar_watcher.tickets_human;
        if (short.TryParse(str, out respawns))
        {
            /*
            if (cvar_watcher.tickets_human != respawns)
            {
                cvar_watcher.Msg_proxy(null, "Human respawns is set to " + respawns, true);
            }
            */
            cvar_watcher.tickets_human = respawns;
        }
        else
        {
            Text_human_respawns.text = cvar_watcher.tickets_human.ToString();
        }
    }
    public void RobotRespawns(string str)
    {
        short respawns = cvar_watcher.tickets_robot;
        if (short.TryParse(str, out respawns))
        {
            /*
            if (cvar_watcher.tickets_robot != respawns)
            {
                cvar_watcher.Msg_proxy(null, "Robot respawns is set to " + respawns, true);
            }
            */
            cvar_watcher.tickets_robot = respawns;
        }
        else
        {
            Text_robot_respawns.text = cvar_watcher.tickets_robot.ToString();
        }
    }
    public void ZombieRespawns(string str)
    {
        short respawns = cvar_watcher.tickets_zombie;
        if (short.TryParse(str, out respawns))
        {
            /*
            if (cvar_watcher.tickets_zombie != respawns)
            {
                cvar_watcher.Msg_proxy(null, "Zombie respawns is set to " + respawns, true);
            }
            */
            cvar_watcher.tickets_zombie = respawns;
        }
        else
        {
            Text_zombie_respawns.text = cvar_watcher.tickets_zombie.ToString();
        }
    }

    public void MaxSkillDeath(string str)
    {
        byte max = cvar_watcher.maxSkillSpentPerDeath;
        if (byte.TryParse(str, out max))
        {
            /*
            if (cvar_watcher.maxSkillSpentPerDeath != max)
            {
                cvar_watcher.Msg_proxy(null, "Max upgrades per death is set to " + max, true);
            }
            */
            cvar_watcher.maxSkillSpentPerDeath = max;
        }
        else
        {
            Text_max_skill_death.text = cvar_watcher.maxSkillSpentPerDeath.ToString();
        }
    }
    public void StartMoney(string str)
    {
        int money = cvar_watcher.insurance_money;
        if (int.TryParse(str, out money))
        {
            /*
            if (cvar_watcher.tickets_human != respawns)
            {
                cvar_watcher.Msg_proxy(null, "Human respawns is set to " + respawns, true);
            }
            */
            cvar_watcher.insurance_money = money;
        }
        else
        {
            Text_start_money.text = cvar_watcher.insurance_money.ToString();
        }
    }
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
