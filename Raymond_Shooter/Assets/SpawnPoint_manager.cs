using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint_manager : MonoBehaviour {
    [SerializeField] private Server_watcher comp_server_watcher;

    List<Info_robot_spawn> spawnPoint_robot = new List<Info_robot_spawn>();
    List<Info_human_spawn> spawnPoint_human = new List<Info_human_spawn>();
    List<Info_zombie_spawn> spawnPoint_zombie = new List<Info_zombie_spawn>();
    public int human_heat_spawn = -1;
    public int robot_heat_spawn = -1;
    public int zombie_heat_spawn = -1;
    float time_to_order_heatspawn = 0;



    // Use this for initialization
    void Start () {
        if (!comp_server_watcher.isServer)
        {
            Destroy(this);
        }
	}
	
	// Update is called once per frame
	void Update () {
        //Team base battle level, respawn at the most heated base
        if (Time.realtimeSinceStartup > time_to_order_heatspawn)
        {
            time_to_order_heatspawn = Time.realtimeSinceStartup + CONSTANTS.STRUCTURE_ORDER_INTERVAL;
            int max_alert = 0;
            human_heat_spawn = -1;
            for (int i = 0; i < spawnPoint_human.Count; i++)
            {
                if (spawnPoint_human[i].structure != null && spawnPoint_human[i].structure.alert_level > max_alert)
                {
                    human_heat_spawn = i;
                    max_alert = spawnPoint_human[i].structure.alert_level;
                }
            }
            max_alert = 0;
            robot_heat_spawn = -1;
            for (int i = 0; i < spawnPoint_robot.Count; i++)
            {
                if (spawnPoint_robot[i].structure != null && spawnPoint_robot[i].structure.alert_level > max_alert)
                {
                    robot_heat_spawn = i;
                    max_alert = spawnPoint_robot[i].structure.alert_level;
                }
            }
            max_alert = 0;
            zombie_heat_spawn = -1;
            for (int i = 0; i < spawnPoint_zombie.Count; i++)
            {
                if (spawnPoint_zombie[i].structure != null && spawnPoint_zombie[i].structure.alert_level > max_alert)
                {
                    zombie_heat_spawn = i;
                    max_alert = spawnPoint_zombie[i].structure.alert_level;
                }
            }
        }
    }

    public void clearAll()
    {
        spawnPoint_robot.Clear();
        spawnPoint_human.Clear();
        spawnPoint_zombie.Clear();
    }

    public void addSpawnPointRobot(Info_robot_spawn spawnPoint)
    {
        spawnPoint_robot.Add(spawnPoint);
    }
    public void addSpawnPointHuman(Info_human_spawn spawnPoint)
    {
        spawnPoint_human.Add(spawnPoint);
    }
    public void addSpawnPointZombie(Info_zombie_spawn spawnPoint)
    {
        spawnPoint_zombie.Add(spawnPoint);
    }

    public void removeSpawnPointRobot(Info_robot_spawn spawnPoint)
    {
        spawnPoint_robot.Remove(spawnPoint);
    }
    public void removeSpawnPointHuman(Info_human_spawn spawnPoint)
    {
        spawnPoint_human.Remove(spawnPoint);
    }
    public void removeSpawnPointZombie(Info_zombie_spawn spawnPoint)
    {
        spawnPoint_zombie.Remove(spawnPoint);
    }

    public int getSpawnPointCountRobot()
    {
        return spawnPoint_robot.Count;
    }
    public int getSpawnPointCountHuman()
    {
        return spawnPoint_human.Count;
    }
    public int getSpawnPointCountZombie()
    {
        return spawnPoint_zombie.Count;
    }

    public BoxCollider2D getSpawnAreaRobot()
    {
        BoxCollider2D area = null;
        if (spawnPoint_robot != null && spawnPoint_robot.Count > 0)
        {
            if (robot_heat_spawn != -1 && robot_heat_spawn < spawnPoint_robot.Count)
            {
                area = spawnPoint_robot[robot_heat_spawn].GetComponent<BoxCollider2D>();
            }
            else
            {
                area = spawnPoint_robot[UnityEngine.Random.Range(0, spawnPoint_robot.Count)].GetComponent<BoxCollider2D>();
            }
        }
        return area;
    }
    public BoxCollider2D getSpawnAreaHuman()
    {
        BoxCollider2D area = null;
        if (spawnPoint_human != null && spawnPoint_human.Count > 0)
        {
            if (human_heat_spawn != -1 && human_heat_spawn < spawnPoint_human.Count)
            {
                area = spawnPoint_human[human_heat_spawn].GetComponent<BoxCollider2D>();
            }
            else
            {
                area = spawnPoint_human[UnityEngine.Random.Range(0, spawnPoint_human.Count)].GetComponent<BoxCollider2D>();
            }
        }
        return area;
    }
    public BoxCollider2D getSpawnAreaZombie()
    {
        BoxCollider2D area = null;
        if (spawnPoint_zombie != null && spawnPoint_zombie.Count > 0)
        {
            if (zombie_heat_spawn != -1 && zombie_heat_spawn < spawnPoint_zombie.Count)
            {
                area = spawnPoint_zombie[zombie_heat_spawn].GetComponent<BoxCollider2D>();
            }
            else
            {
                area = spawnPoint_zombie[UnityEngine.Random.Range(0, spawnPoint_zombie.Count)].GetComponent<BoxCollider2D>();
            }
        }
        return area;
    }
}
