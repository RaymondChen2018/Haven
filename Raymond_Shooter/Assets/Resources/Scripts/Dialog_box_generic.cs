using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dialog_box_generic : MonoBehaviour {
    public Sprite[] dialog_emojis;
    public float unit_width;
    public float unit_height;
    public float width_unit;
    public SpriteRenderer c_t_l;
    public SpriteRenderer c_b_l;
    public SpriteRenderer c_t_r;
    public SpriteRenderer c_b_r;
    public SpriteRenderer m_f;
    public SpriteRenderer b_t;
    public SpriteRenderer b_l;
    public SpriteRenderer b_b;
    public SpriteRenderer b_r;
    public SpriteRenderer c_tri;
    public Sprite dialog_corner;
    public Sprite dialog_cube;
    static float dialog_spr_size = 2.56f;
    [HideInInspector] public Transform parent;
    [HideInInspector] public float offset = 0.5f;
    static int life = 5;

    /*
    void Start()
    {
        show_dialog(new int[] { 0, 1, 4});
    }
    */
    void Update()
    {
        if(parent != null)
        {
            transform.position = (Vector2)parent.position + new Vector2(0, offset);
        }
        
    }
    private IEnumerator remove()
    {
        yield return new WaitForSeconds(life);
        Destroy(transform.parent.gameObject);
    }
    // Use this for initialization
    public void show_dialog (int[] sequence) {
        //dialog bounding box
        float dialog_width = 0;
        float dialog_height = 0;
        if(sequence.Length > width_unit)
        {
            dialog_width = width_unit * unit_width;
            dialog_height = (int)(sequence.Length / width_unit) * unit_height;
            if(sequence.Length % width_unit > 0)
            {
                dialog_height += unit_height;
            }
        }
        else
        {
            dialog_width = sequence.Length * unit_width;
            dialog_height = unit_height;
        }
        float dialog_spr_width = dialog_width / dialog_spr_size;
        float dialog_spr_height = dialog_height / dialog_spr_size;
        float dialog_spr_unit_width = unit_width / dialog_spr_size;
        float dialog_spr_unit_height = unit_height / dialog_spr_size;
        float offset = dialog_spr_height / 2 + 2 * unit_height + 0.1f;
        m_f.transform.localScale = new Vector3(dialog_spr_width, dialog_spr_height, 1);
        m_f.transform.localPosition = new Vector2(0, offset);

        b_l.transform.localScale = new Vector3(dialog_spr_unit_width / 2, dialog_spr_height, 1);
        b_l.transform.localPosition = new Vector3(-(unit_width + dialog_width - dialog_spr_unit_width) / 2 + 0.05f, offset, 0);

        b_r.transform.localScale = new Vector3(dialog_spr_unit_width / 2, dialog_spr_height, 1);
        b_r.transform.localPosition = new Vector3((unit_width + dialog_width - dialog_spr_unit_width) / 2 - 0.05f, offset, 0);

        b_t.transform.localScale = new Vector3(dialog_spr_width, dialog_spr_unit_width / 2, 1);
        b_t.transform.localPosition = new Vector3(0, (unit_height + dialog_height - dialog_spr_unit_width) / 2 + offset - 0.05f, 0);

        b_b.transform.localScale = new Vector3(dialog_spr_width, dialog_spr_unit_width / 2, 1);
        b_b.transform.localPosition = new Vector3(0, -(unit_height + dialog_height - dialog_spr_unit_width) / 2 + offset + 0.05f, 0);

        c_t_l.transform.localScale = new Vector3(dialog_spr_unit_height / 2, dialog_spr_unit_width / 2, 1);
        c_t_l.transform.localPosition = new Vector3(-(unit_width + dialog_width - dialog_spr_unit_width) / 2 + 0.05f, (unit_height + dialog_height - dialog_spr_unit_width) / 2 + offset - 0.05f, 0);
        c_b_l.transform.localScale = new Vector3(dialog_spr_unit_width / 2, dialog_spr_unit_height / 2, 1);
        c_b_l.transform.localPosition = new Vector3(-(unit_width + dialog_width - dialog_spr_unit_width) / 2 + 0.05f, -(unit_height + dialog_height - dialog_spr_unit_width) / 2 + offset + 0.05f, 0);
        c_t_r.transform.localScale = new Vector3(dialog_spr_unit_width / 2, dialog_spr_unit_height / 2, 1);
        c_t_r.transform.localPosition = new Vector3((unit_width + dialog_width - dialog_spr_unit_width) / 2 - 0.05f, (unit_height + dialog_height - dialog_spr_unit_width) / 2 + offset - 0.05f, 0);
        c_b_r.transform.localScale = new Vector3(dialog_spr_unit_height / 2, dialog_spr_unit_width / 2, 1);
        c_b_r.transform.localPosition = new Vector3((unit_width + dialog_width - dialog_spr_unit_width) / 2 - 0.05f, -(unit_height + dialog_height - dialog_spr_unit_width) / 2 + offset + 0.05f, 0);

        c_tri.transform.localScale = new Vector3(dialog_spr_width / 2, dialog_spr_unit_width + 0.05f, 0);
        c_tri.transform.localPosition = new Vector3(0, -(2 * unit_height + dialog_height) / 2 + offset, 0);
        
        //dialog content
        Vector3 slot_one_pos = new Vector3(- (dialog_width - unit_width) / 2, dialog_height / 2 - unit_height / 2 + offset, -0.1f);
        for(int i = 0; i < sequence.Length; i++)
        {
            GameObject emoji = new GameObject("emoji");
            emoji.transform.parent = transform;
            emoji.transform.localScale = new Vector3(0.5f, 0.5f, 1);
            emoji.transform.localPosition = slot_one_pos;
            emoji.AddComponent<SpriteRenderer>();
            emoji.GetComponent<SpriteRenderer>().sprite = dialog_emojis[sequence[i]];
            emoji.GetComponent<SpriteRenderer>().sortingLayerID = c_t_l.sortingLayerID;
            emoji.GetComponent<SpriteRenderer>().sortingOrder = 1;
            emoji.layer = c_t_l.gameObject.layer;
            slot_one_pos.x += unit_width;
            if (slot_one_pos.x > dialog_width / 2)
            {
                slot_one_pos.y -= unit_height;
                slot_one_pos.x = -(dialog_width - unit_width) / 2;
            }
        }
        GetComponent<Animator>().enabled = true;
        StartCoroutine(remove());
        /*
        foreach (Sprite spr in dialog_emojis)
        {
            GameObject emoji = new GameObject("emoji");
            emoji.transform.parent = transform;
            emoji.transform.localScale = new Vector3(0.5f, 0.5f, 1);
            emoji.transform.localPosition = slot_one_pos;
            emoji.AddComponent<SpriteRenderer>();
            emoji.GetComponent<SpriteRenderer>().sprite = spr;
            emoji.GetComponent<SpriteRenderer>().sortingLayerID = c_t_l.sortingLayerID;
            emoji.GetComponent<SpriteRenderer>().sortingOrder = 1;
            emoji.layer = c_t_l.gameObject.layer;
            slot_one_pos.x += unit_width;
            if (slot_one_pos.x > dialog_width / 2)
            {
                slot_one_pos.y -= unit_height;
                slot_one_pos.x = -(dialog_width - unit_width) / 2;
            }
        }
        */
    }
}
