using System.Collections;
using InputManagement;
using UnityEngine;
using Zenject;

namespace Screen
{
    public class VncScreenController: MonoBehaviour
    {

        [SerializeField] public VNCScreen.VNCScreen _screen;
        private CreationParameters _creationParameters;

        private bool isActiveScreen = true;
        private KeyboardAndMouseInput _keyboardAndMouseInput;

        [Inject]
        public void Construct(CreationParameters creationParameters, KeyboardAndMouseInput keyboardAndMouseInput)
        {
            _creationParameters = creationParameters;
            _keyboardAndMouseInput = keyboardAndMouseInput;
        }

        private void Update()
        {
            if (isActiveScreen)
            {
                Event currentEvent = Event.current;
                // _screen.OnKey();

            }
        }

        private void Awake()
        {
            _screen.host = _creationParameters.Host;
            _screen.port = _creationParameters.Port;
            _screen.display = _creationParameters.DisplayNumber;
            _screen.password = _creationParameters.Password;

            _keyboardAndMouseInput.OnKeyboardEvent += handleKeyboardEvent;
            // TODO - mice
            // _keyboardAndMouseInput.onMouseClicked += handleMouseClickEvent;
            // _keyboardAndMouseInput.onMouseMoved += handleMouseMoveEvent;
        }

        private void handleKeyboardEvent(uint keysym, bool pressed)
        {
            if (isActiveScreen)
            {
                _screen.PressKey(keysym, pressed);
            }
        }

        private void Start()
        {
            StartCoroutine(Connect());
        }

        private IEnumerator Connect()
        {
            // TODO: Not realy sure why, but we have to wait a frame before connecting
            yield return new WaitForEndOfFrame();
            _screen.Connect();
        }


        public class Factory : PlaceholderFactory<CreationParameters, VncScreenController>
        {
        }

        public class CreationParameters
        {
            public string Host { get; }
            public int Port { get; }
            public int DisplayNumber { get; }
            public string Password { get; }

            public CreationParameters(string host, int port, int displayNumber, string password)
            {
                Host = host;
                Port = port;
                DisplayNumber = displayNumber;
                Password = password;
            }
        }
    }
}