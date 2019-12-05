using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class button_toggle_generic : MonoBehaviour {
    public GameObject toggle_panel;

    public void press()
    {
        if (toggle_panel.activeSelf)
        {

            toggle_panel.SetActive(false);
        }
        else{

            toggle_panel.SetActive(true);
        }
    }
}
