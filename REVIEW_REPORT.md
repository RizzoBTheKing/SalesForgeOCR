# SalesForgeOCR review and repair report

Date: 13 October 2026

## Result

The original application had build blockers and disconnected OCR workflows. These were
repaired in place. The tested workflow now imports a durable copy, runs real English OCR,
saves extracted artifacts and search entries atomically, exposes processing failures, and
supports viewing, copying, exporting and searching the result.

This is a validated working baseline, not a certification that every possible document
or machine will work perfectly. No user production document corpus was supplied.

## Findings and changes

| Area | Original fault | Repair |
| --- | --- | --- |
| Solutions | Both solution files had no projects; solution test command did no work. | Registered all source and test projects in both formats. |
| Build references | Missing references and a circular Data/Search dependency. | Corrected references; SQLite index implementation compiles in Data while retaining its public namespace. |
| Source integrity | Duplicated/trailing source in metadata and search index files. | Removed malformed duplicate blocks. |
| Dependencies | Nonexistent package versions, downgrade conflicts and vulnerable direct/indirect dependencies. | Resolved real package versions, replaced vulnerable image handling with Windows imaging, updated SQLite, packaging and hosting/JSON dependencies. |
| Startup | Malformed host builder, unassigned static host and Application namespace collision. | Corrected startup and exposed host construction for verification. |
| First run | Password setup service was never registered. | Registered and verified setup service construction. |
| Login | A successful login inserted the same session twice. | Create the session once; verified setup, password checks and session persistence. |
| Document identity | Document had no CurrentVersionId and default IDs were empty. | Added current-version field and safe ID/time defaults. |
| Imports | Uploads created database jobs but never sent them to the worker. | Added import service, durable copying and actual enqueueing. |
| File paths | Processing used only a filename; stored paths disappeared on reload. | Persist and reload StoragePath with an idempotent schema migration. |
| PDF OCR | Scanned PDFs had no text extraction fallback; raw image bytes were saved as PNG. | Render complete scanned pages with PDFium at 300 DPI and recognize them with Tesseract. |
| Digital PDFs | Extraction lost layout spacing and depended on image extraction. | Extract per-page text directly using PdfPig's text extractor. |
| Image OCR | Exceptions were silently converted to empty strings; no language model was included. | Bundle English model and license; propagate actionable failures. |
| TIFF | Only one frame was recognized. | Recognize each frame in page order. |
| Failure status | Pipeline failure could still mark a job completed. | Failed results become failed jobs with persisted errors. |
| Pending jobs | Empty timestamps threw when jobs were loaded. | Treat empty/null timestamps as absent. |
| Shutdown | Worker cancellation/disposal races; interrupted jobs stayed pending forever. | Safe worker lifetime, visible failure on interrupted jobs at next startup. |
| Artifact storage | Version duplicate inserts, text double indexing, partial writes and dropped tables. | One transaction stores all extracted artifacts and matching indexes; replacement is idempotent. |
| Entities | Every extracted entity was labeled page 1. | Retain the actual source page. |
| Search | Raw punctuation/quotes broke FTS; sources, pagination and sorting were ignored. | Escaped literal queries, source filters, whole-document pagination, sorting and supported filter fields. |
| Index rebuild | A reader on one connection could block index writes on another. | Single connection/transaction rebuild and document reindex. |
| Deletions | Deleting entities/metadata removed all document search hits and ran invalid wildcard SQL. | Restrict deletion to the relevant source type and rebuild legacy FTS from remaining records. |
| UI | Empty dashboard, obscured docked controls, noneditable settings, no queue refresh. | Actual counts, corrected layout, editable settings and refresh actions. |
| Text viewer | Text was in single-line list items, with no practical export. | Read-only selectable text area and UTF-8 export. |
| Errors | Several UI event handlers could throw out of the event loop. | Visible failure reporting for affected search, settings, queue and export operations. |
| Logging | Invalid logging configuration and disconnected file logging. | Correct configuration and host logging hookup. |
| Input handling | Unsupported/empty files admitted; unbounded regex execution. | Supported-format checks, file/image limits, regex timeouts and cancellation checks. |
| Tests | Missing test SDK and shared test database files blocked repeat runs. | Runnable test project, pool cleanup and 21 new regression tests. |

