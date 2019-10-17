using System;
using System.IO;
using Newtonsoft.Json;

namespace MicrotingService
{
    public class ConnectionStrings
    {
        public string DefaultConnection { get; set; }
    }
    
    public class MainSettings
    {
        public ConnectionStrings ConnectionStrings { get; set; }
            = new ConnectionStrings();
    }
    
    public class ConnectionStringManager
    {
        public static MainSettings Read(string filePath)
        {
            try
            {
                var deserializedProduct = JsonConvert.DeserializeObject<MainSettings>(File.ReadAllText(filePath));
                return deserializedProduct;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}