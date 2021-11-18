using ByteTraderPoller.Connections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ByteTraderPoller.Services.QuoteStreaming
{
    public class QuoteDataManager
    {
        public bool Subscribed = false;
        enum ResponseType
        {
            notify,
            data,
            other,
            response
        }
        public Dictionary<string, int> QuoteIndex = new Dictionary<string, int>();
        public ByteTraderRepository Repo = new ByteTraderRepository();
        public List<QuoteProcessor> Quotes = new List<QuoteProcessor>();
        public List<Task> QuoteTasks = new List<Task>();
        public AlpacaKeys Keys { get; set; }
        //public List<string> SymbolsActive = new List<string>();
        public List<KeyValuePair<double, List<data>>>QuoteResponses = new List<KeyValuePair<double, List<data>>>();
        public QuoteDataManager(AlpacaKeys keys)
        {
            Keys = keys;
        }

        public async void ParseQuotes(List<data> data)
        {
            var timestamp = data[0].timestamp;
            KeyValuePair<double, List<data>> response = new KeyValuePair<double, List<data>>(timestamp, data);
            if (!QuoteResponses.Contains(response))
            {
                QuoteResponses.Add(response);
                foreach (var item in data[0].content)
                {
                    if (!(QuoteIndex.ContainsKey(item.key)))
                    {
                        var fundamental = await Repo.GetNewestFundamentals(item.key);
                        var quoteProcessor = new QuoteProcessor(item.key, fundamental.Vol3MonthAvg, fundamental.Vol10DayAvg, Keys.AlpacaApiKey, Keys.AlpacaSecretKey);
                        Quotes.Add(quoteProcessor);
                        var index = Quotes.IndexOf(quoteProcessor);
                        QuoteIndex.Add(item.key, index);
                        Quotes[index].ReceiveQuoteData(item, timestamp);                      
                    }
                    else
                    {
                        var index = QuoteIndex.GetValueOrDefault(item.key);
                        Quotes[index].ReceiveQuoteData(item, timestamp);
                    }
                }
            }
        }

        public void ReadStreamResponse(JObject data)
        {
            ResponseType responseType;
            dynamic response = null;
            try
            {
                response = data["notify"];
                if(response != null)
                {
                    responseType = ResponseType.notify;
                }
                else
                {
                    response = data["data"];
                    if (response != null)
                    {
                        responseType = ResponseType.data;
                    }
                    else
                    {
                        response = data["response"];
                        if (response != null)
                        {
                            responseType = ResponseType.response;
                        }
                        else
                        {
                            response = data;
                            responseType = ResponseType.other;

                        }
                    }
                }
            }
            catch(Exception exc)
            {
                response = data;
                responseType = ResponseType.other;
            }

            if(responseType == ResponseType.data)
            {
                var jsonString = JsonConvert.SerializeObject(response);
                var val = JsonConvert.DeserializeObject<List<data>>(jsonString);
                ParseQuotes(val);
                //Console.WriteLine(jsonString);
            }
            else if (responseType == ResponseType.notify)
            {
                var jsonString = JsonConvert.SerializeObject(response);
                var val = JsonConvert.DeserializeObject<List<heartbeats>>(jsonString);
                Console.WriteLine("heartbeat: " + val[0].heartbeat);
            }
            else if (responseType == ResponseType.other)
            {
                var text = JsonConvert.SerializeObject(response);
                Console.WriteLine(text);
            }
        }
    }

    public class heartbeats
    {
        public string heartbeat { get; set; }
    }
    public class data
    {
        public string service { get; set; }
        public double timestamp { get; set; }
        public string command { get; set; }
        public List<content> content { get; set; }
    }
    public class content
    {
        public string? key { get; set; }
        public bool? delayed { get; set; }
        public string? assetMainType { get; set; }
        public string? cusip { get; set; }

        [JsonProperty(PropertyName = "1")]
        public Decimal? BidPrice { get; set; }

        [JsonProperty(PropertyName = "2")]
        public Decimal? AskPrice { get; set; }

        [JsonProperty(PropertyName = "3")]
        public Decimal? LastPrice { get; set; }

        [JsonProperty(PropertyName = "4")]
        public double? BidSize { get; set; }

        [JsonProperty(PropertyName = "5")]
        public double? AskSize { get; set; }

        [JsonProperty(PropertyName = "6")]
        public string? AskId { get; set; }

        [JsonProperty(PropertyName = "7")]
        public string? BidId { get; set; }

        [JsonProperty(PropertyName = "8")]
        public double? TotalVolume { get; set; }

        [JsonProperty(PropertyName = "9")]
        public double? LastSize { get; set; }

        [JsonProperty(PropertyName = "10")]
        public double? TradeTime { get; set; }

        [JsonProperty(PropertyName = "11")]
        public double? QuoteTime { get; set; }

        [JsonProperty(PropertyName = "12")]
        public Decimal? HighPrice { get; set; }

        [JsonProperty(PropertyName = "13")]
        public Decimal? LowPrice { get; set; }

        [JsonProperty(PropertyName = "14")]
        public string? BidTick { get; set; }

        [JsonProperty(PropertyName = "15")]
        public Decimal? ClosePrice { get; set; }

        [JsonProperty(PropertyName = "50")]
        public double? QuoteTimeMs { get; set; }

        [JsonProperty(PropertyName = "51")]
        public double? TradeTimeMs { get; set; }
    }
}
