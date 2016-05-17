using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Remote.Transport.Streaming;

namespace Benchmark
{
    class Program
    {
        private static readonly Config BaseConfig = ConfigurationFactory.ParseString(@"
akka {
    stdout-loglevel = off
    loglevel = off

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
            int concurrency = Environment.ProcessorCount;
            if (args.Length == 1)
                concurrency = int.Parse(args[0]);

            var system1 = ActorSystem.Create("System", CreateNetworkStreamConfig(8080));
            var system2 = ActorSystem.Create("System", CreateNetworkStreamConfig(8081));

            Console.WriteLine("NetworkStreamTransport:");
            RunTest(system1, system2, concurrency);

            system1.Terminate().Wait();
            system2.Terminate().Wait();

            Thread.Sleep(500);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Thread.Sleep(500);

            Console.WriteLine();

            system1 = ActorSystem.Create("System", CreateHeliosConfig(8080));
            system2 = ActorSystem.Create("System", CreateHeliosConfig(8081));

            Console.WriteLine("Helios:");
            RunTest(system1, system2, concurrency);

            system1.Terminate().Wait();
            system2.Terminate().Wait();
        }

        private static void RunTest(ActorSystem system1, ActorSystem system2, int concurrency)
        {
            int messageCount = 20000;
            SemaphoreSlim ready = new SemaphoreSlim(0);
            ManualResetEventSlim start = new ManualResetEventSlim(false);

            var workers = new List<Task>();
            for (int i = 1; i <= concurrency; i++)
            {
                int index = i;
                workers.Add(RunThread(() =>
                {
                    var receiver = system1.ActorOf<ReceiverActor>("Receiver_" + index);
                    system2.ActorOf<EchoActor>("EchoActor_" + index);

                    string echoActorPath = "akka.tcp://System@localhost:8081/user/EchoActor_" + index;

                    var remoteEchoRef = system1.ActorSelection(echoActorPath).ResolveOne(TimeSpan.FromSeconds(5)).Result;

                    receiver.Ask(new ReceiverActor.Init(remoteEchoRef), TimeSpan.FromSeconds(5)).Wait();

                    ready.Release();

                    start.Wait();

                    var task = receiver.Ask(new ReceiverActor.PerformanceRun(messageCount));

                    for (int j = 0; j < messageCount; j++)
                    {
                        remoteEchoRef.Tell("a", ActorRefs.NoSender);
                    }

                    task.Wait();
                }));
            }

            for (int i = 0; i < concurrency; i++)
                ready.Wait(5000);

            start.Set();
            Stopwatch sw = Stopwatch.StartNew();

            Task.WaitAll(workers.ToArray());

            Console.WriteLine("Message per seconds: " + messageCount * concurrency * 2 / sw.Elapsed.TotalSeconds);
        }

        private static Task RunThread(Action action)
        {
            return Task.Factory.StartNew(action, TaskCreationOptions.LongRunning);
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
    }

    public class ReceiverActor : UntypedActor
    {
        public class Init
        {
            public IActorRef EchoActor { get; private set; }

            public Init(IActorRef echoActor)
            {
                EchoActor = echoActor;
            }
        }

        public class PerformanceRun
        {
            public int MessageCount { get; private set; }

            public PerformanceRun(int messageCount)
            {
                MessageCount = messageCount;
            }
        }

        private IActorRef _init;
        private IActorRef _request;
        private int _total;
        private int _received;

        protected override void OnReceive(object message)
        {
            if (message is Init)
            {
                var init = (Init)message;
                _init = Sender;
                init.EchoActor.Tell("a");

                Become(WaitingForEcho);
            }
        }

        private void WaitingForEcho(object message)
        {
            _init.Tell("done");
            Become(Ready);
        }

        private void Ready(object message)
        {
            if (message is PerformanceRun)
            {
                var request = (PerformanceRun)message;
                _request = Sender;
                _total = request.MessageCount;

                Become(Running);
            }
        }

        private void Running(object message)
        {
            ++_received;

            if (_received >= _total)
                _request.Tell("done");
        }
    }

    public class EchoActor : UntypedActor
    {
        private IActorRef _sender;

        protected override void OnReceive(object message)
        {
            if (_sender == null)
                _sender = Sender;

            _sender.Tell(message, ActorRefs.NoSender);
        }
    }
}
