using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class OnRightClick : MonoBehaviour, IPointerClickHandler
{
    public byte signal_value = 0;
    public Menu_watcher menu;


    public void OnPointerClick(PointerEventData data)
    {
        switch (data.button)
        {
            //case PointerEventData.InputButton.Left:
                //Debug.Log("Left click");
                //break;
            case PointerEventData.InputButton.Right:
                menu.ammo_button_minus(signal_value);
                //Debug.Log("signal right");
                break;
            //case PointerEventData.InputButton.Middle:
                //Debug.Log("Middle click");
                //break;
        }

    }
}