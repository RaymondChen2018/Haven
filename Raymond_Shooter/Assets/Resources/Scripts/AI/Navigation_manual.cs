using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

//shared path node optimization
public class Navigation_manual : NetworkBehaviour {
    static public Navigation_manual Singleton;

    public Queue<GameObject> pathRequestQueue = new Queue<GameObject>();
    public int count;


    public int process_thredshold = 5;
    public class _Node
    {
        public Vector2 position;
        public GameObject reference;
        public List<_Node> neighboor = new List<_Node>();
        public List<_Node> link = new List<_Node>();
        public float gCost;
        public float hCost;
        public float fCost
        {
            get
            {
                return gCost + hCost;
            }
        }
        public _Node parent = null;
        public void initialize()
        {
            gCost = 0;
            hCost = 0;
            parent = null;
            link.Clear();
            for(int i = 0; i < neighboor.Count; i++)
            {
                link.Add(neighboor[i]);
            }
        }
    }

    public LayerMask Path_block;
    public LayerMask LOS_block;
    public LayerMask nav_area_layer;
    public BoxCollider2D[] patrol_areas;
    public Nav_area_generic[] nav_areas;
    public List<_Node> area_dict = new List<_Node>();//set of navigation nodes via area code
    public _Node[] nodes;
    public Vector2 hiding_spots;
    public List<_Node> nodes_dyn = new List<_Node>();

    void Awake()
    {
        Singleton = this;
    }

    void Start()
    {
        Node[] nodes_gameobjects = transform.GetComponentsInChildren<Node>();
        for (int i = 0; i < nodes_gameobjects.Length; i++)//create nav nodes and correspond actual nodes with them
        {
            _Node new_node = new _Node();
            new_node.position = nodes_gameobjects[i].transform.position;
            nodes_gameobjects[i].reference = new_node;
            new_node.reference = nodes_gameobjects[i].gameObject;
            Collider2D[] areas = Physics2D.OverlapPointAll(new_node.position, nav_area_layer);
            area_dict.Add(new_node);
            if (areas == null || areas.Length == 0)
            {
                Debug.LogError("Node "+ nodes_gameobjects[i].name + " cant find an nav_area");
            }
            for(int j = 0; j < areas.Length; j++)
            {
                //Debug.Log("node: " + nodes_gameobjects[i].name + " added to: " + areas[j].name);

                areas[j].GetComponent<Nav_area_generic>().area_nodes.Add(new_node);
                
            }
            
        }
        for (int i = 0; i < nodes_gameobjects.Length; i++)//create nav nodes and correspond actual nodes with them
        {
            for (int j = 0; j < nodes_gameobjects[i].neighboor.Count; j++)//Assign neighboors
            {
                
                nodes_gameobjects[i].reference.neighboor.Add(nodes_gameobjects[i].neighboor[j].GetComponent<Node>().reference);
            }
        }


        for (int i = 0; i < nodes_gameobjects.Length; i++)//destroy static nodes
        {
            Destroy(nodes_gameobjects[i].gameObject);
        }
        
    }

    //Need optimization
    //Initialize every subject seekers' surrounding nodes as startnodes; spreading from target and pickup start node and assign to according subject's path
    [ServerCallback]
    void Update () {
        int process_count = process_thredshold;
        while (pathRequestQueue.Count > 0 && process_count > 0)//something missing here?
        {
            
            process_count -= 1;
            for (int i = 0; i < area_dict.Count; i++)
            {
                area_dict[i].initialize();
            }
            GameObject ai_obj = pathRequestQueue.Dequeue();
            if (ai_obj != null)
            {
                AI_generic ai = ai_obj.GetComponent<AI_generic>();
                GameObject destine = null;
                if (ai_obj != null)
                {
                    //addition code here


                    destine = ai.navtoobj;
                    float target_size = -1;
                    Vector2 destine_location = ai.navtopos;
                    if (destine != null && destine.GetComponent<Body_generic>() != null)//if target doesnt have size (preset locations etc.)
                    {
                        target_size = destine.GetComponent<Body_generic>().size;
                    }
                    


                    //Navigate
                    if (destine_location != CONSTANTS.VEC_NULL)
                    {
                        ai.path = Astar_algorithm(ai_obj.transform.position, destine_location, -1, -1);
                        //Debug.DrawLine(ai.transform.position, ai.path[0].position, Color.white, 10);
                        //Debug.DrawLine(ai.path[0].position, destine_location, Color.white, 10);
                        ai.path_index = 0;
                    }
                }
            }
        }
    }
    public void destroy_dynamics()
    {
        for(int i = 0; i < nodes_dyn.Count; i++)
        {
            Destroy(nodes_dyn[i].reference);
        }
    }
    
