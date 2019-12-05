using UnityEngine;
using UnityEngine.UI;
public class Progress_Bar_generic : MonoBehaviour {
    public RectTransform bar;
    public Color low;
    public Color high;
    public bool reverse = false;
    // Use this for initialization
    void Start () {
		
	}
	
	public void update_progress(float ratio)
    {
        if (reverse)
        {
            ratio = 1-ratio;
        }
        bar.sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x * ratio, bar.sizeDelta.y);
        bar.GetComponent<Image>().color = Color.Lerp(low, high, ratio);
    }
}
