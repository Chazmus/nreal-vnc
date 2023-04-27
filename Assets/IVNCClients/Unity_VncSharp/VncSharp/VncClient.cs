// Unity 3D Vnc Client - Unity 3D VNC Client Library
// Copyright (C) 2017 Christophe Floutier
//
// Based on VncSharp - .NET VNC Client Library
// Copyright (C) 2008 David Humphrey
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.Collections.Generic;
using System.Threading;

using VNCScreen.Drawing;

using System.Diagnostics;
using System.Security.Cryptography;

using VNCScreen;
using UnityVncSharp.Encodings;
using UnityVncSharp.Internal;
using UnityVncSharp.Imaging;

using UnityEngine;

namespace UnityVncSharp
{

    public class VNCSharpClient : IVncClient
    {
        RfbProtocol rfb;            // The protocol object handling all communication with server.
        FrameBufferInfos bufferInfos;         // The geometry and properties of the remote framebuffer
        byte securityType;          // The type of Security agreed upon by client/server
        EncodedRectangleFactory factory;

        Bitmap theBitmap;                          // Internal representation of remote image.

        List<IDesktopUpdater> updates = new List<IDesktopUpdater>();


        Thread connectingThread;            // To get the connecting state
        Thread worker;                      // To request and read in-coming updates from server

        ManualResetEvent done;      // Used to tell the worker thread to die cleanly
        IVncInputPolicy inputPolicy;// A mouse/keyboard input strategy

        /// <summary>
        /// Raised when the connection to the remote host is lost.
        /// </summary>
        public event EventHandler ConnectionLost;



        /// <summary>
        /// Raised when the connection to the remote host is set or not
        /// </summary>
        public event OnConnection onConnection;


        /// <summary>
        /// Raised when the server caused the local clipboard to be filled.
        /// </summary>
        public event EventHandler ServerCutText;

        int received = 0;

        public bool updateDesktopImage()
        {      
            for (int i = 0; i < updates.Count; i++)
            {
                received++;
                IDesktopUpdater u = updates[i];
                if (u != null)
                    u.Draw(theBitmap);
            }

       //     UnityEngine.Debug.Log("received " + received);

            updates.Clear();
            return received >= 1;
        }

        public Texture2D getTexture()
        {
            if (theBitmap != null)
                return theBitmap.Texture;

            return null;
        }


        public VNCSharpClient()
        {
        }

        /// <summary>
        /// Gets the Framebuffer representing the remote server's desktop geometry.
        /// </summary>
        public FrameBufferInfos BufferInfos
        {
            get
            {
                return bufferInfos;
            }
        }

        public Size BufferSize
        {
            get { return new Size(bufferInfos.Width, bufferInfos.Height); }
        }


        /// <summary>
        /// Gets the hostname of the remote desktop
        /// </summary>
        public string HostName
        {
            get
            {
                return BufferInfos.DesktopName;
            }
        }

        /// <summary>
        /// Returns True if the VncClient object is View-Only, meaning no mouse/keyboard events are being sent.
        /// </summary>
        public bool IsViewOnly
        {
            get
            {
                return inputPolicy != null && inputPolicy is VncViewInputPolicy;
            }
        }

   

        string host;
        int port;

        /// <summary>
        /// Connect to a VNC Host and determine which type of Authentication it uses. If the host uses Password Authentication, a call to Authenticate() will be required.
        /// </summary>
        /// <param name="host">The IP Address or Host Name of the VNC Host.</param>
        /// <param name="display">The Display number (used on Unix hosts).</param>
        /// <param name="port">The Port number used by the Host, usually 5900.</param>
        /// <param name="viewOnly">True if mouse/keyboard events are to be ignored.</param>
        /// <returns>Returns True if the VNC Host requires a Password to be sent after Connect() is called, otherwise False.</returns>
        public void Connect(string host, int display, int port, bool viewOnly)
        {
            if (host == null) throw new ArgumentNullException("host");

            // If a diplay number is specified (used to connect to Unix servers)
            // it must be 0 or greater.  This gets added to the default port number
            // in order to determine where the server will be listening for connections.
            if (display < 0) throw new ArgumentOutOfRangeException("display", display, "Display number must be non-negative.");
            port += display;

            rfb = new RfbProtocol();

            if (viewOnly)
            {
                inputPolicy = new VncViewInputPolicy(rfb);
            }
            else
            {
                inputPolicy = new VncDefaultInputPolicy(rfb);
            }

            this.host = host;
            this.port = port;
         
            // Lauch connecting thread
            connectingThread = new Thread(new ThreadStart(this.Connection));
            connectingThread.SetApartmentState(ApartmentState.STA);
            connectingThread.IsBackground = true;
            connectingThread.Start();
        }

