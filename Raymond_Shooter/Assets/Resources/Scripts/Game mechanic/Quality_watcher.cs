using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Quality_watcher : MonoBehaviour {

    void Awake()
    {
        Application.targetFrameRate = CONSTANTS.MAX_FPS;
        Renderer[] mats = FindObjectsOfType<Renderer>();
        //public Dictionary<Material, Material> material


        foreach (Renderer rend in mats)
        {
            if (QualitySettings.GetQualityLevel() == 0)
            {
                rend.material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                rend.material.SetFloat("_SpecularHighlights", 0f);
                rend.material.SetTexture("_NormalMap", new Texture());
                rend.material.SetFloat("_NormalMap", 0f);
                GameObject.Find("Background3D_Camera").GetComponent<Camera>().enabled = false;
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
            }
            if (QualitySettings.GetQualityLevel() <= 1)
            {
                rend.receiveShadows = false;
                if (rend.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)
                {
                    Destroy(rend.gameObject);
                }
                else
                {
                    rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }
        
        
       
    }
    
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
