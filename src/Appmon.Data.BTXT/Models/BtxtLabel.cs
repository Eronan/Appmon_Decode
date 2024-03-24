namespace Appmon.Data.BTXT.Models;

public sealed class BtxtLabel
{
    public string Key { get; set; } = string.Empty;

    public List<BtxtString> Values { get; init; } = [];
};