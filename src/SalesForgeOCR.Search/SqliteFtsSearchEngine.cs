using Microsoft.Data.Sqlite;
using SalesForgeOCR.Core.Interfaces;
using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Core.Search;
using SalesForgeOCR.Data.Repositories;
namespace SalesForgeOCR.Search;
public class SqliteFtsSearchEngine : ISearchEngine
{
    private readonly string _dbPath;
    public SqliteFtsSearchEngine(string? dbPath = null) => _dbPath = dbPath ?? "salesforgeocr.db";
    public async Task IndexDocumentTextAsync(Guid documentId, Guid versionId, IEnumerable<(int page,string content)> pages, CancellationToken cancellationToken = default)
    {
        var repository = new DocumentTextRepository(_dbPath);
        var index = new SearchIndexService(_dbPath);
        var existing = (await repository.GetByDocumentAsync(documentId,cancellationToken)).Where(t=>t.VersionId==versionId).ToList();
        foreach(var (page,content) in pages)
        {
            var text = existing.FirstOrDefault(t=>t.PageNumber==page);
            if(text == null)
            {
                text = new DocumentText { DocumentId=documentId,VersionId=versionId,PageNumber=page,Content=content };
                await repository.AddAsync(text,cancellationToken);
                existing.Add(text);
            }
            else { text.Content=content; await repository.UpdateAsync(text,cancellationToken); }
            await index.IndexTextEntryAsync(documentId,text.Id,text.Content,1,cancellationToken);
        }
    }
    public async Task IndexEntitiesAsync(Guid documentId,IEnumerable<(string type,string value)> entities,CancellationToken cancellationToken=default)
    {
        var repository=new DocumentEntityRepository(_dbPath);
        var existing=(await repository.GetByDocumentAsync(documentId,cancellationToken)).ToList();
        var versions=await new SqliteDocumentRepository(_dbPath).GetVersionsAsync(documentId,cancellationToken);
        var version=versions.FirstOrDefault() ?? throw new InvalidOperationException("Document has no version.");
        foreach(var (type,value) in entities)
        {
            var entity=existing.FirstOrDefault(e=>e.Type==type&&e.Value==value);
            if(entity==null)
            {
                entity=new DocumentEntity {DocumentId=documentId,VersionId=version.Id,Type=type,Value=value,PageNumber=1};
                await repository.AddAsync(entity,cancellationToken);existing.Add(entity);
            }
            else await new SearchIndexService(_dbPath).IndexEntityEntryAsync(documentId,entity.Id,value,2,cancellationToken);
        }
    }
    public async Task IndexMetadataAsync(Guid documentId,IEnumerable<(string key,string value)> metadata,CancellationToken cancellationToken=default)
    {
        var repository=new DocumentMetadataRepository(_dbPath);
        var existing=(await repository.GetByDocumentAsync(documentId,cancellationToken)).ToList();
        foreach(var (key,value) in metadata)
        {
            var item=existing.FirstOrDefault(m=>m.Key==key&&m.Value==value);
            if(item==null)
            {
                item=new DocumentMetadata {DocumentId=documentId,Key=key,Value=value};
                await repository.AddAsync(item,cancellationToken);existing.Add(item);
            }
            else await new SearchIndexService(_dbPath).IndexMetadataEntryAsync(documentId,item.Id,value,0.5,cancellationToken);
        }
    }
    private async Task<IEnumerable<(Guid documentId,double score)>> Search(string query,int limit,SearchSourceType? source,CancellationToken ct)
    {
        var response=await new SearchService(_dbPath).SearchAsync(new SearchRequest { Query=query,SourceType=source,Pagination=new SearchPagination {PageSize=limit} },ct);
        return response.Results.Select(r=>(r.DocumentId,r.Score));
    }
    public Task<IEnumerable<(Guid documentId,double score)>> SearchAsync(string query,int limit=50,CancellationToken cancellationToken=default)=>Search(query,limit,null,cancellationToken);
    public Task<IEnumerable<(Guid documentId,double score)>> SearchEntitiesAsync(string query,int limit=50,CancellationToken cancellationToken=default)=>Search(query,limit,SearchSourceType.Entity,cancellationToken);
    public Task<IEnumerable<(Guid documentId,double score)>> SearchMetadataAsync(string query,int limit=50,CancellationToken cancellationToken=default)=>Search(query,limit,SearchSourceType.Metadata,cancellationToken);
}
