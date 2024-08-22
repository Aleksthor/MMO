using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ServerUI : MonoBehaviour
{
    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        gameObject.SetActive(false);
    }
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        gameObject.SetActive(false);
    }
    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        gameObject.SetActive(false);
    }
}
