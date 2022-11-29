using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using VncSharp;

public class VncClient
{
    private Socket _socket;
    private NetworkStream _networkStream;
    private RfbProtocol rfb;
    private bool viewOnlyMode;
    private byte securityType;
    private Framebuffer _framebuffer;
    private EncodedRectangleFactory factory;

    /// <summary>
    /// Connect to a VNC Host and determine which type of Authentication it uses. If the host uses Password Authentication, a call to Authenticate() will be required. Default Display and Port numbers are used.
    /// </summary>
    /// <param name="host">The IP Address or Host Name of the VNC Host.</param>
    /// <returns>Returns True if the VNC Host requires a Password to be sent after Connect() is called, otherwise False.</returns>
    public bool Connect(string host)
    {
        return Connect(host, 0, 5900);
    }

    /// <summary>
    /// Connect to a VNC Host and determine which type of Authentication it uses. If the host uses Password Authentication, a call to Authenticate() will be required. The Port number is calculated based on the Display.
    /// </summary>
    /// <param name="host">The IP Address or Host Name of the VNC Host.</param>
    /// <param name="display">The Display number (used on Unix hosts).</param>
    /// <returns>Returns True if the VNC Host requires a Password to be sent after Connect() is called, otherwise False.</returns>
    public bool Connect(string host, int display)
    {
        return Connect(host, display, 5900);
    }

    public bool Connect(String host, int display, int port)
    {
        var viewOnly = false;

        if (host == null)
        {
            Debug.LogError("Need to provide a host");
            throw new ArgumentNullException(nameof(host));
        }

        // If a diplay number is specified (used to connect to Unix servers)
        // it must be 0 or greater.  This gets added to the default port number
        // in order to determine where the server will be listening for connections.
        if (display < 0) throw new ArgumentOutOfRangeException(nameof(display), display, "Display number must be non-negative.");
        port += display;

        rfb = new RfbProtocol();

        // todo Determine how input works
        // viewOnlyMode = viewOnly;
        // if (viewOnly)
        // {
        //     inputPolicy = new VncViewInputPolicy(rfb);
        // }
        // else
        // {
        //     inputPolicy = new VncDefaultInputPolicy(rfb);
        // }

        // Connect and determine version of server, and set client protocol version to match
        try
        {
            rfb.Connect(host, port);
            rfb.ReadProtocolVersion();

            // Handle possible repeater connection
            if (rfb.ServerVersion == 0.0)
            {
                rfb.WriteProxyAddress();
                // Now we are connected to the real server; read the protocol version of the
                // server
                rfb.ReadProtocolVersion();
                // Resume normal handshake and protocol
            }

            rfb.WriteProtocolVersion();

            // Figure out which type of authentication the server uses
            var types = rfb.ReadSecurityTypes();

            // Based on what the server sends back in the way of supported Security Types, one of
            // two things will need to be done: either the server will reject the connection (i.e., type = 0),
            // or a list of supported types will be sent, of which we need to choose and use one.
            if (types.Length <= 0)
            {
                // Something is wrong, since we should have gotten at least 1 Security Type
                throw new VncProtocolException("Protocol Error Connecting to Server. The Server didn't send any Security Types during the initial handshake.");
            }

            if (types[0] == 0)
            {
                // The server is not able (or willing) to accept the connection.
                // A message follows indicating why the connection was dropped.
                throw new VncProtocolException("Connection Failed. The server rejected the connection for the following reason: " +
                                               rfb.ReadSecurityFailureReason());
            }

            securityType = GetSupportedSecurityType(types);

            if (securityType == 0)
            {
                Debug.LogError("Unknown Security Type(s), The server sent one or more unknown Security Types.");
            }


            rfb.WriteSecurityType(securityType);

            Debug.Log("Security type: " + securityType);

            // If new Security Types are supported in future, add the code here.  For now, only
            // VNC Authentication is supported.
            return Authenticate("password");
        }
        catch (Exception e)
        {
            throw new VncProtocolException("Unable to connect to the server. Error was: " + e.Message, e);
        }

        return false;
    }


