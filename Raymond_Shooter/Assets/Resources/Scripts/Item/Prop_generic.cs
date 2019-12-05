using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

public class Prop_generic : NetworkBehaviour {
    [SyncVar] public float health = 100;
    public float tissue_dense;//bullet-stoping power
    public float physical_resilience;
    public float size;
    public Sprite dead_spr;
    public Material dead_mat;
    public SpriteRenderer dead_hidesprite;
    public int dead_layer;
    [HideInInspector] public bool isDead = false;
    [HideInInspector] public Action OnDeath = null;
    static float dead_fade = 5;
    private float time_to_destroy = 0;
    private float spawn_time = 0;

    // Use this for initialization
    void Start()
    {
        spawn_time = Time.time;
    }

    // Update is called once per frame
    void Update()
    {

    }
    [ServerCallback]
    public void damage(GameObject activator, Vector2 force, float dmg_physics = 0, float dmg_thermal = 0)
    {

        if (activator != null && !activator.GetComponent<Body_generic>().dmg_tags.Contains(gameObject.tag) && activator != gameObject)//the activator is not hostile against the body, disallow damage; if damager is player himself, deal dmg
        {
            return;
        }
        //damage calculation
        float damage = 0;
        if (dmg_physics != 0)
        {
            damage += (dmg_physics) * (1 - Mathf.Pow(100, -(dmg_physics) / physical_resilience));

        }
        if (dmg_thermal != 0)
        {

            
        }

        health -= damage;
        //death
        if (health <= 0)
        {
            Rpc_die(force);
        }
    }
    [ClientRpc]
    public void Rpc_die(Vector2 force)
    {
        if (isDead)
        {
            return;
        }
        isDead = true;
        time_to_destroy = Time.time + dead_fade;
        if (dead_mat != null && dead_spr != null)
        {
            GetComponent<SpriteRenderer>().sprite = dead_spr;
            GetComponent<SpriteRenderer>().material = dead_mat;
        }
        if(dead_hidesprite != null)
        {
            Destroy(dead_hidesprite);
        }
        GetComponent<SpriteRenderer>().sortingLayerName = "Level_interiors";
        gameObject.layer = LayerMask.NameToLayer("Level_interiors");
        

        if (isServer)
        {
            if (OnDeath != null)
            {
                OnDeath.Invoke();
            }
        }
    }
    
}
