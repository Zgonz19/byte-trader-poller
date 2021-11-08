using ByteTraderPoller.Connections;
using ByteTraderPoller.Services.AssetMonitoring;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ByteTraderPoller
{
    public static class ConfigurationManager
    {
        public static IConfiguration AppSetting { get; }
        static ConfigurationManager()
        {
            AppSetting = new ConfigurationBuilder()
                    .SetBasePath(@"C:\Users\Gonzalo\Dropbox\Github_08_2021\byte-trader-daily-jobs")
                    .AddJsonFile("appsettings.json")
                    .Build();
        }
    }
    public class ServiceManager
    {
        public ServiceManager()
        {

        }
        public async void StartServices()
        {
            var tasks = new List<Task>();
            var assetMonitor = new AssetMonitoringService();
            tasks.Add(assetMonitor.Initialize());
            await Task.WhenAll(tasks);
            Console.WriteLine("Service Execution Completed");
        }
    }
}
