// Info_robot_spawn
using UnityEngine;

public class Info_robot_spawn : MonoBehaviour
{
    Server_watcher cvar_watcher;
    public Structure_generic structure;
    private void Start()
	{
        cvar_watcher = FindObjectOfType<Server_watcher>();
        cvar_watcher.comp_spawnPoint_manager.addSpawnPointRobot(this);
	}
    private void OnDestroy()
    {
        cvar_watcher.comp_spawnPoint_manager.removeSpawnPointRobot(this);
    }
    
}
