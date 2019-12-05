using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Npc_skin_generic : MonoBehaviour {
    /// <summary>
    /// This id must be in incremental from 0
    /// </summary>
    public sbyte skin_id = 0;
    public List<Sprite> sprites_pool = new List<Sprite>();
    public SpriteRenderer skin_part;
    public SpriteRenderer skin_part_backcull = null;

}
