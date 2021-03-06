﻿#if !SILICONSTUDIO_PLATFORM_WINDOWS_RUNTIME
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Sockets.Plugin.Abstractions;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Sockets.Plugin
{
    /// <summary>
    ///     Sends and receives data over a TCP socket. Establish a connection with a listening TCP socket using
    ///     <code>ConnectAsync</code>.
    ///     Use the <code>WriteStream</code> and <code>ReadStream</code> properties for sending and receiving data
    ///     respectively.
    /// </summary>
    class TcpSocketClient : ITcpSocketClient
    {
        private readonly TcpClient _backingTcpClient;
        private readonly int _bufferSize;
        private SslStream _secureStream;
        private Stream _writeStream;

        /// <summary>
        ///     Default constructor for <code>TcpSocketClient</code>.
        /// </summary>
        public TcpSocketClient()
        {
            _backingTcpClient = new TcpClient();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpSocketClient"/> class.
        /// </summary>
        /// <param name="bufferSize">Size of the buffer for the write stream.</param>
        public TcpSocketClient(int bufferSize) : this()
        {
            _bufferSize = bufferSize;
        }

        internal TcpSocketClient(TcpClient backingClient, int bufferSize)
        {
            _backingTcpClient = backingClient;
            _bufferSize = bufferSize;
            InitializeWriteStream();
        }

        /// <summary>
        ///     Establishes a TCP connection with the endpoint at the specified address/port pair.
        /// </summary>
        /// <param name="address">The address of the endpoint to connect to.</param>
        /// <param name="port">The port of the endpoint to connect to.</param>
        /// <param name="secure">True to enable TLS on the socket.</param>
        public async Task ConnectAsync(string address, int port, bool secure = false)
        {
            await _backingTcpClient.ConnectAsync(address, port);
            InitializeWriteStream();
            if (secure)
            {
                var secureStream = new SslStream(_writeStream, true, (sender, cert, chain, sslPolicy) => ServerValidationCallback(sender, cert, chain, sslPolicy));
                secureStream.AuthenticateAsClient(address, null, System.Security.Authentication.SslProtocols.Tls, false);
                _secureStream = secureStream;
            }            
        }

        #region Secure Sockets Details
        
        private bool ServerValidationCallback (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            switch (sslPolicyErrors)
            {
                case SslPolicyErrors.RemoteCertificateNameMismatch:
                    Console.WriteLine("Server name mismatch. End communication ...\n");
                    return false;
                case SslPolicyErrors.RemoteCertificateNotAvailable:
                    Console.WriteLine("Server's certificate not available. End communication ...\n");
                    return false;
                case SslPolicyErrors.RemoteCertificateChainErrors:
                    Console.WriteLine("Server's certificate validation failed. End communication ...\n");
                    return false;
            }
            //TODO: Perform others checks using the "certificate" and "chain" objects ...

            Console.WriteLine("Server's authentication succeeded ...\n");
            return true;
        }

        #endregion

        /// <summary>
        ///     Disconnects from an endpoint previously connected to using <code>ConnectAsync</code>.
        ///     Should not be called on a <code>TcpSocketClient</code> that is not already connected.
        /// </summary>
        public Task DisconnectAsync()
        {
            return Task.Run(() => {
#if !SILICONSTUDIO_RUNTIME_CORECLR
                    // As long as we target .NET 4.5 we cannot use `Dispose'.
                _backingTcpClient.Close();
#else
                _backingTcpClient.Dispose();
#endif
                _secureStream = null;
            });
        }

        /// <summary>
        ///     A stream that can be used for receiving data from the remote endpoint.
        /// </summary>
        public Stream ReadStream
        {
            get
            {
                if (_secureStream != null)
                {
                    return _secureStream as Stream;
                }
                return _backingTcpClient.GetStream();
            }
        }

        /// <summary>
        ///     A stream that can be used for sending data to the remote endpoint.
        /// </summary>
        public Stream WriteStream
        {
            get
            {
                if (_secureStream != null)
                {
                    return _secureStream as Stream;
                }
                return _writeStream;
            }
        }

        private IPEndPoint RemoteEndpoint
        {
            get { return _backingTcpClient.Client.RemoteEndPoint as IPEndPoint; }
        }

        /// <summary>
        ///     The address of the remote endpoint to which the <code>TcpSocketClient</code> is currently connected.
        /// </summary>
        public string RemoteAddress
        {
            get { return RemoteEndpoint.Address.ToString(); }
        }

        /// <summary>
        ///     The port of the remote endpoint to which the <code>TcpSocketClient</code> is currently connected.
        /// </summary>
        public int RemotePort
        {
            get { return RemoteEndpoint.Port; }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage collection.
        /// </summary>
        ~TcpSocketClient()
        {
            Dispose(false);
        }

        private void InitializeWriteStream()
        {
#if !SILICONSTUDIO_RUNTIME_CORECLR
            _writeStream = _bufferSize != 0 ? (Stream)new BufferedStream(_backingTcpClient.GetStream(), _bufferSize) : _backingTcpClient.GetStream();
#else
                // Note that BufferedStream is planned for a future release of CoreFX, so once this 
                // is available we can remove the #if.
            _writeStream = _backingTcpClient.GetStream();
#endif
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_writeStream != null)
                    _writeStream.Dispose();
                if (_backingTcpClient != null)
                    ((IDisposable)_backingTcpClient).Dispose();
            }
        }
        
    }
}
#endif