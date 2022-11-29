using UnityEngine;

public class VNCViewer : MonoBehaviour
{

    void Start()
    {

        var host = "127.0.0.1";
        var display = 1;
        var port = 5900;
        var bitsPerPixel = 8; // Must be 8 or 16 or 32
        var colorDepth = 3; // Must be 3, 6, 8 or 16


        var client = new VncClient();
        var success = client.Connect(host, display, port);
        client.Initialize(bitsPerPixel, colorDepth);

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
