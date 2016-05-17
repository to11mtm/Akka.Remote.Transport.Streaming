using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Remote.Transport.Streaming;

namespace Sample
{
    class Program
    {
        private static readonly Config BaseConfig = ConfigurationFactory.ParseString(@"
akka {
    actor {
        provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
        serializers {
            wire = ""Akka.Serialization.WireSerializer, Akka.Serialization.Wire""
        }
        serialization-bindings {
            ""System.Object"" = wire
        }
    }
}
");

        static void Main(string[] args)
        {
            var system1 = ActorSystem.Create("System", CreateNetworkStreamConfig(8080));
            var system2 = ActorSystem.Create("System", CreateNetworkStreamConfig(8081));
            var system3 = ActorSystem.Create("System", CreateHeliosConfig(8082));
            var system4 = ActorSystem.Create("System", CreateHeliosAdapterConfig(8083));

            // Create the echo actor on system 1
            system1.ActorOf<EchoActor>("echo");

            IActorRef echoRef;

            // Create a NetworkStream <-> NetworkStream association
            echoRef = system2.ActorSelection("akka.tcp://System@localhost:8080/user/echo").ResolveOne(TimeSpan.FromSeconds(1)).Result;
            Console.WriteLine(echoRef.Ask<string>("Hello world!", TimeSpan.FromSeconds(1)).Result);

            // Create a NetworkStream <-> Helios association
            echoRef = system3.ActorSelection("akka.tcp://System@localhost:8080/user/echo").ResolveOne(TimeSpan.FromSeconds(1)).Result;
            Console.WriteLine(echoRef.Ask<string>("Hello world!", TimeSpan.FromSeconds(1)).Result);

            // Create a NetworkStream <-> NetworkStream association using the Helios config adapter
            echoRef = system4.ActorSelection("akka.tcp://System@localhost:8080/user/echo").ResolveOne(TimeSpan.FromSeconds(1)).Result;
            Console.WriteLine(echoRef.Ask<string>("Hello world!", TimeSpan.FromSeconds(1)).Result);
        }

        private static Config CreateNetworkStreamConfig(int port)
        {
            var config = ConfigurationFactory.ParseString(@"
akka.remote {
    enabled-transports = [""akka.remote.networkstream""]
    networkstream {
        hostname = ""localhost""
        port = " + port + @"
    }
}");
            return config
                .WithFallback(NetworkStreamDefaultConfig.Instance)
                .WithFallback(BaseConfig);
        }


        private static Config CreateHeliosConfig(int port)
        {
            var config = ConfigurationFactory.ParseString(@"
akka.remote {
    helios.tcp {
        hostname = ""localhost""
        port = " + port + @"
    }
}");
            return config
                .WithFallback(BaseConfig);
        }

        private static Config CreateHeliosAdapterConfig(int port)
        {
            var config = ConfigurationFactory.ParseString(@"
akka.remote {
    helios.tcp {
        transport-class = ""Akka.Remote.Transport.Streaming.NetworkStreamTransportHeliosAdapter, Akka.Remote.Transport.Streaming""
        hostname = ""localhost""
        port = " + port + @"
    }
}");
            return config
                .WithFallback(BaseConfig);
        }
    }

    public class EchoActor : UntypedActor
    {
        protected override void OnReceive(object message)
        {
            Sender.Tell(message, ActorRefs.NoSender);
        }
    }
}
