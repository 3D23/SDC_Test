using Mirror;

namespace SDC_Test.NM.InternalMessages
{
    public struct NetworkMessageWrapper : NetworkMessage
    {
        public ushort MessageTypeId;
        public byte[] Payload;

        public NetworkMessageWrapper(ushort messageTypeId, byte[] payload)
        {
            MessageTypeId = messageTypeId;
            Payload = payload;
        }
    }

    public struct ClientSubscribeMessage : NetworkMessage
    {
        public ushort MessageTypeId;

        public ClientSubscribeMessage(ushort messageTypeId)
        {
            MessageTypeId = messageTypeId;
        }
    }

    public struct ClientUnsubscribeMessage : NetworkMessage
    {
        public ushort MessageTypeId;

        public ClientUnsubscribeMessage(ushort messageTypeId)
        {
            MessageTypeId = messageTypeId;
        }
    }
}