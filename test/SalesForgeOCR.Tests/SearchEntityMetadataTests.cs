using Microsoft.VisualStudio.TestTools.UnitTesting;
using SalesForgeOCR.Data;
using SalesForgeOCR.Data.Repositories;
using SalesForgeOCR.Search;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SalesForgeOCR.Tests
{
    [TestClass]
    public class SearchEntityMetadataTests
    {
        private const string DbFile = "test_search_em.db";

        [TestInitialize]
        public void Init()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(DbFile)) File.Delete(DbFile);
            var init = new DatabaseInitializer(Path.Combine(Directory.GetCurrentDirectory(), DbFile));
            init.InitializeAsync().GetAwaiter().GetResult();
        }

        [TestMethod]
        public async Task EntityIndexAndSearch_ReturnsEnrichedHit()
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), DbFile);
            var docRepo = new SqliteDocumentRepository(dbPath);
            var entRepo = new DocumentEntityRepository(dbPath);
            var search = new SqliteFtsSearchEngine(dbPath);
            var svc = new SearchService(dbPath);

            var doc = new SalesForgeOCR.Core.Entities.Document { Id = Guid.NewGuid(), FileName = "e.doc", Size = 10, CreatedAt = DateTime.UtcNow };
            await docRepo.AddDocumentAsync(doc);
            var version = new SalesForgeOCR.Core.Entities.DocumentVersion { Id = Guid.NewGuid(), DocumentId = doc.Id, VersionNumber = 1, FileName = "e.doc", Size = 10, CreatedAt = DateTime.UtcNow };
            await docRepo.AddVersionAsync(version);

            var entity = new SalesForgeOCR.Core.Entities.DocumentEntity { Id = Guid.NewGuid(), DocumentId = doc.Id, VersionId = version.Id, Type = "Email", Value = "contact@example.com", Confidence = 0.95, PageNumber = 1 };
            await entRepo.AddAsync(entity);

            await search.IndexEntitiesAsync(doc.Id, new[] { (entity.Type, entity.Value) });

            var req = new SalesForgeOCR.Core.Search.SearchRequest { Query = "contact@example.com" };
            var res = await svc.SearchAsync(req);

            Assert.IsTrue(res.Statistics.TotalMatches > 0);
            Assert.IsTrue(res.Results.Count > 0);
            var hit = res.Results[0].Hits[0];
            Assert.AreEqual(doc.Id, hit.DocumentId);
            Assert.AreEqual("Email", hit.EntityType);
        }

        [TestMethod]
        public async Task MetadataIndexAndSearch_ReturnsEnrichedHit()
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), DbFile);
            var docRepo = new SqliteDocumentRepository(dbPath);
            var metaRepo = new DocumentMetadataRepository(dbPath);
            var search = new SqliteFtsSearchEngine(dbPath);
            var svc = new SearchService(dbPath);

            var doc = new SalesForgeOCR.Core.Entities.Document { Id = Guid.NewGuid(), FileName = "m.doc", Size = 10, CreatedAt = DateTime.UtcNow };
            await docRepo.AddDocumentAsync(doc);
            var version = new SalesForgeOCR.Core.Entities.DocumentVersion { Id = Guid.NewGuid(), DocumentId = doc.Id, VersionNumber = 1, FileName = "m.doc", Size = 10, CreatedAt = DateTime.UtcNow };
            await docRepo.AddVersionAsync(version);

            var meta = new SalesForgeOCR.Core.Entities.DocumentMetadata { Id = Guid.NewGuid(), DocumentId = doc.Id, Key = "Author", Value = "John Doe" };
            await metaRepo.AddAsync(meta);

            await search.IndexMetadataAsync(doc.Id, new[] { (meta.Key, meta.Value) });

            var req = new SalesForgeOCR.Core.Search.SearchRequest { Query = "John Doe" };
            var res = await svc.SearchAsync(req);

            Assert.IsTrue(res.Statistics.TotalMatches > 0);
            Assert.IsTrue(res.Results.Count > 0);
            var hit = res.Results[0].Hits[0];
            Assert.AreEqual(doc.Id, hit.DocumentId);
            Assert.AreEqual("Author", hit.MetadataKey);
        }
    }
}
