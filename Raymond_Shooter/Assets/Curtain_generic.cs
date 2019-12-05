using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Curtain_generic : MonoBehaviour {
    public bool touchFadeOut = true;
    public Animator animComp;
    bool isOn = true;

    public void setFade(bool fadeout)
    {
        touchFadeOut = fadeout;
        GetComponent<BoxCollider2D>().enabled = true;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(!(collision.gameObject.tag == CONSTANTS.TAG_PLAYER))
        {
            return;
        }
        GetComponent<BoxCollider2D>().enabled = false;
        if (touchFadeOut && isOn)
        {
            isOn = false;
            animComp.Play("Fade_out");
        }
        else if(!touchFadeOut && !isOn)
        {
            isOn = true;
            animComp.Play("Fade_in");
        }
    }
}
