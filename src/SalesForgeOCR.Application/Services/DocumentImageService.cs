using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Application.Services
{
    public class DocumentImageService
    {
        private readonly IDocumentImageRepository _repo;
        private readonly IDocumentImageLocationRepository _locationRepo;
        private readonly IThumbnailGenerator _thumb;
        private readonly IAuditService _audit;

        public DocumentImageService(IDocumentImageRepository repo, IDocumentImageLocationRepository locationRepo, IThumbnailGenerator thumb, IAuditService audit)
        {
            _repo = repo;
            _locationRepo = locationRepo;
            _thumb = thumb;
            _audit = audit;
        }

        public async Task AddImageAsync(DocumentImage image, CancellationToken cancellationToken = default)
        {
            image.Validate();
            await _repo.AddAsync(image, cancellationToken);
            // generate thumbnail if not present
            if (string.IsNullOrWhiteSpace(image.ThumbnailPath))
            {
                try
                {
                    var thumb = await _thumb.GenerateThumbnailAsync(image.FilePath, 200, 200, cancellationToken);
                    image.ThumbnailPath = thumb;
                    await _repo.UpdateAsync(image, cancellationToken);
                }
                catch (Exception ex)
                {
                    await _audit.LogAsync(new AuditLog { EventType = "Thumbnail.Failure", Actor = "system", Details = ex.Message }, cancellationToken);
                }
            }
            await _audit.LogAsync(new AuditLog { EventType = "DocumentImage.Add", Actor = "system", Details = $"Image {image.Id} added for document {image.DocumentId}" }, cancellationToken);
        }

        public Task<DocumentImage?> GetAsync(Guid id, CancellationToken cancellationToken = default) => _repo.GetAsync(id, cancellationToken);
        public Task<IEnumerable<DocumentImage>> GetByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) => _repo.GetByDocumentAsync(documentId, cancellationToken);
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) => _repo.DeleteAsync(id, cancellationToken);
        public Task UpdateAsync(DocumentImage image, CancellationToken cancellationToken = default) => _repo.UpdateAsync(image, cancellationToken);

        public Task AddLocationAsync(DocumentImageLocation loc, CancellationToken cancellationToken = default) => _locationRepo.AddAsync(loc, cancellationToken);
        public Task<IEnumerable<DocumentImageLocation>> GetLocationsByImageAsync(Guid imageId, CancellationToken cancellationToken = default) => _locationRepo.GetByImageAsync(imageId, cancellationToken);
    }
}
