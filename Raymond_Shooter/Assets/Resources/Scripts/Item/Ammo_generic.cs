using System.Collections;
using UnityEngine;

public class Ammo_generic : MonoBehaviour, IEquiptable {
    /// <summary>
    /// Server-side if unequiped, client-sided if equiped; Update to server when client drops; Update to client when pick up
    /// </summary>
    public ushort amount;
    public enum AmmoType {AT_9mm, AT_45acp, AT_556, AT_762, AT_308, AT_58, AT_12gauge, AT_20gauge, AT_Uranium, AT_Rocket1, AT_SolidFuel, AT_50AE, AT_l477, AT_50BMG}
    public AmmoType ammotype;
    public ushort dispence_amount;
    public ushort bullet_weight;
    public ushort bullet_size;
    public string eject;


	// Use this for initialization
	void Start () {
        gameObject.tag = "pickup_ammo";
    }

    public Equipable_generic.ITEM_TYPE getType()
    {
        return Equipable_generic.ITEM_TYPE.ammo;
    }

    public ushort getWeight()
    {
        return bullet_weight;
    }

    public ushort getSize()
    {
        return bullet_size;
    }
}
