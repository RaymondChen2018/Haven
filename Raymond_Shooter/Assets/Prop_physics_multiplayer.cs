using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(sendInterval = 0.05f)]
public class Prop_physics_multiplayer : NetworkBehaviour {
    [SyncVar] public short syncx_compressed = 0;
    [SyncVar] public short syncy_compressed = 0;
    [SyncVar] public float syncrot = 0;
    [SyncVar] public float syncrotvel = 0;
    [SyncVar] public short syncspeed_compressed = 0;

    Rigidbody2D propRB;
    Vector2 server_pos = new Vector2();

    Vector2 server_vel = new Vector2();
    Vector2 temp = new Vector2();
    Interpolator_generic interpolator;

    //NetworkTransform netform;
    //public GameObject ghost;

	// Use this for initialization
	void Start () {
        propRB = GetComponent<Rigidbody2D>();
        interpolator = GetComponent<Interpolator_generic>();



        //netform = GetComponent<NetworkTransform>();
        sync_server();
    }
    void sync_server()
    {
        syncx_compressed = CONSTANTS.comp_pos(propRB.position.x);
        syncy_compressed = CONSTANTS.comp_pos(propRB.position.y);
        syncspeed_compressed = CONSTANTS.comp_pos(propRB.velocity.magnitude);
        syncrot = propRB.rotation;
        syncrotvel = propRB.angularVelocity;
    }
    void Update()
    {

        if (!isServer)
        {
            temp = server_pos;
            server_pos.x = CONSTANTS.decomp_pos(syncx_compressed);
            server_pos.y = CONSTANTS.decomp_pos(syncy_compressed);


            propRB.velocity = (server_pos - temp).normalized * CONSTANTS.decomp_pos(syncspeed_compressed);
            interpolator.interpolate(server_pos);

            //propRB.angularVelocity = syncrotvel;
            interpolator.interpolate_rot(syncrot);
        }
        else
        {
            sync_server();
            /*
            if(CONSTANTS.comp_pos(propRB.position.x) != syncx_compressed)
            {
                syncx_compressed = CONSTANTS.comp_pos(propRB.position.x);
            }
            if (CONSTANTS.comp_pos(propRB.position.y) != syncy_compressed)
            {
                syncy_compressed = CONSTANTS.comp_pos(propRB.position.y);
            }
            if (CONSTANTS.comp_pos(propRB.velocity.x) != syncvelx_compressed)
            {
                syncvelx_compressed = CONSTANTS.comp_pos(propRB.velocity.x);
            }
            if (CONSTANTS.comp_pos(propRB.velocity.y) != syncvely_compressed)
            {
                syncvely_compressed = CONSTANTS.comp_pos(propRB.velocity.y);
            }
            if(propRB.rotation != syncrot)
            {
                syncrot = transform.rotation.eulerAngles.z;
            }
            if(propRB.angularVelocity != syncrotvel)
            {
                syncrotvel = propRB.angularVelocity;
            }
            */
        }
    }
    /*
    void Update()
    {
        ghost_image.transform.rotation = Quaternion.Euler(0, 0, nettransform.targetSyncRotation2D);
        ghost_image.transform.position = nettransform.targetSyncPosition;
        if (Time.time > time_to_resume_nettransform)
        {
            //GetComponent<SpriteRenderer>().color = Color.white;
            if(nettransform.enabled == false)
            {
                propRB.position = nettransform.targetSyncPosition;
            }
            nettransform.enabled = true;
        }
    }
    public override void OnStartClient()
    {
        //nettransform.
        propRB = GetComponent<Rigidbody2D>();
        nettransform = GetComponent<NetworkTransform>();
    }

    [Command]
    public void Cmd_update(short x, short y, short vel_x, short vel_y)
    {
        propRB.position = new Vector2(CONSTANTS.decomp_pos(x), CONSTANTS.decomp_pos(y));
        propRB.velocity = new Vector2(CONSTANTS.decomp_pos(vel_x), CONSTANTS.decomp_pos(vel_y));
    }
    void OnCollisionStay2D(Collision2D collision)
    {

        NetworkIdentity colliderID = collision.gameObject.GetComponent<NetworkIdentity>();
        if (colliderID == null)
        {
            return;
        }
        if (colliderID.isLocalPlayer && !colliderID.isServer)//local client
        {

            nettransform.enabled = false;

            
            time_to_resume_nettransform = Time.time + 2*(Server_Cvar_watcher.Singleton.local_player.latency / 1000f);
            //GetComponent<SpriteRenderer>().color = Color.red;
            //Debug.Log("resume time: "+time_to_resume_nettransform+"; current time: "+Time.time);
            if (Time.time > time_to_send_pos)
            {
                time_to_send_pos = Time.time + client_update_interval;
                //send position & velocity & angular velocity
                Cmd_update(CONSTANTS.comp_pos(propRB.position.x), CONSTANTS.comp_pos(propRB.position.y), CONSTANTS.comp_pos(propRB.velocity.x), CONSTANTS.comp_pos(propRB.velocity.y));
            }
        }

    }
    */
    /*
    // Update is called once per frame
    void Update () {
        if (isServer)
        {
            if (istouching)
            {
                sync_to_client();
            }
        }
        else
        {
            if (clientupdate_status)
            {
                clientupdate_status = false;
                propRB.position = Vector2.Lerp(propRB.position, server_pos, 0.5f);
                propRB.velocity = Vector2.Lerp(propRB.velocity, server_vel, 0.5f);
            }
        }
        istouching = false;
    }

    [Command]
    public void Cmd_update(short x, short y, short vel_x, short vel_y)
    {
        if (!istouching)//Override server stat if server object is sleeping
        {
            propRB.position = new Vector2(CONSTANTS.decomp_pos(x), CONSTANTS.decomp_pos(y));
            propRB.velocity = new Vector2(CONSTANTS.decomp_pos(vel_x), CONSTANTS.decomp_pos(vel_y));
        }
        else
        {
            propRB.position = new Vector2((CONSTANTS.decomp_pos(x) + propRB.position.x)/2,( CONSTANTS.decomp_pos(y)+ propRB.position.y)/2);
            propRB.velocity += new Vector2(CONSTANTS.decomp_pos(vel_x), CONSTANTS.decomp_pos(vel_y));
        }
        istouching = true;
    }
    [ServerCallback]
    void sync_to_client()
    {
        pos_x_compressed = CONSTANTS.comp_pos(propRB.position.x);
        pos_y_compressed = CONSTANTS.comp_pos(propRB.position.y);
        vel_x_compressed = CONSTANTS.comp_pos(propRB.velocity.x);
        vel_y_compressed = CONSTANTS.comp_pos(propRB.velocity.y);
    }
    
    void OnCollisionStay2D(Collision2D collision)
    {
        
        NetworkIdentity colliderID = collision.gameObject.GetComponent<NetworkIdentity>();
        if(colliderID == null)
        {
            return;
        }
        latestTouching_agent = collision.gameObject;
        if (colliderID.isLocalPlayer && !colliderID.isServer)//local client
        {
            istouching = true;
            
            if (Time.time > time_to_send_pos)
            {
                time_to_send_pos = Time.time + client_update_interval;
                //send position & velocity & angular velocity
                Cmd_update(CONSTANTS.comp_pos(propRB.position.x), CONSTANTS.comp_pos(propRB.position.y), CONSTANTS.comp_pos(propRB.velocity.x), CONSTANTS.comp_pos(propRB.velocity.y));
            }
        }
        else if (colliderID.hasAuthority)//Host or server objects
        {
            istouching = true;
        }

    }
    
    */











    
}
