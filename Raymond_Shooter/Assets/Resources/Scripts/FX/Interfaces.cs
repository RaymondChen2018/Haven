using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface Iprojectile
{
    void emit();
    //This function define how the projectile travel
    void travel();
    //This function define how the projectile predicts
    void detect();
    //This function will detect character collision and calculate damage
    //Return 1: keep the projectile going
    //Return 0: this object shouldn't be impacted
    //Return -1: this projectile is stopped by character
    int impact_character(Body_hitbox_generic hit_box, Vector2 hit_point);
    //This function will detect object collision
    //projectile will stop no matter what
    void impact_object(GameObject obj, Vector2 hit_point);
    //This function define how the projectile behave after stopping impact & removing
    void remove();
}
public interface IEquiptable
{
    Equipable_generic.ITEM_TYPE getType();
    ushort getWeight();
    ushort getSize();
}
public interface IDamageActivator
{
    /// <summary>
    /// FeedBack to the activator when the damage is done to another character
    /// </summary>
    void OnHitCharacter(IDamageVictim victim, float damage, Vector2 hitPoint, Vector2 force, bool isHeadShot, DamageType damageType);
    bool isPlayer();
    bool isServer();
    bool canDamage(IDamageVictim victim);

    GameObject getGameObject();
}
public interface IDamageVictim
{
    /// <summary>
    /// FeedBack to the activator when the damage is done to another character
    /// </summary>
    void OnDamagedBy(IDamageActivator activator, float damage, Vector2 hitPoint, Vector2 force, bool isHeadShot, DamageType damageType);
    GameObject getGameObject();
}


