//  http://azureiotservice.cloudapp.net/RecordSensorData.aspx?Group=1&Device=1&Temperature=31.2&LightLevel=123   

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
    public partial class RecordSensorData : System.Web.UI.Page
    {
        //  Remember the sensor data in memory.  
        //  Also send the sensor data to Azure Event Hub for further analysis.

        //  Maps Group ID to the sensor data.
        public static Dictionary<string, System.Collections.Specialized.NameValueCollection> sensorData =
            new Dictionary<string, System.Collections.Specialized.NameValueCollection>();
        //  Maps Group ID to the timestamp.
        public static Dictionary<string, string> timestampData = new Dictionary<string, string>();

        //  Azure Event Hub for sending sensor data.
        static string sensorHub = "sensorhub";
        static string sensorHubSendRule = "Endpoint=sb://azureiothub.servicebus.windows.net/;SharedAccessKeyName=SendRule;SharedAccessKey=iVZoDRKFlSfflaJxJWOn3z8ZkLIH8HBQ8fwDBgGmSK8=";

        protected void Page_Load(object sender, EventArgs e)
        {
            //  Record the querystring parameters as the sensor data, e.g.
            //  RecordSensorData.aspx?Temperature=31.2&LightLevel=99
            //  Sensor data will be used by Excel Web "Update Sensor Data" addin.
            NewRelic.Api.Agent.NewRelic.SetTransactionName("API", "RecordSensorData"); var watch = Stopwatch.StartNew();
            Response.Expires = -1;
            //  Allow multiple groups of sensors, e.g. Group=1
            var group = "0";  //  If not specified, group = 0.
            if (Request["Group"] != null) group = Request["Group"];
            sensorData[group] = Request.QueryString;
            var timestamp = DateTime.Now.AddHours(8).ToString("s").Replace("T", " ");
            timestampData[group] = timestamp;

            //  Send the sensor data to Azure Event Hub for further processing.
            //  Convert the query string to a JSON message before sending it.
            //  The message will look like {"Timestamp":"2015-01-01 01:01:01", "Group":1, "Device": 2, "Temperature": 31.2, "LightLevel": 99}
            var query = new Dictionary<string, object>();
            query["Timestamp"] = timestamp;
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

            var eventHubClient = EventHubClient.CreateFromConnectionString(sensorHubSendRule, sensorHub);
            System.Diagnostics.Trace.WriteLine(string.Format("{0} > Sending message: {1}", DateTime.Now, message));
            eventHubClient.Send(new EventData(Encoding.UTF8.GetBytes(message)));
            eventHubClient.Close();
            Response.Write("'OK'");

            NewRelic.Api.Agent.NewRelic.RecordResponseTimeMetric("RecordSensorData", watch.ElapsedMilliseconds);
            Response.End();
        }
    }
}