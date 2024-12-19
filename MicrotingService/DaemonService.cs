using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MicrotingService.Infrastructure.Models;
using Newtonsoft.Json;
using Sentry;

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
                    var connectionJson = File.ReadAllText(filePath);
                    var serviceAccountWrapper = JsonConvert.DeserializeObject<ServiceAccountWrapper>(connectionJson);
                    var serviceAccount = serviceAccountWrapper.ServiceAccount;
                    var clientEmail = _config.Value.ClientEmail;
                    var privateKeyId = _config.Value.PrivateKeyId;
                    var privateKey = _config.Value.PrivateKey;
                    var clientId = _config.Value.ClientId;
                    var projectId = _config.Value.ProjectId;
                    if (serviceAccount != null)
                    {
                        if (string.IsNullOrEmpty(clientEmail))
                        {
                            clientEmail = serviceAccount.ClientEmail;
                        }

                        if (string.IsNullOrEmpty(privateKeyId))
                        {
                            privateKeyId = serviceAccount.PrivateKeyId;
                        }

                        if (string.IsNullOrEmpty(privateKey))
                        {
                            privateKey = serviceAccount.PrivateKey;
                        }

                        if (string.IsNullOrEmpty(clientId))
                        {
                            clientId = serviceAccount.ClientId;
                        }

                        if (string.IsNullOrEmpty(projectId))
                        {
                            projectId = serviceAccount.ProjectId;
                        }
                    }

                    Environment.SetEnvironmentVariable("CLIENT_EMAIL", clientEmail);
                    Console.WriteLine("CLIENT_EMAIL: " + clientEmail);
                    Environment.SetEnvironmentVariable("PRIVATE_KEY_ID", privateKeyId);
                    Console.WriteLine("PRIVATE_KEY_ID: " + privateKeyId);
                    Environment.SetEnvironmentVariable("PRIVATE_KEY", privateKey);
                    Console.WriteLine("PRIVATE_KEY: " + privateKey);
                    Environment.SetEnvironmentVariable("CLIENT_ID", clientId);
                    Console.WriteLine("CLIENT_ID: " + clientId);
                    Environment.SetEnvironmentVariable("PROJECT_ID", projectId);
                    Console.WriteLine("PROJECT_ID: " + projectId);
                }
            }
            else
            {
                Console.WriteLine("Using connection string from config");
                Console.WriteLine($"Connection string: {_config.Value.ConnectionString}");
                serverConnectionString = _config.Value.ConnectionString;
                var clientEmail = _config.Value.ClientEmail;
                var privateKeyId =_config.Value.PrivateKeyId;
                var privateKey = _config.Value.PrivateKey;
                var clientId = _config.Value.ClientId;
                var projectId = _config.Value.ProjectId;
                Environment.SetEnvironmentVariable("CLIENT_EMAIL", clientEmail);
                Console.WriteLine("CLIENT_EMAIL: " + clientEmail);
                Environment.SetEnvironmentVariable("PRIVATE_KEY_ID", privateKeyId);
                Console.WriteLine("PRIVATE_KEY_ID: " + privateKeyId);
                Environment.SetEnvironmentVariable("PRIVATE_KEY", privateKey);
                Console.WriteLine("PRIVATE_KEY: " + privateKey);
                Environment.SetEnvironmentVariable("CLIENT_ID", clientId);
                Console.WriteLine("CLIENT_ID: " + clientId);
                Environment.SetEnvironmentVariable("PROJECT_ID", projectId);
                Console.WriteLine("PROJECT_ID: " + projectId);
            }

            string pattern = @"Database=(\d+)_Angular;";
            Match match = Regex.Match(serverConnectionString!, pattern);

            if (match.Success)
            {
                string numberString = match.Groups[1].Value;
                int number = int.Parse(numberString);
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.SetTag("customerNo", number.ToString());
                    Console.WriteLine("customerNo: " + number);
                    scope.SetTag("osVersion", Environment.OSVersion.ToString());
                    Console.WriteLine("osVersion: " + Environment.OSVersion);
                    scope.SetTag("osArchitecture", RuntimeInformation.OSArchitecture.ToString());
                    Console.WriteLine("osArchitecture: " + RuntimeInformation.OSArchitecture);
                    scope.SetTag("osName", RuntimeInformation.OSDescription);
                    Console.WriteLine("osName: " + RuntimeInformation.OSDescription);
                });
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