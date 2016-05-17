using Akka.Configuration;

namespace Akka.Remote.Transport.Streaming
{
    public class NetworkStreamDefaultConfig
    {
        public static readonly Config Instance = ConfigurationFactory.ParseString(@"
akka.remote.networkstream {
      transport-class = ""Akka.Remote.Transport.Streaming.NetworkStreamTransport, Akka.Remote.Transport.Streaming""

      # Transport drivers can be augmented with adapters by adding their
      # name to the applied-adapters list. The last adapter in the
      # list is the adapter immediately above the driver, while
      # the first one is the top of the stack below the standard
      # Akka protocol
      applied-adapters = []

      # The default remote server port clients should connect to.
      # Default is 2552 (AKKA), use 0 if you want a random available port
      # This port needs to be unique for each actor system on the same machine.
      port = 2552

      # The hostname or ip clients should connect to.
      hostname = ""localhost""

      # Use this setting to bind a network interface to a different port
      # than remoting protocol expects messages at. This may be used
      # when running akka nodes in a separated networks (under NATs or docker containers).
      # Use 0 if you want a random available port. Examples:
      #
      # akka.remote.networkstream.port = 2552
      # akka.remote.networkstream.bind-port = 2553
      # Network interface will be bound to the 2553 port, but remoting protocol will
      # expect messages sent to port 2552.
      #
      # akka.remote.networkstream.port = 0
      # akka.remote.networkstream.bind-port = 0
      # Network interface will be bound to a random port, and remoting protocol will
      # expect messages sent to the bound port.
      #
      # akka.remote.networkstream.port = 2552
      # akka.remote.networkstream.bind-port = 0
      # Network interface will be bound to a random port, but remoting protocol will
      # expect messages sent to port 2552.
      #
      # akka.remote.networkstream.port = 0
      # akka.remote.networkstream.bind-port = 2553
      # Network interface will be bound to the 2553 port, and remoting protocol will
      # expect messages sent to the bound port.
      #
      # akka.remote.networkstream.port = 2552
      # akka.remote.networkstream.bind-port = ''
      # Network interface will be bound to the 2552 port, and remoting protocol will
      # expect messages sent to the bound port.
      #
      # akka.remote.networkstream.port if empty
      bind-port = """"

      # Use this setting to bind to a specific ip
      # than remoting protocol expects messages at.
      # Use '0.0.0.0' to bind to all interfaces.
      # akka.remote.networkstream.hostname if empty
      bind-hostname = """"

      # Sets the connectTimeoutMillis of all outbound connections,
      # i.e. how long a connect may take until it is timed out
      connection-timeout = 15s

      # Sets the send buffer size of the Sockets.
      send-buffer-size = 256000b

      # Sets the receive buffer size of the Sockets.
      receive-buffer-size = 256000b

      # Sets the write buffer size of the Stream.
      # This is the buffer that is given to Socket.BeginSend.
      # Setting a higher value might cause send delays, only modify if you know what you are doing
      stream-write-buffer-size = 4096b

      # Sets the read buffer size of the Stream.
      # This is the buffer that is given to Socket.BeginReceive.
      stream-read-buffer-size = 65536b

      # Maximum message size the transport will accept, but at least
      # 32000 bytes.
      # Please note that UDP does not support arbitrary large datagrams,
      # so this setting has to be chosen carefully when using UDP.
      # Both send-buffer-size and receive-buffer-size settings has to
      # be adjusted to be able to buffer messages of maximum size.
      maximum-frame-size = 128000b

      # Sets the size of the connection backlog
      backlog = 4096

      # Enables the TCP_NODELAY flag, i.e. disables Nagle's algorithm
      tcp-nodelay = on

      # Enables TCP Keepalive, subject to the O/S kernel's configuration
      tcp-keepalive = on

      # Messages larger than this threshold will be read by chunk
      # if the data is not already available in the buffer.
      chunked-read-threshold = 4096b

      # Messages larger than this limit will make the connection drop.
      # Default 64MB.
      frame-size-hard-limit = 67108864b
}");
    }
}