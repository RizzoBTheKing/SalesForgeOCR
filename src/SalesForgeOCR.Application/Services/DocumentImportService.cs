using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Core.Interfaces;
namespace SalesForgeOCR.Application.Services;
public class DocumentImportService
{
    private readonly DocumentService _documents;
    private readonly ProcessingJobService _jobs;
    private readonly IJobQueue _queue;
    private readonly string _storageDirectory;
    public DocumentImportService(DocumentService documents, ProcessingJobService jobs, IJobQueue queue, string? storageDirectory = null)
    {
        _documents = documents; _jobs = jobs; _queue = queue;
        _storageDirectory = storageDirectory ?? Path.Combine(Environment.CurrentDirectory, "documents");
    }
    public async Task<ProcessingJob> ImportAsync(string path, CancellationToken cancellationToken = default)
    {
        var source = new FileInfo(path);
        var extension = source.Extension.ToLowerInvariant();
        if (extension is not (".pdf" or ".png" or ".jpg" or ".jpeg" or ".tif" or ".tiff" or ".bmp" or ".txt" or ".csv")) throw new NotSupportedException("Unsupported document type.");
        if (!source.Exists || source.Length == 0) throw new InvalidOperationException("Document is missing or empty.");
        if (source.Length > 100 * 1024 * 1024) throw new InvalidOperationException("Document exceeds the 100 MB import limit.");
        var folder = Path.Combine(_storageDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var storedPath = Path.Combine(folder, source.Name);
        using (var input = source.OpenRead())
        using (var output = File.Create(storedPath)) await input.CopyToAsync(output, cancellationToken);
        var document = await _documents.CreateDocumentAsync(source.Name, source.Length, "admin", cancellationToken);
        var version = new DocumentVersion { DocumentId = document.Id, VersionNumber = 1, FileName = source.Name, StoragePath = storedPath, Size = source.Length, CreatedBy = "admin" };
        await _documents.AddVersionAsync(document.Id, version, cancellationToken);
        var job = new ProcessingJob { DocumentId = document.Id, VersionId = version.Id, CreatedBy = "admin" };
        await _jobs.CreateAsync(job, cancellationToken);
        await _jobs.EnqueueJobAsync(job.Id, _queue, cancellationToken);
        return job;
    }
}
