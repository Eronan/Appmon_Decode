namespace Appmon.Data.BTXT;

using Appmon.Data.BTXT.Models;
using System.Reflection.Emit;
using System.Text;

internal sealed class BtxtAdapter() : IBtxtAdapter
{
    private static readonly byte[] btxtHeader = [0x0, 0x0, 0x0, 0x0, 0x24, 0x10, 0x12, 0xFF];

    public BtxtFile ReadBtxtFileFromStream(Stream stream)
    {
        using var reader = new BinaryReader(stream);
        var header = reader.ReadBytes(8);
        if (!header.SequenceEqual(btxtHeader))
        {
            throw new FileLoadException("The loaded file is not a Btxt File.");
        }

        // Reading number of label and string metadata
        var labelCount = reader.ReadUInt16();
        var stringCount = reader.ReadUInt16();
        var labels = new List<BtxtLabel>();

        // Read label metadata
        var stringsPerLabel = new Dictionary<BtxtLabel, uint>();
        for (var i = 0; i < labelCount; i++)
        {
            var stringsInLabel = reader.ReadUInt32();
            var values = new List<BtxtString>();
            for (var j = 0; j < stringsInLabel; j++)
            {
                var id = reader.ReadUInt32();
                values.Add(new()
                {
                    Id = id
                });
            }

            var newLabel = new BtxtLabel
            {
                Values = values
            };
            labels.Add(newLabel);
            stringsPerLabel.Add(newLabel, stringsInLabel);
        }

        // Get length of label Keys
        var labelLengths = new Dictionary<BtxtLabel, int>();
        var startOffset = reader.ReadUInt32();
        foreach (var label in labels)
        {
            var endOffset = reader.ReadUInt32();
            label.StartOffset = startOffset;
            label.EndOffset = endOffset;
            var length = (int)endOffset - (int)startOffset;
            labelLengths.Add(label, length);
            startOffset = endOffset;
        }

        // Get length of each string value
        var valueLengths = new Dictionary<BtxtString, int>();
        var valuesProcessed = 0;
        foreach (var value in labels.SelectMany(label => label.Values))
        {
            valuesProcessed++;
            var endOffset = valuesProcessed != stringCount ? reader.ReadUInt32() : (uint) reader.BaseStream.Length;
            var length = (int)endOffset - (int)startOffset;
            valueLengths.Add(value, length);
            startOffset = endOffset;
        }

        // Populate label keys
        foreach (var label in labels)
        {
            label.Key = Encoding.ASCII.GetString(reader.ReadBytes(labelLengths[label])).Trim('\0');
        }

        // Populate values for all labels
        foreach (var value in labels.SelectMany(x => x.Values))
        {
            var bytes = reader.ReadBytes(valueLengths[value]);
            var stringValue = Encoding.Unicode.GetString(bytes);
            value.Value = SerializeEscapeCharacters(stringValue.Trim('\0'));
        }

        reader.Close();

        return new BtxtFile
        {
            NumberOfLabels = labelCount,
            NumberOfStrings = stringCount,
            Labels = labels
        };
    }

    public void WriteBtxtFileToStream(BtxtFile btxtFile, Stream stream)
    {
        using var writer = new BinaryWriter(stream);

        // Write header
        writer.Write(btxtHeader);

        // Write label and string counts
        writer.Write((ushort) btxtFile.NumberOfLabels);
        writer.Write((ushort) btxtFile.NumberOfStrings);

        // Write labels
        foreach (var label in btxtFile.Labels)
        {
            // Write number of strings in label
            writer.Write((uint) label.Values.Count);

            // Write string IDs
            foreach (var value in label.Values)
            {
                writer.Write(value.Id);
            }
        }

        // Write label offsets
        var currentOffset = (uint) 0;
        foreach (var label in btxtFile.Labels)
        {
            writer.Write(currentOffset);
            currentOffset = label.EndOffset;
        }

        // Write string offsets
        foreach (var value in btxtFile.Labels.SelectMany(label => label.Values))
        {
            // Account for \0 value between Appmon name and "mon" section.
            value.Value = DeserializeEscapeCharacters(value.Value);
            var paddingSize = CalculatePadding(currentOffset, value.Value.Length, 2) * 2;
            writer.Write(currentOffset);
            currentOffset += (uint) ((value.Value.Length * 2) + paddingSize); // UTF-16 encoding
        }

        // Write label keys
        foreach (var label in btxtFile.Labels)
        {
            var paddingSize = CalculatePadding(writer.BaseStream.Position, label.Key.Length, 3);
            writer.Write(Encoding.ASCII.GetBytes(label.Key));
            writer.Write(new byte[paddingSize]);
        }

        // Write string values
        foreach (var value in btxtFile.Labels.SelectMany(label => label.Values))
        {
            var paddingSize = CalculatePadding(writer.BaseStream.Position, value.Value.Length, 2) * 2;
            writer.Write(Encoding.Unicode.GetBytes(value.Value));
            writer.Write(new byte[paddingSize]);
        }

        static int CalculatePadding(long currentEndOffset, int length, int nullCharacterLength)
        {
            var nextEndOffset = currentEndOffset + length + nullCharacterLength;
            return (int) (nextEndOffset % 2) + nullCharacterLength;
        }
    }

    private static string SerializeEscapeCharacters(string readString)
    {
        return readString.Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }
    private static string DeserializeEscapeCharacters(string readString)
    {
        return readString.Replace("\\r", "\r")
            .Replace("\\n", "\n");
    }
}
