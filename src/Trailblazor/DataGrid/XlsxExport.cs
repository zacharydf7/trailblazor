using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace Trailblazor;

// Writes rows as a minimal .xlsx workbook that Excel, LibreOffice and Google Sheets open: one sheet with a bold, frozen
// header row, numbers, dates and yes-or-no values kept as values and everything else as text. It uses only the zip
// and XML classes in .NET.
internal static class XlsxExport
{
    private const string Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const string PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string SheetType = "application/vnd.openxmlformats-officedocument.spreadsheetml";

    // Styles by their place in cellXfs: plain, the bold header, a date and a date with its time.
    private const int Header = 1;
    private const int Date = 2;
    private const int DateAndTime = 3;

    private const string ContentTypes = $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
        <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
        <Default Extension="xml" ContentType="application/xml"/>
        <Override PartName="/xl/workbook.xml" ContentType="{SheetType}.sheet.main+xml"/>
        <Override PartName="/xl/worksheets/sheet1.xml" ContentType="{SheetType}.worksheet+xml"/>
        <Override PartName="/xl/styles.xml" ContentType="{SheetType}.styles+xml"/>
        </Types>
        """;

    private const string PackageRels = $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="{PackageRelationships}">
        <Relationship Id="rId1" Type="{Relationships}/officeDocument" Target="xl/workbook.xml"/>
        </Relationships>
        """;

