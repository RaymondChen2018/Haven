// Info_zombie_spawn
using UnityEngine;

public class Info_zombie_spawn : MonoBehaviour
{
    Server_watcher cvar_watcher;
    public Structure_generic structure;
    private void Start()
	{
        cvar_watcher = FindObjectOfType<Server_watcher>();
        cvar_watcher.comp_spawnPoint_manager.addSpawnPointZombie(this);
    }
    private void OnDestroy()
    {
        cvar_watcher.comp_spawnPoint_manager.removeSpawnPointZombie(this);
    }
}
