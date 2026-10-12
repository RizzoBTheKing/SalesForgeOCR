using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SalesForgeOCR.Data;
using SalesForgeOCR.Infrastructure.Logging;
using System;
using System.Windows.Forms;

namespace SalesForgeOCR.UI
{
    internal static class Program
    {
        public static IHost Host { get; private set; } = null!;

        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var builder = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // ensure WinForms Application initialization is performed when host is built
                    services.AddSingleton<System.Func<System.Windows.Forms.Form>>(sp => () => sp.GetRequiredService<SalesForgeOCR.UI.Forms.MainForm>());
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // UI-related services
                    services.AddSingleton<System.Windows.Forms.Form, SalesForgeOCR.UI.Forms.MainForm>();
                    services.AddSingleton<DatabaseInitializer>();
                    // Data repositories
                    services.AddSingleton<SalesForgeOCR.Data.Repositories.SqliteDocumentRepository>();
                    services.AddSingleton<SalesForgeOCR.Data.Repositories.AuditRepository>();
                    services.AddSingleton<SalesForgeOCR.Data.Repositories.SettingsRepository>();
                    services.AddSingleton<SalesForgeOCR.Data.Repositories.SessionRepository>();
                    services.AddSingleton<SalesForgeOCR.Data.Repositories.ProcessingJobRepository>();
                    services.AddSingleton<SalesForgeOCR.Data.Repositories.DocumentTextRepository>();
                    services.AddSingleton<SalesForgeOCR.Data.Repositories.DocumentImageRepository>();
                    services.AddSingleton<SalesForgeOCR.Data.Repositories.DocumentImageLocationRepository>();
                    services.AddSingleton<SalesForgeOCR.Data.Repositories.DocumentEntityRepository>();
                    services.AddSingleton<SalesForgeOCR.Data.Repositories.DocumentTableRepository>();
                    services.AddSingleton<SalesForgeOCR.Data.Repositories.DocumentMetadataRepository>();
                    services.AddSingleton<SalesForgeOCR.Data.Repositories.DocumentTemplateRepository>();

                    // Core interface bindings
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.IDocumentRepository>(sp => sp.GetRequiredService<SalesForgeOCR.Data.Repositories.SqliteDocumentRepository>());
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.IDocumentTextRepository>(sp => sp.GetRequiredService<SalesForgeOCR.Data.Repositories.DocumentTextRepository>());
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.IDocumentImageRepository>(sp => sp.GetRequiredService<SalesForgeOCR.Data.Repositories.DocumentImageRepository>());
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.IDocumentImageLocationRepository>(sp => sp.GetRequiredService<SalesForgeOCR.Data.Repositories.DocumentImageLocationRepository>());
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.IDocumentEntityRepository>(sp => sp.GetRequiredService<SalesForgeOCR.Data.Repositories.DocumentEntityRepository>());
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.IDocumentTableRepository>(sp => sp.GetRequiredService<SalesForgeOCR.Data.Repositories.DocumentTableRepository>());
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.IDocumentMetadataRepository>(sp => sp.GetRequiredService<SalesForgeOCR.Data.Repositories.DocumentMetadataRepository>());
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.IDocumentTemplateRepository>(sp => sp.GetRequiredService<SalesForgeOCR.Data.Repositories.DocumentTemplateRepository>());
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.IProcessingJobRepository>(sp => sp.GetRequiredService<SalesForgeOCR.Data.Repositories.ProcessingJobRepository>());

                    // Application services
                    services.AddSingleton<SalesForgeOCR.Application.Services.DocumentService>();
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.IJobQueue, SalesForgeOCR.Application.Background.BackgroundJobQueue>();
                    services.AddSingleton<SalesForgeOCR.Application.Services.DocumentTextService>();
                    services.AddSingleton<SalesForgeOCR.Application.Services.DocumentImageService>();
                    services.AddSingleton<SalesForgeOCR.Application.Services.DocumentEntityService>();
                    services.AddSingleton<SalesForgeOCR.Application.Services.DocumentTableService>();
                    services.AddSingleton<SalesForgeOCR.Application.Services.DocumentMetadataService>();
                    services.AddSingleton<SalesForgeOCR.Application.Services.DocumentTemplateService>();
                    services.AddSingleton<SalesForgeOCR.Application.Services.ProcessingJobService>();
                    services.AddSingleton<SalesForgeOCR.Application.Services.DocumentRetrievalService>();

