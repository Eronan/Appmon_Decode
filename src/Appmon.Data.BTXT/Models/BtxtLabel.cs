namespace Appmon.Data.BTXT.Models;

public sealed class BtxtLabel
{
    public string Key { get; internal set; } = string.Empty;

    public IEnumerable<BtxtString> Values { get; init; } = new List<BtxtString>();
};