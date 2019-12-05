using UnityEngine;
using UnityEngine.Networking;
using System.Collections;



public class Equipable_generic : NetworkBehaviour {

    public enum ITEM_TYPE
    {
        gun,
        ammo,
        grenade,
        prop
    }
    [SyncVar(hook = "Hook_mdl_equip")] public GameObject user;
    [SyncVar(hook = "Hook_mdl_unload")] public bool loaded = true;
    /// <summary>
    /// Switchable via key
    /// </summary>
    [SyncVar] public bool laserAimOn = false;
    public Gradient laserAimColor;
    public Transform laserAimSource;
    public LayerMask laserAimMask;
    public ITEM_TYPE item_type = ITEM_TYPE.gun;
    public int price = 10;
    public float required_experience = 0;
    public bool parented = false;
    public Sprite hud_icon;
    public RuntimeAnimatorController hud_icon_anim;

    public Mesh model;
    public Texture2D tex_n_prop;
    public Texture2D tex_n_prop_unloaded;
    public Texture2D tex_n_equip;
    public Texture2D tex_n_equip_unloaded;
    public Sprite equip_spr;
    public Sprite prop_spr;
    public Sprite equip_spr_unloaded;
    public Sprite prop_spr_unloaded;
    public sbyte anim_equip = 0; //0: pistol; 1: rifle; 2: rpg; 3: machinegun; 4: ammobox; 5: grenade; 6: shotgun
    public bool mdl_state_unloaded = false;
    public bool mdl_state_equiped = false;
    //public ushort item_size;
    //public ushort item_weight;
    static float fade_time = 1;
    private float time_to_destroy = 0;
    [HideInInspector] public bool fade = false;
    private Color original_color;
    public Vector3 mdl_scale;

    public Sprite spr_fix_buffer;
    public Material mat_fix_buffer;
    Rigidbody2D itemRB;
    LineRenderer linerenderer;
    [HideInInspector] public Vector3 position_buffer;
    SpriteRenderer renderer;
    Gun_generic gun;
    bool active = true;
    void Awake()
    {
        if(model == null)
        {
            renderer = GetComponent<SpriteRenderer>();
            original_color = renderer.color;

            if (prop_spr == null)
            {
                prop_spr = renderer.sprite;
            }
            if (tex_n_equip == null)
            {
                //tex_n_equip = (Texture2D)renderer.material.GetTexture("_BumpMap");
            }
            if (prop_spr_unloaded == null)
            {
                prop_spr_unloaded = prop_spr;
            }
            if (tex_n_prop_unloaded == null)
            {
                tex_n_prop_unloaded = tex_n_prop;
            }
            if (equip_spr == null)
            {
                equip_spr = prop_spr;
            }
            if (tex_n_equip == null)
            {
                tex_n_equip = tex_n_prop;
            }
            if (equip_spr_unloaded == null)
            {
                equip_spr_unloaded = equip_spr;
            }
            if (tex_n_equip_unloaded == null)
            {
                tex_n_equip_unloaded = tex_n_equip;
            }
        }
        
        itemRB = GetComponent<Rigidbody2D>();
        gun = GetComponent<Gun_generic>();
        linerenderer = GetComponent<LineRenderer>();
        position_buffer = transform.position;
        position_buffer.z = CONSTANTS.BACKGROUND_OFFSETZ - 0.05f;
        transform.position = position_buffer;
        if (user != null && transform.parent != user)
        {
            transform.parent = user.transform;
            GetComponent<Collider2D>().enabled = false;
            GetComponent<Rigidbody2D>().isKinematic = true;
            //GetComponent<Rigidbody2D>().simulated = false;
            if (model)
            {
                renderer.sortingLayerName = "Equiped";
            }
            
        }

        quality_setting();
    }
    void quality_setting()
    {
        if(QualitySettings.GetQualityLevel() <= 1)
        {
            
        }
        else
        {
            if(model == null)
            {
                GetComponent<SpriteRenderer>().material.SetFloat("_Mode", 1);
                GetComponent<SpriteRenderer>().receiveShadows = true;
            }
        }
    }
    void OnDestroy()
    {
        if(model == null)
        {
            Destroy(GetComponent<SpriteRenderer>().material);
        }
    }
    void Update()
    {

        if (isDedicated())
        {
            return;
        }
        //Laser Aim
        if(linerenderer != null)
        {
            if (laserAimOn && laserAimSource != null && user != null && active)
            {
                linerenderer.colorGradient = laserAimColor;
                linerenderer.enabled = true;
                RaycastHit2D aim = Physics2D.Raycast(laserAimSource.position, gun.get_aim_vec(), 1000f, laserAimMask);
                linerenderer.SetPositions(new Vector3[] { laserAimSource.position, aim.point });
            }
            else
            {
                linerenderer.enabled = false;
            }
        }
        //
    }
    public void toggleLaserAim(bool on)
    {
        laserAimOn = on;
    }

