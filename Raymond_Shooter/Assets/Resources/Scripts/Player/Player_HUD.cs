using UnityEngine;
using UnityStandardAssets.ImageEffects;
using UnityEngine.UI;
//using UnityStandardAssets.ImageEffects;
public class Player_HUD : MonoBehaviour
{
    //public GameObject HUD_camera = null;
    public Sprite missing_texture;
    public Transform HUD;
    private Text ammo_field;
    private Image icon_field;
    private Animator icon_field_anim;
    private Image overlay;
    private float time_to_update_overlay = 0;
    private Camera main_camera;
    private Player_controller controller;
    private Body_generic body;
    static float screen_fade_threashold = 0.7f;
    public int damaged_screen_factor = 150;

    //Cursor
    private float cursor_size = 0;
    public GameObject cursor = null;
    private Transform cursor_l;
    private Transform cursor_r;
    private Transform cursor_u;
    private Transform cursor_d;
    private Transform cursor_marker_ne;
    private Transform cursor_marker_nw;
    private Transform cursor_marker_se;
    private Transform cursor_marker_sw;
    private Animator cursor_anim;


    //Overlay
    static float overlay_update_factor = 0.02f;

    public GameObject menu_prefab;
    public Menu_watcher menu;
    public Server_watcher cvar_watcher;
    Equipable_generic equip = null;
    // Use this for initialization
    void Start()
    {
        
        //Server need this link to assign purchased weapons
        body = GetComponent<Body_generic>();
        cvar_watcher = FindObjectOfType<Server_watcher>();
        main_camera = Camera.main;
        controller = GetComponent<Player_controller>();
        if (!GetComponent<Player_controller>().isLocalPlayer)
        {
            enabled = false;
            return;
        }
        else//Continues as local player
        {
            if(cvar_watcher.map_type == CONSTANTS.MAP_TYPE.PVP)
            {
                //Menu
                menu = Instantiate(menu_prefab).GetComponent<Menu_watcher>();
                menu.body = body;
                menu.buffer_inventory_size = body.inventory_size;

                if (body.isBot())
                {
                    menu.robot_menu.SetActive(true);
                    menu.robot_submenu[controller.character_subtype].SetActive(true);
                    menu.human_menu.SetActive(false);
                    menu.zombie_menu.SetActive(false);
                    menu.purchase_buttons = menu.purchase_buttons_robot;
                    menu.upgrade_buttons = menu.upgrade_buttons_robot;
                    menu.purchasables = cvar_watcher.purchases_robot;
                }
                else if (body.isHuman())
                {
                    menu.robot_menu.SetActive(false);
                    menu.human_menu.SetActive(true);
                    menu.human_submenu[controller.character_subtype].SetActive(true);
                    menu.zombie_menu.SetActive(false);
                    menu.purchase_buttons = menu.purchase_buttons_human;
                    menu.upgrade_buttons = menu.upgrade_buttons_human;
                    menu.purchasables = cvar_watcher.purchases_human;
                }
                else if (body.isZombie())
                {
                    menu.robot_menu.SetActive(false);
                    menu.human_menu.SetActive(false);
                    menu.zombie_menu.SetActive(true);
                    menu.zombie_submenu[controller.character_subtype].SetActive(true);
                    menu.purchase_buttons = menu.purchase_buttons_human;
                    menu.upgrade_buttons = menu.upgrade_buttons_zombie;
                    menu.purchasables = cvar_watcher.purchases_zombie;
                }
                menu.purchase_buttons_robot = null;
                menu.purchase_buttons_human = null;
            }
            

            cursor = Instantiate(cursor);
            cursor_anim = cursor.GetComponent<Animator>();
            cursor_l = cursor.transform.Find("cursor_l");
            cursor_r = cursor.transform.Find("cursor_r");
            cursor_u = cursor.transform.Find("cursor_u");
            cursor_d = cursor.transform.Find("cursor_d");
            cursor_marker_sw = cursor.transform.Find("hit_marker_sw");
            cursor_marker_se = cursor.transform.Find("hit_marker_se");
            cursor_marker_nw = cursor.transform.Find("hit_marker_nw");
            cursor_marker_ne = cursor.transform.Find("hit_marker_ne");
        }
        GetComponent<Body_generic>().OnDamaged = damaged_screen;
        //HUD set-up
        //HUD_camera = Instantiate(HUD_camera, Vector2.zero, Quaternion.identity);
        //HUD_generic hud_generic = HUD_camera.GetComponent<HUD_generic>();
        //HUD_camera.transform.parent = main_camera.transform;
        //HUD_camera.transform.position = main_camera.transform.position;
        HUD = Instantiate(HUD);
        HUD_generic hud_generic = HUD.GetComponent<HUD_generic>();
        ammo_field = hud_generic.AmmoField;
        icon_field = hud_generic.ItemIcon;
        
        icon_field_anim = icon_field.GetComponent<Animator>();
        overlay = hud_generic.Overlay;
        //ammo_field.GetComponent<MeshRenderer>().sortingLayerName = "HUD";

        

        //Disable unwanted screen filters
        
        
        if (body.character_type == Body_generic.Character_type.Robot)
        {
            
            //Destroy(main_camera.GetComponent<ColorCorrectionCurves>());
            Destroy(main_camera.GetComponent<BlurOptimized>());
            //Destroy(main_camera.GetComponent<ContrastStretch>());
        }
        else if (body.character_type == Body_generic.Character_type.Human)
        {
            
            Destroy(main_camera.GetComponent<NoiseAndGrain>());
            Destroy(main_camera.GetComponent<VignetteAndChromaticAberration>());
        }
        else if (body.character_type == Body_generic.Character_type.Zombie)
        {

            Destroy(main_camera.GetComponent<NoiseAndGrain>());
            Destroy(main_camera.GetComponent<VignetteAndChromaticAberration>());
        }
    }
    // Update is called once per frame
    void Update()
    {
        
        //Cursor
        if (cursor != null)
        {
            if (controller.equiped_item == null)
            {
                cursor_size = -1;
            }
            else
            {
                if (controller.equiped_item.tag == "pickup_ammo")
                {
                    cursor_size = -1;
                }
                else if (controller.equiped_item.tag == "pickup_grenade")
                {
                    cursor_size = -1;
                }
                else if (controller.equiped_item.tag == "pickup_gun")
                {
                    Gun_generic gun = controller.equiped_item.GetComponent<Gun_generic>();
                    float firecone = (gun.firecone_angle + gun.precise) * Mathf.PI / 180;
                    if (firecone != 90)
                    {
                        cursor_size = Mathf.Tan(firecone) * Vector2.Distance(controller.mousepos, transform.position);//controller.playerRB.position);
                    }
                    else
                    {
                        cursor_size = 0;
                    }
                }
            }
            cursor_anim.SetFloat(CONSTANTS.ANIM_PARAM_CURSOR_SIZE, cursor_size);
            if (cursor_size == -1)
            {
                Cursor.visible = true;
                //cursor.transform.localScale = new Vector3(0, 0, 0);
            }
            else
            {
                Cursor.visible = false;
                cursor.transform.rotation = main_camera.transform.rotation;
                cursor.transform.localScale = new Vector3(1, 1, 1);
                cursor.transform.position = new Vector3(controller.mousepos.x, controller.mousepos.y, cursor.transform.position.z);
                float cam_cursor_scale = main_camera.orthographicSize / 10;

                cursor_l.localScale = new Vector3(0.3f * cam_cursor_scale, 0.03f * cam_cursor_scale, 1);
                cursor_r.localScale = new Vector3(0.3f * cam_cursor_scale, 0.03f * cam_cursor_scale, 1);
                cursor_u.localScale = new Vector3(0.3f * cam_cursor_scale, 0.03f * cam_cursor_scale, 1);
                cursor_d.localScale = new Vector3(0.3f * cam_cursor_scale, 0.03f * cam_cursor_scale, 1);
                cursor_marker_se.localScale = new Vector3(0.3f * cam_cursor_scale, 0.03f * cam_cursor_scale, 1);
                cursor_marker_sw.localScale = new Vector3(0.3f * cam_cursor_scale, 0.03f * cam_cursor_scale, 1);
                cursor_marker_ne.localScale = new Vector3(0.3f * cam_cursor_scale, 0.03f * cam_cursor_scale, 1);
                cursor_marker_nw.localScale = new Vector3(0.3f * cam_cursor_scale, 0.03f * cam_cursor_scale, 1);

            }
        }







        //Follow camera
        //HUD_camera.GetComponent<Camera>().orthographicSize = main_camera.orthographicSize;
        float cam_scale = main_camera.orthographicSize / 5;
        //HUD.localScale = new Vector3(cam_scale, cam_scale, 1);

        //Health state effect & Damage flash screen
        overlay.color = new Color(Mathf.Clamp01(overlay.color.r - overlay_update_factor), Mathf.Clamp01(overlay.color.g - overlay_update_factor), Mathf.Clamp01(overlay.color.b - overlay_update_factor));

        if (controller.reloading)
        {
            icon_field.color = Color.red;
        }else
        {
            icon_field.color = Color.white;
        }

        //Turn on/off icon and ammo
        if(controller.equiped_item != null)
        {
            equip = controller.equiped_item.GetComponent<Equipable_generic>();
            icon_field.enabled = true;
            ammo_field.enabled = true;
        }
        else
        {
            equip = null;
            icon_field.enabled = false;
            ammo_field.enabled = false;
        }

        //Write to icon and ammo
        if (equip != null)
        {
            int ammo_num = 0;
            if (equip.item_type == Equipable_generic.ITEM_TYPE.gun)
            {
                ammo_num = equip.GetComponent<Gun_generic>().ammo;
            }
            else if (equip.item_type == Equipable_generic.ITEM_TYPE.ammo)
            {
                ammo_num = equip.GetComponent<Ammo_generic>().amount;
            }
            else if (equip.item_type == Equipable_generic.ITEM_TYPE.grenade)
            {
                ammo_num = equip.GetComponent<Grenade_generic>().ammo;
            }

            /*
            if(equip.GetComponent<Equipable_generic>().hud_icon_anim != null)
            {
                icon_field_anim.runtimeAnimatorController = equip.GetComponent<Equipable_generic>().hud_icon_anim;
                icon_field.sprite = null;
            }
            else */if(equip.hud_icon != null)
            {
                icon_field.sprite = equip.hud_icon;
                icon_field_anim.runtimeAnimatorController = null;
            }
            else
            {
                icon_field.sprite = missing_texture;
                icon_field_anim.runtimeAnimatorController = null;
            }
            ammo_field.text = ammo_num.ToString();
        }else
        {
            icon_field_anim.runtimeAnimatorController = null;
        }

    }
    public void hit_marker()
    {

        cursor_anim.Play(CONSTANTS.ANIM_STATE_CURSOR_HIT, 1);
    }
    public void damaged_screen(float damage, Vector2 dmg_dir)
    {
        float red = Mathf.Clamp01(overlay.color.r + damage / damaged_screen_factor);
        //Damage flash screen fx
        overlay.color = new Color(red + CONSTANTS.DMGSCREEN_MIN_RATIO, overlay.color.g, overlay.color.b);
        
        //Health state
        health_state_update();
    }
    public void show_start_up_menu()
    {
        menu.GetComponent<Animator>().Play("Start_load_menuonly");
        
    }
    
