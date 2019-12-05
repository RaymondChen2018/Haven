using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Player_inventory : MonoBehaviour
{
    /// <summary>
    /// Total space of the inventory
    /// </summary>
    //public ushort size;


    public int item_pointer = -1;
    public ushort weight = 0;
    public List<GameObject> item;
    public LayerMask pickup_fltr;
    /// <summary>
    /// Used space
    /// </summary>
    public ushort capacity = 0;
    public float use_reach = 2;

    public float drop_force = 130;
    public float drop_torque = 20;
    //[HideInInspector] public ushort initial_size;

}