                    // Infrastructure services
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.IAuditService, SalesForgeOCR.Infrastructure.Audit.AuditService>(sp => new SalesForgeOCR.Infrastructure.Audit.AuditService(sp.GetRequiredService<SalesForgeOCR.Data.Repositories.AuditRepository>()));
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.ISettingsService, SalesForgeOCR.Infrastructure.Configuration.SettingsService>(sp => new SalesForgeOCR.Infrastructure.Configuration.SettingsService(sp.GetRequiredService<SalesForgeOCR.Data.Repositories.SettingsRepository>()));

                    // Security
                    services.AddSingleton<SalesForgeOCR.Security.AuthenticationService>();
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.IAuthenticationService>(sp => sp.GetRequiredService<SalesForgeOCR.Security.AuthenticationService>());
                    services.AddSingleton<SalesForgeOCR.Security.SessionManager>();

                    // Search and processing engines
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.ISearchEngine, SalesForgeOCR.Search.SqliteFtsSearchEngine>();
                    services.AddSingleton<SalesForgeOCR.Search.SearchIndexService>();
                    services.AddSingleton<SalesForgeOCR.Search.SearchService>();

                    // UI Views and Navigation
                    services.AddSingleton<SalesForgeOCR.UI.Forms.DashboardView>();
                    services.AddSingleton<SalesForgeOCR.UI.Forms.UploadView>();
                    services.AddSingleton<SalesForgeOCR.UI.Forms.DocumentExplorerView>();
                    services.AddSingleton<SalesForgeOCR.UI.Forms.SearchView>();
                    services.AddSingleton<SalesForgeOCR.UI.Forms.QueueView>();
                    services.AddSingleton<SalesForgeOCR.UI.Forms.AuditView>();
                    services.AddSingleton<SalesForgeOCR.UI.Forms.SettingsView>();
                    services.AddSingleton<SalesForgeOCR.UI.Forms.DocumentViewerForm>();
                    services.AddSingleton<SalesForgeOCR.UI.Forms.MainForm>();
                    services.AddSingleton<SalesForgeOCR.UI.Forms.FirstRunWizardForm>();
                    services.AddSingleton<SalesForgeOCR.UI.Forms.LoginForm>();
                    services.AddSingleton<SalesForgeOCR.UI.Forms.MasterPasswordForm>();

                    // Navigation service factory for MainForm host panel will be created at runtime
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.IOcrEngine, SalesForgeOCR.DocumentProcessing.Engines.OcrEngine>();
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.IImageExtractor, SalesForgeOCR.DocumentProcessing.Engines.ImageExtractor>();
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.ITableExtractor, SalesForgeOCR.DocumentProcessing.Engines.TableExtractor>();
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.IEntityExtractor, SalesForgeOCR.DocumentProcessing.Engines.RegexEntityExtractor>();
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.IMetadataExtractor, SalesForgeOCR.DocumentProcessing.Engines.MetadataExtractor>();
                    services.AddSingleton<SalesForgeOCR.Core.Interfaces.IThumbnailGenerator, SalesForgeOCR.DocumentProcessing.Engines.ThumbnailGenerator>();

