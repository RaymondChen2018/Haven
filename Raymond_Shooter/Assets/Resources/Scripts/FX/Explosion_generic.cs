using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Explosion_generic : MonoBehaviour {

    public float radius;
    public float power;
    public float thermal;
    public bool countParticleLife = true;
    public bool isMeshEffect = true;
    public bool isFlashBang = false;
    public LayerMask block_fltr;
    public LayerMask hit_fltr;
    public float windLife = 1;
    public float windRadius_start;
    public float windRadius_end;
    public float windMain_start;
    public float windMain_end;
    public ParticleSystem PS;
    public Light light;
    public SpriteRenderer sprite;
    public Material material = null;
    public GameObject exp_decal;


    [HideInInspector] public GameObject activator = null;
    private ParticleSystem.Particle[] particles;
    private MeshFilter mesh_fltr;
    private MeshRenderer mesh_renderer;
    private Mesh mesh;
    private Vector3[] vertices;
    private Color[] colors_mesh;
    private int[] triangles;
    private bool converted = false;
    private float time_to_destroyWind;
    private float time_to_destroy;
    private Color mesh_color;
    private float light_intensity;
    private float sprite_alpha;

    // Use this for initialization
    void Start () {
        Vector2 epicenter = PS.transform.position;
        AudioSource audio = GetComponent<AudioSource>();
        audio.pitch = Time.timeScale;
        //Damage
        if (Client_watcher.Singleton.isServer)
        {
            //Stun
            if (isFlashBang)
            {
                float stun_radius = light.range * 0.8f;
                Collider2D[] stun_colliders = Physics2D.OverlapCircleAll(epicenter, stun_radius, hit_fltr);
                for (int i = 0; i < stun_colliders.Length; i++)
                {
                    
                    RaycastHit2D hit = Physics2D.Linecast(epicenter, stun_colliders[i].transform.position, block_fltr);
                    if (hit.collider == null && stun_colliders[i].GetComponent<Body_hitbox_generic>() != null)
                    {
                        GameObject char_main = stun_colliders[i].GetComponent<Body_hitbox_generic>().body.gameObject;
                        float dist = Vector2.Distance(char_main.transform.position, epicenter);
                        if (dist <= stun_radius)
                        {
                            float stun_ratio = (1 - dist / stun_radius) * thermal / 10000;
                            char_main.GetComponent<Body_generic>().Rpc_stunned_screen(stun_ratio);
                        }
                    }
                }
            }
            //Hurt
            Collider2D[] colliders = Physics2D.OverlapCircleAll(epicenter, radius, hit_fltr);
            Body_generic activator_body = activator.GetComponent<Body_generic>();
            for (int i = 0; i < colliders.Length; i++)
            {
                RaycastHit2D hit = Physics2D.Linecast(epicenter, colliders[i].transform.position, block_fltr);
                if (hit.collider == null)
                {
                    float dist = Vector2.Distance(colliders[i].transform.position, epicenter);
                    if (dist <= radius)
                    {

                        //force
                        Vector2 force = CONSTANTS.DAMAGE_FORCE_MULTIPLIER * ((Vector2)colliders[i].transform.position - epicenter).normalized * (1 - dist / radius) * power * 6;


                        GameObject char_main;
                        if (colliders[i].GetComponent<Body_hitbox_generic>() != null && colliders[i].tag != "headsquirt")
                        {
                            char_main = colliders[i].GetComponent<Body_hitbox_generic>().body.gameObject;
                        }
                        else {
                            char_main = colliders[i].gameObject;
                        }
                        
                        


                        //damage
                        float dmg_physical = (1 - dist / radius) * power / 3;
                        float dmg_thermal = (1 - dist / radius) * thermal / 3;
                        RaycastHit2D[] hits = Physics2D.LinecastAll(epicenter, char_main.transform.position, hit_fltr);
                        for (int j = 0; j < hits.Length - 1; j++)
                        {
                            if(hits[j].collider.gameObject.tag == "structure" && hits[j].collider.gameObject != char_main)//There are (other) structure blocking
                            {
                                dmg_physical = 0;
                                break;
                            }
                            else if (hits[j].collider.GetComponent<Body_generic>() != null)
                            {
                                dmg_physical -= hits[j].collider.GetComponent<Body_generic>().tissue_dense;
                            }
                        }
                        if(dmg_physical < 0)
                        {
                            dmg_physical = 0;
                        }
                        if(dmg_thermal < 0)
                        {
                            dmg_thermal = 0;
                        }
                        //Characters
                        if (char_main.GetComponent<Body_generic>() != null)
                        {
                            char_main.GetComponent<Body_generic>().damage(activator, force: force, dmg_physics: dmg_physical, dmg_thermal: dmg_thermal);
                        }
                        //Structures
                        else if(char_main.GetComponent<Structure_generic>() != null)
                        {
                            if (char_main.layer == activator.layer)//Ally structure
                            {
                                continue;
                            }
                            else
                            {
                                char_main.GetComponent<Structure_generic>().health -= dmg_physical;
                            }
                        }


                        /*
                        else if (char_main.GetComponent<Prop_generic>() != null)
                        {
                            char_main.GetComponent<Prop_generic>().damage(activator, force: force, dmg_physics: dmg_physical, dmg_thermal: dmg_thermal);
                        }
                        else if (char_main.GetComponent<Bullseye_generic>() != null)
                        {
                            char_main.GetComponent<Bullseye_generic>().damage(activator, dmg_physics: dmg_physical, dmg_thermal: dmg_thermal);
                        }
                        */


                        if (char_main.GetComponent<Body_generic>() != null && char_main.GetComponent<Body_generic>().isPlayer)
                        {
                            char_main.GetComponent<Body_generic>().Rpc_add_force(force);
                        }
                        else if (char_main.GetComponent<Rigidbody2D>() != null)//host player & server object
                        {
                            char_main.GetComponent<Rigidbody2D>().AddForce(force);
                        }
                    }
                }
            }
        }

        //Cosmetic
        if (!Server_watcher.Singleton.isDedicated())
        {
            if (isMeshEffect)//mesh explosion; frag
            {
                mesh = new Mesh();
                mesh_fltr = GetComponent<MeshFilter>();
                mesh_renderer = GetComponent<MeshRenderer>();
                mesh_fltr.mesh = mesh;
                mesh_renderer.sortingLayerName = "Flame";
                mesh_color = mesh_renderer.sharedMaterial.color;
                if (material != null)
                {
                    mesh_renderer.sharedMaterial = material;
                }
                time_to_destroyWind = Time.time + windLife;
            }
            else//no mesh no particle, flashbang
            {
                Destroy(GetComponent<MeshFilter>());
                Destroy(GetComponent<MeshRenderer>());
            }
            PS.transform.position = new Vector3(epicenter.x, epicenter.y, -2.5f);
            if (countParticleLife)
            {
                time_to_destroy = Time.time + PS.main.startLifetime.constant;
            }
            else
            {
                time_to_destroy = Time.time + GetComponent<AudioSource>().clip.length;
            }
            light_intensity = light.intensity;
            if (sprite != null)
            {
                sprite_alpha = sprite.color.a;
            }
            if (exp_decal != null)
            {
                FindObjectOfType<Decal_manager>().add_decal(exp_decal, epicenter, radius / 7, 10);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
	
	// Update is called once per frame
	void Update () {
        if(Time.time > time_to_destroy)
        {
            Destroy(gameObject);
        }

        light.intensity = light_intensity * (time_to_destroy - Time.time) / PS.main.startLifetime.constant;
        if(sprite != null)
        {
            Color temp = sprite.color;
            temp.a = sprite_alpha * (time_to_destroy - Time.time) / PS.main.startLifetime.constant;
            sprite.color = temp;
        }
        if (Time.time < time_to_destroyWind)
        {
            PS.GetComponent<WindZone>().windMain = Mathf.Lerp(windMain_start, windMain_end, Time.time / time_to_destroyWind);
            PS.GetComponent<WindZone>().radius = Mathf.Lerp(windRadius_start, windRadius_end, Time.time / time_to_destroyWind);
        }else if(PS.GetComponent<WindZone>() != null)
        {
            Destroy(PS.GetComponent<WindZone>());
        }
        if (!converted && isMeshEffect)
        {
            converted = true;
            particles = new ParticleSystem.Particle[PS.particleCount];
            PS.GetParticles(particles);
            vertices = new Vector3[particles.Length];
            colors_mesh = new Color[particles.Length];
            triangles = new int[(particles.Length - 2) * 3];
            for(int i=0;i<particles.Length; i++)
            {
                vertices[i] = particles[i].position;
                vertices[i].z = 0;
                colors_mesh[i] = mesh_color;
            }
            for(int i = 0; i < particles.Length - 2; i++)
            {
                triangles[i * 3] = i;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors = colors_mesh;
            mesh.RecalculateNormals();
        }else if(isMeshEffect)
        {
            GetComponent<CircleCollider2D>().enabled = false;
            PS.GetParticles(particles);
            
            for (int i = 0; i < particles.Length; i++)
            {
                Color red = Color.red;
                red.r = 0.1f;
                red.a = 0;
                colors_mesh[i] = Color.Lerp(mesh_color, red, 1 - particles[i].remainingLifetime / particles[i].startLifetime);
                //colors_mesh[i] = Color.Lerp(mesh_color, red, 1 - (time_to_destroy - Time.time));
                vertices[i] = particles[i].position;
                vertices[i].z = 0;
            }
            
            //Color red = Color.red;
            //red.r = 0.1f;
            //red.a = 0;
            //mesh_renderer.material.color = Color.Lerp(mesh_color, red, 1 - (time_to_destroy - Time.time));
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors = colors_mesh;
            mesh.RecalculateNormals();
        }
        //Debug.Log(mesh.colors[4].a);
    }
    private void OnDrawGizmos()
    {
        //Gizmos.DrawWireSphere(PS.transform.position, radius);
    }
    /*
    private void OnTriggerEnter2D(Collider2D collision, LayerMask mask)
    {
        RaycastHit2D hit = Physics2D.Linecast(PS.transform.position, collision.transform.position, block_fltr);
        if(hit.collider == null)
        {
            float dist = Vector2.Distance(collision.transform.position, PS.transform.position);
            if (dist <= radius)
            {
                if (local)
                {
                    //force
                    if (collision.GetComponent<Rigidbody2D>() != null)
                    {
                        collision.GetComponent<Rigidbody2D>().AddForce((collision.transform.position - PS.transform.position).normalized * (1 - dist / radius) * power * 30);
                    }


                    //damage
                    float damage = (1 - dist / radius) * power / 3;
                    RaycastHit2D[] hits = Physics2D.LinecastAll(PS.transform.position, collision.transform.position, hit_fltr);
                    
                    Debug.Log(hits.Length);
                    for (int i = 0; i < hits.Length - 1; i++)
                    {
                        if (hits[i].collider.GetComponent<Body_generic>() != null)
                        {
                            damage -= hits[i].collider.GetComponent<Body_generic>().tissue_dense;
                        }
                        Debug.Log("hey");
                    }
                    
                    
                    if (collision.GetComponent<Body_generic>() != null && damage > 0)
                    {
                        collision.GetComponent<Body_generic>().damage(activator, dmg_physics: damage);
                    }
                    else if (collision.GetComponent<Bullseye_generic>() != null && damage > 0)
                    {
                        collision.GetComponent<Bullseye_generic>().damage(activator, dmg_physics: damage);
                    }
                }
            }
        }
    }
    */
}
