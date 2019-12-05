using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Network_manager_extension : NetworkManager {
    public override void OnClientSceneChanged(NetworkConnection conn)
    {
        ClientScene.Ready(conn);
    }
    //public override void OnClientConnect(NetworkConnection conn)
    //{
        //ClientScene.Ready(conn);
        
    //}
    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        GameObject player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        NetworkServer.AddPlayerForConnection(conn, player, playerControllerId);
        
    }

}
