namespace Appmon.Data.BTXT.Models;

public sealed class BtxtString
{
    public uint EndOffset { get; set; }

    public uint Id { get; init; }

    public uint StartOffset { get; set; }

    public string Value { get; set; } = string.Empty;
}