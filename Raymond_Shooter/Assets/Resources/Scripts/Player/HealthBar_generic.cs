using UnityEngine;
using UnityEngine.UI;
public class HealthBar_generic : MonoBehaviour {
    public RectTransform bar;

    public float ratio = 1;

    public Color low;
    public Color high;
	
	// Update is called once per frame
	void Update () {
		bar.sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x * ratio, bar.sizeDelta.y);
        bar.GetComponent<Image>().color = Color.Lerp(low, high, ratio);
	}
}
