using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityStandardAssets.ImageEffects;
using UnityEngine.UI;
using System;

public class Observer_controller : NetworkBehaviour {
    public KeyCode slowdownTime;
    public KeyCode resetTime;
    public KeyCode zoomout;
    public KeyCode zoomin;
    public KeyCode rotate_cam;
    public KeyCode MoveLeft;
    public KeyCode MoveRight;
    public KeyCode MoveUp;
    public KeyCode MoveDown;
    public KeyCode Stop;
    public KeyCode FastTravel;
    public KeyCode toggle_light;
    public KeyCode switch_character_next;
    public KeyCode switch_character_prev;
    public KeyCode switch_free;
    public KeyCode switch_selected;
    public KeyCode debug;
    public KeyCode debug_clear;
    public KeyCode debug_toggle;
    public LayerMask debug_mask;
    public LayerMask character_mask;
    public float Speed_travel = 5;
    public float Speed_fasttravel = 10;
    public float max_view;
    public float min_view;
    public Light light;
    public Light light_human;
    public Light light_robot;
    public Light light_zombie;
    private Transform cam3D;


    float cam_angle = 0;
    Vector2 cam_vec = Vector2.zero;
    Vector2 mousepos;
    Rigidbody2D obRB;
    Transform main_camera;
    Body_generic[] characters;
    int characters_index = 0;
    bool isFreeMode = true;
    Body_generic track_body;
    Interpolator_generic interpolator;
    [HideInInspector] public HealthBar_generic health_bar;
    
    [HideInInspector] public Text debug_info;
    [HideInInspector] public bool cl_sceneLoaded = false;
    Action state_update;
    [HideInInspector] public List<Team_watcher> waiting_for_teams = new List<Team_watcher>();
    Camera mainCam;

    
    Server_watcher cvar_watcher;
    Message_board_watcher msg_watcher;
    // Use this for initialization
    void Start () {
        cvar_watcher = FindObjectOfType<Server_watcher>();//Need to register on all end cuz its required by timescale
        if (!isLocalPlayer)
        {
            return;
        }
        msg_watcher = FindObjectOfType<Message_board_watcher>();
        obRB = GetComponent<Rigidbody2D>();
        interpolator = GetComponent<Interpolator_generic>();
        GetComponent<SpriteRenderer>().enabled = true;
        mainCam = Camera.main;
        main_camera = mainCam.transform;
        
        Destroy(main_camera.GetComponent<Fisheye>());
        Destroy(main_camera.GetComponent<ColorCorrectionCurves>());
        Destroy(main_camera.GetComponent<BlurOptimized>());
        Destroy(main_camera.GetComponent<ContrastStretch>());
        Destroy(main_camera.GetComponent<NoiseAndGrain>());
        Destroy(main_camera.GetComponent<VignetteAndChromaticAberration>());
        //debug_info = GameObject.Find("Debug_log").GetComponent<Text>();
        cam3D = GameObject.Find("Background3D_Camera").transform;
        Destroy(GameObject.Find("Darkness"));
        Destroy(GameObject.Find("Fade_view"));
        characters = FindObjectsOfType<Body_generic>();
        StartCoroutine(fetch_characters());
    }
    

    [Command]
    public void Cmd_slowTime()
    {
        if(FindObjectsOfType<Player_generic>().Length > 1 || Client_watcher.Singleton == null)
        {
            return;
        }
        Client_watcher.Singleton.sv_timescale(0.2f);
    }
    [Command]
    public void Cmd_normalTime()
    {
        if (FindObjectsOfType<Player_generic>().Length > 1 || Client_watcher.Singleton == null)
        {
            return;
        }
        Client_watcher.Singleton.sv_timescale(1f);
    }

