using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Animation_event : MonoBehaviour {
    public Body_generic character;
    [SerializeField]
    public List<CONSTANTS.IO> I_O;

    /// <summary>
    /// This function seeks to mannually update this character's position after each script's change of character orientation and position
    /// </summary>
    public void AE(CONSTANTS.AE_NAME evnt)
    {
        if (!Client_watcher.Singleton.isServer)
        {
            return;
        }
        OnAE(evnt);
        //Debug.DrawLine(transform.position, sync_lookatpos, Color.red, 10)
        switch (evnt)
        {
            case CONSTANTS.AE_NAME.AE_UPDATE_AI_SCRIPT_POSITION:
                if (character == null)
                {
                    return;
                }
                
                character.bodyRB.position = character.anim_upper.transform.position;
                character.bodyRB.MoveRotation(character.anim_upper.transform.rotation.eulerAngles.z);
                character.Rpc_updatePosition(character.bodyRB.position, character.bodyRB.rotation);
                break;
            case CONSTANTS.AE_NAME.AE_GUNFIRE:

                break;
        }
    }
    
    //Outputs
    void OnAE(CONSTANTS.AE_NAME evnt)
    {

        CONSTANTS.invokeOutput(CONSTANTS.OUTPUT_NAME.OnAE, I_O, (int)evnt);
    }
}
