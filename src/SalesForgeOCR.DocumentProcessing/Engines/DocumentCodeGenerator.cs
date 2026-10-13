using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using SalesForgeOCR.Core.Entities;
using UglyToad.PdfPig;
using PDFtoImage;
using SkiaSharp;

namespace SalesForgeOCR.DocumentProcessing.Engines;

/// <summary>Produces editable C# form instructions, not an Excel-to-PDF conversion.</summary>
public sealed class DocumentCodeGenerator
{
    public async Task<string> GenerateAsync(string path,string methodName="GenerateImportedDocument",CancellationToken ct=default,bool reconstructOfficeLayout=false)
    {
        if (!Regex.IsMatch(methodName,@"^Generate[A-Za-z0-9_]+$")) throw new ArgumentException("Use a method name such as GenerateCustomerForm.");
        var ext=Path.GetExtension(path).ToLowerInvariant();
        if (!reconstructOfficeLayout && ext is ".xlsx" or ".docx" or ".doc")
            path=await OfficePdfRenderer.RenderAsync(path,ct);
        return await Task.Run(()=>Generate(path,methodName,ct),ct);
    }
    private static string Quote(string? text)=>JsonSerializer.Serialize(text ?? "");
    private static string Number(double value)=>value.ToString("0.####",CultureInfo.InvariantCulture);
    private static string TextExpression(string text)
    {
        // Only explicit template tokens are substituted. Inferring personal data from
        // a completed contract could silently hard-code the wrong customer's details.
        var known=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase) {
            ["CustomerName"]="FormatName()",["Branch"]="branchBox.Text",["SalesExecutive"]="salesExecBox.Text",
            ["ChassisNumber"]="chassisNrTxt.Text.ToUpperInvariant()",["Make"]="makeTxt.Text",["Model"]="modelTxt.Text",
            ["DeliveryDate"]="delDate.Value.ToString(\"dd/MM/yyyy\")",["SellingPrice"]="sellingPriceTxt.Text",
            ["CompanyName"]="companyTxt.Text",["CustomerID"]="idNoTxt.Text",["Email"]="emailTxt.Text",["MobileNumber"]="mobileNoTxt.Text",
            ["CustomerAddress"]="resAddresstxt.Text",["PostalAddress"]="posAddressTxt.Text",["BPID"]="bpidTxt.Text",
            ["StockNumber"]="stockNrTxt.Text",["RegistrationNumber"]="regNrTxt.Text",["EngineNumber"]="engineNrTxt.Text",
            ["Colour"]="colourTxt.Text",["ModelYear"]="modelYearTxt.Text",["Kilometres"]="kilometersTxt.Text",
            ["Total"]="totalTxt.Text",["Deposit"]="lessDepositTxt.Text",["AmountDue"]="nettDueTxt.Text",["Notes"]="notesTxt.Text",
            ["TradeIn1Make"]="makeT1Txt.Text",["TradeIn1Model"]="modelT1Txt.Text",["TradeIn1Registration"]="regNrT1Txt.Text",
            ["TradeIn2Make"]="makeT2Txt.Text",["TradeIn2Model"]="modelT2Txt.Text",["TradeIn2Registration"]="regNrT2Txt.Text"
        };
        var expressions=new List<string>();int start=0;
        foreach(Match token in Regex.Matches(text,@"\{\{([A-Za-z][A-Za-z0-9]*)\}\}"))
        {
            if(!known.TryGetValue(token.Groups[1].Value,out var expression))continue;
            if(token.Index>start)expressions.Add(Quote(text[start..token.Index]));
            expressions.Add(expression);start=token.Index+token.Length;
        }
        if(start<text.Length||expressions.Count==0)expressions.Add(Quote(text[start..]));
        return string.Join(" + ",expressions);
    }
    private static string Generate(string path,string methodName,CancellationToken ct)
    {
        if(!Regex.IsMatch(methodName,@"^Generate[A-Za-z0-9_]+$"))throw new ArgumentException("Use a method name such as GenerateCustomerForm.");
        if(!File.Exists(path))throw new FileNotFoundException("Stored source document is missing.",path);
        if(Path.GetExtension(path).Equals(".pdf",StringComparison.OrdinalIgnoreCase))return GenerateExactPdf(path,methodName);
        var code=new StringBuilder();
        code.AppendLine("// Paste this method inside the SalesForge partial form class.");
        code.AppendLine("// Requires PdfFormDocument.cs and the MIT-licensed PDFsharp-GDI package.");
        code.AppendLine("// RECONSTRUCTED OFFICE LAYOUT: verify against an approved PDF before production.");
        code.AppendLine("// Static text is preserved. Review all personal details before using this as a template.");
        code.AppendLine("// Replace values with SalesForge expressions, or use {{CustomerName}}, {{Branch}},");
        code.AppendLine("// {{SalesExecutive}}, {{ChassisNumber}}, {{Make}}, {{Model}}, {{DeliveryDate}}, {{SellingPrice}}.");
        code.AppendLine($"public void {methodName}(string selectedPath)");code.AppendLine("{");
        code.AppendLine("    System.IO.Directory.CreateDirectory(selectedPath);");
        code.AppendLine($"    var fileName = GetFileName({Quote(methodName)});");
        code.AppendLine("    if (System.String.IsNullOrWhiteSpace(fileName)) return;");
        code.AppendLine("    var pdfPath = System.IO.Path.Combine(selectedPath, fileName);");
        code.AppendLine("    using var document = new global::SalesForge.Pdf.PdfFormDocument();");
        int pageNumber=0;
        void Page(string name,int rows=60)
        {
            pageNumber++;string page="page"+pageNumber;
            code.AppendLine($"    var {page} = document.Worksheets.Add({Quote(name)});");
            code.AppendLine($"    {page}.PageSetup.PrintAreas.Add(\"A1:L{rows}\");");
            code.AppendLine($"    {page}.PageSetup.CenterHorizontally = true;");
            for(int c=1;c<=12;c++)code.AppendLine($"    {page}.Column({c}).Width = 7.1;");
            code.AppendLine($"    for (int row = 1; row <= {rows}; row++) {page}.Row(row).Height = 13;");
        }
        void Image(ExtractedDocumentImage image,string? page=null)
        {
            var target=page??"page"+pageNumber;
            using var original=System.Drawing.Image.FromFile(image.FilePath);
            using var buffer=new MemoryStream();original.Save(buffer,System.Drawing.Imaging.ImageFormat.Png);
            int row=image.Role=="Header"?1:image.Role=="Footer"?57:Math.Clamp((int)(image.Y/13)+1,1,55);
            int column=Math.Clamp((int)(image.X/40)+1,1,12);
            double maxWidth=image.Role is "Header" or "Footer"?150:400;
            double width=Math.Min(maxWidth,image.Width>0?image.Width:120),height=width*original.Height/original.Width;
            code.AppendLine($"    // {image.Role} image from the source document; embedded for portable code.");
            code.AppendLine($"    {target}.AddPicture(System.Convert.FromBase64String({Quote(Convert.ToBase64String(buffer.ToArray()))})).MoveTo({target}.Cell({row}, {column})).Scale({Number(width/.75/original.Width)});");
        }
        var extension=Path.GetExtension(path).ToLowerInvariant();
        var images=EmbeddedImageReader.Read(path,ct);
        if(extension==".xlsx")
        {
            using var workbook=new XLWorkbook(path);
            foreach(var sheet in workbook.Worksheets)
            {
                ct.ThrowIfCancellationRequested();pageNumber++;string page="page"+pageNumber;
                var area=sheet.PageSetup.PrintAreas.FirstOrDefault()??sheet.RangeUsed();
                if(area==null)continue;
                code.AppendLine($"    var {page} = document.Worksheets.Add({Quote(sheet.Name)});");
                code.AppendLine($"    {page}.PageSetup.PrintAreas.Add({Quote(area.RangeAddress.ToString())});");
                code.AppendLine($"    {page}.PageSetup.CenterHorizontally = true;");
                for(int c=area.FirstColumn().ColumnNumber();c<=area.LastColumn().ColumnNumber();c++)code.AppendLine($"    {page}.Column({c}).Width = {Number(sheet.Column(c).Width)};");
                for(int r=area.FirstRow().RowNumber();r<=area.LastRow().RowNumber();r++)code.AppendLine($"    {page}.Row({r}).Height = {Number(sheet.Row(r).Height)};");
                foreach(var merge in sheet.MergedRanges)code.AppendLine($"    {page}.Range({Quote(merge.RangeAddress.ToString())}).Merge();");
                foreach(var cell in area.Cells())
                {
                    ct.ThrowIfCancellationRequested();string address=cell.Address.ToString() ?? "A1";string target=$"{page}.Cell({Quote(address)})";
                    if(!cell.IsEmpty()) code.AppendLine($"    {target}.Value = {TextExpression(cell.GetFormattedString())};");
                    var style=cell.Style;
                    code.AppendLine($"    {target}.Style.Font.FontName = {Quote(style.Font.FontName)};");
                    code.AppendLine($"    {target}.Style.Font.FontSize = {Number(style.Font.FontSize)};");
                    if(style.Font.Bold)code.AppendLine($"    {target}.Style.Font.Bold = true;");
                    if(style.Font.Italic)code.AppendLine($"    {target}.Style.Font.Italic = true;");
                    if(style.Alignment.Horizontal is XLAlignmentHorizontalValues.Center or XLAlignmentHorizontalValues.Right)code.AppendLine($"    {target}.Style.Alignment.Horizontal = global::SalesForge.Pdf.PdfHorizontal.{style.Alignment.Horizontal};");
                    code.AppendLine($"    {target}.Style.Alignment.WrapText = true;");
                    foreach(var side in new[]{"Top","Bottom","Left","Right"})
                    {
                        var border=side switch {"Top"=>style.Border.TopBorder,"Bottom"=>style.Border.BottomBorder,"Left"=>style.Border.LeftBorder,_=>style.Border.RightBorder};
                        if(border!=XLBorderStyleValues.None)code.AppendLine($"    {target}.Style.Border.{side}Border = global::SalesForge.Pdf.PdfBorder.Thin;");
                    }
                }
                foreach(var image in images.Where(i=>i.PageNumber==pageNumber))Image(image,page);
                foreach(var (role,section) in new[]{("Header",sheet.PageSetup.Header),("Footer",sheet.PageSetup.Footer)})
                {
                    var text=string.Join(" ",new[]{section.Left.GetText(XLHFOccurrence.AllPages),section.Center.GetText(XLHFOccurrence.AllPages),section.Right.GetText(XLHFOccurrence.AllPages)});
                    if(!string.IsNullOrWhiteSpace(text))code.AppendLine($"    // {role} text: {text.Replace("\n"," ").Replace("\r","")}");
                }
            }
        }
        else if(extension==".docx")
        {
            using var archive=ZipFile.OpenRead(path);var w=OfficeDocuments.W;
            var body=OfficeDocuments.ReadXml(archive,"word/document.xml").Root!.Element(w+"body")!;
            var headers=archive.Entries.Where(e=>e.FullName.StartsWith("word/header")&&e.FullName.EndsWith(".xml")).Select(e=>OfficeDocuments.ParagraphText(OfficeDocuments.ReadXml(archive,e.FullName))).ToList();
            var footers=archive.Entries.Where(e=>e.FullName.StartsWith("word/footer")&&e.FullName.EndsWith(".xml")).Select(e=>OfficeDocuments.ParagraphText(OfficeDocuments.ReadXml(archive,e.FullName))).ToList();
            int row=6;
            void NewPage()
            {
                Page("Imported Word page "+(pageNumber+1));row=6;
                var page="page"+pageNumber;
                code.AppendLine($"    {page}.Range(\"A1:L3\").Merge(); {page}.Cell(\"A1\").Value = {TextExpression(string.Join("\n",headers))};");
                code.AppendLine($"    {page}.Range(\"A58:L60\").Merge(); {page}.Cell(\"A58\").Value = {TextExpression(string.Join("\n",footers))};");
                foreach(var image in images.Where(i=>i.Role is "Header" or "Footer"))Image(image,page);
            }
            NewPage();
            foreach(var element in body.Elements())
            {
                ct.ThrowIfCancellationRequested();
                var paragraphs=element.Name==w+"tbl"?element.Elements(w+"tr").Select(tr=>string.Join(" | ",tr.Elements(w+"tc").Select(OfficeDocuments.ParagraphText))).ToList():new(){OfficeDocuments.ParagraphText(element)};
                foreach(var paragraph in paragraphs.Where(p=>!string.IsNullOrWhiteSpace(p)))
                {
                    int height=Math.Max(2,(int)Math.Ceiling(paragraph.Length/85d)+1);
                    if(row+height>56)NewPage();height=Math.Min(height,50);
                    var page="page"+pageNumber;
                    code.AppendLine($"    {page}.Range(\"A{row}:L{row+height-1}\").Merge();");
                    code.AppendLine($"    {page}.Cell(\"A{row}\").Value = {TextExpression(paragraph)};");
                    code.AppendLine($"    {page}.Cell(\"A{row}\").Style.Alignment.WrapText = true;");
                    if(element.Descendants(w+"b").Any())code.AppendLine($"    {page}.Cell(\"A{row}\").Style.Font.Bold = true;");
                    row+=height;
                }
                if(element.Descendants(w+"br").Any(b=>(string?)b.Attribute(w+"type")=="page"))NewPage();
            }
            foreach(var image in images.Where(i=>i.Role=="Body"))Image(image,"page1");
        }
        else if(extension==".pdf")
        {
            // Render source pages to retain graphics, vectors, logos and footer artwork.
            // The emitted method embeds each page, so it has no source-file dependency.
            using var input=File.OpenRead(path);
            foreach(var bitmap in Conversion.ToImages(input,options:new RenderOptions(Dpi:150)))
            {
                using(bitmap)
                using(var bytes=bitmap.Encode(SKEncodedImageFormat.Png,100))
                {
                    Page("Imported PDF page "+(pageNumber+1));
                    code.AppendLine($"    page{pageNumber}.AddPicture(System.Convert.FromBase64String({Quote(Convert.ToBase64String(bytes.ToArray()))})).MoveTo(page{pageNumber}.Cell(\"A1\")).Scale({Number(575/.75/bitmap.Width)});");
                }
            }
            code.AppendLine("    // Raster-backed source artwork is preserved. Add editable fields using page.Cell(...).Value.");
        }
        else
        {
            Page("Imported document");
            if(extension is ".txt" or ".csv")
            {code.AppendLine($"    page1.Range(\"A1:L60\").Merge();page1.Cell(\"A1\").Value = {TextExpression(File.ReadAllText(path))};");}
            else Image(new(path,1,"Body",0,0,500,700));
        }
        if(pageNumber==0)throw new InvalidOperationException("No document content was found for code generation.");
        code.AppendLine("    document.SavePdf(pdfPath);");
        code.AppendLine("    MarkFileSaved();");code.AppendLine("}");
        return code.ToString();
    }
    private static string GenerateExactPdf(string path,string methodName)
    {
        var data=Convert.ToBase64String(File.ReadAllBytes(path));
        var code=new StringBuilder();
        code.AppendLine("// Paste inside the SalesForge form class. Requires PDFsharp-GDI (MIT).");
        code.AppendLine("// Source artwork and page geometry are embedded. Office is never used at runtime.");
        code.AppendLine("// Only explicit supported {{FieldName}} tokens become live fields. Review static personal data.");
        code.AppendLine($"public void {methodName}(string selectedPath)");code.AppendLine("{");
        code.AppendLine("    System.IO.Directory.CreateDirectory(selectedPath);");
        code.AppendLine($"    var fileName = GetFileName({Quote(methodName)});");
        code.AppendLine("    if (System.String.IsNullOrWhiteSpace(fileName)) return;");
        code.AppendLine("    var pdfPath = System.IO.Path.Combine(selectedPath, fileName);");
        code.AppendLine($"    using var source = new System.IO.MemoryStream(System.Convert.FromBase64String({Quote(data)}));");
        code.AppendLine("    using var document = PdfSharp.Pdf.IO.PdfReader.Open(source, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Modify);");
        using var pdf=PdfDocument.Open(path);
        foreach(var page in pdf.GetPages())
        {
            var letters=page.Letters.ToList();var text=string.Concat(letters.Select(l=>l.Value));
            var offsets=new List<(int start,int end,UglyToad.PdfPig.Content.Letter letter)>();int offset=0;
            foreach(var letter in letters){offsets.Add((offset,offset+letter.Value.Length,letter));offset+=letter.Value.Length;}
            bool graphicsStarted=false;int fieldIndex=0;
            foreach(Match match in Regex.Matches(text,@"\{\{([A-Za-z][A-Za-z0-9]*)\}\}"))
            {
                var expression=TextExpression(match.Value);if(expression==Quote(match.Value))continue;
                var glyphs=offsets.Where(l=>l.start<match.Index+match.Length&&l.end>match.Index).Select(l=>l.letter).ToList();
                if(glyphs.Count==0)continue;
                // A placeholder must occupy one line; splitting it across lines would
                // make its replacement cover unrelated content. Reject rather than guess.
                if(glyphs.Max(l=>l.StartBaseLine.Y)-glyphs.Min(l=>l.StartBaseLine.Y)>2)
                    throw new InvalidOperationException("Keep each {{FieldName}} placeholder on one line in the template.");
                double left=glyphs.Min(l=>l.BoundingBox.Left),right=glyphs.Max(l=>l.BoundingBox.Right);
                double top=glyphs.Max(l=>l.BoundingBox.Top),bottom=glyphs.Min(l=>l.BoundingBox.Bottom);
                if(!graphicsStarted){code.AppendLine($"    using var graphics{page.Number} = PdfSharp.Drawing.XGraphics.FromPdfPage(document.Pages[{page.Number-1}], PdfSharp.Drawing.XGraphicsPdfPageOptions.Append);");graphicsStarted=true;}
                var suffix=page.Number+"_"+(++fieldIndex);double size=Math.Max(1,glyphs.Average(l=>l.FontSize));
                code.AppendLine($"    var value{suffix} = System.Convert.ToString({expression}) ?? \"\";");
                code.AppendLine($"    var font{suffix} = new PdfSharp.Drawing.XFont(\"Arial\", {Number(size)});");
                code.AppendLine($"    var measured{suffix} = graphics{page.Number}.MeasureString(value{suffix}, font{suffix}).Width;");
                code.AppendLine($"    if (measured{suffix} > {Number(right-left)}) font{suffix} = new PdfSharp.Drawing.XFont(\"Arial\", System.Math.Max(1, {Number(size)} * {Number(right-left)} / measured{suffix}));");
                code.AppendLine($"    graphics{page.Number}.DrawRectangle(PdfSharp.Drawing.XBrushes.White, {Number(left-.2)}, {Number(page.Height-top-.2)}, {Number(right-left+.4)}, {Number(top-bottom+.4)});");
                code.AppendLine($"    graphics{page.Number}.DrawString(value{suffix}, font{suffix}, PdfSharp.Drawing.XBrushes.Black, new PdfSharp.Drawing.XPoint({Number(left)}, {Number(page.Height-glyphs.Average(l=>l.StartBaseLine.Y))}));");
            }
            if(graphicsStarted)code.AppendLine($"    graphics{page.Number}.Dispose();");
        }
        code.AppendLine("    document.Save(pdfPath);");
        code.AppendLine("    MarkFileSaved();");code.AppendLine("}");return code.ToString();
    }
}

