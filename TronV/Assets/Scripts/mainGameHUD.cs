using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MainGameHUD : MonoBehaviour
{
    // Start is called before the first frame update
    public void StartPlayer()
    {
        switch(startScreenController.mode) {
            case 0:
            // Join
                NetworkManager.singleton.networkAddress = startScreenController.hostAddress;
                NetworkManager.singleton.StartClient();
                break;
            case 1:
            // Host
                NetworkManager.singleton.StartHost();
                break;
        }
    }

    public void Stop()
    {
        switch(startScreenController.mode) {
            case 0:
            // Join
                NetworkManager.singleton.StopClient();
                break;
            case 1:
            // Host
                NetworkManager.singleton.StopHost();
                break;
        }
    }
}