        /// <summary>
        /// Connect to a VNC Host and determine which type of Authentication it uses. If the host uses Password Authentication, a call to Authenticate() will be required. Default Display and Port numbers are used.
        /// </summary>
        /// <param name="host">The IP Address or Host Name of the VNC Host.</param>
        /// <returns>Returns True if the VNC Host requires a Password to be sent after Connect() is called, otherwise False.</returns>
        public void Connect(string host)
        {
            Connect(host, 0, 5900);
        }

        /// <summary>
        /// Connect to a VNC Host and determine which type of Authentication it uses. If the host uses Password Authentication, a call to Authenticate() will be required. The Port number is calculated based on the Display.
        /// </summary>
        /// <param name="host">The IP Address or Host Name of the VNC Host.</param>
        /// <param name="display">The Display number (used on Unix hosts).</param>
        /// <returns>Returns True if the VNC Host requires a Password to be sent after Connect() is called, otherwise False.</returns>
        public void Connect(string host, int display)
        {
            Connect(host, display, 5900);
        }
       
        public void Connect(string host, int display, int port)
        {
            Connect(host, display, port, false);
        }

        /// <summary>
        /// Examines a list of Security Types supported by a VNC Server and chooses one that the Client supports.  See 6.1.2 of the RFB Protocol document v. 3.8.
        /// </summary>
        /// <param name="types">An array of bytes representing the Security Types supported by the VNC Server.</param>
        /// <returns>A byte that represents the Security Type to be used by the Client.</returns>
        protected byte GetSupportedSecurityType(byte[] types)
        {
            // Pick the first match in the list of given types.  If you want to add support for new
            // security types, do it here:
            for (int i = 0; i < types.Length; ++i)
            {
                if (types[i] == 1   // None
                    || types[i] == 2    // VNC Authentication
                                        // TODO: None of the following are currently supported -------------------
                                        //					|| types[i] == 5	// RA2
                                        //					|| types[i] == 6    // RA2ne
                                        //					|| types[i] == 16   // Tight
                                        //					|| types[i] == 17 	// Ultra
                                        //					|| types[i] == 18 	// TLS
                   ) return types[i];
            }
            return 0;
        }




        /// <summary>
        /// Use a password to authenticate with a VNC Host. NOTE: This is only necessary if Connect() returns TRUE.
        /// </summary>
        /// <param name="password">The password to use.</param>
        /// <returns>Returns True if Authentication worked, otherwise False.</returns>
        public void Authenticate(string password, OnPassword onPassword)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (onPassword == null) throw new ArgumentNullException("onPassword");

            // If new Security Types are supported in future, add the code here.  For now, only 
            // VNC Authentication is supported.
            if (securityType == 2)
            {
                PerformVncAuthentication(password);
            }
            else
            {
                throw new NotSupportedException("Unable to Authenticate with Server. The Server uses an Authentication scheme unknown to the client.");
            }

            if (rfb.ReadSecurityResult() == 0)
            {
                onPassword(true); 
            }
            else
            {
                // Authentication failed, and if the server is using Protocol version 3.8, a 
                // plain text message follows indicating why the error happend.  I'm not 
                // currently using this message, but it is read here to clean out the stream.
                // In earlier versions of the protocol, the server will just drop the connection.
                if (rfb.ServerVersion == 3.8) rfb.ReadSecurityFailureReason();
                rfb.Close();    // TODO: Is this the right place for this???
                onPassword(false);
            }
        }

