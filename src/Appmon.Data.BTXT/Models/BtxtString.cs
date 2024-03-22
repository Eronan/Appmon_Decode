namespace Appmon.Data.BTXT.Models;

public sealed class BtxtString
{
    public uint Id { get; init; }

    public string Value { get; internal set; } = string.Empty;
}