    /// <summary>
    /// Examines a list of Security Types supported by a VNC Server and chooses one that the Client supports.  See 6.1.2 of the RFB Protocol document v. 3.8.
    /// </summary>
    /// <param name="types">An array of bytes representing the Security Types supported by the VNC Server.</param>
    /// <returns>A byte that represents the Security Type to be used by the Client.</returns>
    private static byte GetSupportedSecurityType(IReadOnlyList<byte> types)
    {
        // Pick the first match in the list of given types.  If you want to add support for new
        // security types, do it here:
        for (var i = 0; i < types.Count; ++i)
        {
            if (types[i] == 1 // None
                || types[i] == 2 // VNC Authentication
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
    public bool Authenticate(string password)
    {
        // If new Security Types are supported in future, add the code here.  For now, only
        // VNC Authentication is supported.
        if (securityType == 2)
        {
            PerformVncAuthentication(password);
        }
        else if (securityType == 1)
        {
            Debug.Log("No security enabled, no need to authenticate");
        }
        else if (securityType != 1)
        {
            throw new NotSupportedException("Unable to Authenticate with Server. The Server uses an Authentication scheme unknown to the client.");
        }

        if (rfb.ReadSecurityResult() == 0)
        {
            Debug.Log("Successfully authenticated");
            return true;
        }

        // Authentication failed, and if the server is using Protocol version 3.8, a
        // plain text message follows indicating why the error happend.
        // In earlier versions of the protocol, the server will just drop the connection.
        if (Math.Abs(rfb.ServerVersion - 3.8) < 0.05)
        {
            Debug.Log(rfb.ReadSecurityFailureReason());
        }

        rfb.Close(); // TODO: Is this the right place for this???
        return false;
    }

    /// <summary>
    /// Performs VNC Authentication using VNC DES encryption.  See the RFB Protocol doc 6.2.2.
    /// </summary>
    /// <param name="password">A string containing the user's password in clear text format.</param>
    private void PerformVncAuthentication(string password)
    {
        var challenge = rfb.ReadSecurityChallenge();
        rfb.WriteSecurityResponse(EncryptChallenge(password, challenge));
    }

    /// <summary>
    /// Encrypts a challenge using the specified password. See RFB Protocol Document v. 3.8 section 6.2.2.
    /// </summary>
    /// <param name="password">The user's password.</param>
    /// <param name="challenge">The challenge sent by the server.</param>
    /// <returns>Returns the encrypted challenge.</returns>
    private byte[] EncryptChallenge(string password, byte[] challenge)
    {
        var key = new byte[8];

        // Key limited to 8 bytes max.
        Encoding.ASCII.GetBytes(password, 0, password.Length >= 8 ? 8 : password.Length, key, 0);

        // VNC uses reverse byte order in key
        for (var i = 0; i < 8; i++)
            key[i] = (byte)(((key[i] & 0x01) << 7) |
                            ((key[i] & 0x02) << 5) |
                            ((key[i] & 0x04) << 3) |
                            ((key[i] & 0x08) << 1) |
                            ((key[i] & 0x10) >> 1) |
                            ((key[i] & 0x20) >> 3) |
                            ((key[i] & 0x40) >> 5) |
                            ((key[i] & 0x80) >> 7));

        // VNC uses DES, not 3DES as written in some documentation
        DES des = new DESCryptoServiceProvider()
        {
            Padding = PaddingMode.None,
            Mode = CipherMode.ECB
        };
        var enc = des.CreateEncryptor(key, null);

        var response = new byte[16];
        enc.TransformBlock(challenge, 0, challenge.Length, response, 0);

        return response;
    }
    /// <summary>
    /// Finish setting-up protocol with VNC Host.  Should be called after Connect and Authenticate (if password required).
    /// </summary>
    public void Initialize(int bitsPerPixel, int depth)
    {
        // Finish initializing protocol with host
        rfb.WriteClientInitialisation(true);  // Allow the desktop to be shared
        _framebuffer = rfb.ReadServerInit(bitsPerPixel, depth);

        rfb.WriteSetEncodings(new uint[] {	RfbProtocol.ZRLE_ENCODING,
            RfbProtocol.HEXTILE_ENCODING,
            //	RfbProtocol.CORRE_ENCODING, // CoRRE is buggy in some hosts, so don't bother using
            RfbProtocol.RRE_ENCODING,
            RfbProtocol.COPYRECT_ENCODING,
            RfbProtocol.RAW_ENCODING });

        rfb.WriteSetPixelFormat(_framebuffer);	// set the required framebuffer format

        // Create an EncodedRectangleFactory so that EncodedRectangles can be built according to set pixel layout
        factory = new EncodedRectangleFactory(rfb, _framebuffer);
    }

}