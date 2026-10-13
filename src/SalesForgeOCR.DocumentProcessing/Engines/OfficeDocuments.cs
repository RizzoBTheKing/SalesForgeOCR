using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using ClosedXML.Excel;

namespace SalesForgeOCR.DocumentProcessing.Engines;

public static class OfficeDocuments
{
    internal static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    public static IEnumerable<string> ReadPages(string path, CancellationToken ct)
    {
        if (Path.GetExtension(path).Equals(".docx", StringComparison.OrdinalIgnoreCase))
        {
            using var archive = ZipFile.OpenRead(path);
            var body = ReadXml(archive,"word/document.xml").Root?.Element(W+"body") ?? throw new InvalidDataException("Word document has no body.");
            var headers = archive.Entries.Where(e=>e.FullName.StartsWith("word/header")&&e.FullName.EndsWith(".xml")).Select(e=>ParagraphText(ReadXml(archive,e.FullName))).ToList();
            var footers = archive.Entries.Where(e=>e.FullName.StartsWith("word/footer")&&e.FullName.EndsWith(".xml")).Select(e=>ParagraphText(ReadXml(archive,e.FullName))).ToList();
            var pages = new List<string>(); var text = new StringBuilder(string.Join("\n",headers));
            foreach(var element in body.Elements())
            {
                ct.ThrowIfCancellationRequested();
                text.AppendLine(ParagraphText(element));
                if (element.Descendants(W+"br").Any(b=>(string?)b.Attribute(W+"type")=="page"))
                { text.AppendLine(string.Join("\n",footers));pages.Add(text.ToString());text.Clear().AppendLine(string.Join("\n",headers)); }
            }
            text.AppendLine(string.Join("\n",footers));
            if (text.Length>0) pages.Add(text.ToString());
            return pages;
        }
        using var workbook = new XLWorkbook(path);
        var result = new List<string>();
        foreach(var sheet in workbook.Worksheets)
        {
            ct.ThrowIfCancellationRequested();var text = new StringBuilder(sheet.Name+"\n");
            foreach(var row in sheet.RangeUsed()?.RowsUsed() ?? Enumerable.Empty<IXLRangeRow>())
                text.AppendLine(string.Join("\t",row.Cells().Select(c=>c.GetFormattedString())));
            // Include actual text from print headers/footers, not just the cell grid.
            foreach (var occurrence in new[]{XLHFOccurrence.OddPages,XLHFOccurrence.EvenPages,XLHFOccurrence.FirstPage})
            {
                text.AppendLine(sheet.PageSetup.Header.Left.GetText(occurrence));
                text.AppendLine(sheet.PageSetup.Header.Center.GetText(occurrence));
                text.AppendLine(sheet.PageSetup.Header.Right.GetText(occurrence));
                text.AppendLine(sheet.PageSetup.Footer.Left.GetText(occurrence));
                text.AppendLine(sheet.PageSetup.Footer.Center.GetText(occurrence));
                text.AppendLine(sheet.PageSetup.Footer.Right.GetText(occurrence));
            }
            result.Add(text.ToString());
        }
        return result;
    }
    internal static XDocument ReadXml(ZipArchive archive,string name)
    {
        var entry=archive.GetEntry(name) ?? throw new InvalidDataException("Missing Office part: "+name);
        using var stream=entry.Open();
        using var reader=System.Xml.XmlReader.Create(stream,new System.Xml.XmlReaderSettings { DtdProcessing=System.Xml.DtdProcessing.Prohibit,XmlResolver=null });
        return XDocument.Load(reader);
    }
    internal static string ParagraphText(XContainer root)
    {
        var paragraphs=root is XElement e && e.Name==W+"p" ? new[]{e} : root.Descendants(W+"p");
        return string.Join("\n",paragraphs.Select(p=>string.Concat(p.Descendants().Select(n=>n.Name==W+"t"?n.Value:n.Name==W+"tab"?"\t":n.Name==W+"br"?"\n":""))));
    }
}
