namespace Appmon.Data.BTXT.Models;

using System.Collections.Generic;

public sealed class BtxtFile
{
    public uint NumberOfLabels { get; init; }

    public uint NumberOfStrings { get; init; }

    public IEnumerable<BtxtLabel> Labels { get; init; } = Array.Empty<BtxtLabel>();
}
