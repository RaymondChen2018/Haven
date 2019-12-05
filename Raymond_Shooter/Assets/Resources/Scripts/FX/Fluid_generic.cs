using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Fluid_generic : MonoBehaviour {
    public float DmgParticle_chance = 0.3f;
    public GameObject DmgParticle = null;
    public float DmgParticle_start_size = 0.1f;
    public float DmgParticle_end_size = 0.5f;
    public float DmgParticle_start_dmg = 1.0f;
    public float DmgParticle_end_dmg = 0.1f;
    public float merge_threshold = 0.2f;
    public float z_offset = 0;
    public Material material = null;
    public ParticleSystem PS;

    

    private List<GameObject> DmgParticles = new List<GameObject>();
    private MeshFilter mesh_fltr;
    private MeshRenderer mesh_renderer;
    private Mesh mesh;
    private Color mesh_color;
    private float time_to_vanish;
    [HideInInspector] public bool local = false;
    [HideInInspector] public bool vanish = false;
    

	// Use this for initialization
	void Start () {
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
    }
    public void remove()
    {
        vanish = true;
        GetComponent<Fluid_generic>().PS.GetComponent<WindZone>().windMain = 0;
        GetComponent<Fluid_generic>().PS.gameObject.transform.parent = null;
        PS.Stop();
        time_to_vanish = Time.time + PS.main.startLifetime.constant;
        
    }
    // Update is called once per frame
    void Update () {
        if (((vanish)&&(Time.time > time_to_vanish)) || PS == null)
        {
            if(PS != null)
            {
                Destroy(PS.gameObject);
            }
            Destroy(gameObject);
            return;
        }
        if(PS.particleCount > 3)
        {
            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[PS.particleCount];
            PS.GetParticles(particles);

            //flame damage
            
            if ((local) && (DmgParticle != null))
            {
                int numParticles = (int)(PS.particleCount * DmgParticle_chance);
                if (DmgParticles.Count < numParticles)//emit more
                {
                    for (int i = 0; i < numParticles - DmgParticles.Count; i++)
                    {
                        GameObject DmgP = Instantiate(DmgParticle);
                        DmgP.GetComponent<Particle_damage_generic>().activator = PS.transform.parent.GetComponent<Equipable_generic>().user;
                        DmgParticles.Add(DmgP);
                    }
                }
                else//remove some
                {
                    for(int i = 0; i < DmgParticles.Count - numParticles; i++)
                    {
                        Destroy(DmgParticles[0]);
                        DmgParticles.RemoveAt(0);
                    }
                }
                //Debug.Log("ratio: "+(particles.Length / DmgParticles.Count));
                for(int i = 0, j = 0; i < DmgParticles.Count; i++, j+= (particles.Length / DmgParticles.Count))
                {
                    DmgParticles[i].transform.position = particles[j].position;
                    DmgParticles[i].GetComponent<CircleCollider2D>().radius = Mathf.Lerp(DmgParticle_start_size, DmgParticle_end_size, (1 - particles[j].remainingLifetime / particles[j].startLifetime));
                    DmgParticles[i].GetComponent<Particle_damage_generic>().dmg_thermal = Mathf.Lerp(DmgParticle_start_dmg, DmgParticle_end_dmg, (1 - particles[j].remainingLifetime / particles[j].startLifetime));
                }
            }
            

            //flame Fx
            Vector3[] vertices_mesh = new Vector3[particles.Length];
            Color[] colors_mesh = new Color[particles.Length];
            particles = particles.OrderBy(o => o.remainingLifetime).ToArray();
            /*
            for (int i = 0; i < particles.Length - 1; i++)
            {
                Color color = mesh_color;
                color.a = particles[i].remainingLifetime / particles[i].startLifetime;
                colors_mesh[i] = color;
                vertices_mesh[i] = particles[i].position;
                vertices_mesh[i].z = z_offset;
            }
            */
            for (int i = 0; i < particles.Length - 1; i++)
            {
                Color color = mesh_color;

                float mean;
                if (i < particles.Length - 2)
                {
                    mean = (Vector2.Distance(particles[i].position, particles[i + 1].position) + Vector2.Distance(particles[i].position, particles[i + 2].position) + Vector2.Distance(particles[i + 1].position, particles[i + 2].position)) / 3;
                }
                else
                {
                    mean = Vector2.Distance(particles[i].position, particles[i + 1].position);
                }
                
                color.a = (CONSTANTS.FLAME_TRI_STANDARD_DIST / mean) * (particles[i].remainingLifetime / particles[i].startLifetime);
                //Debug.Log("area: "+ mean);
                colors_mesh[i] = color;
                Vector3 pos = particles[i].position;
                pos.z = CONSTANTS.FX_Z;
                vertices_mesh[i] = pos;
            }
            
            int[] triangles = new int[(vertices_mesh.Length - 2) * 3];
            for (int i = 0; i < vertices_mesh.Length - 3; i++)
            {
                if(Mathf.Abs(particles[i].remainingLifetime - particles[i + 2].remainingLifetime) > merge_threshold)
                {
                    triangles[i * 3] = 0;
                    triangles[i * 3 + 1] = 0;
                    triangles[i * 3 + 2] = 0;
                }else
                {
                    triangles[i * 3] = i;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = i + 2;
                }
            }

            mesh.Clear();
            mesh.vertices = vertices_mesh;
            mesh.triangles = triangles;
            mesh.colors = colors_mesh;
            mesh.RecalculateNormals();
        }else
        {
            if ((local) && (DmgParticle != null))
            {
                for (int i = 0; i < DmgParticles.Count; i++)
                {
                    Destroy(DmgParticles[i]);
                }
                DmgParticles.Clear();
            }
            mesh.Clear();
        }

	}

}
