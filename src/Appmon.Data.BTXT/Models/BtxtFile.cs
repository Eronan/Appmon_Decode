namespace Appmon.Data.BTXT.Models;

using System.Collections.Generic;

[Serializable]
public sealed class BtxtFile
{
    public uint NumberOfLabels { get; init; }

    public uint NumberOfStrings { get; init; }

    public List<BtxtLabel> Labels { get; init; } = [];
}
