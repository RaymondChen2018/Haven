using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trigger_multiple_client : MonoBehaviour {



    public string triggerTag;
    public bool allIn = false;
    int volumeCounter = 0;


    [SerializeField]
    public List<CONSTANTS.IO> I_O;
    


    

    //Inputs
    public void Kill()
    {
        Destroy(gameObject);
    }
    //Outputs
    void OnTriggerEnter2D(Collider2D collider2D)
    {
        if (collider2D.tag != "" && collider2D.tag != triggerTag)
        {
            return;
        }
        volumeCounter++;
        if (allIn)
        {
            Collider2D[] colliders = Physics2D.OverlapAreaAll(CONSTANTS.VEC_NULL, -CONSTANTS.VEC_NULL, gameObject.layer);
            int count = 0;
            if (triggerTag != "")
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i].tag == triggerTag)
                    {
                        count++;
                    }
                }
            }
            else
            {
                count = colliders.Length;
            }
            if (volumeCounter < count)//If not all entities inside volume, dont trigger
            {
                return;
            }
        }
        //Action
        CONSTANTS.invokeOutput(CONSTANTS.OUTPUT_NAME.OnStartTouch, I_O);
    }
    private void OnTriggerExit2D(Collider2D collider2D)
    {
        if ((collider2D.tag != "" && collider2D.tag != triggerTag))
        {
            return;
        }
        volumeCounter--;
        //Action
        CONSTANTS.invokeOutput(CONSTANTS.OUTPUT_NAME.OnEndTouch, I_O);
    }
}
