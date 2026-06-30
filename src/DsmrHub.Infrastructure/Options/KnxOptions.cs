namespace DsmrHub.Infrastructure.Options;

public sealed class KnxOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = default!;
    public int Port { get; set; }

    /// <summary>
    /// KNX individual address (e.g. "1.1.1"), kept as a string so that an empty/unset value
    /// (KNX disabled) does not fail options binding. Parsed lazily when connecting.
    /// </summary>
    public string IndividualAddress { get; set; } = default!;

    public Dictionary<string, string> GroupAddressMapping { get; set; } = default!;
}
