using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MicrotingService
{
    public class DaemonService : IHostedService, IDisposable
    {
        private ServiceLogic _serviceLogic;
        private readonly IOptions<DaemonConfig> _config;
        public DaemonService(ILogger<DaemonService> logger, IOptions<DaemonConfig> config)
        {
            _config = config;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            string serverConnectionString = "";

            if (string.IsNullOrEmpty(_config.Value.ConnectionString))
            {
                Console.WriteLine("No connection string found in _config.Value.ConnectionString");
                var filePath = Path.Combine("connection.json");
                if (!File.Exists(filePath))
                {
                    Console.WriteLine("No connection string found in connection.json");
                    serverConnectionString =
                        @"Server=127.0.0.1;port=3306;Database=420_SDK;user=root;password=secretpassword;Convert Zero Datetime=true;SslMode=none;";
                }
                else
                {
                    Console.WriteLine("Found connection string in connection.json");
                    var mainSettings = ConnectionStringManager.Read(filePath);
                    serverConnectionString = mainSettings?.ConnectionStrings?.DefaultConnection.Replace("Angular", "SDK");
                    Console.WriteLine($"serverConnectionString: {serverConnectionString}");
                }
            }
            else
            {
                Console.WriteLine("Using connection string from config");
                Console.WriteLine($"Connection string: {_config.Value.ConnectionString}");
                serverConnectionString = _config.Value.ConnectionString;
            }

            _serviceLogic = new ServiceLogic();

            _serviceLogic.Start(serverConnectionString);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _serviceLogic.Stop();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
