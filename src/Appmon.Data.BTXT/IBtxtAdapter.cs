namespace Appmon.Data.BTXT;

using Appmon.Data.BTXT.Models;
using System.IO;

public interface IBtxtAdapter
{
    BtxtFile ReadBtxtFileFromStream(Stream stream);

    void WriteBtxtFileToStream(BtxtFile btxtFile, Stream stream);
}