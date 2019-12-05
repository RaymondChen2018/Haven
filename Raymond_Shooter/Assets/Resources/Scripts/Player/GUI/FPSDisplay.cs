using UnityEngine;
using System.Collections;

public class FPSDisplay : MonoBehaviour
{
    public float deltaTime = 0.0f;
    float ping = 0;
    Player_generic local_player;
    float time_prev = 0;
    void Start()
    {

        Player_generic[] players = FindObjectsOfType<Player_generic>();
        for(int i = 0; i < players.Length; i++)
        {
            if (players[i].isLocalPlayer)
            {
                local_player = players[i];
                break;
            }
        }
        
    }
    void Update()
    {
        deltaTime += ((Time.realtimeSinceStartup - time_prev) - deltaTime) * 0.02f;//+= (Time.deltaTime - deltaTime) * 0.02f;
        time_prev = Time.realtimeSinceStartup;
        if (local_player != null)
        {
            ping = local_player.latency;
        }
    }

    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(0, 0, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 100 * 3;
        style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);
        float msec = deltaTime * 1000.0f;
        int fps = (int)(1.0f / deltaTime);
        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps) + "; Latency: "+ ping;
        GUI.Label(rect, text, style);
    }
}