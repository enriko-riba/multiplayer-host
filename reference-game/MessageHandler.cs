using System;
using MultiplayerHost.Messages;
using MultiplayerHost.ReferenceGame.Messages;

namespace MultiplayerHost.ReferenceGame;

public class MessageHandler
{
    public static bool IsMessageValid(in ClientMessage msg)
    {
        var opcode = msg.Code switch
        {
            <= (int)ClientOpCode.InvalidLower => ClientOpCode.InvalidLower,
            >= (int)ClientOpCode.InvalidUpper => ClientOpCode.InvalidUpper,
            _ => Enum.IsDefined(typeof(ClientOpCode), msg.Code) ? (ClientOpCode)msg.Code : ClientOpCode.Unsupported
        };

        //  TODO: implement validation
        return true;
    }
}
