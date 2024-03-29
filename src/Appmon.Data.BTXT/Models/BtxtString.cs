namespace Appmon.Data.BTXT.Models;

public sealed class BtxtString
{
    public uint Id { get; init; }

    public int LeadingNullPadding { get; set; } = 0;

    public string Value { get; set; } = string.Empty;
}