        /// <summary>
        /// Performs VNC Authentication using VNC DES encryption.  See the RFB Protocol doc 6.2.2.
        /// </summary>
        /// <param name="password">A string containing the user's password in clear text format.</param>
        protected void PerformVncAuthentication(string password)
        {
            byte[] challenge = rfb.ReadSecurityChallenge();
            rfb.WriteSecurityResponse(EncryptChallenge(password, challenge));
        }

        /// <summary>
        /// Encrypts a challenge using the specified password. See RFB Protocol Document v. 3.8 section 6.2.2.
        /// </summary>
        /// <param name="password">The user's password.</param>
        /// <param name="challenge">The challenge sent by the server.</param>
        /// <returns>Returns the encrypted challenge.</returns>
        protected byte[] EncryptChallenge(string password, byte[] challenge)
        {
            byte[] key = new byte[8];

            // Key limited to 8 bytes max.
            if (password.Length >= 8)
            {
                System.Text.Encoding.ASCII.GetBytes(password, 0, 8, key, 0);
            }
            else
            {
                System.Text.Encoding.ASCII.GetBytes(password, 0, password.Length, key, 0);
            }

            // VNC uses reverse byte order in key
            for (int i = 0; i < 8; i++)
                key[i] = (byte)(((key[i] & 0x01) << 7) |
                                 ((key[i] & 0x02) << 5) |
                                 ((key[i] & 0x04) << 3) |
                                 ((key[i] & 0x08) << 1) |
                                 ((key[i] & 0x10) >> 1) |
                                 ((key[i] & 0x20) >> 3) |
                                 ((key[i] & 0x40) >> 5) |
                                 ((key[i] & 0x80) >> 7));

            // VNC uses DES, not 3DES as written in some documentation
            DES des = new DESCryptoServiceProvider();
            des.Padding = PaddingMode.None;
            des.Mode = CipherMode.ECB;

            ICryptoTransform enc = des.CreateEncryptor(key, null);

            byte[] response = new byte[16];
            enc.TransformBlock(challenge, 0, challenge.Length, response, 0);

            return response;
        }

        /// <summary>
        /// Finish setting-up protocol with VNC Host.  Should be called after Connect and Authenticate (if password required).
        /// </summary>
        public void Initialize()
        {
            // Finish initializing protocol with host
            rfb.WriteClientInitialisation(false);
            bufferInfos = rfb.ReadServerInit();

            theBitmap = new Bitmap(bufferInfos.Width, bufferInfos.Height);

            rfb.WriteSetPixelFormat(bufferInfos);    // just use the server's framebuffer format

            rfb.WriteSetEncodings(new uint[] {  RfbProtocol.ZRLE_ENCODING,
                                                RfbProtocol.HEXTILE_ENCODING, 
											//	RfbProtocol.CORRE_ENCODING, // CoRRE is buggy in some hosts, so don't bother using
												RfbProtocol.RRE_ENCODING,
                                                RfbProtocol.COPYRECT_ENCODING,
                                                RfbProtocol.RAW_ENCODING });

            // Create an EncodedRectangleFactory so that EncodedRectangles can be built according to set pixel layout
            factory = new EncodedRectangleFactory(rfb);
        }

        /// <summary>
        /// Begin getting updates from the VNC Server.  This will continue until StopUpdates() is called.  NOTE: this must be called after Connect().
        /// </summary>
        public void StartUpdates()
        {
            // Start getting updates on background thread.
            worker = new Thread(new ThreadStart(this.GetRfbUpdates));
            // Bug Fix (Gr�goire Pailler) for clipboard and threading
            worker.SetApartmentState(ApartmentState.STA);
            worker.IsBackground = true;
            done = new ManualResetEvent(false);
            worker.Start();
        }

