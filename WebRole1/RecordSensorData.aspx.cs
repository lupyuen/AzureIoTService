using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebRole1
{
    public partial class RecordSensorData : System.Web.UI.Page
    {
        public static Dictionary<string, System.Collections.Specialized.NameValueCollection> sensorData =
            new Dictionary<string, System.Collections.Specialized.NameValueCollection>();

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
            Response.Write("'OK'");

            NewRelic.Api.Agent.NewRelic.RecordResponseTimeMetric("RecordSensorData", watch.ElapsedMilliseconds);
            Response.End();
        }
    }
}