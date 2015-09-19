using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.SocketBase;

namespace SuperSocket.QuickStart.RemoteProcessService
{
    public class RemoteProcessSession : AppSession<RemoteProcessSession>
    {
        public new RemoteProcessServer AppServer
        {
            get { return (RemoteProcessServer)base.AppServer; }
        }

        protected override void OnSessionStarted()
        {
            Send("Welcome to use this tool!");
            //  TODO: Add to list of active sessions. Remember Group and Device ID.
        }

        protected override void OnSessionClosed(CloseReason reason)
        {
            //  TODO: Remove from list of active sessions.
        }

        protected override void HandleException(Exception e)
        {
            Send("An error has occurred in server side! Error message: " + e.Message + "!");
        }
    }
}
