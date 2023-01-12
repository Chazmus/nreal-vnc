using NRKernal;
using UnityEngine;
using Zenject;

namespace InputManagement
{
    public class NrealcontrollerInput : ITickable
    {
        public void Tick()
        {
            Rotation = NRInput.GetRotation();
            Position = NRInput.GetPosition();
            Touch = NRInput.GetTouch();
            DeltaTouch = NRInput.GetDeltaTouch();
            CameraCenter = NRInput.CameraCenter;
        }

        public Transform CameraCenter { get; private set; }
        public Quaternion Rotation { get; private set; }
        public Vector3 Position { get; private set; }

        public Vector2 Touch { get; private set; }
        public Vector2 DeltaTouch { get; private set; }

        public bool GetButton(ControllerButton button)
        {
            return NRInput.GetButton(button);
        }
        public bool GetButtonDown(ControllerButton button)
        {
            return NRInput.GetButtonDown(button);
        }
        public bool GetButtonUp(ControllerButton button)
        {
            return NRInput.GetButtonUp(button);
        }
    }
}