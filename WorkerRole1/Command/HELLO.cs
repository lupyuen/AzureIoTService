using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.SocketBase.Command;
using SuperSocket.SocketBase.Protocol;
using NRConfig;

namespace SuperSocket.QuickStart.RemoteProcessService.Command
{
    [Instrument]
    public class HELLO : StringCommandBase<RemoteProcessSession>
    {
        //  Command is "HELLO groupID deviceID".  Used by client device to specify the group and device IDs upon conenction.
        #region CommandBase<RemotePrcessSession> Members

        public override void ExecuteCommand(RemoteProcessSession session, StringRequestInfo requestInfo)
        {
            var server = session.AppServer;
            if (requestInfo.Parameters.Length != 2)
            {
                session.Send("Invalid syntax. Try \"HELLO groupID deviceID\"\r\n");
                return;
            }
            //  Update the session list with the new key.
            //  Remove the session from the old key.
            var oldKey = session.getKey();
            if (RemoteProcessSession.allSessions.ContainsKey(oldKey))
                RemoteProcessSession.allSessions.Remove(oldKey);
            session.groupID = requestInfo.Parameters[0];
            session.deviceID = requestInfo.Parameters[1];
            RemoteProcessSession.allSessions[session.getKey()] = session;
            session.Send(string.Format("Your group ID and device ID have been updated. Your group ID is {0} and device ID is {1}.",
                session.groupID, session.deviceID));
        }

        #endregion
    }
}
