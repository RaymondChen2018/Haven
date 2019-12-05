using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class digit_mod_generic : MonoBehaviour {
    public Text label;
    public bool startOneDown = false;
    public float counter = 0;

    public void reset()
    {
        counter = 0;
        label.enabled = false;
    }
    public float get()
    {
        return counter;
    }
    public void set(float value)
    {
        counter = value;
        
        if(counter <= 0 || (startOneDown && (counter >= 1)))
        {
            label.enabled = false;
        }
        else
        {
            label.enabled = true;
            if (startOneDown)
            {
                label.text = "x";
            }
            else
            {
                label.text = "+";
            }
            label.text += counter.ToString();//Mathf.Round(counter).ToString();
        }
    }
    public void add(float value)
    {
        if(counter <= 0)
        {
            label.enabled = true;
        }
        counter += value;
        if (startOneDown)
        {
            label.text = "x";
        }
        else
        {
            label.text = "+";
        }
        label.text += counter.ToString();

    }
    public void subtract(float value)
    {
        counter -= value;
        if (counter <= 0)
        {
            label.enabled = false;
        }
        else
        {
            if (startOneDown)
            {
                label.text = "x";
            }
            else
            {
                label.text = "+";
            }
            label.text += counter.ToString();
        }
    }
}