    public void hide_start_up_menu()
    {
        menu.GetComponent<Animator>().Play("Dead_unload_menuonly");
    }
    public void show_kill_by(GameObject killer, bool teamLost)
    {
        if(menu == null)
        {
            return;
        }
        //menu.Submenu_killed_by.SetActive(true);
        if (!teamLost)
        {
            menu.GetComponent<Animator>().Play("Death_load");
            menu.respawn_countDown();
        }
        else
        {
            menu.GetComponent<Animator>().Play("Death_noTicket");
        }
        
        string activator_name = "Environment";
        string weapon_name = "Science";
        if(killer != null)
        {
            activator_name = killer.GetComponent<Body_generic>().character_name;
            weapon_name = get_weapon_name(killer);
        }
        menu.Text_killed_by_text.text = "Killed by: " + activator_name + " with " + weapon_name;
    }

    public void hide_kill_by()
    {
        menu.GetComponent<Animator>().Play("Death_fade");
    }
    string get_weapon_name(GameObject subject)
    {
        if (subject.GetComponent<Body_generic>().isPlayer)
        {
            Player_controller player = subject.GetComponent<Player_controller>();
            if(player.equiped_item == null)
            {
                return "nothing";
            }
            return subject.GetComponent<Player_controller>().equiped_item.name;
        }
        AI_generic ai = subject.GetComponent<AI_generic>();
        if(ai.equiped_item == null)
        {
            return "nothing";
        }
        return subject.GetComponent<AI_generic>().equiped_item.name;
    }
    public void health_state_update()
    {
        float ratio = 0;
        //If health above threashold the fx is visible, return
        if (body.health < body.max_health * screen_fade_threashold)
        {
            ratio = (1 - body.health / (body.max_health * screen_fade_threashold));
        }

        //If health below threashold the fx is visible, calculate
        if (body.character_type == Body_generic.Character_type.Robot)//bot
        {
            main_camera.GetComponent<NoiseAndGrain>().intensityMultiplier = ratio * 3;
            main_camera.GetComponent<VignetteAndChromaticAberration>().intensity = 0.183f + ratio * 0.4f;
            main_camera.GetComponent<VignetteAndChromaticAberration>().chromaticAberration = ratio * 10;
        }
        else if (body.character_type == Body_generic.Character_type.Human)//human
        {
            main_camera.GetComponent<ColorCorrectionCurves>().saturation = Mathf.Clamp01(1 - ratio);
        }
        else if (body.character_type == Body_generic.Character_type.Zombie)//human
        {
            main_camera.GetComponent<ColorCorrectionCurves>().saturation = Mathf.Clamp01(1 - ratio);
        }
    }
    public void reset_screen()
    {
        overlay.color = Color.clear;
        if(body.character_type == Body_generic.Character_type.Robot)//bot
        {
            main_camera.GetComponent<NoiseAndGrain>().intensityMultiplier = 0;
            main_camera.GetComponent<VignetteAndChromaticAberration>().intensity = 0.183f;
            main_camera.GetComponent<VignetteAndChromaticAberration>().chromaticAberration = 0;
        }
        else if (body.character_type == Body_generic.Character_type.Human)//human
        {
            main_camera.GetComponent<BlurOptimized>().enabled = false;
            main_camera.GetComponent<ColorCorrectionCurves>().saturation = 1;
        }
        else if (body.character_type == Body_generic.Character_type.Zombie)//human
        {
            main_camera.GetComponent<BlurOptimized>().enabled = false;
            main_camera.GetComponent<ColorCorrectionCurves>().saturation = 1;
        }
    }
    public void die_screen()
    {
        if (body.character_type == Body_generic.Character_type.Robot)//bot
        {

        }
        else if (body.character_type == Body_generic.Character_type.Human)//human
        {
            main_camera.GetComponent<BlurOptimized>().enabled = true;
        }
        else if (body.character_type == Body_generic.Character_type.Zombie)//human
        {
            main_camera.GetComponent<BlurOptimized>().enabled = true;
        }
    }
    public void stunned_screen(float ratio)
    {
        overlay.color = new Color(Mathf.Clamp01(overlay.color.r + ratio), Mathf.Clamp01(overlay.color.g + ratio), Mathf.Clamp01(overlay.color.b + ratio));
    }
}
