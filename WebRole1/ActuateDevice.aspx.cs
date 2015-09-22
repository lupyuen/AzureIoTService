//  http://localhost:53385/ActuateDevice.aspx?group=1&device=2&action=led&parameter=on
//  http://azureiotservice.cloudapp.net/ActuateDevice.aspx?group=1&device=2&action=led&parameter=on
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using NRConfig;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebRole1
{
    [Instrument]
    public partial class ActuateDevice : System.Web.UI.Page
    {
        public static string eventHubName = "azureiothub";
        public static string connectionString = "Endpoint=sb://azureiothub.servicebus.windows.net/;SharedAccessKeyName=SendRule;SharedAccessKey=+TNGe5Awzvd0QLFUWbEWxo7atZ1JXgCPkajeEIqVMu8=";

        protected void Page_Load(object sender, EventArgs e)
        {
            //  Actuate a device by sending an action and parameter to a group ID and device ID.
            //  This page sends the actuation info to WorkerRole1 via Azure Event Hub.
            //  WorkerRole1 is a TCP socket server that maintains long-lasting TCP socket connections
            //  initiated by every IoT device (e.g. LinkIt ONE).  The actuation info is transmitted to
            //  the designated device through the TCP socket connection. 

            NewRelic.Api.Agent.NewRelic.SetTransactionName("API", "ActuateDevice"); var watch = Stopwatch.StartNew();
            Response.Expires = -1;
            var group = Request["group"];
            var device = Request["device"];
            var action = Request["action"];
            var parameter = Request["parameter"];

            //  Convert the query string to a JSON message before sending it.
            //  The message will look like {"group":1, "device":2, "action":"led", "parameter":"on"}
            var query = new Dictionary<string, object>();
            foreach (var key in Request.QueryString.AllKeys)
            {
                string value = Request[key];
                //  If value is float or int, we set to the float and int data type.
                float outputFloat;
                int outputInt;
                if (value.Contains(".") && float.TryParse(value, out outputFloat))
                {
                    query[key] = outputFloat;
                }
                else if (int.TryParse(value, out outputInt))
                {
                    query[key] = outputInt;
                }
                else
                {
                    query[key] = value;
                }
            }
            var message = JsonConvert.SerializeObject(query);

            var eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, eventHubName);
            System.Diagnostics.Trace.WriteLine(string.Format("{0} > Sending message: {1}", DateTime.Now, message));
            eventHubClient.Send(new EventData(Encoding.UTF8.GetBytes(message)));
            eventHubClient.Close();
            Response.Write("'OK'");

            NewRelic.Api.Agent.NewRelic.RecordResponseTimeMetric("ActuateDevice", watch.ElapsedMilliseconds);
            Response.End();
        }
    }
}