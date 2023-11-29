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

        public override string ToString()
        {
            var value = Value is not null ? string.Join(",", Value.ToList()) : string.Empty;
            return $"{Address} - {value}";
        }
    }
}
