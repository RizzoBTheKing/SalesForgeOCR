using SalesForgeOCR.Core.Interfaces;
using Tesseract;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using PDFtoImage;
using SkiaSharp;
using System.Drawing;
using System.Drawing.Imaging;

namespace SalesForgeOCR.DocumentProcessing.Engines;

public class OcrEngine : IOcrEngine
{
    private readonly string _tessDataPath;
    public OcrEngine(string? tessDataPath = null) => _tessDataPath = tessDataPath ?? Path.Combine(AppContext.BaseDirectory, "tessdata");

    public async Task<string> ExtractTextAsync(string imagePath, CancellationToken cancellationToken = default)
        => string.Join(Environment.NewLine, await ExtractTextPagesAsync(imagePath, cancellationToken));

    public Task<IEnumerable<string>> ExtractTextPagesAsync(string documentPath, CancellationToken cancellationToken = default)
        => Task.Run<IEnumerable<string>>(() => ExtractPages(documentPath, cancellationToken), cancellationToken);

    private List<string> ExtractPages(string path, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!File.Exists(path)) throw new FileNotFoundException("Document file does not exist.", path);
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext is ".txt" or ".csv") return [File.ReadAllText(path)];
        if (ext == ".pdf")
        {
            using var pdf = PdfDocument.Open(path);
            var pages = new List<string>();
            TesseractEngine? engine = null;
            try
            {
                for (var i = 1; i <= pdf.NumberOfPages; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var pdfPage = pdf.GetPage(i);
                    var text = ContentOrderTextExtractor.GetText(pdfPage);
                    if (string.IsNullOrWhiteSpace(text) || (text.Length < 100 && pdfPage.GetImages().Any()))
                    {
                        engine ??= CreateEngine();
                        using var stream = File.OpenRead(path);
                        using var bitmap = Conversion.ToImage(stream, page: i - 1, options: new RenderOptions(Dpi: 300));
                        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                        using var pix = Pix.LoadFromMemory(data.ToArray());
                        using var result = engine.Process(pix);
                        text = result.GetText();
                    }
                    pages.Add(text ?? string.Empty);
                }
            }
            finally { engine?.Dispose(); }
            ct.ThrowIfCancellationRequested();
            return pages;
        }
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".tif" or ".tiff" or ".bmp"))
            throw new NotSupportedException($"Unsupported document type: {ext}");

        using var ocr = CreateEngine();
        using var image = Image.FromFile(path);
        if ((long)image.Width * image.Height > 50_000_000) throw new InvalidOperationException("Image exceeds the 50 megapixel processing limit.");
        var dimension = new FrameDimension(image.FrameDimensionsList[0]);
        var output = new List<string>();
        for (var i = 0; i < image.GetFrameCount(dimension); i++)
        {
            ct.ThrowIfCancellationRequested();
            image.SelectActiveFrame(dimension, i);
            using var memory = new MemoryStream();
            image.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            using var pix = Pix.LoadFromMemory(memory.ToArray());
            using var page = ocr.Process(pix);
            output.Add(page.GetText() ?? string.Empty);
        }
        ct.ThrowIfCancellationRequested();
        return output;
    }
    private TesseractEngine CreateEngine()
    {
        if (!File.Exists(Path.Combine(_tessDataPath, "eng.traineddata")))
            throw new InvalidOperationException($"English OCR language data is missing from {_tessDataPath}.");
        return new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
    }
}
