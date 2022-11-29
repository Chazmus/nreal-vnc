using UnityEngine;

public class VNCViewer : MonoBehaviour
{

    void Start()
    {

        var host = "127.0.0.1";
        var display = 1;
        var port = 5900;

        var client = new VncClient();
        var success = client.Connect(host, display, port);

        if (success)
        {
            Debug.Log("Client successfully connected.");
        }
        else
        {
            Debug.LogError($"Failed to connect to VNC server, host:{host}, display:{display}, port:{port}");
        }
    }

}
