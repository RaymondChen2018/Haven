using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Decal_manager : MonoBehaviour {

    static public Decal_manager Singleton;

    public int fade_time;
    public Material cheap_lerp;
    public Material cheap;
    public int max_cheaps;
    public List<KeyValuePair<GameObject, float>> decals_expensive = new List<KeyValuePair<GameObject, float>>();
    public Queue<GameObject> decals_cheap = new Queue<GameObject>();
    public List<KeyValuePair<GameObject, float>> decals_cheap_fade = new List<KeyValuePair<GameObject, float>>();

    void Awake()
    {
        Singleton = this;
    }

    void OnDestroy()
    {
        Singleton = null;
    }


    // Update is called once per frame
    void Update () {
        //expensive
		for(int i = 0; i < decals_expensive.Count; i++)
        {
            if(Time.realtimeSinceStartup > decals_expensive[i].Value)
            {
                if (Time.realtimeSinceStartup > decals_expensive[i].Value + fade_time)
                {
                    //Destroy(decals[i].Key);
                    decals_cheap.Enqueue(decals_expensive[i].Key);
                    Destroy(decals_expensive[i].Key.GetComponent<SpriteRenderer>().material);
                    decals_expensive[i].Key.GetComponent<SpriteRenderer>().sharedMaterial = cheap;
                    decals_expensive.RemoveAt(i);
                    i--;
                }
                else
                {
                    float t = (Time.realtimeSinceStartup - decals_expensive[i].Value) / (300 * fade_time);//material lerping too quick
                    decals_expensive[i].Key.GetComponent<SpriteRenderer>().material.Lerp(decals_expensive[i].Key.GetComponent<SpriteRenderer>().sharedMaterial, cheap_lerp, t);
                }
            }
        }
        //cheap
        if(decals_cheap.Count > max_cheaps)
        {
            for (int i = 0; i < decals_cheap.Count - max_cheaps; i++)
            {
                decals_cheap_fade.Add(new KeyValuePair<GameObject, float>(decals_cheap.Dequeue(), Time.realtimeSinceStartup + fade_time));
            } 
        }
        //fade cheaps
        for(int i = 0; i < decals_cheap_fade.Count; i++)
        {

            if (Time.realtimeSinceStartup > decals_cheap_fade[i].Value)
            {
                Destroy(decals_cheap_fade[i].Key.GetComponent<SpriteRenderer>().material);
                Destroy(decals_cheap_fade[i].Key);
                decals_cheap_fade.RemoveAt(i);
                i--;
            }
            else
            {
                float t = (decals_cheap_fade[i].Value - Time.realtimeSinceStartup) / fade_time;
                Color temp = decals_cheap_fade[i].Key.GetComponent<SpriteRenderer>().sharedMaterial.color;
                temp.a = t;
                decals_cheap_fade[i].Key.GetComponent<SpriteRenderer>().material.color = temp;
            } 
        }
    }

    //This will spawn decal and monitor it on the fly
    //This decal will go through to a cheap mode material which is sprite_diffuse
    public void add_decal(GameObject decal, Vector2 position, float scale, float life)
    {
        GameObject _decal = Instantiate(decal, new Vector3(position.x, position.y, CONSTANTS.BACKGROUND_OFFSETZ), Quaternion.Euler(0, 0, Random.Range(-180, 180)));
        _decal.transform.localScale = new Vector3(scale, scale, 1);
        decals_expensive.Add(new KeyValuePair<GameObject,float>(_decal, Time.realtimeSinceStartup + life));
    }
}
