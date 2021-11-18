using ByteTraderPoller.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ByteTraderPoller.Services.QuoteStreaming
{
    public class QuoteProcessor
    {
        public string Symbol { get; set; }
        public string QuoteDataFolder = @"C:\Users\Gonzalo\Dropbox\Github_08_2021\byte-trader-daily-jobs\DailyProcessList";
        public List<KeyValuePair<double, content>> QuoteHistory = new List<KeyValuePair<double, content>>();
        public List<Fields> BidPriceHistory = new List<Fields>();
        public List<Fields> AskPriceHistory = new List<Fields>();
        public List<Fields> LastPriceHistory = new List<Fields>();
        public List<Fields> BidSizeHistory = new List<Fields>();
        public List<Fields> AskSizeHistory = new List<Fields>();
        public List<Fields> AskIdHistory = new List<Fields>();
        public List<Fields> BidIdHistory = new List<Fields>();
        public List<Fields> TotalVolumeHistory = new List<Fields>();
        public List<Fields> LastSizeHistory = new List<Fields>();
        public List<Fields> TradeTimeHistory = new List<Fields>();
        public List<Fields> QuoteTimeHistory = new List<Fields>();

        public ByteTraderRepository Repo = new ByteTraderRepository();
        public EmailEngine Emails = new EmailEngine();
        public AlpacaTradingWrapper AlpacaWrapper { get; set; }
        public List<QuoteChange> Changes = new List<QuoteChange>();

        public decimal TenDayVolumeAvg { get; set; }

        public bool MidMarketVolumeSurge = false;

        public decimal ThirtyDayVolumeAvg { get; set; }


        public List<string> PriceChangeTick = new List<string>();

        public int SequentialUpticks = 0; 

        public Decimal TargetBidAskRatio = (decimal)1.08;

        public QuoteProcessor(string symbol, decimal thirtyDayVolumeAvg, decimal tenDayVolumeAvg, string apiKey, string apiSecret)
        {
            Symbol = symbol;
            ThirtyDayVolumeAvg = thirtyDayVolumeAvg;
            TenDayVolumeAvg = tenDayVolumeAvg;
            AlpacaWrapper = new AlpacaTradingWrapper(apiKey, apiSecret);
        }


        public async void ScanChangesForAction(QuoteChange changes, content content, double timestamp)
        {
            var totalBidSizeHistory = BidSizeHistory.Select(e => (decimal)e.Value).ToList().Sum();
            var totalAskSizeHistory = AskSizeHistory.Select(e => (decimal)e.Value).ToList().Sum();
            var bidAskRatio = totalBidSizeHistory / totalAskSizeHistory;
            var currentVolume = (double)TotalVolumeHistory[TotalVolumeHistory.Count - 1].Value;
            var percentCompleteVolume = (decimal)currentVolume / (decimal)ThirtyDayVolumeAvg;
            var percentCompleteVolume2 = (decimal)currentVolume / (decimal)TenDayVolumeAvg;
            var upticks = PriceChangeTick.Select(e => e = "UP").ToList().Count;
            var downticks = PriceChangeTick.Select(e => e = "DOWN").ToList().Count;
            if ((percentCompleteVolume >= (decimal)0.55 || percentCompleteVolume2 >= (decimal)0.55) & DateTime.Now.TimeOfDay < new TimeSpan(11, 45, 0))
            {
                MidMarketVolumeSurge = true;
            }
            if (upticks >= downticks & SequentialUpticks >= 3 & bidAskRatio >= TargetBidAskRatio & MidMarketVolumeSurge)
            {
                await Repo.InsertBuySignal(Symbol, upticks, downticks, SequentialUpticks, bidAskRatio, MidMarketVolumeSurge.ToString(), DateTime.Now);
                AttemptStockPurchase();
            }

            //if(Symbol == "AMD")
            //{
            //    await Repo.InsertBuySignal(Symbol, upticks, downticks, SequentialUpticks, bidAskRatio, MidMarketVolumeSurge.ToString(), DateTime.Now);
            //    AttemptStockPurchase();
            //}
        }


        public async void AttemptStockPurchase()
        {
            var user = await Repo.GetTradeUser(1);
            if (user.RemainingDayTrades > 0 & user.LockTrading == "N")
            {
                await Repo.UpdateTradeUser(user.UserId, user.RemainingDayTrades, "Y");
                var lastPrice = (decimal)LastPriceHistory[LastPriceHistory.Count - 1].Value;
                var result = await AlpacaWrapper.ExecuteFullBalanceBuy(Symbol, lastPrice);
                if(result != null)
                {
                    await Repo.UpdateTradeUser(user.UserId, user.RemainingDayTrades - 1, "Y");
                    var stopLoss = result.PurchasePrice - (result.PurchasePrice * (decimal)0.025);
                    var strikePrice = result.PurchasePrice + (result.PurchasePrice * (decimal)0.05);
                    await Repo.InsertAssetTracker(444, Symbol, stopLoss, strikePrice, DateTime.Now.Date.AddMonths(2), "Y", "N", user.Email, null, result.SharesPurchased);
                    var htmlBody = new StringBuilder();
                    htmlBody.Append($"Cash Balance: {result.AccountBalance} \r\n");
                    htmlBody.Append($"Market Value: {result.MarketValue}");
                    string body = htmlBody.ToString();
                    string subject = $"{Symbol} Buy Event Triggered. Purchased {result.SharesPurchased} Shares at {result.PurchasePrice}";
                    await Emails.SendEmail(user.Email, body, subject);

                }
                else
                {
                    await Repo.UpdateTradeUser(user.UserId, user.RemainingDayTrades, "N");
                }
            }
        }

        public async void ReceiveQuoteData(content content, double timestamp)
        {
            var pair = new KeyValuePair<double, content>(timestamp, content);
            if (!QuoteHistory.Contains(pair))
            {
                QuoteHistory.Add(pair);
                QuoteHistory = QuoteHistory.OrderBy(e => e.Key).ToList();
                AddQuoteContent(content, timestamp);
                var changes = CombQuoteHistory(timestamp);
                await Repo.InsertRealtimeQuote(content, timestamp);
                ScanChangesForAction(changes, content, timestamp);
            }
        }
        public void AddQuoteContent(content content, double timestamp)
        {
            if (content.BidPrice != null)
            {
                BidPriceHistory.Add(new Fields { Value = content.BidPrice, Timestamp = timestamp });
                BidPriceHistory = BidPriceHistory.OrderBy(e => e.Timestamp).ToList();
            }
            if (content.AskPrice != null)
            {
                AskPriceHistory.Add(new Fields { Value = content.AskPrice, Timestamp = timestamp });
                AskPriceHistory = AskPriceHistory.OrderBy(e => e.Timestamp).ToList();
            }
            if (content.LastPrice != null)
            {
                LastPriceHistory.Add(new Fields { Value = content.LastPrice, Timestamp = timestamp });
                LastPriceHistory = LastPriceHistory.OrderBy(e => e.Timestamp).ToList();
            }
            if (content.BidSize != null)
            {
                BidSizeHistory.Add(new Fields { Value = content.BidSize, Timestamp = timestamp });
                BidSizeHistory = BidSizeHistory.OrderBy(e => e.Timestamp).ToList();
            }
            if (content.AskSize != null)
            {
                AskSizeHistory.Add(new Fields { Value = content.AskSize, Timestamp = timestamp });
                AskSizeHistory = AskSizeHistory.OrderBy(e => e.Timestamp).ToList();
            }
            if (content.AskId != null)
            {
                AskIdHistory.Add(new Fields { Value = content.AskId, Timestamp = timestamp });
                AskIdHistory = AskIdHistory.OrderBy(e => e.Timestamp).ToList();
            }
            if (content.BidId != null)
            {
                BidIdHistory.Add(new Fields { Value = content.BidId, Timestamp = timestamp });
                BidIdHistory = BidIdHistory.OrderBy(e => e.Timestamp).ToList();
            }
            if (content.TotalVolume != null)
            {
                TotalVolumeHistory.Add(new Fields { Value = content.TotalVolume, Timestamp = timestamp });
                TotalVolumeHistory = TotalVolumeHistory.OrderBy(e => e.Timestamp).ToList();
            }
            if (content.LastSize != null)
            {
                LastSizeHistory.Add(new Fields { Value = content.LastSize, Timestamp = timestamp });
                LastSizeHistory = LastSizeHistory.OrderBy(e => e.Timestamp).ToList();
            }
            if (content.TradeTime != null)
            {
                TradeTimeHistory.Add(new Fields { Value = content.TradeTime, Timestamp = timestamp });
                TradeTimeHistory = TradeTimeHistory.OrderBy(e => e.Timestamp).ToList();
            }
            if (content.QuoteTime != null)
            {
                QuoteTimeHistory.Add(new Fields { Value = content.QuoteTime, Timestamp = timestamp });
                QuoteTimeHistory = QuoteTimeHistory.OrderBy(e => e.Timestamp).ToList();
            }
        }
        public QuoteChange CombQuoteHistory(double timestamp)
        {
            var historyList = new List<List<Fields>>();
            historyList.Add(BidPriceHistory);
            historyList.Add(AskPriceHistory);
            historyList.Add(LastPriceHistory);
            historyList.Add(BidSizeHistory);
            historyList.Add(AskSizeHistory);
            historyList.Add(TotalVolumeHistory);
            historyList.Add(LastSizeHistory);
            var changes  = new List<decimal?>();
            var changesAbs = new List<decimal?>();
            foreach(var item in historyList)
            {
                var currentValue = item.FirstOrDefault(e => e.Timestamp == timestamp);
                if (currentValue != null && item.Count >= 2)
                {
                    try
                    {
                        var index = item.IndexOf(currentValue);
                        var previousValue = item[index - 1];
                        var absChange = (decimal)currentValue.Value - (decimal)previousValue.Value;
                        changesAbs.Add(absChange);
                        try
                        {
                            if ((decimal)previousValue.Value != 0)
                            {
                                var percentChange = 100 * (((decimal)currentValue.Value - (decimal)previousValue.Value) / (decimal)previousValue.Value);
                                changes.Add(percentChange);
                            }
                            else
                            {
                                changes.Add(null);
                            }
                        }
                        catch(Exception exc)
                        {
                            changes.Add(null);
                        }
                
                    }
                    catch (Exception exc)
                    {
                        changesAbs.Add(null);
                    }
                }
                else
                {
                    changes.Add(null);
                    changesAbs.Add(null);
                }
            }

            var askPrice = AskPriceHistory.FirstOrDefault(e => e.Timestamp == timestamp);
            var bidPrice = BidPriceHistory.FirstOrDefault(e => e.Timestamp == timestamp);
            Decimal? BidAskSpread = null;
            Decimal? SpreadPercent = null;
            if(askPrice != null && bidPrice != null)
            {
                BidAskSpread = askPrice.Value - bidPrice.Value;
                SpreadPercent = BidAskSpread / askPrice.Value;
            }

            Decimal? BidOverAskRatio = null;
            var askSize = AskSizeHistory.FirstOrDefault(e => e.Timestamp == timestamp);
            var bidSize = BidSizeHistory.FirstOrDefault(e => e.Timestamp == timestamp);
            if (askSize != null && bidSize != null)
            {
                BidOverAskRatio = (decimal)bidSize.Value / (decimal)askSize.Value;
            }
            var change = new QuoteChange();
            change.ChangeRecorded = DateTime.Now;
            change.BidPriceChange = changes[0];
            change.AskPriceChange = changes[1];
            change.LastPriceChange = changes[2];
            change.BidSizeChange = changes[3];
            change.AskSizeChange = changes[4];
            change.TotalVolumeChange = changes[5];
            change.LastSizeChange = changes[6];
            change.BidPriceAbs = changesAbs[0];
            change.AskPriceAbs = changesAbs[1];
            change.LastPriceAbs = changesAbs[2];
            change.BidSizeAbs = changesAbs[3];
            change.AskSizeAbs = changesAbs[4];
            change.TotalVolumeAbs = changesAbs[5];
            change.LastSizeAbs = changesAbs[6];
            change.BidAskSpread = BidAskSpread;
            change.SpreadPercent = SpreadPercent;
            change.BidOverAskRatio = BidOverAskRatio;
            change.CurrentTimestamp = timestamp;
            if (change.LastPriceChange != null)
            {
                var priceChange = Decimal.Round((decimal)change.LastPriceChange, 7);
                if (priceChange != (decimal)0)
                {
                    bool positive = priceChange > (decimal)0;
                    bool negative = priceChange < (decimal)0;
                    if (positive)
                    {
                        SequentialUpticks = SequentialUpticks + 1;
                        PriceChangeTick.Add("UP");
                    }
                    else if (negative)
                    {
                        SequentialUpticks = 0;
                        PriceChangeTick.Add("DOWN");
                    }
                }
            }

            Changes.Add(change);
            Changes = Changes.OrderBy(e => e.CurrentTimestamp).ToList();
            return change;
        }
    }

    public class Fields
    {
        public dynamic Value { get; set; }
        public double Timestamp { get; set; }
    }

    public class QuoteChange
    {
        public DateTime ChangeRecorded { get; set; }
        public Decimal? BidPriceChange { get; set; }
        public Decimal? AskPriceChange { get; set; }
        public Decimal? LastPriceChange { get; set; }
        public Decimal? BidSizeChange { get; set; }
        public Decimal? AskSizeChange { get; set; }
        public Decimal? TotalVolumeChange { get; set; }
        public Decimal? LastSizeChange { get; set; }
        public Decimal? BidPriceAbs { get; set; }
        public Decimal? AskPriceAbs { get; set; }
        public Decimal? LastPriceAbs { get; set; }
        public Decimal? BidSizeAbs { get; set; }
        public Decimal? AskSizeAbs { get; set; }
        public Decimal? TotalVolumeAbs { get; set; }
        public Decimal? LastSizeAbs { get; set; }
        public Decimal? BidAskSpread { get; set; }
        public Decimal? SpreadPercent { get; set; }
        public Decimal? BidOverAskRatio { get; set; }   
        public double CurrentTimestamp { get; set; }
    }
}
