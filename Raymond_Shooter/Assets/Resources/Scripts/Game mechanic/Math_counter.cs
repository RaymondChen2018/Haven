using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Math_counter : Entity_generic {


    public int InitialValue = 0;
    public int MaximumLegalValue = 0;
    public int MinimumLegalValue = 0;

    public enum OUTPUT_NAME
    {
        OutValue,
        OnHitMax,
        OnHitMin
    }
    [SerializeField]
    public List<CONSTANTS.IO> I_O;
    //Inputs
    [ServerCallback]
    public void Add(int value)
    {
        InitialValue += value;
        OutValue();
        if(InitialValue >= MaximumLegalValue)
        {
            OnHitMax();
        }
    }
    [ServerCallback]
    public void Subtract(int value)
    {
        InitialValue -= value;
        OutValue();
        if (InitialValue <= MinimumLegalValue)
        {
            OnHitMin();
        }
    }
    [ServerCallback]
    public void Divide(int value)
    {
        InitialValue /= value;
        OutValue();
        if (InitialValue <= MinimumLegalValue)
        {
            OnHitMin();
        }
    }
    [ServerCallback]
    public void Multiply(int value)
    {
        InitialValue *= value;
        if (InitialValue >= MaximumLegalValue)
        {
            OnHitMax();
        }
    }
    [ServerCallback]
    public void SetValue(int value)
    {
        InitialValue = value;
        OutValue();
        if (InitialValue >= MaximumLegalValue)
        {
            OnHitMax();
        }
        else if (InitialValue <= MinimumLegalValue)
        {
            OnHitMin();
        }
    }
    [ServerCallback]
    public void SetHitMax(int value)
    {
        MaximumLegalValue = value;
    }
    [ServerCallback]
    public void SetHitMin(int value)
    {
        MinimumLegalValue = value;
    }
    [ServerCallback]
    public int GetValue()
    {
        return InitialValue;
    }

    //Outputs
    [ServerCallback]
    public void OutValue()
    {
        CONSTANTS.invokeOutput(CONSTANTS.OUTPUT_NAME.OutValue, I_O);
    }
    [ServerCallback]
    public void OnHitMax()
    {
        CONSTANTS.invokeOutput(CONSTANTS.OUTPUT_NAME.OnHitMax, I_O);
    }
    [ServerCallback]
    public void OnHitMin()
    {
        CONSTANTS.invokeOutput(CONSTANTS.OUTPUT_NAME.OnHitMin, I_O);
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
