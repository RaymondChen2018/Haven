using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Fx_watcher : MonoBehaviour {

    static public Fx_watcher Singleton;

    //Blood Effects
    public List<GameObject> bleedFX_obj_list;
    public List<Vector2> bleedFX_pos_list;
    public List<float> bleedFX_angle_list;
    public List<bool> bleedFX_isHS_list;
    float time_to_spawn_blood = 0;

    //Fluid
    public List<ParticleSystem> particleSystems = new List<ParticleSystem>();
    public float merge_threshold = 0.2f;
    public Material material = null;

    private MeshFilter mesh_fltr;
    private MeshRenderer mesh_renderer;
    private Mesh mesh;
    private Color mesh_color;
    private float time_to_vanish;


    void Awake()
    {
        Singleton = this;
    }
    // Use this for initialization
    void Start () {
        //Remove all unnecessary objects on dedicated instance
        if (Server_watcher.Singleton.isDedicated())
        {
            Destroy(GameObject.Find("Background"));
            Destroy(GameObject.Find("Background3D"));
            Destroy(GameObject.Find("Shadow"));
            Destroy(GameObject.Find("Lights"));
        }
        //Blood effect
        bleedFX_obj_list = new List<GameObject>();
        bleedFX_pos_list = new List<Vector2>();
        bleedFX_angle_list = new List<float>();
        bleedFX_isHS_list = new List<bool>();


        //Polygonal Fluid effect
        mesh = new Mesh();
        mesh_fltr = GetComponent<MeshFilter>();
        mesh_renderer = GetComponent<MeshRenderer>();
        mesh_fltr.mesh = mesh;
        mesh_color = mesh_renderer.sharedMaterial.color;//mesh_renderer.material.color;
        if (material != null)
        {
            mesh_renderer.sharedMaterial = material;
        }
        mesh_renderer.sortingLayerName = "Smoke";
    }
    void OnDestroy()
    {
        Destroy(mesh_renderer.material);
        for (int i = 0; i < bleedFX_obj_list.Count; i++)
        {
            Destroy(bleedFX_obj_list[i]);
        }
        Singleton = null;
    }
    public void request_bleed(GameObject obj, Vector2 hit_point, float angle, bool is_headshot)
    {

        bleedFX_obj_list.Add(obj);
        bleedFX_pos_list.Add(hit_point);
        bleedFX_angle_list.Add(angle);
        bleedFX_isHS_list.Add(is_headshot);
    }
    // Update is called once per frame
    void Update () {
        //Blood effect
        if (Client_watcher.Singleton != null && Client_watcher.Singleton.isServer)
        {

            if (Time.realtimeSinceStartup > time_to_spawn_blood)//Spawn bleed reference real time to increase effect accuracy in slow mo
            {
                time_to_spawn_blood = Time.realtimeSinceStartup + CONSTANTS.BLOOD_SPAWN_INTERVAL;
                if (bleedFX_obj_list.Count > 1)
                {
                    short[] pox = new short[bleedFX_pos_list.Count * 2];
                    short[] ang = new short[bleedFX_angle_list.Count];
                    for (int i = 0; i < bleedFX_pos_list.Count; i++)
                    {
                        pox[i] = (short)(bleedFX_pos_list[i].x * CONSTANTS.SYNC_POS_MUTIPLIER);
                        pox[bleedFX_pos_list.Count + i] = (short)(bleedFX_pos_list[i].y * CONSTANTS.SYNC_POS_MUTIPLIER);
                        ang[i] = CONSTANTS.seed_float_to_short(bleedFX_angle_list[i], 360);
                    }

                    Client_watcher.Singleton.Rpc_spawn_blood(bleedFX_obj_list.ToArray(), pox, ang, bleedFX_isHS_list.ToArray());

                }
                else if (bleedFX_obj_list.Count == 1)
                {
                    bleedFX_obj_list[0].GetComponent<Body_generic>().Rpc_bleed(bleedFX_pos_list[0], bleedFX_angle_list[0], bleedFX_isHS_list[0]);
                }
                bleedFX_obj_list.Clear();
                bleedFX_pos_list.Clear();
                bleedFX_angle_list.Clear();
                bleedFX_isHS_list.Clear();
            }
        }

        //Polygonal fluid effect
        if (particleSystems.Count == 0)
        {
            mesh.Clear();
            return;
        }
        List<Vector3> vertices_list = new List<Vector3>();
        List<Color> colors_list = new List<Color>();
        List<int> triangles_list = new List<int>();
        int triangle_gap = 0;
        int reference = 0;
        for (int x=0;x<particleSystems.Count;x++)
        {
            ParticleSystem PS = particleSystems[x];
            if ((PS == null) || (!PS.isPlaying && PS.particleCount == 0))
            {
                particleSystems.RemoveAt(x);
                continue;
            }
            //draw
            if (PS.particleCount > 3)
            {
                //Debug.Log("burn");
                ParticleSystem.Particle[] particles = new ParticleSystem.Particle[PS.particleCount];
                PS.GetParticles(particles);

                //flame Fx
                
                int vertices_count = particles.Length;
                particles = particles.OrderBy(o => o.remainingLifetime).ToArray();
                
                for (int i = 0; i < vertices_count; i++)
                {
                    Color color = mesh_color;


                    float mean = 0;
                    if (i < particles.Length - 2)
                    {
                        mean = (Vector2.Distance(particles[i].position, particles[i + 1].position) + Vector2.Distance(particles[i].position, particles[i + 2].position) + Vector2.Distance(particles[i + 1].position, particles[i + 2].position)) / 3;
                    }
                    else if(i < particles.Length - 1)
                    {
                        mean = Vector2.Distance(particles[i].position, particles[i + 1].position);
                    }

                    color.a = (CONSTANTS.FLAME_TRI_STANDARD_DIST / mean) * particles[i].remainingLifetime / particles[i].startLifetime;
                    colors_list.Add(color);
                    Vector3 pos = particles[i].position;
                    pos.z = CONSTANTS.FX_Z;
                    vertices_list.Add(pos);
                }

                for (int i = 0; i < vertices_count - 2; i++)
                {
                    if (Mathf.Abs(particles[i].remainingLifetime - particles[i + 2].remainingLifetime) > merge_threshold)
                    {
                        triangles_list.Add(0 + triangle_gap);
                        triangles_list.Add(0 + triangle_gap);
                        triangles_list.Add(0 + triangle_gap);
                    }
                    else
                    {
                        triangles_list.Add(i + triangle_gap);
                        triangles_list.Add(i + 1 + triangle_gap);
                        triangles_list.Add(i + 2 + triangle_gap);
                        if (reference < i + 2 + triangle_gap)
                        {
                            reference = i + 2 + triangle_gap;
                        }
                    }
                }
                triangle_gap += vertices_count;
            }

        }
        mesh.Clear();
        mesh.vertices = vertices_list.ToArray();
        mesh.triangles = triangles_list.ToArray();
        mesh.colors = colors_list.ToArray();
        mesh.RecalculateNormals();
	}
}
