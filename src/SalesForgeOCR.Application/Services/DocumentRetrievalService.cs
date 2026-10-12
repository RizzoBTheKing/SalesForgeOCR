using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Application.Services
{
    public class DocumentRetrievalService
    {
        private readonly IDocumentRepository _docRepo;
        private readonly IDocumentTextRepository _textRepo;
        private readonly IDocumentImageRepository _imageRepo;
        private readonly IDocumentImageLocationRepository _locationRepo;
        private readonly IDocumentEntityRepository _entityRepo;
        private readonly IDocumentTableRepository _tableRepo;
        private readonly IDocumentMetadataRepository _metadataRepo;
        private readonly IProcessingJobRepository _jobRepo;
        private readonly IAuditService _audit;

        public DocumentRetrievalService(IDocumentRepository docRepo,
            IDocumentTextRepository textRepo,
            IDocumentImageRepository imageRepo,
            IDocumentImageLocationRepository locationRepo,
            IDocumentEntityRepository entityRepo,
            IDocumentTableRepository tableRepo,
            IDocumentMetadataRepository metadataRepo,
            IProcessingJobRepository jobRepo,
            IAuditService audit)
        {
            _docRepo = docRepo;
            _textRepo = textRepo;
            _imageRepo = imageRepo;
            _locationRepo = locationRepo;
            _entityRepo = entityRepo;
            _tableRepo = tableRepo;
            _metadataRepo = metadataRepo;
            _jobRepo = jobRepo;
            _audit = audit;
        }

        public async Task<(Document doc, IEnumerable<DocumentVersion> versions, IEnumerable<DocumentText> texts, IEnumerable<DocumentImage> images, IEnumerable<DocumentImageLocation> locations, IEnumerable<DocumentEntity> entities, IEnumerable<DocumentTable> tables, IEnumerable<DocumentMetadata> metadata, IEnumerable<ProcessingJob> jobs)>
            GetDocumentViewAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var doc = await _docRepo.GetDocumentAsync(id, cancellationToken);
            if (doc == null) throw new InvalidOperationException("Document not found");

            var versions = await _docRepo.GetVersionsAsync(id, cancellationToken);
            var texts = await _textRepo.GetByDocumentAsync(id, cancellationToken);
            var images = await _imageRepo.GetByDocumentAsync(id, cancellationToken);
            var locations = await _locationRepo.GetByDocumentAsync(id, cancellationToken);
            var entities = await _entityRepo.GetByDocumentAsync(id, cancellationToken);
            var tables = await _tableRepo.GetByDocumentAsync(id, cancellationToken);
            var metadata = await _metadataRepo.GetByDocumentAsync(id, cancellationToken);
            var jobs = await _jobRepo.GetByDocumentAsync(id, cancellationToken);

            await _audit.LogAsync(new AuditLog { EventType = "Document.Retrieve", Actor = "system", Details = $"Retrieved document view for {id}" }, cancellationToken);

            return (doc, versions, texts, images, locations, entities, tables, metadata, jobs);
        }
    }
}
