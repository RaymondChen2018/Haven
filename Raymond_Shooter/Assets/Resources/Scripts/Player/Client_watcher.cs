using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Background: This 
/// 
/// 
/// In this networked game, a client_watcher is a singleton gameobject("object", in Unity's terms) that performs 
/// a variety of IN-GAME network relays to synchronize functionalities between the server and its clients.
/// 
/// Instead of implementing these functionalities as some dedicated gameobjects themselves, I collected many of 
/// them and shove them into this network view in order to minimize the number of networked objects in the game.
/// 
/// At the stage of any match instance, there is a server_watcher and a client_watcher. They both exist on the 
/// server and on the client and server as network views. Difference is: The client_watcher only lives as long 
/// as a match last. It is created along with other gameobjects when a match scene is loaded and destroyed 
/// like others when the match ends; Server_watcher, on the other hand, lives since when server/client enters 
/// a lobby and over-sees the match along the way. 
/// 
/// Having a larger "scope", server_watcher allows for supplying data to gameobjects that require server parameters 
/// upon match initialization, especially on over-seeing the initial loading process where everyone already has
/// a synchronized server_watcher but not else. Client-watcher could not guarantee that since instances of it 
/// at different end can be created at different time frame and cause disagreement.
/// before other gameobjects.
/// </summary>
public class Client_watcher : NetworkBehaviour {

    //Singleton
    static public Client_watcher Singleton;

    //Sun Light Time
    [Tooltip("The sun object in the scene; Used for updating environment lighting")]
    public Sun_light_real_time sun;
    [SyncVar(hook = "Hook_Sun")] float sun_time = 0;

    //Time Scale
    [SyncVar(hook = "Hook_TimeScale")] float timeScale = 1;

    //Weapon Limit
    List<Ammo_generic> Ammoboxes = new List<Ammo_generic>();
    List<Gun_generic> Guns = new List<Gun_generic>();
    static int max_guns = 15;
    static int max_ammobox = 25;

    //Initialization
    void Awake()
    {
        Singleton = this;
    }


    //Limit the number of items
    [ServerCallback]
    public void register_item(IEquiptable item)
    {
        if (item == null)
        {
            return;
        }

        switch (item.getType())
        {
            case Equipable_generic.ITEM_TYPE.ammo:
                if (Ammoboxes.Contains((Ammo_generic)item)) return;
                Ammoboxes.Add((Ammo_generic)item);
                //Remove additional
                if (Ammoboxes.Count > max_ammobox)
                {
                    NetworkServer.Destroy(Ammoboxes[0].gameObject);
                    Ammoboxes.RemoveAt(0);
                }
                break;

            case Equipable_generic.ITEM_TYPE.gun:
                if (Guns.Contains((Gun_generic)item)) return;
                Guns.Add((Gun_generic)item);
                //Remove additional
                if (Guns.Count > max_guns)
                {
                    NetworkServer.Destroy(Guns[0].gameObject);
                    Guns.RemoveAt(0);
                }
                break;
        }
    }
    [ServerCallback]
    public void deregister_item(IEquiptable item)
    {
        if (item == null)
        {
            return;
        }

        switch (item.getType())
        {
            case Equipable_generic.ITEM_TYPE.ammo:
                if(!Ammoboxes.Contains((Ammo_generic)item)) return;
                Ammoboxes.Remove((Ammo_generic)item);
                break;

            case Equipable_generic.ITEM_TYPE.gun:
                if (!Guns.Contains((Gun_generic)item)) return;
                Guns.Remove((Gun_generic)item);
                break;
        }
    }
    [ServerCallback]
    public void prolong_item(IEquiptable item)
    {
        switch (item.getType())
        {
            case Equipable_generic.ITEM_TYPE.ammo:
                Ammo_generic ammo = (Ammo_generic)item;
                if (Ammoboxes.Contains(ammo))
                {
                    Ammoboxes.Remove(ammo);
                    Ammoboxes.Add(ammo);
                }
                break;

            case Equipable_generic.ITEM_TYPE.gun:
                Gun_generic gun = (Gun_generic)item;
                if (Guns.Contains(gun))
                {
                    Guns.Remove(gun);
                    Guns.Add(gun);
                }
                break;
        }
        
    }

    /// <summary>
    /// Spawn blood effects to all clients every 0.2 seconds, to avoid a high flux of RPC calls transmited every frame during a hot battle
    /// </summary>
    /// <param name="victim">Whose blood template will be used</param>
    /// <param name="pox">A compressed list of cooordinates of blood fx; x1, y1, x2, y2, x3... </param>
    /// <param name="angle">A compressed list of blood fx angle</param>
    /// <param name="isheadshot">Is this a headshot?</param>
    [ClientRpc(channel = 1)]
    public void Rpc_spawn_blood(GameObject[] victim, short[] pox, short[] angle, bool[] isheadshot)
    {
        for (int i = 0; i < victim.Length && victim[i] != null; i++)
        {
            //Decompress coordinates
            float x = pox[i];
            x /= CONSTANTS.SYNC_POS_MUTIPLIER;
            float y = pox[pox.Length / 2 + i];
            y /= CONSTANTS.SYNC_POS_MUTIPLIER;
            Vector2 bleed_pos = new Vector2(x, y);

            //Decompress angle
            float angle_decompressed = CONSTANTS.seed_short_to_float(angle[i], 360);

            victim[i].GetComponent<Body_generic>().bleed(bleed_pos, angle_decompressed, isheadshot[i]);
        }
    }

    //Modify time scale for server and client
    [ServerCallback]
    public void sv_timescale(float scale)
    {
        //Set time scale
        Time.timeScale = scale;
        Time.fixedDeltaTime = CONSTANTS.FIXED_TIMESTEP * scale;
        timeScale = scale;
    }
    public void Hook_TimeScale(float value)
    {
        //Set time scale
        Time.timeScale = value;
        Time.fixedDeltaTime = CONSTANTS.FIXED_TIMESTEP * value;
        //Pitch down volume
        AudioSource[] audios = FindObjectsOfType<AudioSource>();
        for (int i = 0; i < audios.Length; i++)
        {
            audios[i].pitch = value;
        }
    }

    //Real-time sun lighting
    public void setSunTime(float sunTime)
    {
        sun_time = sunTime;
    }
    public void Hook_Sun(float sun_value)
    {
        sun_time = sun_value;
        float sun_offset = sun_value - 0.5f;
        if(sun_offset < 0)
        {
            sun_offset += 1;
        }
        
        sun.update_sun(sun_offset);
    }

    //Destructor
    void OnDestroy()
    {
        Singleton = null;
    }
}