        /// <summary>
        /// Stops sending requests for updates and disconnects from the remote host.  You must call Connect() again if you wish to re-establish a connection.
        /// </summary>
        public void Disconnect()
        {
            

            // Stop the worker thread.
            done.Set();

            // BUG FIX: Simon.Phillips@warwick.ac.uk for UltraVNC disconnect issue
            // Request a tiny screen update to flush the blocking read
            try
            {
                rfb.WriteFramebufferUpdateRequest(0, 0, 1, 1, false);
            }
            catch
            {
                // this may not work, as Disconnect can get called in response to the
                // VncClient raising a ConnectionLost event (e.g., the remote host died).
            }

            worker.Join(3000);  // this number is arbitrary, just so that it doesn't block forever....

            rfb.Close();
            rfb = null;
            updates.Clear();

        }

        private bool CheckIfThreadDone()
        {
            return done.WaitOne(0, false);
        }

        private void Connection()
        {
            // Connect and determine version of server, and set client protocol version to match			
            try
            {
                rfb.Connect(host, port);
                rfb.ReadProtocolVersion();
                rfb.WriteProtocolVersion();

                // Figure out which type of authentication the server uses
                byte[] types = rfb.ReadSecurityTypes();

                // Based on what the server sends back in the way of supported Security Types, one of
                // two things will need to be done: either the server will reject the connection (i.e., type = 0),
                // or a list of supported types will be sent, of which we need to choose and use one.
                if (types.Length > 0)
                {
                    if (types[0] == 0)
                    {
                        // The server is not able (or willing) to accept the connection.
                        // A message follows indicating why the connection was dropped.
                        throw new VncProtocolException("Connection Failed. The server rejected the connection for the following reason: " + rfb.ReadSecurityFailureReason());
                    }
                    else
                    {
                        securityType = GetSupportedSecurityType(types);
                        System.Diagnostics.Debug.Assert(securityType > 0, "Unknown Security Type(s)", "The server sent one or more unknown Security Types.");

                        rfb.WriteSecurityType(securityType);

                        // Protocol 3.8 states that a SecurityResult is still sent when using NONE (see 6.2.1)
                        if (rfb.ServerVersion == 3.8f && securityType == 1)
                        {
                            if (rfb.ReadSecurityResult() > 0)
                            {
                                // For some reason, the server is not accepting the connection.  Get the
                                // reason and throw an exception
                                throw new VncProtocolException("Unable to Connecto to the Server. The Server rejected the connection for the following reason: " + rfb.ReadSecurityFailureReason());
                            }
                        }

                        onConnection(null, (securityType > 1) ? true : false);
                    }
                }
                else
                {
                    onConnection(new VncProtocolException("Protocol Error Connecting to Server. The Server didn't send any Security Types during the initial handshake."), false);

                }
            }
            catch (Exception e)
            {
                onConnection(new VncProtocolException("Unable to connect to the server. Error was: " + e.Message, e), false);
            }
        }

        /// <summary>
        /// Worker thread lives here and processes protocol messages infinitely, triggering events or other actions as necessary.
        /// </summary>
        private void GetRfbUpdates()
        {
            Thread.Sleep(1000);

            int rectangles;
            int enc;

            // Get the initial destkop from the host
            RequestScreenUpdate(true);

            while (true)
            {
                if (CheckIfThreadDone())
                    break;

                try
                {
                    switch (rfb.ReadServerMessageType())
                    {
                        case RfbProtocol.FRAMEBUFFER_UPDATE:
                            rectangles = rfb.ReadFramebufferUpdate();

                            if (CheckIfThreadDone())
                                break;

                            // TODO: consider gathering all update rectangles in a batch and *then* posting the event back to the main thread.
                            for (int i = 0; i < rectangles; ++i)
                            {
                                // Get the update rectangle's info
                                Rectangle rectangle;
                                rfb.ReadFramebufferUpdateRectHeader(out rectangle, out enc);

                                // Build a derived EncodedRectangle type and pull-down all the pixel info
                                EncodedRectangle er = factory.Build(rectangle, BufferInfos.BitsPerPixel, enc);
                                er.Decode();

                                // Let the UI know that an updated rectangle is available, but check
                                // to see if the user closed things down first.
                                if (!CheckIfThreadDone())
                                {
                                    updates.Add(er);
                                   
                                }
                            }
                            break;
                        case RfbProtocol.BELL:
                            Beep(500, 300);  // TODO: are there better values than these?
                            break;
                        case RfbProtocol.SERVER_CUT_TEXT:
                            if (CheckIfThreadDone())
                                break;
                            // TODO: This is invasive, should there be a bool property allowing this message to be ignored?
                            //           Clipboard.SetDataObject(rfb.ReadServerCutText().Replace("\n", Environment.NewLine), true);
                            OnServerCutText();
                            break;
                        case RfbProtocol.SET_COLOUR_MAP_ENTRIES:
                            rfb.ReadColourMapEntry();
                            break;
                    }
                }
                catch (Exception e)
                {
                    OnConnectionLost(e);
                }
            }
        }

