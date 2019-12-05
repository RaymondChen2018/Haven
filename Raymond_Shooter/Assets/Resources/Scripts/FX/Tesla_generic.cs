using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
public class Tesla_generic : MonoBehaviour, Iprojectile {
    public class teslaNode
    {
        
        public GameObject target;
        public float volt_to_parent;
        public float volts_left;
        /*
        {
            get
            {
                return volts_left;
            }
            set
            {
                volts_left = value;
            }
        }
        */
        
        public int[] stream_idxes;
    }
    //public float volts = 1000;
    //public float min_spread_volts = 100;
    public float max_bounce_range = 4;
    public float life = 0.5f;
    float time_spread_finish = 0;
    
    [HideInInspector]
    public Vector2 aimdir;
    public LayerMask hit_fltr;
    public LayerMask obstacle_fltr;
    [HideInInspector] public LayerMask initial_hit_fltr;
    [HideInInspector]
    public bool local = false;
    public GameObject stream_prefab;
    public List<GameObject> collidedlist;
    [HideInInspector] public List<GameObject>[] stream_path;
    List<Transform> stream_trails = new List<Transform>();
    [HideInInspector] public Pool_watcher pool_watcher;
    [HideInInspector] public GameObject activator = null;
    Vector2 spread_origin;
    List<teslaNode> spread_list = new List<teslaNode>();

    public void reset()
    {
        time_spread_finish = 0;
        spread_list.Clear();
        stream_trails.Clear();
        
            
        collidedlist.Clear();
        if(stream_path != null)
        {
            for (int i = 0; i < stream_path.Length; i++)
            {
                stream_path[i] = null;
            }
            stream_path = null;
        }
        
        
    }
    /// <summary>
    /// Local-sided
    /// This calculate how many streams should be created;
    /// What targets should each of the streams go;
    /// </summary>
    public void emit()
    {
        /*
        if (!local)
        {
            return;
        }
        spread_origin = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, aimdir, volts * CONSTANTS.VOLT_DIST_RATIO, hit_fltr + obstacle_fltr);
        
        Body_hitbox_generic hitbox;
        float volts_left = volts;
        int number_streams = 1;
        if (hit)
        {
            hitbox = hit.collider.GetComponent<Body_hitbox_generic>();
            if (hitbox != null) //If hit body, Damage and spread
            {
                volts_left = (1 - (Vector2.Distance(hit.point, transform.position) / (volts * CONSTANTS.VOLT_DIST_RATIO))) * volts;
                collidedlist.Add(hitbox.body.gameObject);
                number_streams = (int)(volts_left / min_spread_volts);
                spread_origin = hit.point;
            }
            else
            {
                remove();
                return;
            }
        }
        else
        {
            remove();
            return;
        }

        stream_path = new List<GameObject>[number_streams];
        spread_list.Clear();
        teslaNode initial_node = new teslaNode();
        initial_node.target = hit.collider.gameObject;
        initial_node.volts_left = volts_left;
        initial_node.stream_idxes = new int[number_streams];
        collidedlist.Add(hit.collider.gameObject);
        for (int i = 0; i < number_streams; i++)
        {
            initial_node.stream_idxes[i] = i;
        }
        //Debug.LogError("distance: "+ Vector2.Distance(hit.point, transform.position) + "; volt left: "+volts_left+ "; number streams: "+number_streams);
        spread_list.Add(initial_node);
        detect();
        */

        //stream_trails.Clear();
        if (stream_path.Length == 0)
        {
            remove();
            return;
        }
        //GameObject[][] trails_path = new GameObject[stream_path.Length][];
        for (int i = 0; i < stream_path.Length; i++)
        {
            if (stream_path[i] != null && stream_path[i].Count > 0)
            {
                Transform stream = pool_watcher.request_tslaBolt();
                if (stream == null)
                {
                    stream = Instantiate(stream_prefab, transform.position, Quaternion.identity).transform;
                }
                else
                {
                    stream.position = transform.position;
                }
                stream_trails.Add(stream);
            }
        }
    }
    
