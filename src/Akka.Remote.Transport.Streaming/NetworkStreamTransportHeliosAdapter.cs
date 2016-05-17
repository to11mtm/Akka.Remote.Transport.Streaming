using System;
using System.Net;
using Akka.Actor;
using Akka.Configuration;
using Akka.Dispatch;

namespace Akka.Remote.Transport.Streaming
{
    class HeliosTransportSettings
    {
        internal readonly Config Config;

        public HeliosTransportSettings(Config config)
        {
            Config = config;
            Init();
        }

        private void Init()
        {
            //TransportMode
            var protocolString = Config.GetString("transport-protocol");
            if (!protocolString.Equals("tcp"))
                throw new ConfigurationException(string.Format("Unknown transport transport-protocol='{0}'", protocolString));
            EnableSsl = Config.GetBoolean("enable-ssl");
            ConnectTimeout = Config.GetTimeSpan("connection-timeout");
            WriteBufferHighWaterMark = OptionSize("write-buffer-high-water-mark");
            WriteBufferLowWaterMark = OptionSize("write-buffer-low-water-mark");
            SendBufferSize = OptionSize("send-buffer-size");
            ReceiveBufferSize = OptionSize("receive-buffer-size");
            var size = OptionSize("maximum-frame-size");
            if (size == null || size < 32000) throw new ConfigurationException("Setting 'maximum-frame-size' must be at least 32000 bytes");
            MaxFrameSize = (int)size;
            Backlog = Config.GetInt("backlog");
            TcpNoDelay = Config.GetBoolean("tcp-nodelay");
            TcpKeepAlive = Config.GetBoolean("tcp-keepalive");
            TcpReuseAddr = Config.GetBoolean("tcp-reuse-addr");
            var configHost = Config.GetString("hostname");
            var publicConfigHost = Config.GetString("public-hostname");
            Hostname = string.IsNullOrEmpty(configHost) ? IPAddress.Any.ToString() : configHost;
            PublicHostname = string.IsNullOrEmpty(publicConfigHost) ? Hostname : publicConfigHost;
            ServerSocketWorkerPoolSize = ComputeWps(Config.GetConfig("server-socket-worker-pool"));
            ClientSocketWorkerPoolSize = ComputeWps(Config.GetConfig("client-socket-worker-pool"));
            Port = Config.GetInt("port");
        }

        public bool EnableSsl { get; private set; }

        public TimeSpan ConnectTimeout { get; private set; }

        public long? WriteBufferHighWaterMark { get; private set; }

        public long? WriteBufferLowWaterMark { get; private set; }

        public long? SendBufferSize { get; private set; }

        public long? ReceiveBufferSize { get; private set; }

        public int MaxFrameSize { get; private set; }

        public int Port { get; private set; }

        public int Backlog { get; private set; }

        public bool TcpNoDelay { get; private set; }

        public bool TcpKeepAlive { get; private set; }

        public bool TcpReuseAddr { get; private set; }

        /// <summary>
        /// The hostname that this server binds to
        /// </summary>
        public string Hostname { get; private set; }

        /// <summary>
        /// If different from <see cref="Hostname"/>, this is the public "address" that is bound to the <see cref="ActorSystem"/>,
        /// whereas <see cref="Hostname"/> becomes the physical address that the low-level socket connects to.
        /// </summary>
        public string PublicHostname { get; private set; }

        public int ServerSocketWorkerPoolSize { get; private set; }

        public int ClientSocketWorkerPoolSize { get; private set; }

        #region Internal methods

        private long? OptionSize(string s)
        {
            var bytes = Config.GetByteSize(s);
            if (bytes == null || bytes == 0) return null;
            if (bytes < 0) throw new ConfigurationException(string.Format("Setting {0} must be 0 or positive", s));
            return bytes;
        }

        private int ComputeWps(Config config)
        {
            return ThreadPoolConfig.ScaledPoolSize(config.GetInt("pool-size-min"), config.GetDouble("pool-size-factor"),
                config.GetInt("pool-size-max"));
        }

        #endregion
    }

    public class NetworkStreamTransportHeliosAdapter : NetworkStreamTransport
    {
        public NetworkStreamTransportHeliosAdapter(ActorSystem system, Config config)
            : base(system, GetSettings(config))
        { }

        private static NetworkStreamTransportSettings GetSettings(Config config)
        {
            var heliosSettings = new HeliosTransportSettings(config);

            return new NetworkStreamTransportSettings(heliosSettings);
        }
    }
}