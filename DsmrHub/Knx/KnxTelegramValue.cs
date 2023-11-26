using Knx.Falcon;

namespace DsmrHub.Knx
{
    internal class KnxTelegramValue
    {
        public KnxTelegramValue(GroupAddress address)
        {
            Address = address;
        }

        public GroupAddress Address { get; }
        public byte[]? Value { get; internal set; }
    }
}
