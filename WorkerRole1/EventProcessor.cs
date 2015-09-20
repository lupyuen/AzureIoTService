using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Diagnostics;
using Newtonsoft.Json;

namespace WorkerRole1
{
    class EventProcessor : IEventProcessor
    {
        Stopwatch checkpointStopWatch;

        async Task IEventProcessor.CloseAsync(PartitionContext context, CloseReason reason)
        {
            Trace.WriteLine(string.Format("Processor Shutting Down. Partition '{0}', Reason: '{1}'.", context.Lease.PartitionId, reason));
            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
            }
        }

        Task IEventProcessor.OpenAsync(PartitionContext context)
        {
            Trace.WriteLine(string.Format("EventProcessor initialized.  Partition: '{0}', Offset: '{1}'", context.Lease.PartitionId, context.Lease.Offset));
            this.checkpointStopWatch = new Stopwatch();
            this.checkpointStopWatch.Start();
            return Task.FromResult<object>(null);
        }

        async Task IEventProcessor.ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (EventData eventData in messages)
            {
                string data = Encoding.UTF8.GetString(eventData.GetBytes());

                Trace.WriteLine(string.Format("Message received.  Partition: '{0}', Data: '{1}'",
                    context.Lease.PartitionId, data));
                NewRelic.Api.Agent.NewRelic.SetTransactionName("ActuateDevice", data); var watch = Stopwatch.StartNew();

                //  Convert data to JSON.
                //  data looks like {"group":"1", "device":"2", "action":"led", "parameter":"on"}
                var query = JsonConvert.DeserializeObject<Dictionary<string, string>>(data);

                //  TODO: Send data to the right session.

                NewRelic.Api.Agent.NewRelic.RecordResponseTimeMetric(data, watch.ElapsedMilliseconds);
            }

            //Call checkpoint every 5 minutes, so that worker can resume processing from the 5 minutes back if it restarts.
            if (this.checkpointStopWatch.Elapsed > TimeSpan.FromMinutes(5))
            {
                await context.CheckpointAsync();
                this.checkpointStopWatch.Restart();
            }
        }
    }
}
