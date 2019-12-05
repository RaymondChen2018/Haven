using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Sound_watcher : MonoBehaviour {
    static public Sound_watcher Singleton;
    public List<AI_generic> tune_in_ais = new List<AI_generic>();
    public List<Structure_generic> tune_in_structures = new List<Structure_generic>();
    static float summon_interval = 1;
    private float time_to_summon = 0;

    void Awake()
    {
        Singleton = this;
    }
    // Use this for initialization


    // Update is called once per frame
    void Update()
    {

    }
    public void tune_in_structure(Structure_generic structure)
    {
        if (tune_in_structures.Contains(structure))
        {
            return;
        }

        tune_in_structures.Add(structure);
    }
    public void tune_out_structure(Structure_generic structure)
    {
        if (!tune_in_structures.Contains(structure))
        {
            return;
        }
        tune_in_structures.Remove(structure);
    }
    public void tune_in_ai(AI_generic listener)
    {
        if (tune_in_ais.Contains(listener))
        {
            return;
        }
        
        listener.tuned_in = true;
        tune_in_ais.Add(listener);
    }
    public void tune_out_ai(AI_generic listener)
    {
        if (!tune_in_ais.Contains(listener))
        {
            return;
        }
        listener.tuned_in = false;
        tune_in_ais.Remove(listener);
    }

    public void summon_listener(Vector2 sound_source, float range, int Ally)
    {
        if(Time.time <= time_to_summon)
        {
            return;
        }
        time_to_summon = Time.time + summon_interval;
        for (int i = 0; i < tune_in_ais.Count; i++)
        {
            if(tune_in_ais[i] == null)
            {
                tune_in_ais.RemoveAt(i);
                i--;
            }
            else if(Vector2.Distance(tune_in_ais[i].transform.position, sound_source) < range)
            {
                //call in
                if (Ally != tune_in_ais[i].gameObject.layer)
                {

                    tune_in_ais[i].alert_new_sound(sound_source);
                }
            }
        }
        for (int i = 0; i < tune_in_structures.Count; i++)
        {
            if (tune_in_structures[i] == null)
            {
                tune_in_structures.RemoveAt(i);
                i--;
            }
            else if (Vector2.Distance(tune_in_structures[i].transform.position, sound_source) < range)
            {
                //Alert structure
                tune_in_structures[i].alert_level++;

            }
        }
    }

    void OnDestroy()
    {
        Singleton = null;
    }
}
