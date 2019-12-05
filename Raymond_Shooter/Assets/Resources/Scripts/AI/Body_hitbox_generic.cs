using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Body_hitbox_generic : MonoBehaviour {
    public enum HitBox_Type
    {
        Body,
        Head
    }
    public Body_generic body;
	public float damage_multiplier = 1.0f;
}
