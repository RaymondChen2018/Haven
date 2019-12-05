using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pool_watcher : MonoBehaviour {

    static public Pool_watcher Singleton;

    public List<Bullet_generic> bullet_pool = new List<Bullet_generic>();
    public List<Laser_generic> laser_pool = new List<Laser_generic>();
    public List<ParticleSystem> flame_pool = new List<ParticleSystem>();
    public List<Tesla_generic> tesla_pool = new List<Tesla_generic>();
    public List<Transform> teslaBolt_pool = new List<Transform>();

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
		
	}

    public Bullet_generic request_blt()
    {
        if (bullet_pool.Count <= 0)
        {
            return null;
        }
        Bullet_generic ret = bullet_pool[0];
        bullet_pool.RemoveAt(0);
        
        ret.gameObject.SetActive(true);
        ret.reset();
        return ret;
    }
    
    public void recycle_blt(Bullet_generic blt)
    {
        bullet_pool.Add(blt);
        blt.gameObject.SetActive(false);
    }
    public void recycle_lsr(Laser_generic lsr)
    {
        laser_pool.Add(lsr);
        lsr.gameObject.SetActive(false);
    }
    public Laser_generic request_lsr()
    {
        if (laser_pool.Count <= 0)
        {
            return null;
        }
        Laser_generic ret = laser_pool[0];
        laser_pool.RemoveAt(0);

        ret.gameObject.SetActive(true);
        ret.reset();
        return ret;
    }
    public ParticleSystem request_flame()
    {
        if (flame_pool.Count <= 0)
        {
            return null;
        }
        ParticleSystem ret = flame_pool[0];
        flame_pool.RemoveAt(0);

        ret.gameObject.SetActive(true);
        ret.Play();
        return ret;
    }
    public void recycle_flame(ParticleSystem flame)
    {
        flame_pool.Add(flame);
        flame.Stop();
        flame.gameObject.SetActive(false);
    }
    public Tesla_generic request_tsla()
    {
        if (tesla_pool.Count <= 0)
        {
            return null;
        }

        Tesla_generic ret = tesla_pool[0];
        tesla_pool.RemoveAt(0);

        ret.gameObject.SetActive(true);
        ret.reset();
        return ret;
    }

    public void recycle_tsla(Tesla_generic tsla)
    {
        tesla_pool.Add(tsla);
        tsla.gameObject.SetActive(false);
    }

    public Transform request_tslaBolt()
    {
        if (teslaBolt_pool.Count <= 0)
        {
            return null;
        }
        Transform ret = teslaBolt_pool[0];
        teslaBolt_pool.RemoveAt(0);

        ret.gameObject.SetActive(true);
        ret.GetComponent<TrailRenderer>().Clear();
        return ret;
    }

    public void recycle_tslaBolt(Transform tsla)
    {
        teslaBolt_pool.Add(tsla);
        tsla.gameObject.SetActive(false);
    }
}
