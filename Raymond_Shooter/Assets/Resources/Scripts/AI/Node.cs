using System.Collections;
using System.Collections.Generic;
//using UnityEditor;
using UnityEngine;


public class Node : MonoBehaviour {
    public int objective = 0;
    public int area_code = 0;
    public bool isDynamic = false;
    public List<GameObject> neighboor;
    public Navigation_manual._Node reference;
    
    
    //private void OnDrawGizmos()
    //{
        
    //    for(int i = 0; i < neighboor.Count; i++)
    //    {
    //        if (neighboor[i] != null)
    //        {
    //            if (neighboor[i].GetComponent<Node>().neighboor.Contains(gameObject))
    //            {
    //                Gizmos.color = Color.green;
    //            }
    //            else
    //            {
    //                Gizmos.color = Color.red;
    //            }
    //            Gizmos.DrawLine(transform.position, neighboor[i].transform.position);
    //        } 
    //    }
    //    Handles.Label(transform.position, gameObject.name);
    //}
    
}
