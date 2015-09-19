//  http://localhost:53385/ActuateDevice.aspx?group=1&device=2&action=led&parameter=on
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebRole1
{
    public partial class ActuateDevice : System.Web.UI.Page
    {
        static string eventHubName = "azureiothub";
        static string connectionString = "Endpoint=sb://azureiothub.servicebus.windows.net/;SharedAccessKeyName=SendRule;SharedAccessKey=+TNGe5Awzvd0QLFUWbEWxo7atZ1JXgCPkajeEIqVMu8=";

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Expires = -1;
            var group = Request["group"];
            var device = Request["device"];
            var action = Request["action"];
            var parameter = Request["parameter"];

            //  Convert the query string to a JSON message before sending it.
            //  The message will look like {"group":"1", "device":"2", "action":"led", "parameter":"on"}
            var query = new Dictionary<string, string>();
            foreach (var key in Request.QueryString.AllKeys)
            {
                query[key] = Request[key];
            }
            var message = JsonConvert.SerializeObject(query);

            var eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, eventHubName);
            System.Diagnostics.Trace.WriteLine(string.Format("{0} > Sending message: {1}", DateTime.Now, message));
            eventHubClient.Send(new EventData(Encoding.UTF8.GetBytes(message)));

            Response.Write("'OK'");
            Response.End();
        }
    }
}