using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebRole1
{
    public partial class GetSensorData : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //  Return a Javascript row to be populated in Excel Web.  Called by Excel Web "Update Sensor Data" addin.
            Response.Expires = -1;
            //  Accept a list of field names, e.g. fields=Timestamp,Temperature,LightLevel
            var fields = "Timestamp,Temperature,LightLevel";  //  Default fields if none specified.
            if (Request["fields"] != null) fields = Request["fields"];
            //  Accept a group name for selecting sensors by group, e.g. Group=1
            var group = "0";  //  If not specified, group = 0.
            if (Request["Group"] != null) group = Request["Group"];

            //  Get the row values from the values sent earlier by the sensor.
            System.Collections.Specialized.NameValueCollection sensorData = null;
            if (RecordSensorData.sensorData.ContainsKey(group))
                sensorData = RecordSensorData.sensorData[group];
            RecordSensorData.sensorData[group] = null;
            if (sensorData == null)
            {
                //  If no values sent by the sensor, we return random values for testing.
                sensorData = new System.Collections.Specialized.NameValueCollection();
                sensorData["Temperature"] = (new Random().Next(200, 300) / 10.0).ToString();
                sensorData["LightLevel"] = new Random().Next(100, 200).ToString();
            }
            var timestamp = DateTime.Now.AddHours(8).ToString("s").Replace("T", " ");

            //  Return the row values as a Javascript array, e.g. ["2015-09-11 16:23:42", 28.5, 185]
            var newRow = new System.Text.StringBuilder();
            newRow.Append("newRow = [");
            bool firstField = true;
            //  Add each field indicated in the field list.
            foreach (var field in fields.Split(','))
            {
                if (!firstField) newRow.Append(", ");
                firstField = false;
                var value = sensorData[field];
                if (field == "Timestamp") value = timestamp;
                if (value == null)
                {
                    //  If value not specified by sensor, we provide the empty string.
                    value = "''";
                }
                else
                {
                    //  If value is not numeric, we surround with quotes.
                    float output;
                    if (!float.TryParse(value, out output))
                    {
                        value = "'" + value + "'";
                    }
                }
                newRow.AppendFormat("{0}", value);
            }
            newRow.AppendLine("];");
            Response.Write(newRow.ToString());
            Response.End();
        }
    }
}