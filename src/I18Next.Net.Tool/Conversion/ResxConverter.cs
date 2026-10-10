using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace I18Next.Net.Tool.Conversion;

/// <summary>
///     Converts the string resources of .resx files from and to flat keys.
/// </summary>
internal static class ResxConverter
{
    private const string FormsVersion = "Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

    private static readonly XNamespace XmlNamespace = XNamespace.Xml;

    public static IEnumerable<KeyValuePair<string, string>> Read(string xml)
    {
        XDocument document;

        try
        {
            document = XDocument.Parse(xml);
        }
        catch (XmlException e)
        {
            throw new FormatException(e.Message, e);
        }

        var result = new List<KeyValuePair<string, string>>();

        foreach (var data in document.Root?.Elements("data") ?? [])
        {
            var name = (string)data.Attribute("name");

            if (name != null && data.Attribute("type") == null && data.Attribute("mimetype") == null)
                result.Add(new KeyValuePair<string, string>(name, (string)data.Element("value") ?? string.Empty));
        }

        return result;
    }

    public static string Write(IEnumerable<KeyValuePair<string, string>> entries)
    {
        var root = new XElement("root",
            CreateHeader("resmimetype", "text/microsoft-resx"),
            CreateHeader("version", "2.0"),
            CreateHeader("reader", "System.Resources.ResXResourceReader, System.Windows.Forms, " + FormsVersion),
            CreateHeader("writer", "System.Resources.ResXResourceWriter, System.Windows.Forms, " + FormsVersion));

        foreach (var entry in entries)
        {
            root.Add(new XElement("data",
                new XAttribute("name", entry.Key),
                new XAttribute(XmlNamespace + "space", "preserve"),
                new XElement("value", entry.Value)));
        }

        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\n"
        };

        using var stream = new MemoryStream();

        using (var writer = XmlWriter.Create(stream, settings))
            new XDocument(new XDeclaration("1.0", "utf-8", null), root).Save(writer);

        return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
    }

    private static XElement CreateHeader(string name, string value)
    {
        return new XElement("resheader", new XAttribute("name", name), new XElement("value", value));
    }
}