        protected void OnConnectionLost(string reason)
        {
            ConnectionLost(this, new ErrorEventArg(reason));
        }

        protected void OnConnectionLost(Exception e)
        {
            ConnectionLost(this, new ErrorEventArg(e));
        }

        protected void OnServerCutText()
        {
            ServerCutText(this, EventArgs.Empty);
        }

        // There is no managed way to get a system beep (until Framework v.2.0). So depending on the platform, something external has to be called.
#if Win32
		[System.Runtime.InteropServices.DllImport("kernel32.dll")]
		private static extern bool Beep(int freq, int duration);
#else
        private bool Beep(int freq, int duration)   // bool just so it matches the Win32 API signature
        {
            // TODO: How to do this under Unix?
            System.Console.Write("Beep!");
            return true;
        }
#endif

        /// <summary>
        /// Changes the input mode to view-only or interactive.
        /// </summary>
        /// <param name="viewOnly">True if view-only mode is desired (no mouse/keyboard events will be sent).</param>
        public void SetInputMode(bool viewOnly)
        {
            if (viewOnly)
                inputPolicy = new VncViewInputPolicy(rfb);
            else
                inputPolicy = new VncDefaultInputPolicy(rfb);
        }

        public virtual void WriteClientCutText(string text)
        {
            try
            {
                rfb.WriteClientCutText(text);
            }
            catch (Exception e)
            {
                OnConnectionLost(e);
            }
        }

        // TODO: This needs to be pushed into the protocol rather than expecting keysym from the caller.
        public virtual void WriteKeyboardEvent(uint keysym, bool pressed)
        {
            try
            {
                inputPolicy.WriteKeyboardEvent(keysym, pressed);
            }
            catch (Exception e)
            {
                OnConnectionLost(e);
            }
        }

        public void UpdateMouse(Point pos, bool button0, bool button1, bool button2)
        {
            byte mask = 0;

            if (button0) mask += 1;
            if (button1) mask += 2;
            if (button2) mask += 4;

            WritePointerEvent(mask, pos);
        }

      
        void WritePointerEvent(byte buttonMask, Point point)
        {
            try
            {
                inputPolicy.WritePointerEvent(buttonMask, point);
            }
            catch (Exception e)
            {
                OnConnectionLost(e);
            }
        }

        /// <summary>
        /// Requests that the remote host send a screen update.
        /// </summary>
        /// <param name="refreshFullScreen">TRUE if the entire screen should be refreshed, FALSE if only a partial region needs updating.</param>
        /// <remarks>RequestScreenUpdate needs to be called whenever the client screen needs to be updated to reflect the state of the remote 
        ///	desktop.  Typically you only need to have a particular region of the screen updated and can still use the rest of the 
        /// pixels on the client-side (i.e., when moving the mouse pointer, only the area around the pointer changes).  Therefore, you should
        /// almost always set refreshFullScreen to FALSE.  If the client-side image becomes corrupted, call RequestScreenUpdate with
        /// refreshFullScreen set to TRUE to get the complete image sent again.
        /// </remarks>
        public void RequestScreenUpdate(bool refreshFullScreen)
        {
            try
            {
                rfb.WriteFramebufferUpdateRequest(0, 0, (ushort)BufferInfos.Width, (ushort)BufferInfos.Height, !refreshFullScreen);
            }
            catch (Exception e)
            {
                OnConnectionLost(e);
            }
        }
    }
}