    [Command]
    public void Cmd_sendReady()
    {
        cl_sceneLoaded = true;
    }
    void clear_debug_info()
    {
        debug_info.text = "";
    }
    string print_debug_info(GameObject obj)
    {
        if (obj == null)
        {
            return "This object is NULL";
        }

        string info = "";
        Equipable_generic item = obj.GetComponent<Equipable_generic>();
        Body_generic body = obj.GetComponent<Body_generic>();
        
        if (item != null)
        {
            info += "Item: " + obj.name + "\nUser: " + item.user + "\nFaded: "+item.fade+"\n";
            Gun_generic gun = obj.GetComponent<Gun_generic>();
            Ammo_generic ammo = obj.GetComponent<Ammo_generic>();
            if (gun != null)
            {
                info += "Ammo: " + gun.ammo + "\n";
            }
            else if (ammo != null)
            {
                info += "Amount: " + ammo.amount + "\n";
            }
        }
        else if (body != null)
        {
            if (body.isPlayer)
            {
                info += "\nPlayer: " + obj.name;
                info += "\nEquiped: " + obj.GetComponent<Player_controller>().equiped_item;
            }
            else
            {
                info += "\nNPC: " + obj.name;
                info += "\nEquiped: " + obj.GetComponent<AI_generic>().equiped_item;
            }
            info += "\nHealth: " + body.health + "/" + body.max_health;
            info += "\nReload * " + body.reload_multiplier;
            info += "\nStress # " + body.stress_resistent;
            info += "\nStrength: " + body.strength;
            info += "\nSpeed: "+ body.speed_run;
            info += "\nAim: " + body.aim_suppress;
            if (isServer)
            {
                info += "\nServer-sided Var---------------";
                info += "\nExperience # " + body.experience;
                info += "\nSPs # " + body.skill_points;
                info += "\nPhysical Resiliance # " + body.physical_resilience;
            }
        }
        
        return info;
    }
    public void debug_check_object(GameObject obj)
    {
        debug_info.text = "==== Local info ====\n";
        debug_info.text += print_debug_info(obj);
        Cmd_debug_check_object(obj);
    }
    [Command]
    public void Cmd_debug_check_object(GameObject obj)
    {   
        Target_debug_check_object(connectionToClient, print_debug_info(obj));
    }
    [TargetRpc]
    public void Target_debug_check_object(NetworkConnection target, string info)
    {
        debug_info.text += "\n==== Server feedback ====\n";
        debug_info.text += info;
    }