    public bool isDedicated()
    {
        return isServer && !isClient;
    }
    /// <summary>
    /// Check if the item has a user or is faded, before picking it up
    /// </summary>
    /// <returns></returns>
    public bool isAvailable()
    {
        return user == null && !fade;
    }
    /// <summary>
    /// When the character switch to other equipable
    /// </summary>
    public void setInactive()
    {
        renderer.enabled = false;
        active = false;
    }
    /// <summary>
    /// When the character switch to this equipable
    /// </summary>
    public void setActive()
    {
        renderer.enabled = true;
        active = true;
    }
    public ushort get_size()
    {
        if(item_type == ITEM_TYPE.gun)
        {
            return GetComponent<Gun_generic>().weapon_size;
        }
        else if(item_type == ITEM_TYPE.ammo)
        {
            Ammo_generic ammo = GetComponent<Ammo_generic>();
            return (ushort)(ammo.bullet_size * ammo.amount);
        }
        return 0;
    }
    [ServerCallback]
    public void Input_switch_firemode()
    {
        if (item_type == ITEM_TYPE.gun)
        {
            Gun_generic gun = GetComponent<Gun_generic>();
            gun.switch_mode();
        }
    }
    [ClientRpc]
    public void Rpc_fade()
    {
        faded();
    }

    [ClientRpc]
    public void Rpc_set_ammo(ushort amount)
    {
        set_ammo(amount);
    }
    public void set_ammo(ushort amount)
    {
        GetComponent<Ammo_generic>().amount = amount;
    }
    public void faded()
    {
        fade = true;
        time_to_destroy = Time.time + fade_time;
    }

    public void set_user(GameObject item_user)
    {
        user = item_user;
        if (isDedicated())
        {
            Hook_mdl_equip(item_user);
        }
    }

    public void OnClientReady()
    {
        Sample_color(user);
    }
    void Sample_color(GameObject the_user)
    {
        Color chosen_color;
        
        Body_generic user_body = the_user.GetComponent<Body_generic>();

        if (user_body.isLocalPlayer)
        {
            chosen_color = CONSTANTS.COLOR_PLAYERLOCAL;
        }
        else if (user_body.isPlayer)
        {
            if (user_body.character_type == user_body.cvar_watcher.local_player.character_type)//Player ally
            {
                chosen_color = CONSTANTS.COLOR_PLAYERALLYOTHER;
            }
            else//Player Enemy
            {
                chosen_color = CONSTANTS.COLOR_PLAYERENEMYOTHER;
            }
        }
        else//NPC
        {
            if (user_body.character_type == user_body.cvar_watcher.local_player.character_type)//NPC ally
            {
                chosen_color = CONSTANTS.COLOR_ALLY;
            }
            else//NPC Enemy
            {
                chosen_color = CONSTANTS.COLOR_ENEMY;
            }
        }
        laserAimColor.SetKeys(new GradientColorKey[] { new GradientColorKey(chosen_color, 0), new GradientColorKey(chosen_color, 1) }, laserAimColor.alphaKeys);
    }
    //better use the referenced user, to prevent misusing the old user
    void colorCode_laserAim(GameObject code_ref_obj)
    {

        if (code_ref_obj == null || laserAimSource == null || isDedicated())
        {
            return;
        }

        //Laser Aim
        Body_generic user_body = code_ref_obj.GetComponent<Body_generic>();
        if (!user_body.cvar_watcher.cl_preroundStarted)
        {
            user_body.cvar_watcher.onClientReady.Add(OnClientReady);
            return;
        }

        Sample_color(code_ref_obj);
    }
    //startclient didnt update equip hook
    //still some that drop weapon equiped
    public void Hook_mdl_equip(GameObject new_user)
    {
        user = new_user;
        
        colorCode_laserAim(new_user);

        if (new_user != null)//Is equipped
        {
            sprite_matrix(true, loaded);
            parented = true;
        }
        else//prop spr
        {
            sprite_matrix(false, loaded);
            parented = false;
        }
    }
    public void Hook_mdl_unload(bool isLoaded)//absolutely equiping when changing the state of unload
    {
        loaded = isLoaded;
        //Debug.LogError("loaded: "+isLoaded);
        if (user != null)//Is equipped
        {
            sprite_matrix(true, isLoaded);
        }
        else//Not equiped, act as prop
        {
            sprite_matrix(false, isLoaded);
        }
    }
    
