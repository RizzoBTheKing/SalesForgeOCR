# SalesForgeOCR

Windows desktop OCR for printed English documents. Imports DOCX/DOC, XLSX, PDFs, PNG/JPEG/BMP images,
single or multipage TIFFs, and UTF-8 text/CSV files. Office templates use installed Word/Excel once to preserve their printable layout. Exported SalesForge code uses PDFsharp only. Scanned PDF pages are rendered at
300 DPI and recognized with Tesseract; PDFs with a text layer use PdfPig extraction.
The English language model is included. No cloud OCR service or API key is required.

## Run

Open `publish/win-x64/SalesForgeOCR.UI.exe` after publishing. A self-contained publish includes the Windows Desktop runtime. A framework-dependent publish requires the .NET 9 Windows Desktop runtime.
On first launch, create a master password and log in. Select Upload, import supported
documents, then check Queue. Open Explorer and double-click a document to copy text or
export a UTF-8 text file. The Images tab previews extracted graphics; SalesForge code generates, copies or exports a C# method. See [OFFICE_TEMPLATE_GUIDE.md](OFFICE_TEMPLATE_GUIDE.md). Search supports literal phrases and prefix matching.

Database, durable source copies and logs are stored under `%LOCALAPPDATA%/SalesForgeOCR`.
Back up that whole directory with the application closed. The password controls application
login; files and SQLite data are **not encrypted**. Restrict Windows access to that directory.
Existing databases in other directories are not moved automatically.

## Build and verify

```powershell
dotnet build SalesForgeOCR.slnx
dotnet test test/SalesForgeOCR.Tests/SalesForgeOCR.Tests.csproj
dotnet publish src/SalesForgeOCR.UI/SalesForgeOCR.UI.csproj -c Release -r win-x64 --self-contained true -o publish/win-x64
```

Both solution formats include all projects. OCR, application and test projects target
Windows because their image handling uses Windows imaging APIs. A .NET 9 or newer SDK
can build this repository. Native OCR dependencies must be available; the regression
suite executes them rather than substituting mocked recognition.

## Behavior and limits

- Unsupported, empty, missing, corrupt or unreadable files fail visibly.
- Imports have a 100 MB size ceiling. Images have a 50 megapixel processing ceiling.
- The Settings file-size limit is additionally enforced during processing.
- Each job persists its status. Jobs interrupted by application shutdown are marked failed
  at next startup; import the file again to retry.
- Text, entities, metadata, images, tables and search entries are committed in one database
  transaction. Reprocessing a version replaces its prior artifacts rather than duplicating them.
- Word/Excel tables are extracted from their source packages. CSV/text rows are also supported; scanned tables are not reconstructed semantically.
- Automatic template classification and fuzzy search are not provided.
- Printed English is the tested OCR language. Handwriting, other languages, rotated/blurred
  scans, complex layouts and unusual PDFs require representative acceptance testing.
- A successful OCR job does not certify every recognized character. Review important text.

See `REVIEW_REPORT.md` for findings, fixes and validation evidence.
