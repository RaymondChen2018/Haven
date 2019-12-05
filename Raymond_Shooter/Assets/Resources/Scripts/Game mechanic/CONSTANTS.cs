using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Character = Prototype.NetworkLobby.LobbyPlayer.CharacterType;
public enum DamageType : byte
{
    Physical = 0,
    Thermal = 1,
    Energy = 2,
}
public static class CONSTANTS{
    //Increase rate of fire
    public enum GUN_UPGRADEPRICE_ROF
    {
        M92fs = 50,
        Glock17 = 60,
        DesertEagle = 250,
        SV10 = 400,
        R870 = 1400,
        AA = 2000,
        M4a1 = 1200,
        Fnfal = 1300,
        MP5 = 300,
        MAC10 = 300,
        M249 = 1300,
        RPG7 = 2000,
        AWP = 1300,
        HS50 = 2000
    }
    //Expand gun clip
    public enum GUN_UPGRADEPRICE_CLIP
    {
        M92fs = 40,
        Glock17 = 50,
        DesertEagle = 300,
        SV10 = 2500,
        R870 = 1100,
        AA = 2000,
        M4a1 = 2000,
        Fnfal = 2500,
        MP5 = 800,
        MAC10 = 900,
        M249 = 2300,
        RPG7 = 5000,
        AWP = 2300,
        HS50 = 2500
    }
    //Increase bullet speed
    public enum GUN_UPGRADEPRICE_MUZZLEEFFCIENCY
    {
        M92fs = 250,
        Glock17 = 270,
        DesertEagle = 1800,
        SV10 = 2200,
        R870 = 2500,
        AA = 3700,
        M4a1 = 2800,
        Fnfal = 3000,
        MP5 = 1400,
        MAC10 = 1600,
        M249 = 2500,
        RPG7 = 500,
        AWP = 4000,
        HS50 = 6000
    }
    //Decreases recoil, crosshair enlargement, recoil force
    public enum GUN_UPGRADEPRICE_RECOILSUPPRESSOR
    {
        M92fs = 50,
        Glock17 = 50,
        DesertEagle = 1300,
        SV10 = 200,
        R870 = 300,
        AA = 400,
        M4a1 = 2300,
        Fnfal = 3000,
        MP5 = 1400,
        MAC10 = 1500,
        M249 = 2800,
        RPG7 = 3000,
        AWP = 3500,
        HS50 = 4500
    }
    



    public enum OUTPUT_NAME
    {
        OnMapSpawn,
        OnNewGame,
        OnStartTouch,
        OnEndTouch,
        OutValue,
        OnHitMax,
        OnHitMin,
        OnEntitySpawned,
        OnPass,
        OnIgnited,
        OnExtinguished,
        OnTimer,
        OnBeginSequence,
        OnCancelSequence,
        OnEndSequence,
        OnAE,
        OnDeath,
        OnSpawn,
        OnTrigger
    }
    [System.Serializable]
    public class IO
    {
        public OUTPUT_NAME outputName;
        public int outputParam = -1;
        public UnityEvent input;
        public int invokeTimes = 1;
    };

    public enum ANIM_CODE
    {
        None = -1,
        OnFire,
        TiltPeekWallLeftPistol,
        TiltPeekWallLeftPistolInitAttack,
        TiltPeekWallRightPistol,
        TiltPeekWallRightPistolInitAttack,
        HandsBack,
        TiltPeekWallLeftPistolAmbush,
        TiltPeekWallRightPistolAmbush,
        HiJack
    }
    
    public enum AE_NAME
    {
        None,
        AE_UPDATE_AI_SCRIPT_POSITION,
        AE_GUNFIRE,
        AE_BREACH_STARTLE,
        AE_BREACH_UNSTARTLE,
        AE_BREACH_ALERT_T
    }
    
