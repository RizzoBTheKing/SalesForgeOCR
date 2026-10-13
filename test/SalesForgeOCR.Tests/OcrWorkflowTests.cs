using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Core.Interfaces;
using SalesForgeOCR.Core.Search;
using SalesForgeOCR.Data;
using SalesForgeOCR.Data.Repositories;
using SalesForgeOCR.DocumentProcessing.Engines;
using SalesForgeOCR.DocumentProcessing.Processing;
using SalesForgeOCR.DocumentProcessing.Processing.Stages;
using SalesForgeOCR.Application.Background;
using SalesForgeOCR.Application.Services;
using SalesForgeOCR.Search;
using System.Drawing;
using System.Drawing.Imaging;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;

namespace SalesForgeOCR.Tests;
[TestClass]
[DoNotParallelize]
public class OcrWorkflowTests
{
    private string _directory = null!;
    private string _db = null!;
    [TestInitialize]
    public async Task Setup()
    {
        _directory = Path.Combine(Path.GetTempPath(), "sfocr-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _db = Path.Combine(_directory, "test.db");
        await new DatabaseInitializer(_db).InitializeAsync();
    }
    [TestCleanup]
    public void Cleanup()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(_directory, true);
    }
    private string Image(string name = "image.png", string text = "SALES FORGE OCR 12345")
    {
        var path = Path.Combine(_directory, name);
        using var bitmap = new Bitmap(1600, 260);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        using var font = new Font("Arial", 46);
        graphics.DrawString(text, font, Brushes.Black, 40, 75);
        bitmap.SetResolution(300, 300);
        bitmap.Save(path, ImageFormat.Png);
        return path;
    }
    [TestMethod]
    public async Task ActualImageOcrRecognizesPrintedText()
    {
        var text = await new OcrEngine().ExtractTextAsync(Image());
        StringAssert.Contains(text, "12345");
        StringAssert.Contains(text.ToUpperInvariant(), "SALES");
    }
    [TestMethod]
    public async Task ScannedPdfUsesRendererAndActualOcr()
    {
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(800, 130);
        page.AddPng(File.ReadAllBytes(Image()), new PdfRectangle(0, 0, 800, 130));
        var path = Path.Combine(_directory, "scanned.pdf");
        File.WriteAllBytes(path, builder.Build());
        var text = await new OcrEngine().ExtractTextAsync(path);
        StringAssert.Contains(text, "12345");
    }
    [TestMethod]
    public async Task DigitalPdfPreservesPagesAndSpacesWithoutTesseractData()
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        builder.AddPage(600, 800).AddText("First page unique phrase", 18, new PdfPoint(40, 600), font);
        builder.AddPage(600, 800).AddText("Second page another phrase", 18, new PdfPoint(40, 600), font);
        var path = Path.Combine(_directory, "digital.pdf");
        File.WriteAllBytes(path, builder.Build());
        var pages = (await new OcrEngine(_directory).ExtractTextPagesAsync(path)).ToList();
        Assert.AreEqual(2, pages.Count);
        StringAssert.Contains(pages[0], "First page unique phrase");
        StringAssert.Contains(pages[1], "Second page another phrase");
    }
    [TestMethod]
    public async Task MissingLanguageDataIsAnExplicitFailure()
    {
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => new OcrEngine(_directory).ExtractTextAsync(Image()));
    }
    [TestMethod]
    public async Task MissingFileAndUnsupportedFormatsFailExplicitly()
    {
        await Assert.ThrowsExceptionAsync<FileNotFoundException>(() => new OcrEngine().ExtractTextAsync(Path.Combine(_directory, "missing.png")));
        var path = Path.Combine(_directory, "bad.exe");
        File.WriteAllText(path,"not an image");
        await Assert.ThrowsExceptionAsync<NotSupportedException>(() => new OcrEngine().ExtractTextAsync(path));
    }
    [TestMethod]
    public async Task OcrHonorsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        try { await new OcrEngine().ExtractTextAsync(Image(), cancellation.Token); Assert.Fail("Cancellation was ignored."); }
        catch (OperationCanceledException) { }
    }
    [TestMethod]
    public async Task VersionStoragePathSurvivesRepositoryReload()
    {
        var repo = new SqliteDocumentRepository(_db);
        var document = new Document { FileName="sample.png" };
        await repo.AddDocumentAsync(document);
        var version = new DocumentVersion { DocumentId=document.Id, VersionNumber=1, FileName=document.FileName, StoragePath=Image() };
        await repo.AddVersionAsync(version);
        var reloaded = (await repo.GetVersionsAsync(document.Id)).Single();
        Assert.AreEqual(version.StoragePath,reloaded.StoragePath);
        Assert.AreEqual(version.Id,(await repo.GetDocumentAsync(document.Id))!.CurrentVersionId);
        Assert.AreEqual(1,(await repo.SearchDocumentsAsync("")).Count());
    }
    [TestMethod]
    public async Task ExistingEmptyDatabaseGetsSchemaAndCanReinitialize()
    {
        var path = Path.Combine(_directory,"empty.db");
        File.WriteAllBytes(path,[]);
        await new DatabaseInitializer(path).InitializeAsync();
        await new DatabaseInitializer(path).InitializeAsync();
        await new SqliteDocumentRepository(path).AddDocumentAsync(new Document { FileName="test" });
    }
    [TestMethod]
    public async Task PendingJobWithEmptyTimestampsCanBeRead()
    {
        var repo = new ProcessingJobRepository(_db);
        var job = new ProcessingJob { DocumentId=Guid.NewGuid(),VersionId=Guid.NewGuid() };
        await repo.AddAsync(job);
        var loaded = await repo.GetByIdAsync(job.Id);
        Assert.IsNotNull(loaded);
        Assert.IsNull(loaded!.StartedAt);
        Assert.IsNull(loaded.CompletedAt);
        Assert.AreEqual(1,(await repo.GetByDocumentAsync(job.DocumentId)).Count());
        await repo.RecoverInterruptedAsync();
        Assert.AreEqual(ProcessingStatus.Failed,(await repo.GetByIdAsync(job.Id))!.Status);
    }
    [TestMethod]
    public async Task AtomicStorageIsIdempotentAndRollbackPreservesPreviousText()
    {
        var doc = new Document { FileName="sample.txt" };
        await new SqliteDocumentRepository(_db).AddDocumentAsync(doc);
        var version = new DocumentVersion { DocumentId=doc.Id, VersionNumber=1, FileName=doc.FileName };
        await new SqliteDocumentRepository(_db).AddVersionAsync(version);
        var text = new DocumentText { DocumentId=doc.Id, VersionId=version.Id, PageNumber=1, Content="unique original phrase" };
        var store = new DocumentArtifactStore(_db);
        await store.SaveAsync(doc.Id,version.Id,[text],[],[],[],[]);
        await store.SaveAsync(doc.Id,version.Id,[text],[],[],[],[]);
        Assert.AreEqual(1,(await new DocumentTextRepository(_db).GetByDocumentAsync(doc.Id)).Count());
        Assert.AreEqual(0,(await new SearchIndexService(_db).ValidateIndexConsistencyAsync()).DuplicateEntries.Count);
        var invalid = new DocumentText { Id=text.Id,DocumentId=doc.Id,VersionId=version.Id,PageNumber=2,Content="other" };
        await Assert.ThrowsExceptionAsync<Microsoft.Data.Sqlite.SqliteException>(() => store.SaveAsync(doc.Id,version.Id,[text,invalid],[],[],[],[]));
        Assert.AreEqual("unique original phrase",(await new DocumentTextRepository(_db).GetByDocumentAsync(doc.Id)).Single().Content);
        Assert.AreEqual(1,(await new SearchService(_db).SearchAsync(new SearchRequest { Query="unique original" })).Results.Count);
    }
    [TestMethod]
    public async Task DeletingEntitiesPreservesDocumentTextAndOtherDocuments()
    {
        var repo=new SqliteDocumentRepository(_db);
        var textRepo=new DocumentTextRepository(_db);
        var index=new SearchIndexService(_db);
        var entities=new DocumentEntityRepository(_db);
        var a=new Document {FileName="a"}; var b=new Document {FileName="b"};
        await repo.AddDocumentAsync(a);await repo.AddDocumentAsync(b);
        var text=new DocumentText {DocumentId=a.Id,VersionId=Guid.NewGuid(),PageNumber=1,Content="retainedtext"};
        await textRepo.AddAsync(text);await index.IndexTextEntryAsync(a.Id,text.Id,text.Content);
        await entities.AddAsync(new DocumentEntity {DocumentId=a.Id,VersionId=text.VersionId,Type="Email",Value="remove@example.com"});
        await entities.AddAsync(new DocumentEntity {DocumentId=b.Id,VersionId=Guid.NewGuid(),Type="Email",Value="keep@example.com"});
        await entities.DeleteByDocumentAsync(a.Id);
        var search=new SearchService(_db);
        Assert.AreEqual(1,(await search.SearchAsync(new SearchRequest {Query="retainedtext"})).Results.Count);
        Assert.AreEqual(1,(await search.SearchAsync(new SearchRequest {Query="keep@example.com"})).Results.Count);
        Assert.AreEqual(0,(await search.SearchAsync(new SearchRequest {Query="remove@example.com"})).Results.Count);
    }
    [TestMethod]
    public async Task SearchHandlesPunctuationQuotesAndEmptyInput()
    {
        var search=new SearchService(_db);
        foreach(var query in new[]{""," ","contact@example.com","a \"quoted\" phrase","foo-bar","*","AND OR"})
            await search.SearchAsync(new SearchRequest {Query=query});
    }
    [TestMethod]
    public async Task WorkerContinuesAfterFailedJobAndRejectsUseAfterDisposal()
    {
        var queue=new BackgroundJobQueue();
        var done=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        queue.StartWorker();queue.StartWorker();
        await queue.EnqueueAsync(_=>throw new InvalidOperationException("intentional failure"));
        await queue.EnqueueAsync(_=>{done.SetResult(true);return Task.CompletedTask;});
        await done.Task.WaitAsync(TimeSpan.FromSeconds(10));
        queue.Dispose();queue.Dispose();
        await Assert.ThrowsExceptionAsync<ObjectDisposedException>(()=>queue.EnqueueAsync(_=>Task.CompletedTask));
    }
    [TestMethod]
    public void ApplicationServicesAndEveryViewCanBeConstructed()
    {
        Exception? failure=null;
        var previous=Environment.CurrentDirectory;
        var thread=new Thread(()=>
        {
            try
            {
                using var host=SalesForgeOCR.UI.Program.CreateHost(_directory);
                host.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync().GetAwaiter().GetResult();
                foreach(var type in typeof(SalesForgeOCR.UI.Forms.MainForm).Assembly.GetTypes().Where(t=>!t.IsAbstract && (typeof(SalesForgeOCR.UI.Services.IView).IsAssignableFrom(t) || typeof(Form).IsAssignableFrom(t)) && t.Namespace=="SalesForgeOCR.UI.Forms"))
                    host.Services.GetRequiredService(type);
                host.Services.GetRequiredService<DocumentImportService>();
                host.Services.GetRequiredService<DocumentProcessingJob>();
                host.Services.GetRequiredService<SalesForgeOCR.Security.MasterPasswordSetupService>();
            }
            catch(Exception error){failure=error;}
        });
        thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();
        Environment.CurrentDirectory=previous;
        if(failure!=null) Assert.Fail(failure.ToString());
    }
    [TestMethod]
    public async Task FullImportWorkerStorageAndSearchWorkflow()
    {
        var previous=Environment.CurrentDirectory;
        try
        {
            using var host=SalesForgeOCR.UI.Program.CreateHost(_directory);
            await host.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();
            host.Services.GetRequiredService<IJobQueue>().AsWorker().StartWorker();
            var path=Image();
            var job=await host.Services.GetRequiredService<DocumentImportService>().ImportAsync(path);
            File.Delete(path);
            var repository=host.Services.GetRequiredService<IProcessingJobRepository>();
            ProcessingJob? loaded=null;
            for(var i=0;i<200;i++)
            {
                loaded=await repository.GetByIdAsync(job.Id);
                if(loaded?.Status is ProcessingStatus.Completed or ProcessingStatus.Failed) break;
                await Task.Delay(100);
            }
            Assert.AreEqual(ProcessingStatus.Completed,loaded!.Status,loaded.ErrorMessage);
            var response=await host.Services.GetRequiredService<SearchService>().SearchAsync(new SearchRequest {Query="12345"});
            Assert.AreEqual(job.DocumentId,response.Results.Single().DocumentId);
            Assert.AreEqual(1,(await host.Services.GetRequiredService<IDocumentTextRepository>().GetByDocumentAsync(job.DocumentId)).Count());
        }
        finally {Environment.CurrentDirectory=previous;}
    }
    [TestMethod]
    public async Task MultipageTiffPreservesPageCountAndText()
    {
        using var first = (Bitmap)System.Drawing.Image.FromFile(Image("one.png", "FIRST PAGE 12345"));
        using var second = (Bitmap)System.Drawing.Image.FromFile(Image("two.png", "SECOND PAGE 67890"));
        var encoder = ImageCodecInfo.GetImageEncoders().Single(codec => codec.MimeType == "image/tiff");
        var path = Path.Combine(_directory,"multiple.tiff");
        using var parameters = new EncoderParameters(1);
        parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.SaveFlag,(long)EncoderValue.MultiFrame);
        first.Save(path,encoder,parameters);
        parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.SaveFlag,(long)EncoderValue.FrameDimensionPage);
        first.SaveAdd(second,parameters);
        parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.SaveFlag,(long)EncoderValue.Flush);
        first.SaveAdd(parameters);
        var pages = (await new OcrEngine().ExtractTextPagesAsync(path)).ToList();
        Assert.AreEqual(2,pages.Count);
        StringAssert.Contains(pages[0],"12345");
        StringAssert.Contains(pages[1],"67890");
    }
    [TestMethod]
    public async Task AuthenticationSetupLoginAndSessionPersistenceWork()
    {
        var auth = new SalesForgeOCR.Security.AuthenticationService(new SettingsRepository(_db),new SessionRepository(_db),new AuditRepository(_db));
        await auth.SetupMasterPasswordAsync("sample strong password");
        Assert.IsFalse(await auth.VerifyMasterPasswordAsync("incorrect"));
        Assert.IsTrue(await auth.VerifyMasterPasswordAsync("sample strong password"));
        var session = await auth.CreateSessionAsync("admin");
        Assert.IsTrue(session.ExpiresAt > DateTime.UtcNow);
        await auth.InvalidateSessionAsync(session.Id.ToString());
        Assert.IsFalse(SalesForgeOCR.Security.PasswordHasher.Verify("test",""));
        Assert.IsFalse(SalesForgeOCR.Security.PasswordHasher.Verify("test","not base64"));
    }
    [TestMethod]
    public async Task SearchRebuildAndReindexDoNotDuplicateOrLoseArtifacts()
    {
        var doc = new Document {FileName="rebuild.txt"};
        var repository = new SqliteDocumentRepository(_db);await repository.AddDocumentAsync(doc);
        var version = new DocumentVersion {DocumentId=doc.Id,VersionNumber=1,FileName=doc.FileName};await repository.AddVersionAsync(version);
        var text = new DocumentText {DocumentId=doc.Id,VersionId=version.Id,PageNumber=1,Content="rebuild token"};
        await new DocumentArtifactStore(_db).SaveAsync(doc.Id,version.Id,[text],[],[],[],[]);
        var index = new SearchIndexService(_db);
        await index.RebuildAllAsync();await index.ReindexDocumentAsync(doc.Id);await index.RebuildAllAsync();
        Assert.AreEqual(0,(await index.ValidateIndexConsistencyAsync()).DuplicateEntries.Count);
        Assert.AreEqual(1,(await new SearchService(_db).SearchAsync(new SearchRequest {Query="rebuild"})).Results.Count);
        Assert.AreEqual(1,(await repository.SearchDocumentsAsync("rebuild")).Count());
    }
    [TestMethod]
    public async Task SearchPaginationSourceAndFiltersWork()
    {
        var repository = new SqliteDocumentRepository(_db);
        for(var i=0;i<3;i++)
        {
            var doc=new Document {FileName="document"+i};await repository.AddDocumentAsync(doc);
            var version=new DocumentVersion {DocumentId=doc.Id,VersionNumber=1,FileName=doc.FileName};await repository.AddVersionAsync(version);
            var text=new DocumentText {DocumentId=doc.Id,VersionId=version.Id,PageNumber=1,Content="shared term"};
            var entity=new DocumentEntity {DocumentId=doc.Id,VersionId=version.Id,Type="Email",Value="shared@example.com",PageNumber=1};
            await new DocumentArtifactStore(_db).SaveAsync(doc.Id,version.Id,[text],[entity],[],[],[]);
        }
        var search=new SearchService(_db);
        var first=await search.SearchAsync(new SearchRequest {Query="shared",SourceType=SearchSourceType.Text,Pagination=new SearchPagination {Page=1,PageSize=1},Sort=new SearchSortOption {Field="documentname",Descending=false}});
        var second=await search.SearchAsync(new SearchRequest {Query="shared",SourceType=SearchSourceType.Text,Pagination=new SearchPagination {Page=2,PageSize=1},Sort=new SearchSortOption {Field="documentname",Descending=false}});
        Assert.AreEqual(3L,first.Statistics.TotalMatches);
        Assert.AreEqual("document0",first.Results.Single().DocumentName);
        Assert.AreEqual("document1",second.Results.Single().DocumentName);
        Assert.AreEqual(SearchSourceType.Text,first.Results.Single().Hits.Single().SourceType);
        var filtered=await search.SearchAsync(new SearchRequest {Query="shared",FilterGroups=[new SearchFilterGroup {Filters=[new SearchFilter {Field="DocumentName",Value="document2"}]}]});
        Assert.AreEqual("document2",filtered.Results.Single().DocumentName);
    }
    [TestMethod]
    public async Task ImportRejectsEmptyAndUnsupportedFiles()
    {
        var previous=Environment.CurrentDirectory;
        try
        {
            using var host=SalesForgeOCR.UI.Program.CreateHost(_directory);
            await host.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();
            var importer=host.Services.GetRequiredService<DocumentImportService>();
            var empty=Path.Combine(_directory,"empty.png");File.WriteAllBytes(empty,[]);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>importer.ImportAsync(empty));
            var bad=Path.Combine(_directory,"bad.exe");File.WriteAllText(bad,"test");
            await Assert.ThrowsExceptionAsync<NotSupportedException>(()=>importer.ImportAsync(bad));
        }
        finally { Environment.CurrentDirectory=previous; }
    }
    [TestMethod]
    public async Task FailedOcrJobIsNotMarkedCompleted()
    {
        var previous=Environment.CurrentDirectory;
        try
        {
            using var host=SalesForgeOCR.UI.Program.CreateHost(_directory);
            await host.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();
            host.Services.GetRequiredService<IJobQueue>().AsWorker().StartWorker();
            var corrupt=Path.Combine(_directory,"corrupt.png");File.WriteAllText(corrupt,"not an image");
            var job=await host.Services.GetRequiredService<DocumentImportService>().ImportAsync(corrupt);
            var repository=host.Services.GetRequiredService<IProcessingJobRepository>();
            ProcessingJob? loaded=null;
            for(var i=0;i<100;i++)
            {
                loaded=await repository.GetByIdAsync(job.Id);
                if(loaded?.Status is ProcessingStatus.Completed or ProcessingStatus.Failed) break;
                await Task.Delay(100);
            }
            Assert.AreEqual(ProcessingStatus.Failed,loaded!.Status);
            Assert.IsFalse(string.IsNullOrWhiteSpace(loaded.ErrorMessage));
            Assert.AreEqual(0,(await host.Services.GetRequiredService<IDocumentTextRepository>().GetByDocumentAsync(job.DocumentId)).Count());
        }
        finally {Environment.CurrentDirectory=previous;}
    }

}
internal static class QueueTestExtensions
{
    public static BackgroundJobQueue AsWorker(this IJobQueue queue)=>(BackgroundJobQueue)queue;
}
