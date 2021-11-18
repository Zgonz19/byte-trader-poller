using ByteTraderPoller.Connections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ByteTraderPoller.Services.AssetMonitoring
{
    public class AssetTrackerManager
    {
        public bool EnableMonitor = true;
        public List<AssetTracker> ActiveTrackers = new List<AssetTracker>();
        public List<Task> TrackerTasks = new List<Task>();
        public ByteTraderRepository Repo = new ByteTraderRepository();
        public EmailEngine Emails = new EmailEngine();
        public AmeritradeApiWrapper ApiWrapper = new AmeritradeApiWrapper();
        public AssetTrackerManager()
        {
            
        }

        public async Task InitializeManager()
        {
            EnableMonitor = true;
            var apiKey = await Repo.GetSystemDefault("TDA Api Key");
            var accessToken = await Repo.GetSystemDefault("TDA Access Token");
            var alpacaKeys = await Repo.GetSystemDefault("Alpaca Keys");
            var alpacaKeyObj = JsonConvert.DeserializeObject<AlpacaKeys>(alpacaKeys.AttributeValue);
            
            var accessTokenObj = JsonConvert.DeserializeObject<AccessToken>(accessToken.AttributeValue);
            ApiWrapper.InitializeApiWrapper(accessTokenObj, apiKey.AttributeValue);
            int maxId = 0;
            var assets = await Repo.GetAssetTrackerSetup();
            if(assets.Count != 0)
            {
                maxId = assets.Max(e => e.TrackerId);
            }
            
            //var expiredAssets = assets.Where(e => e.Expiration <= DateTime.Now).ToList();
            //foreach(var asset in expiredAssets)
            //{
            //    await Emails.SendEmail(asset.UserToAlert, $" Tracker {asset.Symbol} Expired. Reached Target Date {asset.Expiration.Date} For ID: {asset.TrackerId}", $" Tracker {asset.Symbol} Expired. Reached Target Date {asset.Expiration.Date} For ID: {asset.TrackerId}");
            //    await Repo.SetTrackerInactive(asset.TrackerId, "N", "Y", $" Tracker {asset.Symbol} Expired. Reached Target Date {asset.Expiration.Date} For ID: {asset.TrackerId}");
            //    assets.Remove(asset);
            //}
            foreach (var asset in assets)
            {
                var tracker = new AssetTracker(asset, ApiWrapper, Emails, Repo, alpacaKeyObj);
                var task = Task.Run(() => tracker.InitializeMonitor());
                TrackerTasks.Add(task);
                ActiveTrackers.Add(tracker);
            }
            int count = 0;
            while (EnableMonitor)
            { 
                Thread.Sleep(new TimeSpan(0, 1, 0));
                var CheckAssets = await Repo.GetAssetTrackerSetup();
                if(CheckAssets.Count != 0)
                {
                    if (CheckAssets.Max(e => e.TrackerId) > maxId)
                    {
                        var ListToTrack = CheckAssets.Where(e => e.TrackerId > maxId).ToList();
                        foreach (var asset in ListToTrack)
                        {
                            var tracker = new AssetTracker(asset, ApiWrapper, Emails, Repo, alpacaKeyObj);
                            var task = Task.Run(() => tracker.InitializeMonitor());
                            TrackerTasks.Add(task);
                            ActiveTrackers.Add(tracker);
                        }
                        maxId = CheckAssets.Max(e => e.TrackerId);
                    }
                }
                if (count >= 20)
                {
                    ApiWrapper.SetRefreshToken();
                    count = 0;
                }
                count++;
            }
            foreach (var tracker in  ActiveTrackers)
            {
                tracker.TerminateTracker = true;
            }
            await Task.WhenAll(TrackerTasks);
        }
    }
}
