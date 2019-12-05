using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Requires both server and client objects
/// </summary>
public class Point_template : Entity_generic {
    public GameObject[] templates;

    public bool DontRemoveTemplateEntities = false;


    [SerializeField]
    public List<CONSTANTS.IO> I_O;

    private bool spawnFirstTime = true;
    // Use this for initialization
    void Start () {
        //Removing templated objects
        if (!DontRemoveTemplateEntities)
        {
            toggle_templates(false);
        }
    }

    //Inputs
    public void Kill()
    {
        if (!spawnFirstTime)
        {
            //Removing unspawned object
            spawn_destroy_templates(false);
        }
        base.Kill();
    }
    public void ForceSpawn()
    {
        if(templates == null)
        {
            return;
        }

        if (spawnFirstTime)
        {
            spawnFirstTime = false;
            Rpc_spawnFirstTime();
            toggle_templates(true);
            OnEntitySpawned();
            return;
        }

        spawn_destroy_templates(true);
        OnEntitySpawned();
    }

    [ClientRpc]
    public void Rpc_spawnFirstTime()
    {
        if (isServer)
        {
            return;
        }
        toggle_templates(true);
    }

    void spawn_destroy_templates(bool isSpawn)
    {
        for (int i = 0; i < templates.Length; i++)
        {
            if (templates[i] == null)
            {
                continue;
            }
            if (isSpawn)
            {
                templates[i] = Instantiate(templates[i], templates[i].transform.position, templates[i].transform.rotation);
                NetworkServer.Spawn(templates[i]);
            }
            else
            {
                NetworkServer.Destroy(templates[i]);
            }
        }
    }
    void toggle_templates(bool isActive)
    {
        for (int i = 0; i < templates.Length; i++)
        {
            if (templates[i] == null)
            {
                continue;
            }
            templates[i].SetActive(isActive);
        }
        return;
    }


    //Outputs
    public void OnEntitySpawned()
    {
        CONSTANTS.invokeOutput(CONSTANTS.OUTPUT_NAME.OnEntitySpawned, I_O);
    }


}