    private const string WorkbookRels = $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="{PackageRelationships}">
        <Relationship Id="rId1" Type="{Relationships}/worksheet" Target="worksheets/sheet1.xml"/>
        <Relationship Id="rId2" Type="{Relationships}/styles" Target="styles.xml"/>
        </Relationships>
        """;

    private const string Styles = $"""
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="{Main}">
        <numFmts count="1"><numFmt numFmtId="164" formatCode="yyyy-mm-dd hh:mm:ss"/></numFmts>
        <fonts count="2">
        <font><sz val="11"/><name val="Calibri"/></font>
        <font><b/><sz val="11"/><name val="Calibri"/></font>
        </fonts>
        <fills count="2">
        <fill><patternFill patternType="none"/></fill>
        <fill><patternFill patternType="gray125"/></fill>
        </fills>
        <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
        <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
        <cellXfs count="4">
        <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
        <xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/>
        <xf numFmtId="14" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>
        <xf numFmtId="164" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>
        </cellXfs>
        <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
        </styleSheet>
        """;

    private static readonly DateTime Epoch = new(1899, 12, 30);

    // Excel shows earlier dates as #### or, through its 1900 leap-year bug, a day off, so they're written as text.
    private static readonly DateTime FirstDate = new(1900, 3, 1);

    public static byte[] Write(string sheetName, IReadOnlyList<string> headings,
        IEnumerable<IEnumerable<ExportCell>> rows)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddText(zip, "[Content_Types].xml", ContentTypes);
            AddText(zip, "_rels/.rels", PackageRels);
            AddText(zip, "xl/_rels/workbook.xml.rels", WorkbookRels);
            AddText(zip, "xl/styles.xml", Styles);
            AddXml(zip, "xl/workbook.xml", xml =>
            {
                xml.WriteStartElement("workbook", Main);
                xml.WriteAttributeString("xmlns", "r", null, Relationships);
                xml.WriteStartElement("sheets", Main);
                xml.WriteStartElement("sheet", Main);
                xml.WriteAttributeString("name", SheetName(sheetName));
                xml.WriteAttributeString("sheetId", "1");
                xml.WriteAttributeString("id", Relationships, "rId1");
                xml.WriteEndElement();
                xml.WriteEndElement();
                xml.WriteEndElement();
            });
            AddXml(zip, "xl/worksheets/sheet1.xml", xml => WriteSheet(xml, headings, rows));
        }

        return stream.ToArray();
    }

    private static void WriteSheet(XmlWriter xml, IReadOnlyList<string> headings,
        IEnumerable<IEnumerable<ExportCell>> rows)
    {
        xml.WriteStartElement("worksheet", Main);
        xml.WriteStartElement("sheetViews", Main);
        xml.WriteStartElement("sheetView", Main);
        xml.WriteAttributeString("workbookViewId", "0");
        xml.WriteStartElement("pane", Main);
        xml.WriteAttributeString("ySplit", "1");
        xml.WriteAttributeString("topLeftCell", "A2");
        xml.WriteAttributeString("activePane", "bottomLeft");
        xml.WriteAttributeString("state", "frozen");
        xml.WriteEndElement();
        xml.WriteEndElement();
        xml.WriteEndElement();
        xml.WriteStartElement("sheetData", Main);
        WriteRow(xml, 1, headings.Select(heading => new ExportCell(heading, heading)), Header);
        var number = 2;
        foreach (var row in rows)
        {
            WriteRow(xml, number++, row, style: 0);
        }

        xml.WriteEndElement();
        xml.WriteEndElement();
    }

    private static void WriteRow(XmlWriter xml, int number, IEnumerable<ExportCell> cells, int style)
    {
        xml.WriteStartElement("row", Main);
        xml.WriteAttributeString("r", number.ToString(CultureInfo.InvariantCulture));
        var column = 0;
        foreach (var cell in cells)
        {
            var reference = ColumnName(column++) + number.ToString(CultureInfo.InvariantCulture);
            if (cell.Value is null)
            {
                continue;
            }

            xml.WriteStartElement("c", Main);
            xml.WriteAttributeString("r", reference);
            var (type, value, cellStyle) = Cell(cell);
            if (style != 0 || cellStyle != 0)
            {
                xml.WriteAttributeString("s", (style != 0 ? style : cellStyle).ToString(CultureInfo.InvariantCulture));
            }

            if (type is not null)
            {
                xml.WriteAttributeString("t", type);
            }

            if (type == "inlineStr")
            {
                xml.WriteStartElement("is", Main);
                xml.WriteStartElement("t", Main);
                xml.WriteAttributeString("xml", "space", null, "preserve");
                xml.WriteString(value);
                xml.WriteEndElement();
                xml.WriteEndElement();
            }
            else
            {
                xml.WriteElementString("v", Main, value);
            }

            xml.WriteEndElement();
        }

        xml.WriteEndElement();
    }

    // A cell's type, its text in the file and its style. A number, date or yes-or-no is a value Excel can sum and sort;
    // anything else, including an infinite or NaN number and a date Excel can't hold, is the text the grid shows, with
    // characters XML can't hold left out.
    private static (string? Type, string Value, int Style) Cell(ExportCell cell) => cell.Value switch
    {
        bool yes => ("b", yes ? "1" : "0", 0),
        double or float when !double.IsFinite(Convert.ToDouble(cell.Value, CultureInfo.InvariantCulture)) =>
            ("inlineStr", Clean(cell.Text), 0),
        _ when cell.IsNumber => (null, Convert.ToString(cell.Value, CultureInfo.InvariantCulture)!, 0),
        DateOnly date when date.ToDateTime(TimeOnly.MinValue) >= FirstDate =>
            (null, Serial(date.ToDateTime(TimeOnly.MinValue)), Date),
        DateTime time when time >= FirstDate =>
            (null, Serial(time), time.TimeOfDay == TimeSpan.Zero ? Date : DateAndTime),
        DateTimeOffset offset when offset.DateTime >= FirstDate => (null, Serial(offset.DateTime), DateAndTime),
        _ => ("inlineStr", Clean(cell.Text), 0),
    };

    private static string Serial(DateTime time) =>
        (time - Epoch).TotalDays.ToString("R", CultureInfo.InvariantCulture);

    // Characters past the Basic Multilingual Plane, such as emoji, are kept as their surrogate pairs.
    private static string Clean(string? text)
    {
        var kept = new StringBuilder();
        foreach (var rune in (text ?? "").EnumerateRunes())
        {
            if (!rune.IsBmp || XmlConvert.IsXmlChar((char)rune.Value))
            {
                kept.Append(rune.ToString());
            }
        }

        return kept.ToString();
    }

    // Columns are lettered A to Z, then AA, AB and so on.
    private static string ColumnName(int index)
    {
        var name = "";
        for (index++; index > 0; index = (index - 1) / 26)
        {
            name = (char)('A' + (index - 1) % 26) + name;
        }

        return name;
    }

    // A sheet's name is at most 31 characters, without any of []:*?/\ and not starting or ending with an apostrophe.
    private static string SheetName(string name)
    {
        var kept = new string([.. Clean(name).Where(letter => !"[]:*?/\\".Contains(letter))]);
        var length = Math.Min(kept.Length, 31);
        if (length < kept.Length && char.IsHighSurrogate(kept[length - 1]))
        {
            length--;
        }

        kept = kept[..length].Trim('\'', ' ');
        // Excel reserves the name History for a sheet of its own.
        return kept.Length == 0 || kept.Equals("History", StringComparison.OrdinalIgnoreCase) ? "Sheet1" : kept;
    }

    private static void AddText(ZipArchive zip, string path, string text)
    {
        using var writer = new StreamWriter(zip.CreateEntry(path).Open(), new UTF8Encoding(false));
        writer.Write(text);
    }

    private static void AddXml(ZipArchive zip, string path, Action<XmlWriter> write)
    {
        using var xml = XmlWriter.Create(zip.CreateEntry(path).Open(),
            new XmlWriterSettings { Encoding = new UTF8Encoding(false), CloseOutput = true });
        xml.WriteStartDocument(standalone: true);
        write(xml);
    }
}
