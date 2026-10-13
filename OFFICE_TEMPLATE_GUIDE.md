# Office templates and SalesForge code

Upload DOCX, DOC or XLSX from Upload. Word/Excel must be installed on this OCR authoring PC. Office opens the source read-only with macros disabled, renders its printable pages once and closes its automation instance. Repeated imports/code generation reuse a private content-hash cache under `%LOCALAPPDATA%/SalesForgeOCR/OfficeRenderCache`.

Excel uses explicitly defined print areas when available, excluding calculation/input sheets. Set print areas in your workbook before import if necessary. Office-rendered layout reflects the source's current print setup and installed fonts. The Windows default printer is unchanged. Import failures remain visible in Queue; encrypted files and documents requiring interactive prompts need to be resolved in Office first.

Open the document in Explorer, check extracted text and Images, then select **SalesForge code**. Enter a unique method name beginning with Generate and choose Generate code. Copy the method inside the SalesForge form class, or Export code to get a partial-class source file. Add it to SalesForge and install PDFsharp-GDI 6.2.4 if that project does not already reference it. Export includes renderer helper sources for projects that need them; do not add duplicates. When replacing an existing method, remove/replace that method before adding the generated one.

PDF/Office code embeds original PDF page artwork, fonts, logos, vectors, headers and footers. SalesForge runtime never starts Office and does not depend on the original uploaded template. Generated methods use SalesForge's GetFileName and MarkFileSaved helpers.

## Live fields

Use explicit placeholders in blank templates. Completed customer documents remain static and retain their personal text: the program does not infer which details to replace. Review the generated code before using it.

Supported placeholders are:

CustomerName, CompanyName, Branch, SalesExecutive, DeliveryDate, SellingPrice, CustomerID, Email, MobileNumber, CustomerAddress, PostalAddress, BPID, ChassisNumber, Make, Model, StockNumber, RegistrationNumber, EngineNumber, Colour, ModelYear, Kilometres, Total, Deposit, AmountDue, Notes, TradeIn1Make, TradeIn1Model, TradeIn1Registration, TradeIn2Make, TradeIn2Model and TradeIn2Registration.

Wrap names in double braces, for example `{{CustomerName}}`. Keep each placeholder on one line on a white/blank field background, with enough width for the replacement. Only that rectangle is filled; surrounding artwork remains in place. Longer replacements reduce their font size to fit. Replacement font appearance may differ from the template; verify it visually. Unknown placeholders remain static and need manual bindings. Automatic binding of arbitrary form fields, rich coloured fields or handwriting is not provided.

The reconstructed-grid generator is an explicit developer option, not the default UI path. It approximates Office layouts and requires visual validation. Default PDF-preservation output requires no grid helpers.

## Git and another PC using Visual Studio 2026

Commit and push the complete reviewed repository changes through Git Changes. On the other PC, clone/pull, open `SalesForgeOCR.slnx`, build, then right-click the UI project → Publish → Folder. Choose Release, win-x64 and Self-contained. Both .sln and .slnx contain the same projects; you push the whole change, not only one solution file. Distribute the entire published folder, including tessdata and CodeTemplates. Word/Excel are required where Office templates are imported, not where the generated SalesForge methods run.

## Validation

30 regression tests passed, including actual OCR, package text/images, compiled exported C#, live fields and pixel/page-size preservation. All 18 supplied PDFs passed pixel equality at 96 DPI. The supplied Word document rendered to 5 pages/5 images, quotation XLSX to 1 printed page/2 images and titleholder XLSX to 2 pages/3 images. A complete synthetic DOCX rendered with header/footer logos. These results do not guarantee perfect recognition or exact appearance of all filled customer variants. Review representative documents before production.
