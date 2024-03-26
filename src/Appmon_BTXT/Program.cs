using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Appmon.Data.BTXT;
using Appmon.Data.BTXT.Models;

internal class Program
{
    private enum Operation
    {
        Read,
        Write,
        Invalid
    }

    private static void Main(string[] args)
    {
        if (args.Length == 0 || Array.Exists(args, arg => arg is "-h" or "--help"))
        {
            Console.WriteLine("Appmon_BTXT.exe [-o (R|W)] [-b (BTXT File Path)] [-x (XML File Path)]");
            Environment.Exit(0);
            return;
        }

        var operationInput = ReadArgument(args, "-o", "Would you like to read or write to a BTXT File (r/w): ", input =>
        {
            var normalized = input.ToUpper();
            return normalized is "R" or "W";
        });
        var operation = operationInput.ToUpper() switch
        {
            "R" => Operation.Read,
            "W" => Operation.Write,
            _ => Operation.Invalid
        };

        if (operation is Operation.Invalid)
        {
            Console.WriteLine("Invalid operation defined. Please run the progrma again with the correct operation (r/w).");
            Environment.Exit(1);
            return;
        }

        var btxtLocation = ReadArgument(args, "-b", "Please provide the location of the BTXT File: ", GetValidateFileFunc(".btxt", operation is Operation.Read));
        var xmlLocation = ReadArgument(args, "-x", "Please provide the location of the XML File: ", GetValidateFileFunc(".xml", operation is Operation.Write));

        var fileAdapter = IBtxtAdapterExtensions.GetDefaultBtxtAdapter();
        if (operation == Operation.Read)
        {
            Console.WriteLine($"Reading BTXT File from '{xmlLocation}'...");
            using var fileStream = new FileStream(btxtLocation, FileMode.Open);
            var btxtFile = fileAdapter.ReadBtxtFileFromStream(fileStream);

            Console.WriteLine($"Finished reading BTXT File, now writing file to '{xmlLocation}'...");
            var xmlSerializer = new XmlSerializer(typeof(BtxtFile));
            using var xmlWriter = new StringWriter();
            xmlSerializer.Serialize(xmlWriter, btxtFile);
            File.WriteAllText(xmlLocation, xmlWriter.ToString(), Encoding.Unicode);
        }
        else
        {
            Console.WriteLine($"Reading XML File from '{xmlLocation}'");
            var xmlSerializer = new XmlSerializer(typeof(BtxtFile));
            var xmlSerializerSettings = new XmlReaderSettings();
            using var xmlFileStream = new FileStream(xmlLocation, FileMode.Open);

            if (xmlSerializer.Deserialize(xmlFileStream) is not BtxtFile btxtFile)
            {
                Console.WriteLine("There was a problem loading the XML file. The XML File is not in the correct format.");
                Environment.Exit(1);
                return;
            }
            xmlFileStream.Close();

            using var btxtFileStream = new FileStream(btxtLocation, FileMode.Create);
            fileAdapter.WriteBtxtFileToStream(btxtFile, btxtFileStream);
            btxtFileStream.Close();
        }

        Environment.Exit(0);
    }

    private static string ReadArgument(string[] args, string argShortcut, string message, Func<string, bool> validateFunc)
    {
        string? input = null;
        int argumentIndex;
        if ((argumentIndex = Array.IndexOf(args, argShortcut)) != -1)
        {
            input = args[argumentIndex + 1];
        }

        while (string.IsNullOrEmpty(input) || !validateFunc(input))
        {
            Console.Write(message);
            input = Console.ReadLine();
        }

        return input;
    }

    private static Func<string, bool> GetValidateFileFunc(string fileExt, bool fileMustExist = false)
    {
        return (string input) =>
        {
            if (string.IsNullOrEmpty(input))
            {
                Console.WriteLine("File Location cannot be empty. Please try again.");
                return false;
            }
            else if (!input.EndsWith(fileExt))
            {
                Console.WriteLine($"Invalid file extension ({fileExt}). Please try again.");
                return false;
            }
            else if (fileMustExist && !File.Exists(input))
            {
                Console.WriteLine("Could not find the specified file. Please try again.");
            }

            return true;
        };
    }
}