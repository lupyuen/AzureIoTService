using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using SuperSocket.SocketEngine;
using SuperSocket.SocketBase;
using Microsoft.ServiceBus.Messaging;

namespace WorkerRole1
{
    public class WorkerRole : RoleEntryPoint
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        EventProcessorHost eventProcessorHost = null;

        public override void Run()
        {
            Trace.TraceInformation("WorkerRole1 is running");
            NewRelic.Api.Agent.NewRelic.SetTransactionName("Worker", "Run"); var watch = Stopwatch.StartNew();
            //  Start listening for actuation events.
            string eventHubConnectionString = "Endpoint=sb://azureiothub.servicebus.windows.net/;SharedAccessKeyName=ReceiveRule;SharedAccessKey=905MauqIRlwOAdzbFOrctA3+YtO8Od4lUzFPL0AqAsA=";
            string eventHubName = "azureiothub";
            string storageAccountName = "azureiotstorage";
            string storageAccountKey = "OE8ELPPu30uc1BVRW21WH3Sb6aoTkRNbP4vmYX0eLAukYNS9prF13laVUJHQkx0hVrIeDt88a5TAwQflEcTqNg==";
            string storageConnectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}",
                storageAccountName, storageAccountKey);

            string eventProcessorHostName = Guid.NewGuid().ToString();
            eventProcessorHost = new EventProcessorHost(eventProcessorHostName, eventHubName, EventHubConsumerGroup.DefaultGroupName, eventHubConnectionString, storageConnectionString);
            Trace.WriteLine("Registering EventProcessor...");
            eventProcessorHost.RegisterEventProcessorAsync<EventProcessor>().Wait();

            NewRelic.Api.Agent.NewRelic.RecordResponseTimeMetric("Run", watch.ElapsedMilliseconds);

            try
            {
                this.RunAsync(this.cancellationTokenSource.Token).Wait();
            }
            finally
            {
                this.runCompleteEvent.Set();
            }

        }

        IBootstrap m_Bootstrap = null;

        public override bool OnStart()
        {
            Trace.TraceInformation("WorkerRole1 is starting");
            NewRelic.Api.Agent.NewRelic.SetTransactionName("Worker", "OnStart"); var watch = Stopwatch.StartNew();

            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 200;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            m_Bootstrap = BootstrapFactory.CreateBootstrap();

            var endpoints = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints.ToDictionary
                (p => p.Key, p => p.Value.IPEndpoint);
            if (!m_Bootstrap.Initialize(endpoints))
            {
                Trace.WriteLine("Failed to initialize SuperSocket!", "Error");
                NewRelic.Api.Agent.NewRelic.RecordResponseTimeMetric("OnStart", watch.ElapsedMilliseconds);
                return false;
            }

            var result = m_Bootstrap.Start();

            switch (result)
            {
                case (StartResult.None):
                    Trace.WriteLine("No server is configured, please check you configuration!");
                    NewRelic.Api.Agent.NewRelic.RecordResponseTimeMetric("OnStart", watch.ElapsedMilliseconds);
                    return false;

                case (StartResult.Success):
                    Trace.WriteLine("The server has been started!");
                    break;

                case (StartResult.Failed):
                    Trace.WriteLine("Failed to start SuperSocket server! Please check error log for more information!");
                    NewRelic.Api.Agent.NewRelic.RecordResponseTimeMetric("OnStart", watch.ElapsedMilliseconds);
                    return false;

                case (StartResult.PartialSuccess):
                    Trace.WriteLine("Some server instances were started successfully, but the others failed to start! Please check error log for more information!");
                    break;
            }

            Trace.TraceInformation("WorkerRole1 has started");
            NewRelic.Api.Agent.NewRelic.RecordResponseTimeMetric("OnStart", watch.ElapsedMilliseconds);

            return base.OnStart();
        }

        public override void OnStop()
        {
            Trace.TraceInformation("WorkerRole1 is stopping");
            NewRelic.Api.Agent.NewRelic.SetTransactionName("Worker", "OnStop"); var watch = Stopwatch.StartNew();

            //  Stop listening for actuation events.
            eventProcessorHost.UnregisterEventProcessorAsync().Wait();

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
            NewRelic.Api.Agent.NewRelic.RecordResponseTimeMetric("OnStop", watch.ElapsedMilliseconds);
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested)
            {
                ////Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }
    }
}
