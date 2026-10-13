using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SalesForgeOCR.Core.Entities;
using UglyToad.PdfPig;

namespace SalesForgeOCR.DocumentProcessing.Engines;

public static class EmbeddedImageReader
{
    private static readonly XNamespace R="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    public static List<ExtractedDocumentImage> Read(string path,CancellationToken ct=default)
    {
        var output=new List<ExtractedDocumentImage>();
        var assets=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!,"extracted-images");
        Directory.CreateDirectory(assets);
        string Save(byte[] bytes,string extension)
        {
            ct.ThrowIfCancellationRequested();
            var filename=Path.Combine(assets,Convert.ToHexString(SHA256.HashData(bytes))+extension);
            if(!File.Exists(filename)) File.WriteAllBytes(filename,bytes);
            return filename;
        }
        var extension=Path.GetExtension(path).ToLowerInvariant();
        if(extension==".pdf")
        {
            using var pdf=PdfDocument.Open(path);
            foreach(var page in pdf.GetPages()) foreach(var image in page.GetImages())
            {
                ct.ThrowIfCancellationRequested();
                byte[]? bytes=null;string ext=".png";
                if(image.TryGetPng(out var png)) bytes=png;
                else if(image.RawBytes.Length>2 && image.RawBytes[0]==0xff && image.RawBytes[1]==0xd8) {bytes=image.RawBytes.ToArray();ext=".jpg";}
                if(bytes==null) continue;
                var box=image.BoundingBox;double y=page.Height-box.Top;
                var role=y<page.Height*.2?"Header":y+box.Height>page.Height*.8?"Footer":"Body";
                output.Add(new(Save(bytes,ext),page.Number,role,box.Left,y,box.Width,box.Height));
            }
            return output;
        }
        if(extension is not (".docx" or ".xlsx")) return output;
        using var archive=ZipFile.OpenRead(path);
        var seen=new HashSet<string>(StringComparer.Ordinal);
        foreach(var entry in archive.Entries.Where(e=>e.FullName.EndsWith(".rels",StringComparison.OrdinalIgnoreCase)))
        {
            ct.ThrowIfCancellationRequested();
            var owner=entry.FullName.Replace("/_rels/","/")[..^5];
            if(archive.GetEntry(owner)==null) continue;
            var xml=OfficeDocuments.ReadXml(archive,owner);
            var rels=OfficeDocuments.ReadXml(archive,entry.FullName);
            foreach(var rel in rels.Root!.Elements().Where(e=>((string?)e.Attribute("Type"))?.EndsWith("/image")==true))
            {
                if((string?)rel.Attribute("TargetMode")=="External") continue;
                string id=(string?)rel.Attribute("Id")??"";
                string target=(string?)rel.Attribute("Target")??"";
                var uri=new Uri(new Uri("https://office.invalid/"+owner),target);
                var media=archive.GetEntry(Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')));
                if(media==null) continue;
                using var input=media.Open();using var memory=new MemoryStream();input.CopyTo(memory);
                var imagePath=Save(memory.ToArray(),Path.GetExtension(media.FullName));
                var refs=xml.Descendants().Where(e=>e.Attributes().Any(a=>(a.Name.Namespace==R || a.Name.LocalName=="relid")&&a.Value==id)).ToList();
                if(refs.Count==0) refs.Add(xml.Root!);
                foreach(var reference in refs)
                {
                    var role=owner.Contains("/header",StringComparison.OrdinalIgnoreCase)?"Header":owner.Contains("/footer",StringComparison.OrdinalIgnoreCase)?"Footer":"Body";
                    var anchor=reference.Ancestors().FirstOrDefault(a=>a.Name.LocalName is "inline" or "anchor" or "twoCellAnchor" or "oneCellAnchor" or "absoluteAnchor" or "shape");
                    var shapeId=(string?)anchor?.Attribute("id")??"";
                    if(owner.EndsWith(".vml",StringComparison.OrdinalIgnoreCase)) role=shapeId.EndsWith("H")?"Header":shapeId.EndsWith("F")?"Footer":role;
                    double width=120,height=60,x=36,y=role=="Footer"?760:24;
                    var extent=anchor?.Descendants().FirstOrDefault(e=>e.Name.LocalName is "extent" or "ext");
                    if(double.TryParse((string?)extent?.Attribute("cx"),out var cx))width=cx/12700;
                    if(double.TryParse((string?)extent?.Attribute("cy"),out var cy))height=cy/12700;
                    var style=(string?)anchor?.Attribute("style");
                    double Measure(string key,double fallback)
                    {
                        var match=Regex.Match(style??"",key+@":\s*([0-9.]+)(pt|px|in)");
                        if(!match.Success)return fallback;var value=double.Parse(match.Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture);
                        return value*(match.Groups[2].Value=="in"?72:match.Groups[2].Value=="px"?.75:1);
                    }
                    width=Measure("width",width);height=Measure("height",height);
                    if(shapeId.StartsWith("C"))x=(595-width)/2;
                    if(shapeId.StartsWith("R"))x=559-width;
                    int page=1;
                    var sheetMatch=Regex.Match(owner,@"(?:sheet|drawing)(\d+)");if(sheetMatch.Success)page=int.Parse(sheetMatch.Groups[1].Value);
                    output.Add(new(imagePath,page,role,x,y,width,height));seen.Add(media.FullName);
                }
            }
        }
        // Preserve orphaned/legacy media too, rather than losing header/footer graphics.
        foreach(var entry in archive.Entries.Where(e=>(e.FullName.StartsWith("word/media/")||e.FullName.StartsWith("xl/media/"))&&!seen.Contains(e.FullName)&&e.Length>0))
        {
            using var stream=entry.Open();using var memory=new MemoryStream();stream.CopyTo(memory);
            output.Add(new(Save(memory.ToArray(),Path.GetExtension(entry.Name)),1,"Body",36,80,120,60));
        }
        return output;
    }
}
