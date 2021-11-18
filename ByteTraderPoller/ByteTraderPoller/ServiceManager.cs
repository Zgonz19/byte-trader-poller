using ByteTraderPoller.Connections;
using ByteTraderPoller.Services.AssetMonitoring;
using ByteTraderPoller.Services.QuoteStreaming;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
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
            var quoteStreaming = new QuoteStreamingService();

            var task1 = Task.Run(() => assetMonitor.Initialize());
            tasks.Add(task1);
            var task2 = Task.Run(() => quoteStreaming.Initialize());
            tasks.Add(task2);
            //quoteStreaming.Initialize();
            //assetMonitor.Initialize();

            //tasks.Add(quoteStreaming.Initialize());
            //tasks.Add(assetMonitor.Initialize());
            while (true)
            {
                Thread.Sleep(new TimeSpan(0, 5, 10));
            }
            await Task.WhenAll(tasks);
            Console.WriteLine("Service Execution Completed");
        }
    }
}
