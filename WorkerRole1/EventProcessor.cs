using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Diagnostics;
using Newtonsoft.Json;
using NRConfig;
using SuperSocket.QuickStart.RemoteProcessService;

namespace WorkerRole1
{
    [Instrument]
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
            //  Actuate a device by sending an action and parameter to a group ID and device ID.
            //  This method receives the actuation info from WebRole1.ActuateDevice.aspx via Azure Event Hub.
            //  WorkerRole1 is a TCP socket server that maintains long-lasting TCP socket connections
            //  initiated by every IoT device (e.g. LinkIt ONE).  The actuation info is transmitted to
            //  the designated device through the TCP socket connection. 

            foreach (EventData eventData in messages)
            {
                string data = Encoding.UTF8.GetString(eventData.GetBytes());

                Trace.WriteLine(string.Format("Message received.  Partition: '{0}', Data: '{1}'",
                    context.Lease.PartitionId, data));
                NewRelic.Api.Agent.NewRelic.SetTransactionName("ActuateDevice", data); var watch = Stopwatch.StartNew();

                //  Convert data to JSON.
                //  data looks like {"group":1, "device":2, "action":"led", "parameter":"on"}
                var query = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);

                //  If there is no action, then this is a sensor data message, not an actuation message. We skip it.
                //  Else we process the action by transmitting to the right TCP client.
                if (query.ContainsKey("action"))
                {
                    //  Send data to the right session, according to the group and device IDs provided.
                    SendMessage(query);
                }
                NewRelic.Api.Agent.NewRelic.RecordResponseTimeMetric(data, watch.ElapsedMilliseconds);
            }

            //Call checkpoint every 5 minutes, so that worker can resume processing from the 5 minutes back if it restarts.
            if (this.checkpointStopWatch.Elapsed > TimeSpan.FromMinutes(5))
            {
                await context.CheckpointAsync();
                this.checkpointStopWatch.Restart();
            }
        }

        static void SendMessage(Dictionary<string, object> query)
        {
            //  Send data to the right session, according to the group and device IDs provided.
            //  query contains keys and values like {"group":1, "device":2, "action":"led", "parameter":"on"}
            var group = "";
            if (query.ContainsKey("group"))
                group = query["group"].ToString();
            var device = "";
            if (query.ContainsKey("device"))
                device = query["device"].ToString();

            //  Get the action and parameter.
            var action = "";
            if (query.ContainsKey("action"))
                action = query["action"].ToString();
            var parameter = "";
            if (query.ContainsKey("parameter"))
                parameter = query["parameter"].ToString();
            //  Send the action and parameter to the session, separated by space, e.g. "led on".
            var msg = string.Format("{0} {1}\r\n", action, parameter);

            //  If group and device are both specified, send directly to the device.
            if (group != "" && device != "")
            {
                var key = group + "_" + device;
                if (RemoteProcessSession.allSessions.ContainsKey(key))
                {
                    Trace.WriteLine(string.Format("Sending message to group {0} device {1}: {2}",
                        group, device, msg));
                    try { RemoteProcessSession.allSessions[key].Send(msg); }
                    catch (Exception) { }
                }
                else
                {
                    Trace.WriteLine(string.Format("Group {0} device {1} not found", group, device));
                }
                return;
            }
            if (group != "" && device == "")
            {
                //  If group is specified but not device, we send to all devices in that group.
                Trace.WriteLine(string.Format("Sending to group {0}: {1}", group, msg));
                foreach (var k in RemoteProcessSession.allSessions.Keys)
                {
                    if (k.StartsWith(group + "_"))
                    {
                        try { RemoteProcessSession.allSessions[k].Send(msg); }
                        catch (Exception) { }
                    }  
                }
                return;
            }
            //  We blast all devices.
            Trace.WriteLine(string.Format("Sending to everyone: {0}", msg));
            foreach (var k in RemoteProcessSession.allSessions.Keys)
            {
                try { RemoteProcessSession.allSessions[k].Send(msg); }
                catch (Exception) { }
            }
        }
    }
}
