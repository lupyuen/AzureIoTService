using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.SocketBase;
using NRConfig;

namespace SuperSocket.QuickStart.RemoteProcessService
{
    [Instrument]
    public class RemoteProcessSession : AppSession<RemoteProcessSession>
    {
        //  This class represents a long-last TCP socket session initiated by an IoT device e.g. LinkIt ONE.
        //  We keep this socket connection alive so we can send commands and control it.

        //  Maps groupID_deviceID to the session for the device.
        public static Dictionary<string, RemoteProcessSession> allSessions = new Dictionary<string, RemoteProcessSession>();
        //  Quick hack to get device ID: We increment the device ID every time a device connects.  By right each device should transmit its own deviceID.
        static int nextDeviceID = 1;

        //  Group and device IDs for the device connected to this session.
        string groupID;
        string deviceID;

        public new RemoteProcessServer AppServer
        {
            get { return (RemoteProcessServer)base.AppServer; }
        }

        protected override void OnSessionStarted()
        {
            groupID = "1";  //  Hardcode for now.
            deviceID = nextDeviceID++.ToString();  //  Generate a running unique device ID for now.
            //  Add to list of active sessions.
            var key = groupID + "_" + deviceID;
            allSessions[key] = this;
            Send(string.Format("Welcome to AzureIoTService! Your group ID is {0} and device ID is {1}",
                groupID, deviceID));
        }

        protected override void OnSessionClosed(CloseReason reason)
        {
            //  Remove from list of active sessions.
            var key = groupID + "_" + deviceID;
            if (allSessions.ContainsKey(key))
                allSessions.Remove(key);
        }

        protected override void HandleException(Exception e)
        {
            Send("An error has occurred in server side! Error message: " + e.Message + "!");
        }
    }
}
