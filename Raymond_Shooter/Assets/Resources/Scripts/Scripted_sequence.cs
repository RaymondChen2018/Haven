using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// pre-movement animation -> movetoposition -> post-movement animation
/// </summary>
public class Scripted_sequence : Entity_generic {
    public AI_generic TargetNPC;
    /// <summary>
    /// This happens before moving
    /// </summary>
    public CONSTANTS.ANIM_CODE ActionAnimation;
    /// <summary>
    /// Specify movement speed here
    /// </summary>
    public float moveToPosition = 0;
    /// <summary>
    /// This happens after moving
    /// </summary>
    public CONSTANTS.ANIM_CODE PostActionAnimation;

    public bool Repeatable = false;
    public bool StartOnSpawn = false;
    public bool LoopInPostIdle = true;
    //Making sure animations are played in the angle of the entity
    public bool AlignAngle = true;
    public Scripted_sequence nextScript;

    public STATE state = STATE.PostMovement;
    public enum STATE {
        PreMovement,
        Movement,
        PostMovement
    }
    float time_prev = 0;
    float stateTimeLeft = 1;

    [SerializeField]
    public List<CONSTANTS.IO> I_O;

	// Use this for initialization
	void Start () {
        Server_watcher.Singleton.onClientReady.Add(OnClientReady);
        if (!isServer)
        {
            Destroy(this);
        }
	}
    [HideInInspector]
    public void OnClientReady()
    {
        if (StartOnSpawn)
        {
            enabled = true;
        }
    }
    // Update is called once per frame
    void Update ()
    {
        if (TargetNPC.body.isDead)
        {
            remove_influence();
            enabled = false;
            return;
        }
        if(state == STATE.PreMovement)
        {
            if (TargetNPC.body.anim_upper.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1)
            {
                //TargetNPC.body.anim_upper.Play("Movement");
                TargetNPC.Script_to_move(gameObject, moveToPosition);
                state = STATE.Movement;
                TargetNPC.body.set_script_state(-1);
                TargetNPC.body.staticRB = false;
            }
        }
        if(state == STATE.Movement)
        {
            if (moveToPosition <= 0 || Vector2.Distance(TargetNPC.transform.position, transform.position) <= TargetNPC.body.size)
            {
                state = STATE.PostMovement;
                TargetNPC.Script_cease_control();

                TargetNPC.body.set_script_state((sbyte)PostActionAnimation);
                TargetNPC.body.staticRB = true;
                if (AlignAngle)
                {
                    TargetNPC.set_pos_n_orient(transform.position, transform.rotation.eulerAngles.z);
                }
            }
        }
        if(state == STATE.PostMovement)
        {
            if (PostActionAnimation == CONSTANTS.ANIM_CODE.None || TargetNPC.body.anim_upper.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1)
            {
                enabled = false;
                if (LoopInPostIdle)
                {
                    OnEndSequence();
                }
                else
                {
                    if(nextScript == null)
                    {
                        remove_influence();
                    }
                    else
                    {
                        transfer_incluence();
                    }
                    OnEndSequence();
                }
            }
        }


        
    }
    void evaluate()
    {
        
    }
    void transfer_incluence()
    {
        TargetNPC.body.scriptedSequence = nextScript;
        nextScript.BeginSequence();
    }
    void remove_influence()
    {
        if (TargetNPC.body.scriptedSequence == this && TargetNPC.body.scriptedSequence == this)
        {
            //TargetNPC.body.anim_upper.Play("Movement");
            TargetNPC.body.scriptedSequence = null;
            TargetNPC.body.set_script_state(-1);
            TargetNPC.body.character_cond = Body_generic.Character_condition.FREE;
            //TargetNPC.body.anim_upper.SetLayerWeight(4,0);
            TargetNPC.body.staticRB = false;
        }
    }
    public new void Kill()
    {
        remove_influence();
        base.Kill();
    }

    //Inputs
    /// <summary>
    /// This will play the entire sequence
    /// </summary>
    public void BeginSequence()
    {
        OnBeginSequence();
        enabled = true;

        if(TargetNPC.body.scriptedSequence != null && TargetNPC.body.scriptedSequence != this)//Steal control if the ai is under another influence
        {
            TargetNPC.body.scriptedSequence.enabled = false;
        }
        TargetNPC.body.scriptedSequence = this;

        TargetNPC.body.character_cond = Body_generic.Character_condition.SCRIPTED;
        //TargetNPC.body.anim_upper.SetLayerWeight(4, 1);
        if (ActionAnimation != CONSTANTS.ANIM_CODE.None)
        {
            state = STATE.PreMovement;
            TargetNPC.body.set_script_state((sbyte)ActionAnimation);
            TargetNPC.body.staticRB = true;
        }
        else if(moveToPosition > 0)
        {
            state = STATE.Movement;
            TargetNPC.body.set_script_state(-1);// Don't add this in as it creates animation artifect
            TargetNPC.Script_to_move(gameObject, moveToPosition);
            TargetNPC.body.staticRB = false;
        }
        else
        {
            state = STATE.PostMovement;
            TargetNPC.Script_cease_control();

            TargetNPC.body.set_script_state((sbyte)PostActionAnimation);
            TargetNPC.body.staticRB = true;
            if (AlignAngle)
            {
                TargetNPC.set_pos_n_orient(transform.position, transform.rotation.eulerAngles.z);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void MoveToPosition()
    {
        TargetNPC.body.staticRB = false;
        enabled = true;
        state = STATE.Movement;
        //TargetNPC.body.bodyRB.bodyType = RigidbodyType2D.Dynamic;
        TargetNPC.body.character_cond = Body_generic.Character_condition.SCRIPTED;
        //TargetNPC.body.anim_upper.SetLayerWeight(4, 1);
        TargetNPC.Script_to_move(gameObject, moveToPosition);
        TargetNPC.body.set_script_state(-1);
        if (TargetNPC.body.scriptedSequence != null && TargetNPC.body.scriptedSequence != this)//Steal control if the ai is under another influence
        {
            TargetNPC.body.scriptedSequence.enabled = false;
        }
        TargetNPC.body.scriptedSequence = this;
    }
    /// <summary>
    /// This will cancel the entire sequence
    /// </summary>
    public void CancelSequence()
    {
        TargetNPC.Script_cease_control();
        enabled = false;
        TargetNPC.body.character_cond = Body_generic.Character_condition.FREE;
        //TargetNPC.body.anim_upper.SetLayerWeight(4, 0);
        if (TargetNPC.body.scriptedSequence == this)
        {
            TargetNPC.body.scriptedSequence = null;
        }
        TargetNPC.body.staticRB = false;
    }


    //Outputs
    public void OnBeginSequence()
    {
        CONSTANTS.invokeOutput(CONSTANTS.OUTPUT_NAME.OnBeginSequence, I_O);
    }

    /// <summary>
    /// Cancelling event wont fire this
    /// </summary>
    public void OnEndSequence()
    {
        CONSTANTS.invokeOutput(CONSTANTS.OUTPUT_NAME.OnEndSequence, I_O);
    }
    
    //
}
