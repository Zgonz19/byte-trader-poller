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
        public List<QuoteChange> Changes = new List<QuoteChange>();
        public QuoteProcessor(string symbol)
        {
            Symbol = symbol;
        }


        public void ScanChangesForAction(QuoteChange changes, content content, double timestamp)
        {
            //BidAskRatio:
            //totalvolume compared to avg 5 day and 10 day
            //totalvolume compared to closest avg., measure % completion towards avg vs time of day
            //bid size change
            //
            //
            //implement trade logic to buy assets based on some obsered correlation between volume, bid, ask,
        }


        public void SaveQuoteData()
        {
            
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
                    var index = item.IndexOf(currentValue);
                    var previousValue = item[index - 1];
                    var percentChange = 100 * ((currentValue.Value - previousValue.Value) / previousValue.Value);
                    var absChange = currentValue.Value - previousValue.Value;
                    changes.Add(percentChange);
                    changesAbs.Add(absChange);
                }
                else
                {
                    changes.Add(null);
                    changesAbs.Add(null);
                }
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
            change.CurrentTimestamp = timestamp;
            Changes.Add(change);
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

        public double CurrentTimestamp { get; set; }
    }
}
