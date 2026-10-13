namespace SalesForgeOCR.Core.Entities;
public record ExtractedDocumentImage(string FilePath, int PageNumber, string Role, double X, double Y, double Width, double Height);
