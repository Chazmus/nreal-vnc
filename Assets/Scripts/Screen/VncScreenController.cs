using System;
using System.Collections;
using UnityEngine;
using Zenject;

namespace Screen
{
    public class VncScreenController: MonoBehaviour
    {

        [SerializeField] public VNCScreen.VNCScreen _screen;
        private CreationParameters _creationParameters;

        [Inject]
        public void Construct(CreationParameters creationParameters)
        {
            _creationParameters = creationParameters;
        }

        private void Awake()
        {
            _screen.host = _creationParameters.Host;
            _screen.port = _creationParameters.Port;
            _screen.display = _creationParameters.DisplayNumber;
            _screen.password = _creationParameters.Password;
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