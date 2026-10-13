using System.Runtime.InteropServices;
using System.Diagnostics;
using PdfSharp.Pdf.IO;
using System.Security.Cryptography;

namespace SalesForgeOCR.DocumentProcessing.Engines;

/// <summary>Authoring-time rendering only. Exported SalesForge methods never use COM.</summary>
public static class OfficePdfRenderer
{
    private static readonly SemaphoreSlim Gate = new(1,1);
    public static async Task<string> RenderAsync(string source,CancellationToken cancellationToken=default)
    {
        source=Path.GetFullPath(source);
        string hash;
        using(var input=File.OpenRead(source)) hash=Convert.ToHexString(await SHA256.HashDataAsync(input,cancellationToken));
        var cache=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SalesForgeOCR","OfficeRenderCache");Directory.CreateDirectory(cache);
        var output=Path.Combine(cache,"print-areas-v2-"+hash+".pdf");
        if(File.Exists(output))return output;
        await Gate.WaitAsync(cancellationToken);
        try
        {
            if(File.Exists(output))return output;
            var completed=new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var thread=new Thread(()=>
            {
                var temporary=output+".tmp.pdf";
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var extension=Path.GetExtension(source).ToLowerInvariant();
                    if(extension==".xlsx")RenderExcel(source,temporary,cancellationToken);
                    else if(extension is ".docx" or ".doc")RenderWord(source,temporary,cancellationToken);
                    else throw new NotSupportedException("Office rendering accepts DOCX, DOC and XLSX files.");
                    cancellationToken.ThrowIfCancellationRequested();
                    File.Move(temporary,output,true);completed.SetResult(output);
                }
                catch(Exception error){completed.SetException(new InvalidOperationException("Exact-layout template rendering failed. Check that Word/Excel can open this document and use an installed printer driver. "+error.Message,error));}
                finally{if(File.Exists(temporary))File.Delete(temporary);}
            }){IsBackground=true};
            thread.SetApartmentState(ApartmentState.STA);thread.Start();
            return await completed.Task;
        }
        finally{Gate.Release();}
    }
    private static void Release(object? value){if(value!=null&&Marshal.IsComObject(value))Marshal.FinalReleaseComObject(value);}
    private static void RenderExcel(string source,string output,CancellationToken cancellationToken)
    {
        var existingProcesses=ProcessIds("EXCEL");bool ownsApplication=false;
        object? app=null,books=null,book=null;
        try
        {
            var type=Type.GetTypeFromProgID("Excel.Application")??throw new InvalidOperationException("Microsoft Excel is not installed.");
            app=Activator.CreateInstance(type)!;dynamic excel=app;
            excel.Visible=false;excel.DisplayAlerts=false;excel.EnableEvents=false;excel.AutomationSecurity=3;
            using var guard=new OfficeProcessGuard((int)excel.Hwnd,existingProcesses,cancellationToken);
            ownsApplication=guard.OwnsProcess;
            if(!ownsApplication)throw new InvalidOperationException("Excel did not create an isolated authoring process.");
            books=excel.Workbooks;dynamic workbooks=books;
            book=workbooks.Open(source,UpdateLinks:0,ReadOnly:true,AddToMru:false,
                Password:Guid.NewGuid().ToString("N"),IgnoreReadOnlyRecommended:true,Notify:false);dynamic workbook=book;
            // Excel can select a driver only after a workbook is open. This changes
            // this automation instance, never the Windows default printer.
            using(var devices=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\Devices"))
                if(devices?.GetValue("Microsoft Print to PDF") is string mapping)
                    excel.ActivePrinter="Microsoft Print to PDF on "+mapping.Split(',')[1];
            // Preserve the source's own page setup, print areas, breaks and header/footer artwork.
            var printable=new List<object>();object worksheets=workbook.Worksheets;
            try
            {
                dynamic sheets=worksheets;
                for(int i=1;i<=sheets.Count;i++)
                {
                    object sheet=sheets[i];object? setup=null;
                    try
                    {
                        dynamic ws=sheet;setup=ws.PageSetup;
                        if((int)ws.Visible==-1 && !string.IsNullOrWhiteSpace((string)((dynamic)setup).PrintArea))printable.Add(sheet);
                        else Release(sheet);
                    }
                    finally{Release(setup);}
                }
                if(printable.Count==0)workbook.ExportAsFixedFormat(0,output,0,true,false,Type.Missing,Type.Missing,false);
                else
                {
                    // Explicit print areas identify document pages; input/calculation
                    // sheets are not part of the printable template when areas exist.
                    using var merged=new PdfSharp.Pdf.PdfDocument();
                    for(int i=0;i<printable.Count;i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();var part=output+".sheet-"+i+".pdf";
                        try
                        {
                            ((dynamic)printable[i]).ExportAsFixedFormat(0,part,0,true,false,Type.Missing,Type.Missing,false);
                            using var pdf=PdfReader.Open(part,PdfDocumentOpenMode.Import);
                            foreach(var page in pdf.Pages)merged.AddPage(page);
                        }
                        finally{if(File.Exists(part))File.Delete(part);}
                    }
                    merged.Save(output);
                }
            }
            finally{foreach(var sheet in printable)Release(sheet);Release(worksheets);}
        }
        finally
        {
            if(book!=null){try{((dynamic)book).Close(false);}catch{}Release(book);}
            Release(books);if(app!=null){if(ownsApplication){try{((dynamic)app).Quit();}catch{}}Release(app);}
        }
    }
    private static HashSet<int> ProcessIds(string name)
    {
        var result=new HashSet<int>();foreach(var process in Process.GetProcessesByName(name))
            using(process)result.Add(process.Id);return result;
    }
    // A hung Office dialog must not leave a processing job waiting forever. This
    // guard owns only the process created by this automation instance.
    private sealed class OfficeProcessGuard : IDisposable
    {
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint processId);
        private readonly Process _process;private readonly Timer _timer;private readonly CancellationTokenRegistration _cancellation;
        private int _stopped;public bool OwnsProcess { get; }
        public OfficeProcessGuard(int hwnd,HashSet<int> existingProcesses,CancellationToken cancellationToken)
        {
            GetWindowThreadProcessId(new IntPtr(hwnd),out var id);
            _process=Process.GetProcessById((int)id);OwnsProcess=!existingProcesses.Contains((int)id);
            _timer=new Timer(_=>Stop(),null,TimeSpan.FromMinutes(2),Timeout.InfiniteTimeSpan);
            _cancellation=cancellationToken.Register(Stop);
        }
        private void Stop()
        {
            if(!OwnsProcess || Interlocked.Exchange(ref _stopped,1)!=0)return;
            try{if(!_process.HasExited)_process.Kill();}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}
        }
        public void Dispose(){_timer.Dispose();_cancellation.Dispose();_process.Dispose();}
    }
    private static void RenderWord(string source,string output,CancellationToken cancellationToken)
    {
        var existingProcesses=ProcessIds("WINWORD");bool ownsApplication=false;
        object? app=null,documents=null,document=null;
        try
        {
            var type=Type.GetTypeFromProgID("Word.Application")??throw new InvalidOperationException("Microsoft Word is not installed.");
            app=Activator.CreateInstance(type)!;ownsApplication=ProcessIds("WINWORD").Except(existingProcesses).Any();dynamic word=app;word.Visible=false;word.DisplayAlerts=0;word.AutomationSecurity=3;
            documents=word.Documents;dynamic docs=documents;
            document=docs.Open(FileName:source,ConfirmConversions:false,ReadOnly:true,AddToRecentFiles:false,Visible:false,PasswordDocument:Guid.NewGuid().ToString("N"));
            using var guard=new OfficeProcessGuard((int)((dynamic)document).ActiveWindow.Hwnd,existingProcesses,cancellationToken);
            ownsApplication=guard.OwnsProcess;
            if(!ownsApplication)throw new InvalidOperationException("Word did not create an isolated authoring process.");
            ((dynamic)document).ExportAsFixedFormat(output,17,OpenAfterExport:false);
        }
        finally
        {
            if(document!=null){try{((dynamic)document).Close(0);}catch{}Release(document);}
            Release(documents);if(app!=null){if(ownsApplication){try{((dynamic)app).Quit(0);}catch{}}Release(app);}
        }
    }
}