                    // Processing pipeline and stages
                    services.AddSingleton<SalesForgeOCR.DocumentProcessing.Processing.ProcessingPipeline>();
                    services.AddSingleton<SalesForgeOCR.DocumentProcessing.Processing.ProcessorFactory>(sp =>
                    {
                        var factory = new SalesForgeOCR.DocumentProcessing.Processing.ProcessorFactory();
                        // Register default stages with resolved services
                        factory.Register(sp.GetRequiredService<SalesForgeOCR.DocumentProcessing.Processing.Stages.ValidationStage>());
                        factory.Register(sp.GetRequiredService<SalesForgeOCR.DocumentProcessing.Processing.Stages.ImageExtractionStage>());
                        factory.Register(sp.GetRequiredService<SalesForgeOCR.DocumentProcessing.Processing.Stages.TextExtractionStage>());
                        factory.Register(sp.GetRequiredService<SalesForgeOCR.DocumentProcessing.Processing.Stages.TableExtractionStage>());
                        factory.Register(sp.GetRequiredService<SalesForgeOCR.DocumentProcessing.Processing.Stages.EntityExtractionStage>());
                        factory.Register(sp.GetRequiredService<SalesForgeOCR.DocumentProcessing.Processing.Stages.MetadataExtractionStage>());
                        factory.Register(sp.GetRequiredService<SalesForgeOCR.DocumentProcessing.Processing.Stages.ClassificationStage>());
                        factory.Register(sp.GetRequiredService<SalesForgeOCR.DocumentProcessing.Processing.Stages.IndexingStage>());
                        factory.Register(sp.GetRequiredService<SalesForgeOCR.DocumentProcessing.Processing.Stages.StorageStage>());
                        return factory;
                    });
                    services.AddSingleton<SalesForgeOCR.DocumentProcessing.Processing.Stages.ValidationStage>();
                    services.AddSingleton<SalesForgeOCR.DocumentProcessing.Processing.Stages.ImageExtractionStage>();
                    services.AddSingleton<SalesForgeOCR.DocumentProcessing.Processing.Stages.TextExtractionStage>();
                    services.AddSingleton<SalesForgeOCR.DocumentProcessing.Processing.Stages.TableExtractionStage>();
                    services.AddSingleton<SalesForgeOCR.DocumentProcessing.Processing.Stages.EntityExtractionStage>();
                    services.AddSingleton<SalesForgeOCR.DocumentProcessing.Processing.Stages.MetadataExtractionStage>();
                    services.AddSingleton<SalesForgeOCR.DocumentProcessing.Processing.Stages.ClassificationStage>();
                    services.AddSingleton<SalesForgeOCR.DocumentProcessing.Processing.Stages.IndexingStage>();
                    services.AddSingleton<SalesForgeOCR.DocumentProcessing.Processing.Stages.StorageStage>();

                    // Register document processing job
                    services.AddSingleton<SalesForgeOCR.DocumentProcessing.Processing.DocumentProcessingJob>(sp =>
                    {
                        var pipeline = new SalesForgeOCR.DocumentProcessing.Processing.ProcessingPipeline();
                        foreach (var stage in sp.GetRequiredService<SalesForgeOCR.DocumentProcessing.Processing.ProcessorFactory>().CreateDefaultPipeline())
                        {
                            pipeline.AddStage(stage);
                        }
                        return new SalesForgeOCR.DocumentProcessing.Processing.DocumentProcessingJob(pipeline, sp.GetRequiredService<SalesForgeOCR.Core.Interfaces.IDocumentRepository>());
                    });




                .UseSerilog((hostingContext, services, loggerConfiguration) =>
                {
                    var logger = SerilogLogger.CreateLogger();
                    Log.Logger = logger;
                });

            var host = builder.Build();

            using (var scope = host.Services.CreateScope())
            {
                var dbInit = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
                dbInit.InitializeAsync().GetAwaiter().GetResult();
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Determine whether master password exists and show FirstRunWizard or Login
                        using (var scope2 = host.Services.CreateScope())
                        {
                            var settings = scope2.ServiceProvider.GetRequiredService<SalesForgeOCR.Core.Interfaces.ISettingsService>();
                            var auth = scope2.ServiceProvider.GetRequiredService<SalesForgeOCR.Core.Interfaces.IAuthenticationService>();
                            var mp = settings.GetAsync("__MASTER_PASSWORD__").GetAwaiter().GetResult();
                            if (mp == null)
                            {
                                var wizard = scope2.ServiceProvider.GetRequiredService<SalesForgeOCR.UI.Forms.FirstRunWizardForm>();
                                if (wizard.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                                {
                                    var login = scope2.ServiceProvider.GetRequiredService<SalesForgeOCR.UI.Forms.LoginForm>();
                                    if (login.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                                    {
                                        var main = scope2.ServiceProvider.GetRequiredService<SalesForgeOCR.UI.Forms.MainForm>();
                                        Application.Run(main);
                                    }
                                }
                            }
                            else
                            {
                                var login = scope2.ServiceProvider.GetRequiredService<SalesForgeOCR.UI.Forms.LoginForm>();
                                if (login.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                                {
                                    var main = scope2.ServiceProvider.GetRequiredService<SalesForgeOCR.UI.Forms.MainForm>();
                                    Application.Run(main);
                                }
                            }
                        }
        }
    }
}
