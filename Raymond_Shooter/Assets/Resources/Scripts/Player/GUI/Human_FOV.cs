using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Human_FOV : MonoBehaviour {
    public float angle_check;
    //public float viewRadius;
    //[Range(0,360)]
    //public float viewAngle;
    public float meshResolution;
    public int edgeResolveiteration;
    public float edgeDstThreshold;
    public float maskCutAwayDst;
    public MeshFilter viewMeshFilter;
    public Mesh viewMesh;
    public LayerMask targetMask;
    public LayerMask obstacleMask;
    public GameObject shadow_fin_template;
    public List<GameObject> shadow_fin_pool = new List<GameObject>();
    public bool fullview;
    public bool reverse;
    private Player_controller controller;

    public Transform sprite_orient;
    GameObject fade_view;
    public List<Transform> visibleTargets = new List<Transform>();
    Body_generic body;
    GameObject Darkness;
    Transform fade_view_left;
    Transform fade_view_right;
    Server_watcher cvar_watcher;
    // Use this for initialization
    void Start()
    {
        
        cvar_watcher = FindObjectOfType<Server_watcher>();
        Darkness = GameObject.Find("Darkness");
        fade_view = GameObject.Find("Fade_view");
        if (!cvar_watcher.losVision)
        {
            Destroy(Darkness);
            Destroy(fade_view);
            Destroy(this);
            return;
        }
        fade_view_left = fade_view.transform.Find("Fade_view_leaf_left");
        fade_view_right = fade_view.transform.Find("Fade_view_leaf_right");
        controller = GetComponent<Player_controller>();
        if (!controller.isLocalPlayer)
        {
            Destroy(this);
            return;
        }
        body = GetComponent<Body_generic>();
        viewMesh = new Mesh();
        viewMesh.name = "View Mesh";
        viewMeshFilter.mesh = viewMesh;
        sprite_orient = GetComponent<Player_controller>().sprite_orient;
        fade_view.transform.localScale = new Vector2(body.viewRadius, body.viewRadius);
        fade_view_left.localRotation = Quaternion.Euler(0, 0, body.viewAngle / 2);
        fade_view_right.localRotation = Quaternion.Euler(0, 0, -body.viewAngle / 2);
        //StartCoroutine("FindTargetsWithDelay", 0.2f);

    }
    IEnumerator FindTargetsWithDelay(float delay)
    {

        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }
    public void Toggle_LOS(bool on)
    {
        if (!cvar_watcher.losVision)
        {
            return;
        }
        if (!on)
        {
            fade_view.SetActive(false);
            fade_view_left.gameObject.SetActive(false);
            fade_view_right.gameObject.SetActive(false);
            Darkness.SetActive(false);
            for(int i = 0; i < shadow_fin_pool.Count; i++)
            {
                shadow_fin_pool[i].GetComponent<SpriteRenderer>().enabled = false;
            }
        }
        else
        {
            fade_view.SetActive(false);
            fade_view_left.gameObject.SetActive(false);
            fade_view_right.gameObject.SetActive(false);
            Darkness.SetActive(true);
            for (int i = 0; i < shadow_fin_pool.Count; i++)
            {
                shadow_fin_pool[i].GetComponent<SpriteRenderer>().enabled = true;
            }
        }
    }
    void FindVisibleTargets()
    {
        visibleTargets.Clear();
        Collider2D[] targetsInViewRadius = Physics2D.OverlapCircleAll(transform.position, body.viewRadius, targetMask);
        

        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;
            Vector2 dirToTarget = (target.position - transform.position).normalized;
            if(Vector2.Angle(transform.right, dirToTarget) < body.viewAngle / 2)
            {
                float dstToTarget = Vector2.Distance(transform.position, target.position);
                if(!Physics2D.Raycast(transform.position,dirToTarget, dstToTarget, obstacleMask))
                {
                    visibleTargets.Add(target);
                    //Debug.Log("in: ");
                }
            }
        }
    }
    void DrawFieldOfView()
    {
        float orientation = sprite_orient.eulerAngles.z;
        fade_view.transform.rotation =  Quaternion.Euler(0, 0, orientation);
        float FOV = body.viewAngle;
        if (reverse)
        {
            orientation -= 180;
            FOV = 360 - FOV;
        }
        int stepCount = Mathf.RoundToInt(FOV * meshResolution);
        float stepAngleSize = FOV / stepCount;
        List<Vector3> viewPoints = new List<Vector3>();
        ViewCastInfo oldViewCast = new ViewCastInfo();
        //Casting rays to detect corners
        int shadow_fins_count = 0;
        for (int i = 0; i <= stepCount; i++)
        {
            float angle;
            if (fullview)
            {
                angle = 180 - stepAngleSize * i;
            }
            else
            {
                angle = orientation + FOV / 2 - stepAngleSize * i;
            }
            


            //Debug.DrawLine(transform.position, transform.position + DirFromAngle(angle, true) * viewRadius, Color.red);
            ViewCastInfo newViewCast = ViewCast(angle);
            
            if (i > 0)
            {
                //bool edgeDsThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDstThreshold;
                float edge_dist = Mathf.Abs(oldViewCast.dst - newViewCast.dst);
                if (oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edge_dist > edgeDstThreshold))
                {
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
                    if (edge.pointA != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointA);
                        //vec_shadow = edge.pointA;
                    }
                    if (edge.pointB != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointB);
                        //vec_clear = edge.pointB;
                    }

                    

                    

                    if(QualitySettings.GetQualityLevel() > 2 && edge_dist > CONSTANTS.SHADOW_FIN_THREDSHOLD)
                    {
                        bool fade_in = true;//This boolean is gonna be very confusing
                        Vector3 vec_shadow = newViewCast.point;
                        Vector3 vec_clear = oldViewCast.point;
                        if (oldViewCast.dst < newViewCast.dst)
                        {
                            fade_in = false;
                            vec_shadow = oldViewCast.point;
                            vec_clear = newViewCast.point;
                        }
                        //Shadow fin
                        GameObject fin;
                        if (shadow_fins_count >= shadow_fin_pool.Count)
                        {
                            fin = Instantiate(shadow_fin_template, vec_shadow, Quaternion.identity);
                            shadow_fin_pool.Add(fin);
                        }
                        else
                        {
                            fin = shadow_fin_pool[shadow_fins_count];
                            fin.SetActive(true);
                        }
                        shadow_fins_count++;
                        Vector2 base_vec = vec_shadow - transform.position;
                        float base_vec_dist = base_vec.magnitude;
                        float base_angle = Mathf.Atan2(base_vec.y, base_vec.x) * 180 / 3.14f - 90;
                        
                        //Extend the soft side of the shadow
                        float offset = Mathf.Atan2(body.size, base_vec_dist) * 180 / 3.14f;
                        float soft_angle = 0;
                        if (fade_in)
                        {
                            soft_angle = base_angle - offset;
                        }
                        else
                        {
                            soft_angle = base_angle + offset;
                        }
                        
                        /*
                        if (fade_left)
                        {
                            base_angle += offset;
                        }
                        else
                        {
                            base_angle -= offset;
                        }
                        */

                        //
                        vec_shadow.z = CONSTANTS.SHADOW_FIN_Z;
                        fin.transform.position = vec_shadow;
                        fin.transform.rotation = Quaternion.Euler(0, 0, base_angle);
                        //bool face_left = fin.transform.localScale.x >= 0;
                        Vector3 flip = fin.transform.localScale;
                        flip.y = Mathf.Max(edge_dist, body.viewRadius - base_vec_dist) * 1.2f;
                        flip.x = (body.viewRadius - base_vec_dist) * (body.size / base_vec_dist) * 1.2f;
                        if (!fade_in)
                        {
                            flip.x *= -1;
                        }
                        fin.transform.localScale = flip;
                        
                    }
                    
                }
            }

            
            //
            viewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
        }
        //Debug.DrawLine(transform.position, transform.position + DirFromAngle(transform.eulerAngles.z, true) * viewRadius, Color.blue);
        //Clean redundant fins
        for (int j = shadow_fins_count; j < shadow_fin_pool.Count; j++)
        {
            shadow_fin_pool[j].SetActive(false);
        }
        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];
        vertices[0] = Vector3.zero;

        //Analyzing points
        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]) + Vector3.right * maskCutAwayDst;
            if(i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }
        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();

    }
    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += sprite_orient.eulerAngles.z;
        }
        return new Vector2(Mathf.Cos(angleInDegrees * Mathf.Deg2Rad),Mathf.Sin(angleInDegrees * Mathf.Deg2Rad));
    }

    //Refine two vertices that form a shadow edge
    EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
    {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;
        for (int i = 0; i < edgeResolveiteration; i++){
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo newViewCast = ViewCast(angle);
            bool edgeDsThresholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > edgeDstThreshold;
            if (newViewCast.hit == minViewCast.hit && (!edgeDsThresholdExceeded))
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }
        return new EdgeInfo(minPoint, maxPoint);
    }
    ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, body.viewRadius, obstacleMask);
        if(hit)
        {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }else
        {
            return new ViewCastInfo(false, transform.position + dir * body.viewRadius, body.viewRadius, globalAngle);
        }
    }
    public struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float dst;
        public float angle;

        public ViewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle)
        {
            hit = _hit;
            point = _point;
            dst = _dst;
            angle = _angle;
        }
    }
    public struct EdgeInfo
    {
        public Vector3 pointA;
        public Vector3 pointB;

        public EdgeInfo(Vector3 _pointA, Vector3 _pointB)
        {
            pointA = _pointA;
            pointB = _pointB;
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (!controller.isLocalPlayer)
        {
            return;
        }
        DrawFieldOfView();
        //StartCoroutine("FindTargetsWithDelay", 0.2f);
        //Vector2 viewAngleA = DirFromAngle(-viewAngle / 2, false);
        //Vector2 viewAngleB = DirFromAngle(viewAngle / 2, false);

        //Debug.DrawLine(transform.position, (Vector2)transform.position + viewAngleA * viewRadius);
        //Debug.DrawLine(transform.position, (Vector2)transform.position + viewAngleB * viewRadius);
        //foreach (Transform visibleTarget in visibleTargets)
        //{
            //Debug.DrawLine(transform.position, visibleTarget.position);
        //}
        //DrawWireArc(fow.transform.position, Vector3.forward, Vector2.left, 360, fow.viewRadius);

    }
}





