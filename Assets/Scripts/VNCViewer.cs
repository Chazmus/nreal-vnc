using System;
using UnityEngine;

public class VNCViewer : MonoBehaviour
{

    void Start()
    {

        var host =

        var client = new VncClient();
        var success = client.Connect("127.0.0.1", 1);

        if (success)
        {
            Debug.Log("");
        }
        else
        {
            Debug.LogError($"Failed to connect to VNC server, host:{host}, port:  ");
        }
    }

}
