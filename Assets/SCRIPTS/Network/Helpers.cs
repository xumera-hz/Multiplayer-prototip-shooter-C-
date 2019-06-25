using LiteNetLib;
using LiteNetLib.Utils;

//TODO: разбить MiscHelper
public static class MiscHelper
{
    public const int MASK_REGISTRY_OR_CONNECTED = (int)(ClientState.Register | ClientState.Connected);
    public const int MASK_CONNECTION_OR_REGISTER = (int)(ClientState.Connection | ClientState.Register);
    public const int MASK_CONNECTION_OR_DISCONNECTED = (int)(ClientState.Connection | ClientState.Disconnected);


    public static int CountPackets(DeliveryMethod method) { return method == DeliveryMethod.Unreliable ? GameConstants.COUNT_PACKETS_UNRELIABLE : 1; }

    public static bool CheckIncludeInMask(ClientState value, int mask)
    {
        return value != ClientState.None &&  CheckIncludeInMask((int)value, mask);
    }

    public static bool CheckIncludeInMask(int value, int mask)
    {
        return value == mask || (mask & value) == value;
    }
}

public static class ReaderGameHelper
{
    public static ServerCommands GetCommand(NetDataReader reader)
    {
        return (ServerCommands)reader.GetByte();
    }
    public static void AddCommand(NetDataWriter writer, ServerCommands value)
    {
        writer.Put((byte)value);
    }


    public static TypeWorldUpdate GetWorldUpdate(NetDataReader reader)
    {
        return (TypeWorldUpdate)reader.GetByte();
    }
    public static void AddWorldUpdate(NetDataWriter writer, TypeWorldUpdate value)
    {
        writer.Put((byte)value);
    }


    public static ClientState GetClientState(NetDataReader reader)
    {
        return (ClientState)reader.GetByte();
    }
    public static void AddClientState(NetDataWriter writer, ClientState value)
    {
        writer.Put((byte)value);
    }
}
