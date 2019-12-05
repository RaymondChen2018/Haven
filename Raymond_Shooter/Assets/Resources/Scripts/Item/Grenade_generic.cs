using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Grenade_generic : NetworkBehaviour, IEquiptable {
    public int exp_delay = 4;
    public float power = 1000;//physical
    public float thermal;      //thermal
    public float radius = 10;
    public float toss_force_multiplier = 365;
    public ushort ammo = 1;
    public ushort grenade_weight;
    public ushort grenade_size;
    public GrenadeType grenadetype;
    public string eject;
    public GameObject explosion;
    public GameObject activator;

    [HideInInspector] [SyncVar] public bool pulled = false;
    [HideInInspector] public bool exploded = false;
    [HideInInspector] public float time_to_explode = 0;

    public enum GrenadeType { frag, stun };

    // Use this for initialization
    void Start () {
    }
	
	// Update is called once per frame
    [ServerCallback]
	void Update () {
		if(pulled && Time.time > time_to_explode && !exploded)
        {
            //explode
            explode();
        }
        //Debug.Log("time: "+ Time.time + "; explode time: "+time_to_explode + "; pulled: "+pulled);
    }
    bool isDedicated()
    {
        return isServer && !isClient;
    }
    void explode()
    {
        exploded = true;
        GameObject user = GetComponent<Equipable_generic>().user;
        if (user != null)
        {
            if(user.GetComponent<Body_generic>().anim_upper != null)
            {
                
                user.GetComponent<Player_controller>().Rpc_anim_switch();
            }
            user.GetComponent<Player_controller>().Rpc_drop(gameObject, Vector2.zero, 1, 0, Gun_generic.FireMode.Semi_auto);
        }
        Rpc_explode(transform.position, activator, radius, power, thermal);
    }
    [ClientRpc]
    public void Rpc_set(ushort _ammo)
    {
        ammo = _ammo;
    }
    [ClientRpc]
    public void Rpc_explode(Vector2 position, GameObject activator, float radius, float power, float thermal)
    {
        if (explosion != null)
        {
            GameObject exp = Instantiate(explosion, Vector3.zero, Quaternion.identity);
            Explosion_generic exp_generic = exp.GetComponent<Explosion_generic>();
            exp_generic.PS.transform.position = position;
            exp_generic.activator = activator;
            exp_generic.power = power;
            exp_generic.thermal = thermal;
            exp_generic.radius = radius;
        }
        
        if (isServer)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    public Equipable_generic.ITEM_TYPE getType()
    {
        return Equipable_generic.ITEM_TYPE.grenade;
    }

    public ushort getWeight()
    {
        return grenade_weight;
    }

    public ushort getSize()
    {
        return grenade_size;
    }
}
