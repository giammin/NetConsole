using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreConsole
{
    public class ConsoleApp
    {
        private readonly ConsoleConfig _consoleConfig;
        private readonly ILogger<ConsoleApp> _logger;
        private readonly ConsoleConfig _config;

        public ConsoleApp(IOptions<ConsoleConfig> configuration, ConsoleConfig consoleConfig, ILogger<ConsoleApp> logger)
        {
            _consoleConfig = consoleConfig;
            _logger = logger;
            _config = configuration.Value;
        }
        
        public void Run()
        {
            _logger.LogInformation("Run");
            _logger.LogInformation($"IOptions: {_config.Setting1}");
            _logger.LogInformation($"DI: {_consoleConfig.Setting1}");
        }
        
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("RunAsync");
            _logger.LogInformation($"IOptions: {_config.Setting1}");
            _logger.LogInformation($"DI: {_consoleConfig.Setting1}");
        }

    }
}