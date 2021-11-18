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
        public AlpacaTradingWrapper AlpacaWrapper { get; set; }
        public ByteTraderRepository Repo { get; set; }
        public EmailEngine Emails { get; set; }
        public AssetTracker(AssetTrackerSetup trackerSetup, AmeritradeApiWrapper apiWrapper, EmailEngine emails, ByteTraderRepository repo, AlpacaKeys alpacaKeys) 
        {
            TrackerSetup = trackerSetup;
            ApiWrapper = apiWrapper;
            Emails = emails;
            Repo = repo;
            AlpacaWrapper = new AlpacaTradingWrapper(alpacaKeys.AlpacaApiKey, alpacaKeys.AlpacaSecretKey);
        }
        public void InitializeMonitor()
        {
            while(TerminateTracker == false)
            {
                Thread.Sleep(new TimeSpan(0, 1, 0));
                CallMinuteBars();
            }
        }

        public async Task<SellOrderStatus> ExecuteSellOrder(decimal price)
        {
            var user = await Repo.GetTradeUser(1);
            var result = await AlpacaWrapper.ExecuteFullBalanceSell(TrackerSetup.Symbol, TrackerSetup.ShareQuantity, price);
            if (result != null)
            {
                await Repo.UpdateTradeUser(user.UserId, user.RemainingDayTrades, "N");
            }
            return result;
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
                        var result = await ExecuteSellOrder(TrackerSetup.StrikePrice);
                        var htmlBody = new StringBuilder();
                        htmlBody.Append($"Cash Balance: {result.AccountBalance} \r\n");
                        htmlBody.Append($"Market Value: {result.MarketValue}");
                        string body = htmlBody.ToString();
                        string subject = $"{TrackerSetup.Symbol} Hit Strike Price {TrackerSetup.StrikePrice}. Sold {TrackerSetup.ShareQuantity} Shares at {result.SellPrice}";
                        await Emails.SendEmail(TrackerSetup.UserToAlert, body, subject);
                        await Repo.SetTrackerInactive(TrackerSetup.TrackerId, "N", "Y", $"{TrackerSetup.Symbol} Strike Price Hit {TrackerSetup.StrikePrice}. Sold {TrackerSetup.ShareQuantity} Shares at {result.SellPrice}");
                        TerminateTracker = true;
                        break;
                    }
                    else if (close <= TrackerSetup.StopLoss || low <= TrackerSetup.StopLoss)
                    {
                        var result = await ExecuteSellOrder(TrackerSetup.StopLoss);
                        var htmlBody = new StringBuilder();
                        htmlBody.Append($"Cash Balance: {result.AccountBalance} \r\n");
                        htmlBody.Append($"Market Value: {result.MarketValue}");
                        string body = htmlBody.ToString();
                        string subject = $"{TrackerSetup.Symbol} Hit Stop Loss {TrackerSetup.StrikePrice}. Sold {TrackerSetup.ShareQuantity} Shares at {result.SellPrice}";
                        await Emails.SendEmail(TrackerSetup.UserToAlert, body, subject);
                        await Repo.SetTrackerInactive(TrackerSetup.TrackerId, "N", "Y", $"{TrackerSetup.Symbol} Stop Loss Hit {TrackerSetup.StopLoss}. Sold {TrackerSetup.ShareQuantity} Shares at {result.SellPrice}");
                        TerminateTracker = true;
                        break;
                    }
                    else if(TrackerSetup.Expiration <= DateTime.Now)
                    {
                        var result = await ExecuteSellOrder(close);
                        var htmlBody = new StringBuilder();
                        htmlBody.Append($"Cash Balance: {result.AccountBalance} \r\n");
                        htmlBody.Append($"Market Value: {result.MarketValue}");
                        string body = htmlBody.ToString();
                        string subject = $"{TrackerSetup.Symbol} Tracker Expired. Sold {TrackerSetup.ShareQuantity} Shares at {result.SellPrice}";
                        await Emails.SendEmail(TrackerSetup.UserToAlert,body, subject);
                        await Repo.SetTrackerInactive(TrackerSetup.TrackerId, "N", "Y", $" Tracker {TrackerSetup.Symbol} Expired. Reached Target Date {TrackerSetup.Expiration}. Sold {TrackerSetup.ShareQuantity} Shares at {result.SellPrice}");
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
