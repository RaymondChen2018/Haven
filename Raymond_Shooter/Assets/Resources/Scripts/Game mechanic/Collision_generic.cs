using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collision_generic : MonoBehaviour {

    /// <summary>
    /// Only collide with objects with tag (under the layer)
    /// </summary>
    public string collideTag;
    Server_watcher cvar_watcher;

    void Start()
    {
        cvar_watcher = Server_watcher.Singleton;
        if(cvar_watcher.local_player == null)
        {
            cvar_watcher.onClientReady.Add(OnClientReady);
            return;
        }
        examine_collision();
    }
    
    void examine_collision()
    {
        if(cvar_watcher.local_player == null)
        {
            return;
        }
        int layermask = Physics2D.GetLayerCollisionMask(cvar_watcher.local_player.gameObject.layer);
        if (layermask == (layermask | (1 << gameObject.layer)))
        {
            GetComponent<SpriteMask>().enabled = true;
        }
    }
    public void OnClientReady()
    {
        examine_collision();
    }
    void OnCollisionEnter2D(Collision2D collision2D)
    {
        if (collideTag != "" && collision2D.gameObject.tag != collideTag)
        {
            Physics2D.IgnoreCollision(collision2D.collider, GetComponent<Collider2D>());
        }
    }
}
