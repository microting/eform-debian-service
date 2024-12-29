namespace MicrotingService.Infrastructure.Models;

public class ServiceAccount
{
    public string ProjectId { get; set; }
    public string PrivateKeyId { get; set; }
    public string PrivateKey { get; set; }
    public string ClientEmail { get; set; }
    public string ClientId { get; set; }
}