    public enum MAP_TYPE
    {
        PVP,
        Objective,
        ZSurvival,
        Custom
    }
    public enum UPGRADE_TYPE {
        /// <summary>
        /// Increase HP
        /// </summary>
        Health,
        /// <summary>
        /// Increase physical resiliance and increase tissue dense, lower damage for physical damage and reduce bullet momentum more, 
        /// </summary>
        Phys_resistance,
        /// <summary>
        /// Increase reload speed
        /// </summary>
        Reload,
        /// <summary>
        /// Increase melee damage
        /// </summary>
        Strength,
        /// <summary>
        /// Reduce movement penalty for weight carrying, modifying stress_resistant
        /// </summary>
        Stress,
        /// <summary>
        /// Aim suppression
        /// </summary>
        Aim,
        /// <summary>
        /// Movement speed
        /// </summary>
        Movement,
        /// <summary>
        /// Increase inventory size (on the following round)
        /// </summary>
        Inventory

    }
    public static Color[] PLAYERCOLORS = new Color[] { Color.red, Color.cyan, Color.blue, Color.green, Color.yellow, Color.magenta, new Color(1, 0.5f, 0, 1), Color.white };


    public static Character[][] map_characters = new Character[][]
    {
        new Character[]{Character.Observer, Character.Human, Character.Robot, Character.Zombie},
        new Character[]{Character.Observer, Character.Human, Character.Robot},
        new Character[]{Character.Observer, Character.Human, Character.Robot},
        new Character[]{Character.Observer, Character.Robot}
    };
    
    public static Color COLOR_ALLY = Color.blue;
    public static Color COLOR_ENEMY = Color.red;
    public static Color COLOR_PLAYERLOCAL = Color.green;
    public static Color COLOR_PLAYERALLYOTHER = Color.cyan;
    public static Color COLOR_PLAYERENEMYOTHER = new Color(1, 0.5f, 0, 1);
    public static int NUM_NULL = 872410;
    public static float SYNC_POS_MUTIPLIER = 100f;
    public static int WORLD_WIDTH = 300;
    public static Vector2 VEC_NULL = new Vector2(NUM_NULL,NUM_NULL);
    public static int DROP_TORQUE = 20;
    public static int MAX_BLOOD = 10;
    public static float BLOOD_SPAWN_INTERVAL = 0.1f;
    public static float LATENCY_TEST_INTERVAL = 1;
    public static float BACKGROUND_OFFSETZ = 0.25f;
    public static int CHARACTER_DEAD_Z = 100;//USE ANIMATOR TO HIDE CHARACTER
    public static int CHARACTER_ALIVE_Z = 0;
    public static int RESPAWN_TIME = 20;
    public static Vector2 SPAWN_ITEM_POSITION = new Vector2(500,500);
    public static int MAX_UPGRADE_PER_DEATH = 10;
    public static string TAG_AMMO = "pickup_ammo";
    public static string TAG_GUN = "pickup_gun";
    public static string TAG_GRENADE = "pickup_grenade";
    public static string TAG_PLAYER = "Player";
    public static float NPC_DROP_CHANCE = 0.2f;
    public static int INSSURANCE_MONEY = 150;
    public static float LEVEL_XP_MULTIPLIER = 1.2f;
    public static float AI_SAVE_PERCENT = 0.3f;
    public static float AI_XP_BONUS_RATIO = 1.5f;
    public static int SPAWN_FREEZE_TIME = 10;
    public static int AI_relax_time = 1;
    public static float CAM_MAX_VIEW = 50;
    public static float CAM_MIN_VIEW = 3;
    public static float CAM3D_MAX_Z = 63.1f;
    public static float CAM3D_MIN_Z = -6.3f;
    public static float AI_DETECT_ANGLE = 30;
    public static float AI_DETECT_DIST = 1f;
    public static float AI_DETECT_STEER_FACT = 1.5f;
    public static Vector2 PICK_UP_SIZE = new Vector2(0.5f, 0.5f);
    public static float AI_EYE_SIGHT_TOLERANCE = 1;
    public static int ZOMBIE_SKILLADD_INTERVAL = 180;
    public static float SPAWN_PROTECTION_LENGTH = 0.5f;
    public static float BULLET_FADE_TIME = 0.3f;
    public static float EXPLOSION_OFFSET_WALL = 0.1f;
    public static float DMGSCREEN_MIN_RATIO = 0.1f;
    public static float FX_Z = -0.1f;
    public static int MAX_FPS = 60;
    public static float FPS_SCALE = 45;//Just a constant, not supposed to be changed
    public static float LASER_BASE_WIDTH = 0.01f;
    public static float LASER_TEMP_RAMP = 0.005f;
    public static float FLAME_TRI_STANDARD_DIST = 1f;
    public static float VOLT_DIST_RATIO = 0.05f;
    public static int TESLA_MAX_SPLIT = 3;
    public static int ASH_UNIT_COUNT = 300;
    public static float PROJECTILE_SPEED_MULTI = 0.9f;
    public static float AI_FIND_ZONE_INTERVAL = 1;//The interval AI examine area to see if it can build a base
    public static float STRUCTURE_ALERT_FLUSH_INTERVAL = 10;
    public static int STRUCTURE_ORDER_INTERVAL = 1;
    public static float SCREENSHAKE_DIST = 0.2f;
    public static float SCREENSHAKE_RECOVER_DIST = 12;
    public static float SHADOW_FIN_THREDSHOLD = 1f;
    public static float DAMAGE_FORCE_MULTIPLIER = 4f;
    public static float AI_AIM_TOLERANCE = 2;
    public static float SHADOW_FIN_Z = -5;
    public static float NETWORK_TICK_RATE = 0.5f;
    public static float OBJ_ALLYNPC_REGEN = 5;//hp
    public static float OBJ_ALLYNPC_REGEN_DMGINTERVAL = 3;//seconds;
    public static float FIXED_TIMESTEP = 0.02f;
    public static string ANIM_PARAM_CURSOR_SIZE = "size";
    public static string ANIM_STATE_CURSOR_HIT = "Hit";
    public static int MAX_ROF_FRAMERATE_OVERLOAD = 20;//The maximum amount of additional bullets allowed in case of low framerate
    public static float MAX_GUN_BIAS = 30;


