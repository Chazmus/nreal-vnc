using NRKernal;
using UnityEngine;

public class InputManager: MonoBehaviour
{
    public Quaternion Rotation { get; private set; }
    public Vector3 Position { get; private set; }



    private void Update()
    {
        Rotation = NRInput.GetRotation();
        Position = NRInput.GetPosition();
    }
}