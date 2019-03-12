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
        public DaemonService(ILogger<DaemonService> logger)
        {
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            string serverConnectionString = "";
            
            var filePath = Path.Combine("connection.json");
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Enter database to use:");
                Console.WriteLine("> If left blank, it will use 'Microting'");
                Console.WriteLine("  Enter name of database to be used");
                string databaseName = Console.ReadLine();

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    if (databaseName.ToUpper() != "")
                        serverConnectionString = @"Server = localhost; port = 3306; Database = " + databaseName +
                                                 "; user = root; Convert Zero Datetime = true;";
                    if (databaseName.ToUpper() == "T")
                        serverConnectionString =
                            @"Server=localhost;port=3306;Database=MicrotingTest;user=root;Convert Zero Datetime=true;SslMode=none;";
                    if (databaseName.ToUpper() == "O")
                        serverConnectionString =
                            @"Server=localhost;port=3306;Database=MicrotingOdense;user=root;Convert Zero Datetime=true;SslMode=none;";
                    if (serverConnectionString == "")
                        serverConnectionString =
                            @"Server=localhost;port=3306;Database=420_SDK;user=root;Convert Zero Datetime=true;SslMode=none;";
                }
                else
                {
                    if (databaseName.ToUpper() != "")
                        serverConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=" + databaseName +
                                                 ";Integrated Security=True";
                    if (databaseName.ToUpper() == "T")
                        serverConnectionString =
                            @"Data Source=.\SQLEXPRESS;Initial Catalog=MicrotingTest;Integrated Security=True";
                    if (databaseName.ToUpper() == "O")
                        serverConnectionString =
                            @"Data Source=.\SQLEXPRESS;Initial Catalog=MicrotingOdense;Integrated Security=True";
                    if (serverConnectionString == "")
                        serverConnectionString =
                            @"Data Source=.\SQLEXPRESS;Initial Catalog=420_SDK;Integrated Security=True";
                }
            }
            else
            {
                var mainSettings = ConnectionStringManager.Read(filePath);
                serverConnectionString = mainSettings?.ConnectionStrings?.DefaultConnection.Replace("Angular", "SDK");
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