## Validation

24 automated tests pass with no skipped tests. They exercise actual recognition rather
than mocking Tesseract, and include:

- Printed PNG image recognition, scanned PDF rendering/OCR, two-page digital PDF spacing,
  and two-page TIFF recognition.
- Explicit missing language model, missing file, unsupported format, corrupt image and
  cancellation handling.
- Startup service registration and construction of every registered form/view on a
  Windows STA thread.
- Password setup/verification and session persistence/invalidation.
- Full import, background processing, durable storage and search after deleting the
  original source file.
- Failed jobs remaining failed rather than appearing completed.
- Database initialization for an existing empty file and repeat initialization.
- Storage-path roundtrip, pending timestamps and interrupted job recovery.
- Transaction rollback and duplicate-free artifact replacement.
- Index rebuild/reindex, source filtering, punctuation, pagination and entity deletion
  preserving unrelated text and other documents.
- Worker survival after an individual job exception and disposal handling.

The Windows x64 release is published at `publish/win-x64/SalesForgeOCR.UI.exe`.
The output includes English traineddata, Tesseract/Leptonica native libraries and PDFium.
The release requires the .NET 9 Windows Desktop runtime, present on this machine.

## Practical limits and unverified areas

- OCR quality was tested on controlled printed-English fixtures, not the user's actual
  files. Handwriting, damaged/rotated scans, uncommon fonts, unusual PDF encodings,
  encrypted PDFs, other languages and complex layouts have not been certified.
- Recognition can produce incorrect characters even when a job completes. There is no
  measured production-corpus accuracy score, and this report does not claim 100% accuracy.
- Form construction and service workflows are tested; a complete manual click-through,
  visual layout inspection, accessibility audit and installer/clean-machine test were not performed.
- The master password gates application access. SQLite records and stored documents remain
  unencrypted; Windows account access and whole-directory backups remain relevant.
- Interrupted jobs are marked failed, not automatically retried. Reimport to retry.
- The import ceiling is 100 MB, with a 50 megapixel image ceiling; very large collections
  and adversarial native-parser resource consumption need further load/fuzz testing.
- CSV/text tables are basic rows, not semantic table recognition from scans. Automatic
  template classification and fuzzy search are not implemented.
- Native parser security cannot be guaranteed by a NuGet package audit alone.

## Dependency and model provenance

The review checked official package pages and security advisories when selecting repairs:

- [PdfPig package and text-extraction guidance](https://www.nuget.org/packages/PdfPig)
- [PDFtoImage renderer](https://github.com/sungaila/PDFtoImage)
- [SQLitePCLRaw bundle](https://www.nuget.org/packages/SQLitePCLRaw.bundle_e_sqlite3)
- [SQLite native dependency advisory](https://github.com/advisories/GHSA-2m69-gcr7-jv3q)
- [Packaging advisory](https://github.com/advisories/GHSA-f32c-w444-8ppv)
- [JSON advisory](https://github.com/advisories/GHSA-hh2w-p6rv-4g7w)
- [Official English language model](https://github.com/tesseract-ocr/tessdata_fast)

Model SHA-256: `7d4322bd2a7749724879683fc3912cb542f19906c83bcc1a52132556427170b2`.
The upstream Apache 2.0 license is included with the model.

## Final checks

- Full Release solution build: succeeded, 0 warnings, 0 errors.
- Release regression suite after dependency updates: 24 passed, 0 failed, 0 skipped.
- Direct and transitive dependency audit: no known vulnerable packages reported for any of the ten projects on 13 October 2026.
- Added single-instance startup protection to prevent competing instances from interfering with job recovery.
- Final Windows x64 publish succeeded; required native OCR libraries, PDF renderer, English model and license are present.
