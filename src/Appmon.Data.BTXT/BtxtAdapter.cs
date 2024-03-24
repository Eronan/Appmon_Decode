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
            var length = (int)endOffset - (int)startOffset;
            labelLengths.Add(label, length);
            startOffset = endOffset;
        }

        // Get length of each string value
        var valueLengths = new Dictionary<BtxtString, int>();
        int valuesProcessed = 0;
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
            value.Value = Encoding.Unicode.GetString(reader.ReadBytes(valueLengths[value])).Trim('\0');
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
        writer.Write((ushort)btxtFile.NumberOfLabels);
        writer.Write((ushort)btxtFile.NumberOfStrings);

        // Write label metadata
        foreach (var label in btxtFile.Labels)
        {
            writer.Write((uint)label.Values.Count());
            foreach (var value in label.Values)
            {
                writer.Write(value.Id);
            }
        }

        // Write label offsets
        foreach (var label in btxtFile.Labels)
        {
            var labelStartOffset = writer.BaseStream.Position;
            foreach (var value in label.Values)
            {
                writer.Write((uint)0); // Placeholder for start offset
                writer.Write((uint)0); // Placeholder for end offset
            }

            var labelEndOffset = writer.BaseStream.Position;

            // Update start offsets
            foreach (var value in label.Values)
            {
                writer.Seek((int) (labelStartOffset + (value.Id * 8)), SeekOrigin.Begin);
                writer.Write((uint) (labelStartOffset + (value.Id * 8) + 4)); // Update start offset
                writer.Seek(0, SeekOrigin.End);
            }

            // Write end offset
            writer.Seek((int)labelEndOffset, SeekOrigin.Begin);
        }

        // Write string offsets and values
        foreach (var label in btxtFile.Labels)
        {
            foreach (var value in label.Values)
            {
                var stringValueBytes = Encoding.Unicode.GetBytes(value.Value);
                writer.Write((uint)0); // Placeholder for start offset
                writer.Write((uint)stringValueBytes.Length); // Placeholder for end offset
                var stringValueStartOffset = writer.BaseStream.Position;
                writer.Write(stringValueBytes);
                var stringValueEndOffset = writer.BaseStream.Position;
                writer.Seek((int)(stringValueStartOffset - 8), SeekOrigin.Begin);
                writer.Write((uint)stringValueStartOffset); // Update start offset
                writer.Write((uint)stringValueEndOffset); // Update end offset
                writer.Seek(0, SeekOrigin.End);
            }
        }

        writer.Close();
    }
}
