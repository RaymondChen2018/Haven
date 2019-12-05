// Info_human_spawn
using UnityEngine;

public class Info_human_spawn : MonoBehaviour
{
    Server_watcher cvar_watcher;
    public Structure_generic structure;
	private void Start()
	{
        cvar_watcher = FindObjectOfType<Server_watcher>();
        cvar_watcher.comp_spawnPoint_manager.addSpawnPointHuman(this);
    }
    private void OnDestroy()
    {
        cvar_watcher.comp_spawnPoint_manager.removeSpawnPointHuman(this);
    }
    
}