    public void activate_nodes(int objective)
    {
        if (nodes_dyn != null)
        {
            nodes_dyn.Clear();
        }
        GameObject[] nodes_gameobjects = GameObject.FindGameObjectsWithTag("node");
        List<_Node> nodes_list = new List<_Node>();
        for (int i = 0; i < nodes_gameobjects.Length; i++)//create nav nodes and correspond actual nodes with them
        {
            if (nodes_gameobjects[i].GetComponent<Node>().objective == objective)
            {
                _Node new_node = new _Node();
                new_node.position = nodes_gameobjects[i].transform.position;
                nodes_gameobjects[i].GetComponent<Node>().reference = new_node;
                new_node.reference = nodes_gameobjects[i];
                nodes_list.Add(new_node);
            }
        }
        for (int i = 0; i < nodes_list.Count; i++)//Assign neighboors
        {
            List<GameObject> neighboor = nodes_list[i].reference.GetComponent<Node>().neighboor;
            for (int j = 0; j < neighboor.Count; j++)
            {
                nodes_list[i].neighboor.Add(neighboor[j].GetComponent<Node>().reference);
            }
        }
        for (int i = 0; i < nodes_list.Count; i++)//destroy static nodes
        {
            if (!nodes_list[i].reference.GetComponent<Node>().isDynamic)
            {
                Destroy(nodes_list[i].reference);

            }else
            {
                nodes_dyn.Add(nodes_list[i]);
            }
        }
    }
    //This algorithm doesnt include target objects' positions as nodes
    List<_Node> Astar_algorithm(Vector2 zombie, Vector2 player, float zombie_size, float player_size)
    {
        List<_Node> startNode = surround_nodes(zombie, -1, 0.1f);
        List<_Node> targetNode = surround_nodes(player, -1, 0.1f);

        if ((targetNode.Count == 0) || (startNode.Count == 0))//if there is no nearby node to keep the player/zombie in touch, abort search
        {
            startNode = surround_nodes(zombie, -1, zombie_size);
            targetNode = surround_nodes(player, -1, player_size);
        }
        
        if ((targetNode.Count == 0) || (startNode.Count == 0))
        {
            return null;
        }
        for (int i = 0; i < startNode.Count; i++)
        {
            startNode[i].gCost = Vector2.Distance(startNode[i].position, zombie);
            startNode[i].hCost = Vector2.Distance(startNode[i].position, player);
        }
        List<_Node> openSet = startNode;
        HashSet<_Node> closeSet = new HashSet<_Node>();
        while (openSet.Count > 0)
        {
            _Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                {
                    currentNode = openSet[i];
                }
            }
            openSet.Remove(currentNode);
            closeSet.Add(currentNode);
            if (targetNode.Contains(currentNode))
            {
                return RetracePath(startNode, currentNode);
            }
            for (int i = 0; i < currentNode.link.Count; i++)
            {
                _Node neighboor = currentNode.link[i];

                if (closeSet.Contains(neighboor))
                {
                    continue;
                }
                float newMovementCostToNeighboor = currentNode.gCost + Vector2.Distance(neighboor.position, currentNode.position);
                if (newMovementCostToNeighboor < neighboor.gCost || !openSet.Contains(neighboor))
                {
                    neighboor.gCost = newMovementCostToNeighboor;
                    neighboor.hCost = Vector2.Distance(neighboor.position, player);
                    neighboor.parent = currentNode;
                    if (!openSet.Contains(neighboor))//neighboor unexplored
                    {
                        openSet.Add(neighboor);
                    }
                }
            }
        }
        return null;
    }
    List<_Node> RetracePath(List<_Node> startNode, _Node endNode)
    {
        List<_Node> path = new List<_Node>();
        _Node currentNode = endNode;
        
        while (!startNode.Contains(currentNode) && currentNode != null)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;

    }

    //Get the surrounding nodes within the area
    public List<_Node> surround_nodes(Vector2 position, float size, float min_dist)
    {
        List<_Node> ret = new List<_Node>();
        Nav_area_generic area = get_area(position);
        if (area != null)
        {
            for (int i = 0; i < area.area_nodes.Count; i++)
            {
                bool seen = LOS(position, area.area_nodes[i].position, size, Path_block);
                if (seen && Vector2.Distance(position, area.area_nodes[i].position) > size)
                {
                    ret.Add(area.area_nodes[i]);
                }
            }
        }
        else if(ret.Count == 0)
        {
            //Debug.LogError("Position: " + position + " cant find surrounding nodes");
        }
        return ret;
    }
    public Vector2 nearest_cover(Vector2 pos, Vector2 enemy_pos)
    {
        List<_Node> ret = new List<_Node>();
        Nav_area_generic area = get_area(pos);

        //Get a list of nodes where the enemy doesn't have a LOS to
        if (area != null)
        {
            int idx = 0;
            do
            {
                for (int i = 0; i < area.area_nodes.Count; i++)
                {
                    bool seen = LOS(enemy_pos, area.area_nodes[i].position, -1, LOS_block);
                    if (!seen)
                    {
                        ret.Add(area.area_nodes[i]);
                    }
                }
                if (ret.Count > 0)
                {
                    break;
                }
                area = nav_areas[idx];
                idx++;
            }
            while (idx <= nav_areas.Length);
        }
        if (ret.Count == 0)
        {
            Debug.LogError("Position: " + pos + "cant find a cover");
            return CONSTANTS.VEC_NULL;
        }

        //Find the closest cover
        float min_dist = Vector2.Distance(pos, ret[0].position);
        int j = 0;
        for (int i = 1; i < ret.Count; i++)
        {
            if (Vector2.Distance(pos, ret[i].position) < min_dist)
            {
                min_dist = Vector2.Distance(pos, ret[i].position);
                j = i;
            }
        }
        return ret[j].position;
    }

    Nav_area_generic get_area(Vector2 position)
    {
        Collider2D area = Physics2D.OverlapPoint(position, nav_area_layer);
        if(area == null)
        {
            //Debug.LogError("Navigation area Error: " + position + "  cant find a navigation area");
            return null;
        }
        return area.GetComponent<Nav_area_generic>();
    }
    /*
    List<_Node> Astar_algorithm(Vector2 zombie, Vector2 player, float zombie_size, float player_size)
    {
        List<_Node> startNode = surround_nodes(zombie, zombie_size, 0.1f);
        List<_Node> targetNode = surround_nodes(player, player_size, 0.1f);
        
        if ((targetNode.Count == 0) || (startNode.Count == 0))//if there is no nearby node to keep the player/zombie in touch, abort search
        {
            startNode = surround_nodes(zombie, -1, zombie_size);
            targetNode = surround_nodes(player, -1, player_size);
        }
        if ((targetNode.Count == 0) || (startNode.Count == 0))
        {
            return null;
        }
        for (int i = 0; i < startNode.Count; i++)
        {
            startNode[i].gCost = Vector2.Distance(startNode[i].position, zombie);
            startNode[i].hCost = Vector2.Distance(startNode[i].position, player);
        }
        List<_Node> openSet = startNode;
        HashSet<_Node> closeSet = new HashSet<_Node>();
        while (openSet.Count > 0)
        {
            _Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                {
                    currentNode = openSet[i];
                }
            }
            openSet.Remove(currentNode);
            closeSet.Add(currentNode);
            if (targetNode.Contains(currentNode))
            {
                //Debug.DrawLine(currentNode.position, player.transform.position, Color.red);
                return RetracePath(startNode, currentNode);
            }
            for (int i = 0; i < currentNode.link.Count; i++) //foreach (Node neighboor in currentNode.link)
            {
                _Node neighboor = currentNode.link[i];
                if (closeSet.Contains(neighboor))
                {
                    continue;
                }
                float newMovementCostToNeighboor = currentNode.gCost + Vector2.Distance(neighboor.position, currentNode.position);
                if (newMovementCostToNeighboor < neighboor.gCost || !openSet.Contains(neighboor))
                {
                    neighboor.gCost = newMovementCostToNeighboor;
                    neighboor.hCost = Vector2.Distance(neighboor.position, player);
                    neighboor.parent = currentNode;
                    if (!openSet.Contains(neighboor))//neighboor unexplored
                    {
                        openSet.Add(neighboor);
                    }
                }
            }
        }
        return null;
    }

    
    List<_Node> RetracePath(List<_Node> startNode, _Node endNode)
    {
        List<_Node> path = new List<_Node>();
        _Node currentNode = endNode;
        while (!startNode.Contains(currentNode) && currentNode != null)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        
        return path;
        
    }
    
    public List<_Node> surround_nodes(Vector2 position, float size, float min_dist)
    {
        List<_Node> ret = new List<_Node>();
        for (int i = 0; i < nodes.Length; i++)
        {
            bool seen = LOS(position, nodes[i].position, size, Path_block);
            if (seen && Vector2.Distance(position, nodes[i].position) > size)
            {
                ret.Add(nodes[i]);
            }
        }
        return ret;
    }
    
    public Vector2 nearest_cover(Vector2 pos, Vector2 enemy_pos)
    {
        List<_Node> ret = new List<_Node>();
        for (int i = 0; i < nodes.Length; i++)
        {
            bool seen = LOS(enemy_pos, nodes[i].position, -1, LOS_block);
            if (!seen)
            {
                ret.Add(nodes[i]);
            }
        }
        if (ret == null)
        {
            return new Vector2();
        }
        float min_dist = Vector2.Distance(pos, ret[0].position);
        int j = 0;
        for (int i = 1; i < ret.Count; i++)
        {
            if (Vector2.Distance(pos, ret[i].position) < min_dist)
            {
                min_dist = Vector2.Distance(pos, ret[i].position);
                j = i;
            }
        }
        return ret[j].position;
    }
    */
    //return true when sight/path clear
    //return false when something between
    public bool LOS(Vector2 start, Vector2 end, float size, LayerMask los_b)
    {
        if(size != -1)
        {
            float viewdist = Vector2.Distance(start, end);
            RaycastHit2D Hit = Physics2D.BoxCast(start, new Vector2(size, size), Vector2.Distance(end, start), end - start, viewdist, los_b);
            if (Hit.collider == null)
            {
                return true;
            }
            return false;
        }else
        {
            RaycastHit2D Hit = Physics2D.Linecast(start, end, los_b);
            if (Hit.collider == null)
            {
                return true;
            }
            return false;
        }
    }



    public void RequestPath(GameObject zombie)//, GameObject player, float z_size, float p_size)
    {
        if (!pathRequestQueue.Contains(zombie))
        {
            pathRequestQueue.Enqueue(zombie);
        }
    }

    void OnDestroy()
    {
        Singleton = null;
    }
}