    public void detect()
    {
        /*
        int path_size = 0;
        //while there are stem that is branching out
        while(spread_list.Count > 0)//Each step
        {
            //Debug.LogError("step: "+spread_list.Count);
            List<teslaNode> spread_temp = new List<teslaNode>();
            //For each of the stem
            for (int i = 0; i < spread_list.Count; i++)
            {
                int j;
                teslaNode node = spread_list[i];
                spread_origin = node.target.transform.position;
                //Debug.LogError("From node: "+node.target + "; volt: "+node.volts_left);
                //Mark all the stream with its index
                for (j = 0; j < node.stream_idxes.Length; j++)
                {
                    if(stream_path[node.stream_idxes[j]] == null)
                    {
                        stream_path[node.stream_idxes[j]] = new List<GameObject>();
                    }
                    stream_path[node.stream_idxes[j]].Add(node.target);
                    path_size++;
                    //Debug.LogError("add: " + node.target);
                }
                //overlap circle

                Collider2D[] victims = Physics2D.OverlapCircleAll(spread_origin, Mathf.Min(node.volts_left * CONSTANTS.VOLT_DIST_RATIO, max_bounce_range), hit_fltr);
                //Debug.LogError("hit count: " + victims.Length);
                if (victims.Length == 0)
                {
                    continue;
                }
                
                //Sort circle
                victims = victims.OrderBy(o => Vector2.Distance(o.transform.position, spread_origin)).ToArray();
                //Pointer
                j = 0;
                //Compute distance for the next stem, put on dist_to_parent
                float volt = node.volts_left;
                float total_volt_to_parent = 0;
                teslaNode next_node = new teslaNode();
                next_node.target = victims[j].gameObject;
                next_node.volt_to_parent = Vector2.Distance(victims[j].transform.position, spread_origin) / CONSTANTS.VOLT_DIST_RATIO;
                total_volt_to_parent = next_node.volt_to_parent;
                //While this stem has voltage && can reach next stem
                while (volt > min_spread_volts && next_node.volt_to_parent < volt)
                {
                    //If the next stemmed object isnt on collided list
                    if (!collidedlist.Contains(next_node.target) && !Physics2D.Linecast(spread_origin, next_node.target.transform.position, obstacle_fltr))
                    {

                        //subtract copy of dist/volt_ratio\
                        volt -= next_node.volt_to_parent + min_spread_volts;
                        total_volt_to_parent += next_node.volt_to_parent;

                        //Copy the stem onto a spread_temp list
                        spread_temp.Add(next_node);
                        
                        //Put the stemmed object on collided list
                        collidedlist.Add(next_node.target);
                        //Debug.LogError("spread to: " + next_node.target + "; volt distance: " + next_node.volt_to_parent);
                    }
                    
                    //Increment pointer
                    j++;

                    if(j >= victims.Length)
                    {
                        break;
                    }
                    next_node = new teslaNode();
                    //take the next stem on the sorted list
                    next_node.target = victims[j].gameObject;
                    //Compute distance for the next stem
                    next_node.volt_to_parent = Vector2.Distance(victims[j].transform.position, spread_origin) / CONSTANTS.VOLT_DIST_RATIO;
                    
                }
                //Pointer current_idx = 0
                int current_idx = 0;
                //For each on the spread_temp list
                for (int k = 0; k < spread_temp.Count; k++)
                {
                    next_node = spread_temp[k];
                    //stem volt = (1 - dist_to_parent / volt_of_current_stem) * volt_of_current_stem
                    if(spread_temp.Count == 1)
                    {
                        next_node.volts_left = volt;
                    }
                    else
                    {
                        next_node.volts_left = ((total_volt_to_parent - next_node.volt_to_parent) / (spread_temp.Count - 1)) * volt / total_volt_to_parent;
                    }
                    
                    //Debug.LogError("spread to: " + next_node.target + "; volt distance: " + next_node.volt_to_parent);
                    //Debug.LogError("volts total: "+node.volts_left+"; total parent: "+ total_volt_to_parent + "; this: "+ next_node.volts_left+ "; volt to parent: "+ next_node.volt_to_parent);
                    //Initialize volt / minimum for the stream idxes
                    next_node.stream_idxes = new int[Mathf.Max(1,(int)(next_node.volts_left / min_spread_volts))];//Must be at least one stream
                    //For volt / minimum
                    
                    for(int y = 0; y < next_node.stream_idxes.Length; y++)
                    {
                        //stream_idxes[] = current_stream_idxes[k]
                        //Debug.LogError("spread count: " + spread_temp.Count + "; number rays: "+next_node.stream_idxes.Length+"; number ray current: "+ node.stream_idxes.Length+"; currect:"+current_idx);
                        next_node.stream_idxes[y] = node.stream_idxes[current_idx];
                        //current_idx++
                        current_idx++;
                    }
                }
            }
            spread_list.Clear();
            spread_list = spread_temp;
        }
        //Instantiate trails
        stream_trails.Clear();
        if (stream_path.Length == 0)
        {
            remove();
            return;
        }
        //GameObject[][] trails_path = new GameObject[stream_path.Length][];
        GameObject[] serialized_path = new GameObject[path_size + stream_path.Length];
        int serialized_index = 0;
        for (int i = 0; i < stream_path.Length; i++)
        {
            for(int j = 0; j < stream_path[i].Count; j++)
            {
                serialized_path[serialized_index] = stream_path[i][j];
                serialized_index++;
            }
            serialized_index++;
            if(stream_path[i] != null && stream_path[i].Count > 0)
            {

                Transform stream = pool_watcher.request_tslaBolt();
                if(stream == null)
                {
                    stream = Instantiate(stream_prefab, transform.position, Quaternion.identity).transform;
                }
                else
                {
                    stream.position = transform.position;
                }
                
                stream_trails.Add(stream);
                //trails_path[i] = stream_path[i].ToArray();
            }
        }
        
        
        //Send trail paths
        Body_generic activator_body = activator.GetComponent<Body_generic>();
        if(activator_body.isPlayer && !activator_body.hasAuthority)
        {
            //activator_body.Cmd_send_tesla_path(serialized_path);
        }
        else
        {
            //activator_body.Rpc_send_tesla_path(serialized_path);
        }



        if (activator_body.isPlayer)
        {

            // && !activator.GetComponent<Body_generic>().isServer
            //activator_body.Cmd_send_tesla_path(trails_path);
        }
        else
        {
            //activator_body.Rpc_send_tesla_path(trails_path, activator);
        }
        //Calculation damage
        */
    }
    

    public int impact_character(Body_hitbox_generic hit_box, Vector2 hit_point)
    {
        throw new System.NotImplementedException();
    }

    public void impact_object(GameObject obj, Vector2 hit_point)
    {
        throw new System.NotImplementedException();
    }

    public void remove()
    {
        pool_watcher.recycle_tsla(this);
        for (int i = 0; i < stream_trails.Count; i++)
        {
            pool_watcher.recycle_tslaBolt(stream_trails[i]);

        }
        stream_trails.Clear();
        

    }

    public void travel()
    {

        bool isSpreading = false;
        
        if(stream_path != null)
        {
            
            for (int i = 0; i < stream_path.Length; i++)
            {
                if (stream_path[i] != null && stream_path[i].Count > 0 && stream_path[i][0] != null)
                {
                    stream_trails[i].position = stream_path[i][0].transform.position;
                    stream_path[i].RemoveAt(0);
                    isSpreading = true;
                }
            }
        }
        
        if (!isSpreading)
        {
            if(time_spread_finish <= 0)
            {
                time_spread_finish = Time.time;
            }
            else if(Time.time > time_spread_finish + life)
            {
                remove();
            }
        }
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		travel();
	}
}
