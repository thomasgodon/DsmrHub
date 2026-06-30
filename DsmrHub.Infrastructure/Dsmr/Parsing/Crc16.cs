namespace DsmrHub.Infrastructure.Dsmr.Parsing;

/// <summary>
/// CRC16/ARC (a.k.a. CRC-16/IBM): reflected polynomial 0xA001, initial value 0x0000.
/// This is the checksum DSMR / Fluvius e-MUCS appends after the telegram terminator '!',
/// computed over every byte from the leading '/' up to and including the '!'.
/// </summary>
internal static class Crc16
{
    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        ushort crc = 0;

        foreach (var b in data)
        {
            crc ^= b;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0
                    ? (ushort)((crc >> 1) ^ 0xA001)
                    : (ushort)(crc >> 1);
            }
        }

        return crc;
    }
}
