using Microsoft.Data.Sqlite;
using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Core.Interfaces;
namespace SalesForgeOCR.Data;
public class DocumentArtifactStore : IDocumentArtifactStore
{
    private readonly string _path;
    public DocumentArtifactStore(string? dbPath = null) => _path = dbPath ?? "salesforgeocr.db";
    public async Task SaveAsync(Guid documentId, Guid versionId, IEnumerable<DocumentText> texts, IEnumerable<DocumentEntity> entities, IEnumerable<DocumentMetadata> metadata, IEnumerable<DocumentImage> images, IEnumerable<DocumentTable> tables, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _path }.ToString());
        await connection.OpenAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();
        async Task Execute(string sql, params (string key, object? value)[] parameters)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            foreach (var (key, value) in parameters) command.Parameters.AddWithValue(key, value ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        async Task Index(Guid id, string type, string content, double weight)
        {
            await Execute("INSERT INTO SearchIndexFTS (DocumentId,SourceRecordId,SourceType,SearchableText,SearchWeight,CreatedDate,LastModifiedDate) VALUES ($doc,$id,$type,$text,$weight,$now,$now)", ("$doc", documentId.ToString()), ("$id", id.ToString()), ("$type", type), ("$text", content), ("$weight", weight), ("$now", DateTime.UtcNow.ToString("o")));
        }
        // Remove only replaced version artifacts and document-level metadata.
        await Execute("DELETE FROM SearchIndexFTS WHERE SourceRecordId IN (SELECT Id FROM DocumentText WHERE VersionId=$ver UNION SELECT Id FROM DocumentEntities WHERE VersionId=$ver UNION SELECT Id FROM DocumentImages WHERE VersionId=$ver UNION SELECT Id FROM DocumentMetadata WHERE DocumentId=$doc)", ("$ver", versionId.ToString()), ("$doc", documentId.ToString()));
        foreach (var table in new[] { "DocumentText", "DocumentEntities", "DocumentImages", "DocumentTables" })
            await Execute($"DELETE FROM {table} WHERE VersionId=$ver", ("$ver", versionId.ToString()));
        await Execute("DELETE FROM DocumentTextFTS WHERE VersionId=$ver", ("$ver", versionId.ToString()));
        await Execute("DELETE FROM DocumentMetadata WHERE DocumentId=$doc", ("$doc", documentId.ToString()));
        foreach (var t in texts)
        {
            await Execute("INSERT INTO DocumentText (Id,DocumentId,VersionId,PageNumber,Content,Confidence) VALUES ($id,$doc,$ver,$page,$content,$conf)", ("$id",t.Id.ToString()), ("$doc",documentId.ToString()), ("$ver",versionId.ToString()), ("$page",t.PageNumber), ("$content",t.Content), ("$conf",t.Confidence));
            await Execute("INSERT INTO DocumentTextFTS (DocumentId,VersionId,PageNumber,Content) VALUES ($doc,$ver,$page,$content)", ("$doc",documentId.ToString()), ("$ver",versionId.ToString()), ("$page",t.PageNumber), ("$content",t.Content));
            await Index(t.Id, "Text", t.Content, 1);
        }
        foreach (var e in entities)
        {
            await Execute("INSERT INTO DocumentEntities (Id,DocumentId,VersionId,Type,Value,Confidence,PageNumber) VALUES ($id,$doc,$ver,$type,$value,$conf,$page)", ("$id",e.Id.ToString()), ("$doc",documentId.ToString()), ("$ver",versionId.ToString()), ("$type",e.Type), ("$value",e.Value), ("$conf",e.Confidence), ("$page",e.PageNumber));
            await Index(e.Id,"Entity",e.Value,2);
        }
        foreach (var m in metadata)
        {
            await Execute("INSERT INTO DocumentMetadata (Id,DocumentId,Key,Value) VALUES ($id,$doc,$key,$value)", ("$id",m.Id.ToString()), ("$doc",documentId.ToString()), ("$key",m.Key), ("$value",m.Value));
            await Index(m.Id,"Metadata",m.Value,0.5);
        }
        foreach (var i in images)
        {
            await Execute("INSERT INTO DocumentImages (Id,DocumentId,VersionId,PageNumber,FilePath,ThumbnailPath,ContentType,ExtractedAt,Hash,Role) VALUES ($id,$doc,$ver,$page,$path,$thumb,$type,$now,$hash,$role)", ("$id",i.Id.ToString()), ("$doc",documentId.ToString()), ("$ver",versionId.ToString()), ("$page",i.PageNumber), ("$path",i.FilePath), ("$thumb",i.ThumbnailPath), ("$type",i.ContentType), ("$now",i.ExtractedAt.ToString("o")), ("$hash",i.Hash), ("$role",i.Role));
            await Index(i.Id,"Image",Path.GetFileName(i.FilePath),0.2);
        }
        foreach (var t in tables)
            await Execute("INSERT INTO DocumentTables (Id,DocumentId,VersionId,PageNumber,Html) VALUES ($id,$doc,$ver,$page,$html)", ("$id",t.Id.ToString()), ("$doc",documentId.ToString()), ("$ver",versionId.ToString()), ("$page",t.PageNumber), ("$html",t.Html));
        await Execute("DELETE FROM EntityFTS; INSERT INTO EntityFTS (Type,Value) SELECT Type,Value FROM DocumentEntities; DELETE FROM MetadataFTS; INSERT INTO MetadataFTS (Key,Value) SELECT Key,Value FROM DocumentMetadata;");
        transaction.Commit();
    }
}