    static public void invokeOutput(OUTPUT_NAME outputName, List<IO> I_O, int outputParameter = -1)
    {
        
        for (int i = 0; i < I_O.Count; i++)
        {
            //Debug.Log("param: "+ I_O[i].outputParam);
            if (I_O[i].outputName == outputName && (I_O[i].outputParam <= 0 || I_O[i].outputParam == outputParameter))
            {
                //Invoke time
                if (I_O[i].input != null)
                {
                    
                    I_O[i].input.Invoke();
                    I_O[i].invokeTimes--;
                }
                else//null input, trash
                {
                    I_O[i].invokeTimes = 0;
                }

                //Trash depleted inputs
                if (I_O[i].invokeTimes <= 0)
                {
                    I_O.RemoveAt(i);
                    i--;
                }
            }
        }
    }



    /// <summary>
    /// The character can still be shot during this period of time after death
    /// </summary>
    public static float DEAD_DAMAGE_PERIOD = 0.2f;

    static public sbyte seed_float_to_sbyte(float seed_angle_float, float range)
    {
        return (sbyte)(127f * (seed_angle_float / range));
    }
    static public float seed_sbyte_to_float(sbyte seed_angle_sbyte, float range)
    {
        return range * ((float)seed_angle_sbyte / 127f);
    }
    static public short seed_float_to_short(float seed_angle_float, float range)
    {
        return (short)(32767f * (seed_angle_float / range));
    }
    static public float seed_short_to_float(short seed_angle_short, float range)
    {
        return range * ((float)seed_angle_short / 32767f);
    }
    static public short comp_rot(float a)
    {
        return (short)a;
    }
    static public float decomp_rot(short a)
    {
        return a;
    }
    static public short comp_pos(float a)
    {
        return (short)(a * SYNC_POS_MUTIPLIER);
    }
    static public float decomp_pos(float a)
    {
        return a / SYNC_POS_MUTIPLIER;
    }
    static public float heat_to_physics(float heat)
    {
        return heat / 200f;
    }
    static public bool int_to_bool(int integer)
    {
        if(integer > 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    static public int bool_to_int(bool boolean)
    {
        if (boolean)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}

    
    

