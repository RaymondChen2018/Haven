using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class anim_robot2_movement : StateMachineBehaviour {
    private Animator lower;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
    {
        if(animator.gameObject.tag == "Player")
        {
            if(lower == null)
            {
                //lower = animator.gameObject.transform.parent.parent.gameObject.GetComponent<Body_generic>().anim_lower.GetComponent<Animator>();
            }
            //lower.Play("Movement", 0, normalizedTime: 0);
        }
    }
}
