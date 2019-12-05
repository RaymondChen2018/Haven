using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Env_gunfire : Entity_generic, IDamageActivator
{
    public enum COLLISION_DETECTION
    {
        NoneCheapForPerformance,
        NormalCollisionDetection
    }
    public bool StartDisabled = false;
    public Transform Target;
    public int MinBurstSize = 2;
    public int MaxBurstSize = 7;
    public float MinDelayBetweenBurst = -1;
    public float MaxDelayBetweenBurst = -1;
    public float RateOfFire = 10;
    public float BulletSpread = 5;
    public COLLISION_DETECTION collisionDetection;
    public AudioClip ShootSound;
    public Gradient Tracer;


    public float bullet_speed = 20;
    public float bullet_mass = 1;
    public float bullet_damp = 0.1f;
    public float bullet_speed_min = 10;
    public bool noDamage = true;


    [SerializeField] private GameObject bullet;
    [SerializeField] private GameObject spark;
    float time_to_shoot = 0;
    int burstLeft = 5;
    float time_to_resume = 0;
	// Use this for initialization
	void Start () {
        Server_watcher.Singleton.onClientReady.Add(OnClientReady);
        if(collisionDetection == COLLISION_DETECTION.NoneCheapForPerformance)
        {
            spark = null;
        }
	}
	public void OnClientReady()
    {
        if (!StartDisabled && isServer)
        {
            enabled = true;
            burstLeft = UnityEngine.Random.Range(MinBurstSize, MaxBurstSize+1);
        }
    }
    [ServerCallback]
	// Update is called once per frame
	void Update () {
        //if burst left
        if (burstLeft > 0)//Due to unity bug, Rpc_shoot() fails to fire simutaneously, increment by one make bullet count correct
        {
            if (Time.time > time_to_shoot && Target != null)
            {
                //shoot
                //Debug.Log("shoot: "+burstLeft);
                if (isDedicatedServer() && !noDamage)//if no damage, dedicated server wont simulate the bullet
                {
                    //Debug.Log("dedshoot: " + burstLeft);
                    shoot();
                }
                Rpc_shoot();
                time_to_shoot = Time.time + 1 / RateOfFire;
                burstLeft--;
                //if out
                if (burstLeft <= 0)
                {
                    if(MinDelayBetweenBurst < 0 || MaxDelayBetweenBurst < 0)
                    {
                        enabled = false;
                    }
                    //set next resume
                    time_to_resume = Time.time + UnityEngine.Random.Range(MinDelayBetweenBurst, MaxDelayBetweenBurst);
                }
            }
        }
        //If time to resume
        else if(Time.time > time_to_resume)
        {
            //refill burst
            burstLeft = UnityEngine.Random.Range(MinBurstSize, MaxBurstSize+1);
        }
    }
    bool isDedicatedServer()
    {
        return !isClient && isServer;
    }
    void shoot()
    {
        //Debug.Log("bullet");
        //Withdraw from pool
        Bullet_generic the_bullet = Pool_watcher.Singleton.request_blt();
        //Get new ones
        if (the_bullet == null)
        {
            the_bullet = Instantiate(bullet, transform.position, Quaternion.identity).GetComponent<Bullet_generic>();
            the_bullet.pool_watcher = Pool_watcher.Singleton;
            the_bullet.initial_hit_fltr = the_bullet.hit_fltr;
            the_bullet.default_gradient = the_bullet.GetComponent<TrailRenderer>().colorGradient;
            the_bullet.default_texture = the_bullet.GetComponent<TrailRenderer>().material.mainTexture;
        }
        else
        {
            the_bullet.GetComponent<TrailRenderer>().Clear();
            the_bullet.transform.position = transform.position;

        }
        Vector2 aimdir = (Target.position - transform.position).normalized;
        float angle = Mathf.Atan2(aimdir.y,aimdir.x) + UnityEngine.Random.Range(-BulletSpread, BulletSpread) * 3.14f / 180;
        the_bullet.aimdir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        the_bullet.GetComponent<TrailRenderer>().widthMultiplier = bullet_mass / 20;
        the_bullet.mass = bullet_mass;
        the_bullet.speed = bullet_speed;
        the_bullet.speed_muzzle = bullet_speed;
        the_bullet.speed_damp = bullet_damp;
        the_bullet.speed_min = bullet_speed_min;
        the_bullet.spark = spark;
        the_bullet.activator = gameObject;
        the_bullet.local = isServer;
        the_bullet.isDedicated = Server_watcher.Singleton.isDedicated();
        the_bullet.lag_comp = Server_watcher.Singleton.local_player.latency / 2;
        the_bullet.noDamage = noDamage;
        //Customize color
        the_bullet.GetComponent<TrailRenderer>().colorGradient = Tracer;
    }
    [ClientRpc]
    void Rpc_shoot()
    {
        
        shoot();
    }


    //Inputs
    public void Enable()
    {
        enabled = true;
        burstLeft = UnityEngine.Random.Range(MinBurstSize, MaxBurstSize + 1);
    }
    public void Disable()
    {
        enabled = false;
    }

    public void OnHitCharacter(IDamageVictim victim, float damage, Vector2 hitPoint, Vector2 force, bool isHeadShot, DamageType damageType)
    {

    }

    public bool isPlayer()
    {
        return false;
    }

    bool IDamageActivator.isServer()
    {
        return true;
    }

    public GameObject getGameObject()
    {
        return null;
    }

    public bool canDamage(IDamageVictim victim)
    {
        return true;
    }


    //Outputs
}