    //Wait till all characters are ready
	public IEnumerator fetch_characters()
    {
        yield return new WaitForSeconds(CONSTANTS.SPAWN_FREEZE_TIME / 2);
        characters = FindObjectsOfType<Body_generic>();
    }
	// Update is called once per frame
	void Update () {
        if (!isLocalPlayer)
        {
            return;
        }

        //Camera
        mousepos = new Vector2(mainCam.ScreenToWorldPoint(Input.mousePosition).x, mainCam.ScreenToWorldPoint(Input.mousePosition).y);
        main_camera.position = new Vector3(transform.position.x, transform.position.y, main_camera.position.z);


        if (!msg_watcher.isEditingMsg)
        {
            if (Input.GetKey(zoomout))
            {
                mainCam.orthographicSize *= 1.03f;
            }
            if (Input.GetKey(zoomin))
            {
                mainCam.orthographicSize /= 1.03f;
            }

        }
        if (Input.GetKeyDown(rotate_cam))
        {
            cam_vec = mousepos - (Vector2)transform.position;
        }
        else if (Input.GetKey(rotate_cam))
        {
            Vector2 offset_vec = (mousepos - (Vector2)transform.position);
            float offset = cam_angle - Mathf.Atan2((offset_vec).y, (offset_vec).x) * 180 / Mathf.PI + Mathf.Atan2((cam_vec).y, (cam_vec).x) * 180 / Mathf.PI;
            cam_angle = offset;
            main_camera.rotation = Quaternion.Euler(0, 0, offset);
        }
        if (mainCam.orthographicSize > max_view)
        {
            mainCam.orthographicSize = max_view;
        }
        else if (mainCam.orthographicSize < min_view)
        {
            mainCam.orthographicSize = min_view;
        }
        cam3D.localPosition = new Vector3(cam3D.localPosition.x, cam3D.localPosition.y, -Mathf.Lerp(CONSTANTS.CAM3D_MIN_Z, CONSTANTS.CAM3D_MAX_Z, (mainCam.orthographicSize - CONSTANTS.CAM_MIN_VIEW) / (CONSTANTS.CAM_MAX_VIEW - CONSTANTS.CAM_MIN_VIEW)));


        if (msg_watcher.isEditingMsg)
        {
            return;
        }

        //Timescale
        if (Input.GetKeyDown(slowdownTime) && FindObjectsOfType<Player_generic>().Length<=1)
        {
            Cmd_slowTime();
        }
        else if (Input.GetKeyDown(resetTime) && FindObjectsOfType<Player_generic>().Length <= 1)
        {
            Cmd_normalTime();
        }

        //Lighting
        if (Input.GetKeyDown(toggle_light))
        {
            if (light.enabled)
            {
                light.enabled = false;
                light_zombie.enabled = false;
                light_human.enabled = false;
                light_robot.enabled = false;
            }
            else
            {
                light.enabled = true;
                light_zombie.enabled = true;
                light_human.enabled = true;
                light_robot.enabled = true;
            }
        }

        //Debug
        if (Input.GetKeyDown(debug))
        {
            Collider2D check = Physics2D.OverlapCircle(transform.position, 1, debug_mask);
            if (check != null && check.tag != "structure")
            {
                GameObject the_object = check.gameObject;
                if (check.GetComponent<Body_hitbox_generic>() != null)
                {
                    the_object = check.GetComponent<Body_hitbox_generic>().body.gameObject;
                }
                debug_check_object(the_object);
            }
        }
        else if (Input.GetKeyDown(debug_clear))
        {
            clear_debug_info();
        }
        else if (Input.GetKeyDown(debug_toggle))
        {
            if (debug_info.enabled)
            {
                debug_info.enabled = false;
                Client_watcher.Singleton.GetComponent<FPSDisplay>().enabled = false;
            }
            else
            {
                debug_info.enabled = true;
                Client_watcher.Singleton.GetComponent<FPSDisplay>().enabled = true;
            }
        }

        //Movement
        if (track_body == null)
        {
            float move_angle = 0;
            float move_force = 1;
            Vector2 move_dir = Vector2.zero;
            if (Input.GetKey(FastTravel))
            {
                move_force = Speed_fasttravel;
            }
            else
            {
                move_force = Speed_travel;
            }

            if (Input.GetKey(MoveUp))
            {
                move_dir.y = 1;
            }
            else if (Input.GetKey(MoveDown))
            {
                move_dir.y = -1;
            }
            else
            {
                move_dir.y = 0;
            }
            if (Input.GetKey(MoveLeft))
            {
                move_dir.x = -1;
            }
            else if (Input.GetKey(MoveRight))
            {
                move_dir.x = 1;
            }
            else
            {
                move_dir.x = 0;
            }

            if (move_dir.magnitude != 0)
            {
                move_angle = Mathf.Atan2(move_dir.y, move_dir.x) * 180 / Mathf.PI;
                move_angle += mainCam.transform.rotation.eulerAngles.z;
                move_dir = new Vector2(Mathf.Cos(move_angle * Mathf.PI / 180), Mathf.Sin(move_angle * Mathf.PI / 180));
            }
            obRB.AddForce(move_dir.normalized * move_force * Time.unscaledDeltaTime / (50 * Time.fixedDeltaTime));
            if (Input.GetKeyDown(Stop))
            {
                obRB.velocity = Vector2.zero;
            }
        }
        else
        {
            interpolator.interpolate(track_body.transform.position);
            //obRB.position = track_body.transform.position;
            health_bar.ratio = track_body.health / track_body.max_health;
        }



        //Switch characters
        if ((Input.GetKeyDown(switch_character_next) || Input.GetKeyDown(switch_character_prev) || Input.GetKeyDown(switch_selected)) && characters != null && characters.Length > 0)
        {
            if (Input.GetKeyDown(switch_selected))
            {
                Collider2D selected = Physics2D.OverlapCircle(transform.position, 1, character_mask);
                if (selected != null && selected.tag != "structure")
                {
                    Body_generic selected_character = selected.GetComponent<Body_hitbox_generic>().body;
                    for (int i = 0; i < characters.Length; i++)
                    {
                        if (characters[i] == selected_character)
                        {
                            characters_index = i;
                            break;
                        }
                    }
                    track_body = selected_character;
                    health_bar.gameObject.SetActive(true);
                    isFreeMode = false;
                }
            }
            else if (isFreeMode)
            {
                isFreeMode = false;
                track_body = characters[characters_index];
                health_bar.gameObject.SetActive(true);
            }
            else if (Input.GetKeyDown(switch_character_next))
            {
                characters_index++;
                if (characters_index == characters.Length)
                {
                    characters_index = 0;
                }
                track_body = characters[characters_index];
            }
            else if (Input.GetKeyDown(switch_character_prev))
            {
                characters_index--;
                if (characters_index < 0)
                {
                    characters_index = characters.Length - 1;
                }
                track_body = characters[characters_index];
            }

        }
        else if (Input.GetKeyDown(switch_free))
        {
            if (!isFreeMode)
            {
                isFreeMode = true;
                track_body = null;
                health_bar.gameObject.SetActive(false);
            }
        }
    }
}
