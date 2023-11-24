using Knx.Falcon;

namespace DsmrHub.Knx
{
    internal class KnxTelegramValue
    {
        public KnxTelegramValue(GroupAddress address, string dataPointType)
        {
            Address = address;
            DataPointType = dataPointType;
        }

        public GroupAddress Address { get; }
        public string DataPointType { get; }
        public byte[]? Value { get; internal set; }
    }
}
