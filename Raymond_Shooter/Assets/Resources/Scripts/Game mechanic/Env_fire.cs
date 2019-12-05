using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Env_fire : Entity_generic {
    public float size = 64;
    public float thermal = 500;
    public bool StartOn = false;

    /// <summary>
    /// Choose to customize particle yourself instead of using the size parameter
    /// </summary>
    public bool customFire = false;




    public enum OUTPUT_NAME
    {
        OnIgnited,
        OnExtinguished
    }
    [SerializeField]
    public List<CONSTANTS.IO> I_O;
    public LayerMask hitFilter;
    [SyncVar(hook = "Hook_isActive")] bool isActive;
    ParticleSystem particle = null;
    Fx_watcher fluid_watcher;
    CircleCollider2D circleCollider;
    float time_to_damage = 0;
    // Use this for initialization
    void Start () {
		particle = GetComponent<ParticleSystem>();
        circleCollider = GetComponent<CircleCollider2D>();
        Server_watcher.Singleton.onClientReady.Add(OnClientReady);
        
        if (!customFire)
        {
            ParticleSystem.EmissionModule particleEmit = particle.emission;
            ParticleSystem.ShapeModule particleShape = particle.shape;
            particleShape.radius = 1.5f * size / 64;
            particleEmit.rateOverTime = 250 * size / 64;
            circleCollider.radius = 1.25f * size / 64;
        }
        
        
        if (!isServer || thermal <= 0)
        {
            Destroy(circleCollider);
        }
        else if(Server_watcher.Singleton.local_player != null)
        {

            hitFilter = Physics2D.GetLayerCollisionMask(Server_watcher.Singleton.local_player.gameObject.layer);
        }
    }
    
    public void OnClientReady()
    {
        fluid_watcher = Fx_watcher.Singleton;
        if (!isServer)
        {
            return;
        }
        if (!StartOn)
        {
            gameObject.SetActive(false);
            isActive = false;
        }
        else
        {
            Ignite();
        }
        
    }

    //Inputs

    [ServerCallback]
    public void StartFire()
    {
        Ignite();
        OnIgnited();
    }
    [ServerCallback]
    public void Extinguish()
    {
        gameObject.SetActive(false);
        isActive = false;
        OnExtinguished();
    }
    //Outputs
    [ServerCallback]
    public void OnIgnited()
    {
        CONSTANTS.invokeOutput(CONSTANTS.OUTPUT_NAME.OnIgnited, I_O);
    }
    [ServerCallback]
    public void OnExtinguished()
    {
        CONSTANTS.invokeOutput(CONSTANTS.OUTPUT_NAME.OnExtinguished, I_O);
    }


    //
    public void Hook_isActive(bool value)
    {
        gameObject.SetActive(value);
        isActive = value;
    }
    [ServerCallback]
    private void OnTriggerStay2D(Collider2D collision)
    {
        if(Time.time <= time_to_damage)
        {
            return;
        }
        
        time_to_damage = Time.time + CONSTANTS.NETWORK_TICK_RATE;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, circleCollider.radius, hitFilter);
        if(hits != null)
        {
            for(int i = 0; i < hits.Length; i++)
            {
                Body_hitbox_generic hitbox = hits[i].GetComponent<Body_hitbox_generic>();
                if (hitbox != null)
                {
                    hitbox.body.damage(null, Vector2.zero, dmg_thermal: thermal);
                }
            }
        }
        
        
    }

    void Ignite()
    {
        gameObject.SetActive(true);
        isActive = true;
        if (!fluid_watcher.particleSystems.Contains(particle))
        {
            fluid_watcher.particleSystems.Add(particle);
        }
    }
}
