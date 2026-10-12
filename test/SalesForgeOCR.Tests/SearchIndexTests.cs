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
    public class SearchIndexTests
    {
        private const string DbFile = "test_search.db";

        [TestInitialize]
        public void Init()
        {
            if (File.Exists(DbFile)) File.Delete(DbFile);
            var init = new DatabaseInitializer(Path.Combine(Directory.GetCurrentDirectory(), DbFile));
            init.InitializeAsync().GetAwaiter().GetResult();
        }

        [TestMethod]
        public async Task IndexAndSearch_Text_ReturnsDocument()
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), DbFile);
            var docRepo = new SqliteDocumentRepository(dbPath);
            var search = new SqliteFtsSearchEngine(dbPath);

            // create document and version
            var doc = new SalesForgeOCR.Core.Entities.Document { Id = Guid.NewGuid(), FileName = "test.txt", Size = 100, CreatedAt = DateTime.UtcNow };
            await docRepo.AddDocumentAsync(doc);
            var version = new SalesForgeOCR.Core.Entities.DocumentVersion { Id = Guid.NewGuid(), DocumentId = doc.Id, VersionNumber = 1, FileName = "test.txt", Size = 100, CreatedAt = DateTime.UtcNow };
            await docRepo.AddVersionAsync(version);

            var pages = new[] { (1, "The quick brown fox jumps over the lazy dog uniquephrase123") };
            await search.IndexDocumentTextAsync(doc.Id, version.Id, pages);

            var results = await search.SearchAsync("uniquephrase123");
            Assert.IsTrue(results != null);
            Assert.IsTrue(results.GetEnumerator().MoveNext());
        }
    }
}
