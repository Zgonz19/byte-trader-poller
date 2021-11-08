using ByteTraderPoller.Connections;
using ByteTraderPoller.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ByteTraderPoller.Services.AssetMonitoring
{
    public class AssetTracker
    {
        public Logger Logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
        public bool TerminateTracker = false;
        public string Symbol { get; set; }
        public AssetTrackerSetup TrackerSetup { get; set; }
        public AmeritradeApiWrapper ApiWrapper { get; set; }
        public ByteTraderRepository Repo { get; set; }
        public EmailEngine Emails { get; set; }
        public AssetTracker(AssetTrackerSetup trackerSetup, AmeritradeApiWrapper apiWrapper, EmailEngine emails, ByteTraderRepository repo) 
        {
            TrackerSetup = trackerSetup;
            ApiWrapper = apiWrapper;
            Emails = emails;
            Repo = repo;
        }
        public void InitializeMonitor()
        {
            while(TerminateTracker == false)
            {
                Thread.Sleep(new TimeSpan(0, 1, 0));
                CallMinuteBars();
            }
        }
        public async void CallMinuteBars()
        {
            try
            {
                var bars = await ApiWrapper.GetLatestMinuteBars(TrackerSetup.Symbol, DateTime.Now.Date, false);
                if(bars != null)
                {
                    if (bars.candles == null)
                    {
                        Logger.Info($"Tracking Asset: {TrackerSetup.Symbol} No Bars Retrieved");
                    }
                    else
                    {
                        CombResults(bars.candles);
                        Logger.Info($"Tracking Asset: {bars.symbol} Minute Bar Count: {bars.candles.Count}");
                    }
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
            }
        }
        public async void CombResults(List<candles> candles)
        {
            foreach(var candle in candles)
            {
                try
                {
                    var low = NaNToNull(candle.low);
                    var high = NaNToNull(candle.high);
                    var close = NaNToNull(candle.close);
                    if (close >= TrackerSetup.StrikePrice || high >= TrackerSetup.StrikePrice)
                    {
                        await Emails.SendEmail(TrackerSetup.UserToAlert, $"{TrackerSetup.Symbol} Strike Price Hit {TrackerSetup.StrikePrice} For ID: {TrackerSetup.TrackerId}", $"{TrackerSetup.Symbol} Strike Price Hit {TrackerSetup.StrikePrice} For ID: {TrackerSetup.TrackerId}");
                        await Repo.SetTrackerInactive(TrackerSetup.TrackerId, "N", "Y", $"{TrackerSetup.Symbol} Strike Price Hit {TrackerSetup.StrikePrice} For ID: {TrackerSetup.TrackerId}");
                        TerminateTracker = true;
                        break;
                    }
                    else if (close <= TrackerSetup.StopLoss || low <= TrackerSetup.StopLoss)
                    {
                        await Emails.SendEmail(TrackerSetup.UserToAlert, $"{TrackerSetup.Symbol} Stop Loss Hit {TrackerSetup.StopLoss} For ID: {TrackerSetup.TrackerId}", $"{TrackerSetup.Symbol} Stop Loss Hit {TrackerSetup.StopLoss} For ID: {TrackerSetup.TrackerId}");
                        await Repo.SetTrackerInactive(TrackerSetup.TrackerId, "N", "Y", $"{TrackerSetup.Symbol} Stop Loss Hit {TrackerSetup.StopLoss} For ID: {TrackerSetup.TrackerId}");
                        TerminateTracker = true;
                        break;
                    }
                }
                catch (Exception exc)
                {
                    Logger.Info(exc.ToString());
                }
            }
        }

        public dynamic NaNToNull(string t)
        {
            if (t == "NaN" || String.IsNullOrWhiteSpace(t))
            {
                return null;
            }
            else
            {
                try
                {
                    var test = Convert.ToDecimal(t);
                    return test;
                }
                catch (Exception exc)
                {
                    if (!decimal.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                    {
                        return null;
                    }
                    else
                    {
                        return result.ToString();
                    }
                }
            }
        }
    }
}
