// TextureScale_generic
using UnityEngine;

public class TextureScale_generic : MonoBehaviour
{
	public float unit_x = 1f;

	public float unit_y = 1f;
    public bool DontStretchX = false;
    public bool DontStretchY = false;
    public bool DontShiftX = false;
    public bool DontShiftY = false;
    private void Start()
	{
        Vector3 mainTextureScale;
        Material material;
        if (GetComponent<MeshRenderer>() != null)
        {
            mainTextureScale = GetComponent<MeshRenderer>().material.mainTextureScale;
            material = GetComponent<MeshRenderer>().material;
        }
        else
        {
            mainTextureScale = GetComponent<SpriteRenderer>().material.mainTextureScale;
            material = GetComponent<SpriteRenderer>().material;
            GetComponent<SpriteRenderer>().receiveShadows = true;
        }

        float x_scaled = mainTextureScale.x;
        float y_scaled = mainTextureScale.y;
        if (!DontStretchX)
        {
            x_scaled = mainTextureScale.x * transform.localScale.x / unit_x;
        }
        if (!DontStretchY)
        {
            y_scaled = mainTextureScale.y * transform.localScale.y / unit_y;
        }
        material.SetTextureScale("_MainTex", new Vector2(x_scaled, y_scaled));


        float ratio_x = unit_x / mainTextureScale.x;
        float ratio_y = unit_y / mainTextureScale.y;
        Vector2 warp = (Vector2)transform.position - new Vector2(transform.localScale.x / 2, transform.localScale.y / 2);
        float x_shifted = 0;
        float y_shifted = 0;
        if (!DontShiftX)
        {
            x_shifted = warp.x / ratio_x;
        }
        if (!DontShiftY)
        {
            y_shifted = warp.y / ratio_y;
        }
        material.SetTextureOffset("_MainTex", new Vector2(x_shifted, y_shifted));
        Destroy(this);
	}
}
