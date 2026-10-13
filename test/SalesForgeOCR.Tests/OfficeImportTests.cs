using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using System.Xml.Linq;
using ClosedXML.Excel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Core.Interfaces;
using SalesForgeOCR.Application.Services;
using SalesForgeOCR.Application.Background;
using SalesForgeOCR.Data;
using SalesForgeOCR.DocumentProcessing.Engines;
using UglyToad.PdfPig;

namespace SalesForgeOCR.Tests;
[TestClass,DoNotParallelize]
public class OfficeImportTests
{
    private string _directory=null!;
    [TestInitialize] public void Setup(){_directory=Path.Combine(Path.GetTempPath(),"sf-office-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(_directory);}
    [TestCleanup] public void Cleanup(){Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();Directory.Delete(_directory,true);}
    private byte[] Logo()
    {
        using var bitmap=new System.Drawing.Bitmap(120,50);
        using var graphics=System.Drawing.Graphics.FromImage(bitmap);graphics.Clear(System.Drawing.Color.Navy);
        using var font=new System.Drawing.Font("Arial",14);graphics.DrawString("TEST LOGO",font,System.Drawing.Brushes.White,4,10);
        using var memory=new MemoryStream();bitmap.Save(memory,System.Drawing.Imaging.ImageFormat.Png);return memory.ToArray();
    }
    private static void Part(ZipArchive archive,string name,string text)
    {if(archive.Mode!=ZipArchiveMode.Create)archive.GetEntry(name)?.Delete();using var writer=new StreamWriter(archive.CreateEntry(name).Open());writer.Write(text);}
    private static void Binary(ZipArchive archive,string name,byte[] bytes)
    {using var stream=archive.CreateEntry(name).Open();stream.Write(bytes);}
    private string Word()
    {
        var path=Path.Combine(_directory,"sample.docx");using var archive=ZipFile.Open(path,ZipArchiveMode.Create);
        const string start="<w:document xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main'><w:body>";
        Part(archive,"word/document.xml",start+"<w:p><w:r><w:t>Customer: {{CustomerName}}</w:t></w:r></w:p><w:tbl><w:tr><w:tc><w:p><w:r><w:t>Chassis</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>{{ChassisNumber}}</w:t></w:r></w:p></w:tc></w:tr></w:tbl></w:body></w:document>");
        foreach(var role in new[]{"header","footer"})
        {
            Part(archive,$"word/{role}1.xml",$"<w:{(role=="header"?"hdr":"ftr")} xmlns:w='http://schemas.openxmlformats.org/wordprocessingml/2006/main' xmlns:a='http://schemas.openxmlformats.org/drawingml/2006/main' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships' xmlns:wp='http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing'><w:p><w:r><w:t>{role} text</w:t><w:drawing><wp:inline><wp:extent cx='1524000' cy='635000'/><a:graphic><a:blip r:embed='logo'/></a:graphic></wp:inline></w:drawing></w:r></w:p></w:{(role=="header"?"hdr":"ftr")}>" );
            Part(archive,$"word/_rels/{role}1.xml.rels",$"<Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='logo' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/image' Target='media/{role}.png'/></Relationships>");
            Binary(archive,$"word/media/{role}.png",Logo());
        }
        return path;
    }
    private string Excel()
    {
        var path=Path.Combine(_directory,"sample.xlsx");
        using(var workbook=new XLWorkbook())
        {
            var sheet=workbook.AddWorksheet("Customer form");sheet.Cell("A4").Value="Customer: {{CustomerName}}";sheet.Range("A4:C4").Merge();
            sheet.Cell("A5").Value="Chassis";sheet.Cell("B5").Value="{{ChassisNumber}}";
            sheet.Cell("A4").Style.Font.Bold=true;sheet.Range("A4:C5").Style.Border.OutsideBorder=XLBorderStyleValues.Thin;
            sheet.PageSetup.PrintAreas.Add("A1:D12");
            sheet.PageSetup.Header.Left.AddText("header text");sheet.PageSetup.Footer.Right.AddText("footer text");
            using var logo=new MemoryStream(Logo());sheet.AddPicture(logo).MoveTo(sheet.Cell("B2"));workbook.SaveAs(path);
        }
        using var archive=ZipFile.Open(path,ZipArchiveMode.Update);
        XDocument types;using(var stream=archive.GetEntry("[Content_Types].xml")!.Open())types=XDocument.Load(stream);
        types.Root!.Add(new XElement(types.Root.Name.Namespace+"Default",new XAttribute("Extension","vml"),new XAttribute("ContentType","application/vnd.openxmlformats-officedocument.vmlDrawing")));
        Part(archive,"[Content_Types].xml",types.ToString());
        const string ns="http://schemas.openxmlformats.org/package/2006/relationships";
        var relEntry=archive.GetEntry("xl/worksheets/_rels/sheet1.xml.rels")!;
        XDocument rels;using(var stream=relEntry.Open())rels=XDocument.Load(stream);
        rels.Root!.Add(new XElement(XName.Get("Relationship",ns),new XAttribute("Id","headerDrawing"),new XAttribute("Type","http://schemas.openxmlformats.org/officeDocument/2006/relationships/vmlDrawing"),new XAttribute("Target","../drawings/header.vml")));
        Part(archive,relEntry.FullName,rels.ToString());
        Part(archive,"xl/drawings/header.vml","<xml xmlns:v='urn:schemas-microsoft-com:vml' xmlns:o='urn:schemas-microsoft-com:office:office'><v:shape id='LH' style='width:120pt;height:50pt'><v:imagedata o:relid='headerLogo'/></v:shape><v:shape id='RF' style='width:120pt;height:50pt'><v:imagedata o:relid='footerLogo'/></v:shape></xml>");
        Part(archive,"xl/drawings/_rels/header.vml.rels",$"<Relationships xmlns='{ns}'><Relationship Id='headerLogo' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/image' Target='../media/header.png'/><Relationship Id='footerLogo' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/image' Target='../media/footer.png'/></Relationships>");
        Binary(archive,"xl/media/header.png",Logo());Binary(archive,"xl/media/footer.png",Logo());return path;
    }
    [TestMethod] public async Task WordBodyTableHeaderFooterAndImagesAreRead()
    {
        var path=Word();var text=await new OcrEngine().ExtractTextAsync(path);
        StringAssert.Contains(text,"Customer: {{CustomerName}}");StringAssert.Contains(text,"header text");StringAssert.Contains(text,"footer text");
        var images=(await new ImageExtractor().ExtractImageDetailsAsync(path)).ToList();
        Assert.AreEqual(2,images.Count);Assert.IsTrue(images.Any(i=>i.Role=="Header"));Assert.IsTrue(images.Any(i=>i.Role=="Footer"));
        Assert.AreEqual(1,(await new TableExtractor().ExtractTablesAsync(path)).Count());
    }
    [TestMethod] public async Task ExcelCellsAndLegacyHeaderFooterImagesAreRead()
    {
        var path=Excel();var text=await new OcrEngine().ExtractTextAsync(path);
        StringAssert.Contains(text,"Customer: {{CustomerName}}");StringAssert.Contains(text,"header text");StringAssert.Contains(text,"footer text");
        var images=(await new ImageExtractor().ExtractImageDetailsAsync(path)).ToList();
        Assert.IsTrue(images.Any(i=>i.Role=="Header"));Assert.IsTrue(images.Any(i=>i.Role=="Footer"));Assert.IsTrue(images.Any(i=>i.Role=="Body"));
        Assert.AreEqual(1,(await new TableExtractor().ExtractTablesAsync(path)).Count());
    }
    [TestMethod] public async Task GeneratedWordAndExcelCodeCompilesAndCreatesPdfWithLiveFieldsAndLogos()
    {
        foreach(var path in new[]{Word(),Excel()})
        {
            var code=await new DocumentCodeGenerator().GenerateAsync(path,"GenerateVerifiedTemplate",reconstructOfficeLayout:true);
            StringAssert.Contains(code,"FormatName()");StringAssert.Contains(code,"FromBase64String");
            Assert.IsFalse(code.Contains("XLWorkbook"));Assert.IsFalse(code.Contains("Interop.Excel"));
            var renderer=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"CodeTemplates","PdfFormDocument.cs.txt"));
            var profile=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"CodeTemplates","PdfLayoutProfile.cs.txt"));
            var scaffold="namespace SalesForge { public partial class SalesForge { private string FormatName()=>\"Live Customer\"; private void MarkFileSaved(){} private string GetFileName(string title)=>title+\".pdf\"; private Field chassisNrTxt=new(){Text=\"LIVE-VIN\"}; private sealed class Field { public string Text {get;set;}=\"\"; } "+code+" } }";
            var refs=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(p=>MetadataReference.CreateFromFile(p)).ToList();
            refs.Add(MetadataReference.CreateFromFile(typeof(PdfSharp.Pdf.PdfDocument).Assembly.Location));
            var compilation=CSharpCompilation.Create("Generated"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText("global using System;global using System.IO;global using System.Linq;global using System.Collections.Generic;"),CSharpSyntaxTree.ParseText(renderer),CSharpSyntaxTree.ParseText(profile),CSharpSyntaxTree.ParseText(scaffold)},refs,new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using var memory=new MemoryStream();var emitted=compilation.Emit(memory);
            Assert.IsTrue(emitted.Success,string.Join("\n",emitted.Diagnostics.Where(d=>d.Severity==DiagnosticSeverity.Error)));
            memory.Position=0;var assembly=AssemblyLoadContext.Default.LoadFromStream(memory);var type=assembly.GetType("SalesForge.SalesForge")!;
            type.GetMethod("GenerateVerifiedTemplate")!.Invoke(Activator.CreateInstance(type),new object[]{_directory});
            using var pdf=PdfDocument.Open(Path.Combine(_directory,"GenerateVerifiedTemplate.pdf"));
            StringAssert.Contains(string.Join(" ",pdf.GetPages().Select(p=>p.Text)),"Live Customer");
            Assert.IsTrue(pdf.GetPages().Sum(p=>p.GetImages().Count())>=2,"Header/footer images were lost in generated PDF.");
        }
    }
    [TestMethod] public async Task CodeGeneratorRejectsUnsafeMethodNames()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(()=>new DocumentCodeGenerator().GenerateAsync(Word(),"GenerateBad();"));
    }
    [TestMethod] public async Task PdfCodePreservesPageSizeGraphicsAndRenderedPixels()
    {
        var builder=new UglyToad.PdfPig.Writer.PdfDocumentBuilder();
        var font=builder.AddStandard14Font(UglyToad.PdfPig.Fonts.Standard14Fonts.Standard14Font.Helvetica);
        var page=builder.AddPage(782.84,1107.95);
        page.AddText("Approved header",14,new UglyToad.PdfPig.Core.PdfPoint(41,1040),font);
        page.AddText("Approved footer",9,new UglyToad.PdfPig.Core.PdfPoint(41,32),font);
        page.AddPng(Logo(),new UglyToad.PdfPig.Core.PdfRectangle(560,990,720,1060));
        var second=builder.AddPage(600.97,850.55);second.AddText("Second page artwork",12,new UglyToad.PdfPig.Core.PdfPoint(36,800),font);
        var source=Path.Combine(_directory,"approved.pdf");File.WriteAllBytes(source,builder.Build());
        var code=await new DocumentCodeGenerator().GenerateAsync(source,"GenerateExactTemplate");
        Assert.IsFalse(code.Contains("new RenderOptions"));Assert.IsFalse(code.Contains("new SalesForge.Pdf.PdfFormDocument"));
        var scaffold="namespace SalesForge { public class Verified { private void MarkFileSaved(){} private string GetFileName(string title)=>title+\".pdf\"; "+code+" } }";
        var refs=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(p=>MetadataReference.CreateFromFile(p)).ToList();
        refs.Add(MetadataReference.CreateFromFile(typeof(PdfSharp.Pdf.PdfDocument).Assembly.Location));
        var compilation=CSharpCompilation.Create("Exact"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(scaffold)},refs,new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var memory=new MemoryStream();var emitted=compilation.Emit(memory);
        Assert.IsTrue(emitted.Success,string.Join("\n",emitted.Diagnostics.Where(d=>d.Severity==DiagnosticSeverity.Error)));
        memory.Position=0;var type=AssemblyLoadContext.Default.LoadFromStream(memory).GetType("SalesForge.Verified")!;
        type.GetMethod("GenerateExactTemplate")!.Invoke(Activator.CreateInstance(type),new object[]{_directory});
        var result=Path.Combine(_directory,"GenerateExactTemplate.pdf");
        using var original=PdfDocument.Open(source);using var output=PdfDocument.Open(result);
        Assert.AreEqual(original.NumberOfPages,output.NumberOfPages);
        for(int i=1;i<=original.NumberOfPages;i++)
        {
            Assert.AreEqual(original.GetPage(i).Width,output.GetPage(i).Width);
            Assert.AreEqual(original.GetPage(i).Height,output.GetPage(i).Height);
            using var input=File.OpenRead(source);using var final=File.OpenRead(result);
            using var before=PDFtoImage.Conversion.ToImage(input,page:i-1,options:new PDFtoImage.RenderOptions(Dpi:96));
            using var after=PDFtoImage.Conversion.ToImage(final,page:i-1,options:new PDFtoImage.RenderOptions(Dpi:96));
            Assert.IsTrue(before.Bytes.SequenceEqual(after.Bytes),"Source artwork changed on page "+i);
        }
    }
    [TestMethod] public async Task ExactPdfPlaceholderCreatesLiveFieldWithoutChangingHeaderOrFooter()
    {
        var builder=new UglyToad.PdfPig.Writer.PdfDocumentBuilder();
        var font=builder.AddStandard14Font(UglyToad.PdfPig.Fonts.Standard14Fonts.Standard14Font.Helvetica);
        var page=builder.AddPage(600,850);
        page.AddText("Unchanged header",14,new UglyToad.PdfPig.Core.PdfPoint(40,800),font);
        page.AddText("{{CustomerName}}",12,new UglyToad.PdfPig.Core.PdfPoint(40,500),font);
        page.AddText("Unchanged footer",9,new UglyToad.PdfPig.Core.PdfPoint(40,35),font);
        var source=Path.Combine(_directory,"live-source.pdf");File.WriteAllBytes(source,builder.Build());
        var code=await new DocumentCodeGenerator().GenerateAsync(source,"GenerateLiveTemplate");
        StringAssert.Contains(code,"FormatName()");
        var scaffold="namespace SalesForge { public class Verified { private string FormatName()=>\"Live Customer\";private void MarkFileSaved(){} private string GetFileName(string title)=>title+\".pdf\"; "+code+" } }";
        var refs=((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(p=>MetadataReference.CreateFromFile(p)).ToList();
        refs.Add(MetadataReference.CreateFromFile(typeof(PdfSharp.Pdf.PdfDocument).Assembly.Location));
        var compilation=CSharpCompilation.Create("Live"+Guid.NewGuid().ToString("N"),new[]{CSharpSyntaxTree.ParseText(scaffold)},refs,new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var memory=new MemoryStream();var emitted=compilation.Emit(memory);
        Assert.IsTrue(emitted.Success,string.Join("\n",emitted.Diagnostics.Where(d=>d.Severity==DiagnosticSeverity.Error)));
        memory.Position=0;var type=AssemblyLoadContext.Default.LoadFromStream(memory).GetType("SalesForge.Verified")!;
        type.GetMethod("GenerateLiveTemplate")!.Invoke(Activator.CreateInstance(type),new object[]{_directory});
        var result=Path.Combine(_directory,"GenerateLiveTemplate.pdf");
        using var output=PdfDocument.Open(result);StringAssert.Contains(output.GetPage(1).Text,"Live Customer");
        using var originalStream=File.OpenRead(source);using var outputStream=File.OpenRead(result);
        using var before=PDFtoImage.Conversion.ToImage(originalStream,options:new PDFtoImage.RenderOptions(Dpi:72));
        using var after=PDFtoImage.Conversion.ToImage(outputStream,options:new PDFtoImage.RenderOptions(Dpi:72));
        for(int y=0;y<before.Height;y++)for(int x=0;x<before.Width;x++)
            if(y<300||y>380)Assert.AreEqual(before.GetPixel(x,y),after.GetPixel(x,y),"Artwork outside the live field moved.");
    }

}
