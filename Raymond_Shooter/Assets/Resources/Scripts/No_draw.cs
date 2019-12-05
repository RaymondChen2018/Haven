using UnityEngine;

public class No_draw : MonoBehaviour {

	// Use this for initialization
    void Start()
    {
        Destroy(GetComponent<SpriteRenderer>());
    }
}
