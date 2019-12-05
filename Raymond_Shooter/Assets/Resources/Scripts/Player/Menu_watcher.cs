using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class Menu_watcher : MonoBehaviour {

    static public Menu_watcher Singleton;

    public GameObject Submenu_killed_by;
    public Text Text_killed_by_text;
    public GameObject Submenu_purchase;
    public Text Text_money;
    public Text Text_sp;
    public GameObject human_menu;
    public GameObject[] human_submenu;
    public GameObject robot_menu;
    public GameObject[] robot_submenu;
    public GameObject zombie_menu;
    public GameObject[] zombie_submenu;
    public GameObject Submenu_summary;
    public Text Text_summary;
    public Text Text_count_human;
    public Text Text_count_robot;
    public Text Text_count_zombie;
    public UnityEngine.UI.Button respawn_button;
    /// <summary>
    /// This track how many points had been spent, added to unused skill points to determine what level the character is
    /// </summary>
    byte used_skill_points;

    public ushort buffer_inventory_size;
    ushort buffer_inventory_capacity = 0;
    int buffer_money;
    ushort buffer_sp;
    

    /// <summary>
    /// Only server as a local reference, cannot be referenced by server for spawning
    /// </summary>
    public GameObject[] purchasables;
    public Color SubPurchase_selected_normal;
    public Color SubPurchase_selected_highlight;
    public Color SubPurchase_selected_pressed;
    public Color SubPurchase_selected_disabled;
    public Color SubPurchase_disable_normal;
    public Color SubPurchase_disable_highlight;
    public Color SubPurchase_disable_pressed;
    public Color SubPurchase_disable_disabled;
    public Color SubPurchase_unselected_normal;
    public Color SubPurchase_unselected_highlight;
    public Color SubPurchase_unselected_pressed;
    public Color SubPurchase_unselected_disabled;



    [HideInInspector] public UnityEngine.UI.Button[] purchase_buttons;
    [HideInInspector] public UnityEngine.UI.Button[] upgrade_buttons;
    public UnityEngine.UI.Button[] purchase_buttons_human;
    public UnityEngine.UI.Button[] purchase_buttons_robot;
    public UnityEngine.UI.Button[] upgrade_buttons_human;
    public UnityEngine.UI.Button[] upgrade_buttons_robot;
    public UnityEngine.UI.Button[] upgrade_buttons_zombie;
    List<byte> cart;
    List<byte> s_cart;
    //[HideInInspector] public byte menu_type = 0;
    [HideInInspector] public Body_generic body;
    private Server_watcher cvar_watcher;
    [HideInInspector] public bool respawnTimed = false;
    float respawn_timer = 1;
    public Progress_Bar_generic respawnTimer;
    float prev_time = 0;

    void Awake()
    {
        Singleton = this;
    }
    void OnDestroy()
    {
        Singleton = null;
    }

    // Use this for initialization
    void Start () {
        cvar_watcher = FindObjectOfType<Server_watcher>();
        cvar_watcher.onClientReady.Add(OnClientReady);

        cart = new List<byte>();
        s_cart = new List<byte>();
        //SubPurchase_load();
        
    }
	
	// Update is called once per frame
	void Update () {
        float delta_time = Time.realtimeSinceStartup - prev_time;
        prev_time = Time.realtimeSinceStartup;
        if (respawnTimed && respawnTimer!=null)
        {
            if(respawn_timer <= 0)
            {
                respawnTimer.update_progress(1);
                respawnTimed = false;
            }
            else
            {
                respawnTimer.update_progress(1 - respawn_timer / cvar_watcher.respawn_time);
            }
            respawn_timer -= delta_time;
        }
	}
    public void OnClientReady()
    {
        if(cvar_watcher.map_type == CONSTANTS.MAP_TYPE.PVP)
        {
            GetComponent<Animator>().Play("Start_load_menuonly");
        }
        
    }
    public void respawn_countDown()
    {
        respawn_timer = cvar_watcher.respawn_time;
        respawnTimed = true;
    }
    public void request_respawn()
    {
        body.request_respawn();
    }
    /// <summary>
    /// Animation function
    /// This function reset menu and remove all items in cart
    /// This load function assume the menu starts when player owns nothing (death)
    /// </summary>
    public void SubPurchase_load()
    {
        buffer_inventory_size = body.inventory_size;
        buffer_inventory_capacity = 0;
        buffer_money = body.money + cvar_watcher.insurance_money;
        buffer_sp = body.skill_points;
        
        cart.Clear();
        s_cart.Clear();
        //Render
        Text_money.text = "$ " + buffer_money;
        Text_sp.text = "SP: " + Mathf.Clamp(cvar_watcher.maxSkillSpentPerDeath - s_cart.Count, 0, buffer_sp) + " / " + buffer_sp;
        SubPurchase_item_refresh();
        for (int i = 0; i < purchase_buttons.Length; i++)
        {
            if (purchase_buttons[i].GetComponent<digit_mod_generic>() != null)
            {
                purchase_buttons[i].GetComponent<digit_mod_generic>().reset();
            }
        }
        for (int i = 0; i < upgrade_buttons.Length; i++)
        {
            if (can_spend_sp())//only turn button unselected if theres point to use and is allowed to use
            {
                upgrade_buttons[i].colors = set_button_color(0);
            }
        }

    }
    
    /// <summary>
    /// Animation function: time when the menu should disable
    /// </summary>
    public void SubPurchase_hide()
    {

    }
    public void item_button_toggle(int item_idx)
    {
        if (!cart.Contains((byte)item_idx))
        {
            SubPurchase_item_add((byte)item_idx);
        }
        else
        {
            SubPurchase_item_remove((byte)item_idx);
        }
    }
    public void ammo_button_add_minus(int item_idx)
    {
        if (Input.GetMouseButtonUp(0))
        {
            SubPurchase_item_add((byte)item_idx);
        }
        else if(Input.GetMouseButtonUp(1) && cart.Contains((byte)item_idx))
        {
            SubPurchase_item_remove((byte)item_idx);
        }
        
    }
    public void ammo_button_minus(int item_idx)
    {
        if (cart.Contains((byte)item_idx))
        {
            SubPurchase_item_remove((byte)item_idx);
        }
    }

    public void upgrade_button_toggle(int item_idx)
    {
        SubPurchase_upgrade_add((byte)item_idx);
    }

    
    public void SubPurchase_upgrade_add(byte upgrade_idx)
    {
        if (s_cart.Count < cvar_watcher.maxSkillSpentPerDeath && buffer_sp > 0)
        {
            buffer_sp -= 1;
            s_cart.Add(upgrade_idx);
            //Render
            upgrade_buttons[upgrade_idx].GetComponent<digit_mod_generic>().set(body.upgrade_value(upgrade_idx, upgrade_buttons[upgrade_idx].GetComponent<digit_mod_generic>().get(), -1));
            //upgrade_buttons[upgrade_idx].GetComponent<digit_mod_generic>().add(1);
            Text_sp.text = "SP: " + Mathf.Clamp(cvar_watcher.maxSkillSpentPerDeath - s_cart.Count, 0, buffer_sp) + " / " + buffer_sp;
            if (buffer_sp <= 0 || s_cart.Count >= cvar_watcher.maxSkillSpentPerDeath)//If no point to use, turn all button selected
            {
                for (int i = 0; i < upgrade_buttons.Length; i++)
                {
                    upgrade_buttons[i].colors = set_button_color(2);
                }
            }
        }
        else
        {
            //Debug.LogError("not enough sp");
        }
    }

    public void SubPurchase_upgrade_removeall()
    {
        for (int i = 0; i < s_cart.Count; i++)//reduce the points in those selected buttons
        {
            digit_mod_generic digit = upgrade_buttons[s_cart[i]].GetComponent<digit_mod_generic>();
            digit.set(body.upgrade_value(s_cart[i], digit.get(), -1, true));
            //upgrade_buttons[s_cart[i]].GetComponent<digit_mod_generic>().subtract(1);
        }


        buffer_sp += (ushort)s_cart.Count;
        s_cart.Clear();

        //Render
        Text_sp.text = "SP: " + Mathf.Clamp(cvar_watcher.maxSkillSpentPerDeath - s_cart.Count, 0, buffer_sp) + " / " + buffer_sp;
        if (can_spend_sp())//turn all buttons unselected if can spend point
        {
            for (int i = 0; i < upgrade_buttons.Length; i++)
            {
                upgrade_buttons[i].colors = set_button_color(0);
            }
        }
    }

    bool can_spend_sp()
    {
        return buffer_sp > 0 && cvar_watcher.maxSkillSpentPerDeath > 0;
    }

    /// <summary>
    /// This function evaluate if player has enough resources to purchase
    /// </summary>
    /// <param name="item_idx"></param>
    public void SubPurchase_item_add(byte item_idx)
    {
        Equipable_generic item = purchasables[item_idx].GetComponent<Equipable_generic>();
        ushort available_inventory = (ushort)(buffer_inventory_size - buffer_inventory_capacity);
        ushort item_size = item.get_size();
        if (item.price <= buffer_money && item_size <= available_inventory && body.experience >= item.required_experience)
        {
            buffer_money -= item.price;
            buffer_inventory_capacity += item_size;
            cart.Add(item_idx);
            //Render
            Text_money.text = "$ " + buffer_money;
            //purchase_buttons[item_idx].colors = set_button_color(1);

            if (item.tag == CONSTANTS.TAG_AMMO)
            {
                purchase_buttons[item_idx].GetComponent<digit_mod_generic>().add(item.GetComponent<Ammo_generic>().amount);
            }
        }
        else
        {
            //Debug.Log("not enough resources");
        }
        SubPurchase_item_refresh();
    }
    
    /// <summary>
    /// This function remove items from the cart and return resources
    /// </summary>
    /// <param name="item_idx"></param>
    public void SubPurchase_item_remove(byte item_idx)
    {
        Equipable_generic item = purchasables[item_idx].GetComponent<Equipable_generic>();
        buffer_money += item.price;
        buffer_inventory_capacity -= item.get_size();
        cart.Remove(item_idx);
        //Render
        Text_money.text = "$ " + buffer_money;
        if(item.tag == CONSTANTS.TAG_GUN)
        {
            //purchase_buttons[item_idx].colors = set_button_color(0);
        }
        else if(item.tag == CONSTANTS.TAG_AMMO)
        {
            
            purchase_buttons[item_idx].GetComponent<digit_mod_generic>().subtract(item.GetComponent<Ammo_generic>().amount);
            /*
            if (!cart.Contains(item_idx))//If this ammo has reached zero, turn button unselected
            {
                purchase_buttons[item_idx].colors = set_button_color(0);
            }
            */
        }
        SubPurchase_item_refresh();
    }
    
    public void SubPurchase_item_refresh()
    {
        for(int i = 0; i < purchasables.Length; i++)
        {
            Equipable_generic item = purchasables[i].GetComponent<Equipable_generic>();
            if (cart.Contains((byte)i))//selected
            {
                purchase_buttons[i].colors = set_button_color(1);
            }
            else if (item.get_size() > buffer_inventory_size || item.price > buffer_money || item.required_experience > body.experience)//disable
            {
                purchase_buttons[i].colors = set_button_color(2);
            }
            else//unselected
            {
                purchase_buttons[i].colors = set_button_color(0);
            }
        }
    }
    /// <summary>
    /// Unselected: 0; Selected: 1; Disabled: 2
    /// </summary>
    /// <param name="mode"></param>
    /// <returns></returns>
    ColorBlock set_button_color(int mode)
    {
        ColorBlock new_btn_color = upgrade_buttons[0].colors;//Dummy button color
        if (mode == 0)//Unselected
        {
            new_btn_color.highlightedColor = SubPurchase_unselected_highlight;
            new_btn_color.normalColor = SubPurchase_unselected_normal;
            new_btn_color.pressedColor = SubPurchase_unselected_pressed;
            new_btn_color.disabledColor = SubPurchase_unselected_disabled;
        }
        else if(mode == 1)//Selected
        {
            new_btn_color.highlightedColor = SubPurchase_selected_highlight;
            new_btn_color.normalColor = SubPurchase_selected_normal;
            new_btn_color.pressedColor = SubPurchase_selected_pressed;
            new_btn_color.disabledColor = SubPurchase_selected_disabled;
        }
        else//Disabled
        {
            new_btn_color.highlightedColor = SubPurchase_disable_highlight;
            new_btn_color.normalColor = SubPurchase_disable_normal;
            new_btn_color.pressedColor = SubPurchase_disable_pressed;
            new_btn_color.disabledColor = SubPurchase_disable_disabled;
        }
        return new_btn_color;
    }
    public void SubPurchase_checkout()
    {
        if(body == null)
        {
            //Debug.LogError("Purchases go to no recipant!");
            return;
        }

        //Host player only send command that will modify on server-side; Client send both cmd and local modification
        body.GetComponent<Player_controller>().Cmd_buy(cart.ToArray(), s_cart.ToArray());
        //if (!body.isServer)
        //{
        //    body.upgrade_stat(s_cart.ToArray());
        //}
    }

    public void summary(Body_generic.Character_type winner)
    {
        if(body.character_type == winner)
        {
            Text_summary.text = "You won";
        }
        else if (body.character_type == Body_generic.Character_type.Nothing)
        {
            Text_summary.text = "No one won";
        }
        else
        {
            Text_summary.text = "You lost";
        }
        Destroy(Submenu_killed_by);
        Destroy(Submenu_purchase);
        Submenu_summary.SetActive(true);
    }
}
