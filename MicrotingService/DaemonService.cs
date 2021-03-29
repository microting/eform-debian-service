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
                var filePath = Path.Combine("connection.json");
                if (!File.Exists(filePath))
                {
                    serverConnectionString =
                        @"Server=localhost;port=3306;Database=420_SDK;user=root;password=secretpassword;Convert Zero Datetime=true;SslMode=none;";
                }
                else
                {
                    var mainSettings = ConnectionStringManager.Read(filePath);
                    serverConnectionString = mainSettings?.ConnectionStrings?.DefaultConnection.Replace("Angular", "SDK");
                }
            }
            else
            {
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
