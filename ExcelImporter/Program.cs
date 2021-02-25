using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;

namespace ExcelImporter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "ExcelImporter";
            //arguments
            
            var logger = LogManager.GetCurrentClassLogger();
            try
            {
                var services = new ServiceCollection();
                new Startup(args).ConfigureServices(services);

                logger.Debug("Required services configured");
                await using var servicesProvider = services.BuildServiceProvider();
                logger.Info("Starting...");
                //servicesProvider.GetRequiredService<ConsoleApp>().Run();
                await servicesProvider.GetRequiredService<RowDtoExcelReader>().RunAsync("D:\\Temp\\test.xlsx", CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Stopped program because of exception");
                throw;
            }
            finally
            {
                logger.Info("Exit");
                LogManager.Shutdown();
            }
        }
    }

    public class Startup
    {

        public static bool ConfigurationDebug
#if DEBUG
            => true;
#else
            => false;
#endif
        private readonly IConfigurationRoot _configuration;

        public Startup(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddCommandLine(args);

            _configuration = builder.Build();
            LogManager.Configuration = new NLogLoggingConfiguration(_configuration.GetSection("NLog"));

        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(loggingBuilder =>
            {
                // configure Logging with NLog
                loggingBuilder.ClearProviders();
                loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                loggingBuilder.AddNLog(_configuration);
            });

            services.AddTransient<RowDtoExcelReader>();

            //for IOptions
            services.AddOptions();
            var consoleSection = _configuration.GetSection("ConsoleConfig");
            services.Configure<ConsoleConfig>(consoleSection);

            //needed if not using IOptions
            services.AddSingleton(consoleSection.Get<ConsoleConfig>());
        }
    }
}
