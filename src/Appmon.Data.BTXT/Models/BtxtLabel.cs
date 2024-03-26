namespace Appmon.Data.BTXT.Models;

public sealed class BtxtLabel
{
    public uint EndOffset { get; set; }

    public string Key { get; set; } = string.Empty;

    public uint StartOffset { get; set; }

    public List<BtxtString> Values { get; init; } = [];
};