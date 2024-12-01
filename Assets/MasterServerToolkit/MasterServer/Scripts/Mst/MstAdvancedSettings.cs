using MasterServerToolkit.Logging;
using MasterServerToolkit.Networking;
using System;
using System.Security.Authentication;

namespace MasterServerToolkit.MasterServer
{
    /// <summary>
    /// Advanced settings wrapper
    /// </summary>
    public class MstAdvancedSettings
    {
        /// <summary>
        /// 
        /// </summary>
        public string ApplicationKey => Mst.Args.AsString(Mst.Args.Names.ApplicationKey, "mst");

        /// <summary>
        /// 
        /// </summary>
        public bool HasApplicationKey => !string.IsNullOrEmpty(ApplicationKey);

        /// <summary>
        /// Whether or not to use secure connection 
        /// </summary>
        public bool UseSecure => Mst.Args.AsBool(Mst.Args.Names.UseSecure);

        /// <summary>
        /// Path to certificate
        /// </summary>
        public string CertificatePath => Mst.Args.CertificatePath;

        /// <summary>
        /// Path to certificate
        /// </summary>
        public string CertificatePassword => Mst.Args.CertificatePassword;

        /// <summary>
        /// Ssl protocol
        /// </summary>
        public SslProtocols SslProtocols { get; set; }

        /// <summary>
        /// Factory, used to create client sockets
        /// </summary>
        public Func<IClientSocket> ClientSocketFactory = () => new WsClientSocket();

        /// <summary>
        /// Factory, used to create server sockets
        /// </summary>
        public Func<IServerSocket> ServerSocketFactory = () => new WsServerSocket();

        /// <summary>
        /// Message factory
        /// </summary>
        public IMessageFactory MessageFactory => new MessageFactory();

        /// <summary>
        /// Global logging settings
        /// </summary>
        public MstLogController Logging { get; private set; }

        public MstAdvancedSettings()
        {
            Logging = new MstLogController(LogLevel.All);
        }
    }
}