    void sprite_matrix(bool Equiped, bool Loaded)
    {
        if(model == null)
        {
            GetComponent<SpriteRenderer>().material.EnableKeyword("_NORMALMAP");
        }
        if (Equiped)//Is equipped
        {
            if (!Loaded)
            {
                if(model == null)
                {
                    GetComponent<SpriteRenderer>().sprite = equip_spr_unloaded;
                    GetComponent<SpriteRenderer>().material.SetTexture("_BumpMap", tex_n_equip_unloaded);
                }
                else
                {
                    model_attachment(true);
                }
            }
            else
            {
                if(model == null)
                {
                    GetComponent<SpriteRenderer>().sprite = equip_spr;
                    GetComponent<SpriteRenderer>().material.SetTexture("_BumpMap", tex_n_equip);
                }
                else
                {
                    model_attachment(true);
                }
            }
        }
        else//Not equiped, act as prop
        {
            if (!Loaded)
            {
                if(model == null)
                {
                    GetComponent<SpriteRenderer>().sprite = prop_spr_unloaded;
                    GetComponent<SpriteRenderer>().material.SetTexture("_BumpMap", tex_n_prop_unloaded);
                }
                else
                {
                    model_attachment(false);
                }
            }
            else
            {
                if(model == null)
                {
                    GetComponent<SpriteRenderer>().sprite = prop_spr;
                    GetComponent<SpriteRenderer>().material.SetTexture("_BumpMap", tex_n_prop);
                }
                else
                {
                    model_attachment(false);
                }
            }
        }
    }
    void model_attachment(bool attached)
    {
        if (attached)
        {
            Vector3 rot = transform.localRotation.eulerAngles;
            transform.localRotation = Quaternion.Euler(-90, 0, rot.z);
        }
        else
        {
            Vector3 rot = transform.localRotation.eulerAngles;
            transform.localRotation = Quaternion.Euler(0, 0, rot.z);
        }
    }
    public void detach(Vector2 dump_position)
    {
        
        position_buffer = dump_position;
        itemRB.position = position_buffer;
        transform.position = position_buffer;
        GetComponent<Rigidbody2D>().isKinematic = false;
        //GetComponent<Rigidbody2D>().simulated = true;
        GetComponent<Collider2D>().enabled = true;
        transform.parent = null;
        transform.localScale = mdl_scale;
        GetComponent<NetworkTransform>().enabled = true;
        if (model == null)
        {
            renderer.sortingLayerName = "Items";
        }
        else
        {
            model_attachment(false);
        }
    }
    public void attach(Transform trans_weapon_bone)
    {
        
        transform.position = trans_weapon_bone.position;//cant use rb position, because rb will stop simulate and not update transform
        transform.parent = trans_weapon_bone;
        transform.localScale = mdl_scale;
        transform.rotation = trans_weapon_bone.rotation;
        if (model == null)
        {
            
        }
        else
        {
            model_attachment(true);
        }
        GetComponent<Rigidbody2D>().isKinematic = true;
        //GetComponent<Rigidbody2D>().simulated = false;
        GetComponent<Collider2D>().enabled = false;
        if(model == null)
        {
            GetComponent<SpriteRenderer>().sortingLayerName = "Equiped";
        }
        
        GetComponent<NetworkTransform>().enabled = false;
    }
}
