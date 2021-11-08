using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog;
using NLog.Web;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ByteTraderPoller.Connections
{
    public class AmeritradeApiWrapper
    {
        public Logger Logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
        public string TargetUrl { get; set; }
        public string ResponseString { get; set; }
        public dynamic ResponseObject { get; set; }
        public string TDAmeritradeApiKey { get; set; }
        public AccessToken AccessToken { get; set; }
        public static RefreshToken RefreshToken { get; set; }
        private readonly object RefreshTokenLock = new object();
        public AmeritradeApiWrapper()
        {

        }
        public void InitializeApiWrapper(AccessToken accessToken, string apiKey)
        {
            TDAmeritradeApiKey = apiKey;
            AccessToken = accessToken;
            SetRefreshToken();
        }
        public async void SetRefreshToken()
        {
            RefreshToken refreshToken = null;
            try
            {
                var client = new HttpClient();
                // Create the HttpContent for the form to be posted.
                var requestContent = new FormUrlEncodedContent(new[] {
                        new KeyValuePair<string, string>("grant_type", "refresh_token"),
                        new KeyValuePair<string, string>("refresh_token", $"{AccessToken.refresh_token}"),
                        new KeyValuePair<string, string>("client_id", $"{TDAmeritradeApiKey}@AMER.OAUTHAP")});

                // Get the response.
                HttpResponseMessage response = client.PostAsync("https://api.tdameritrade.com/v1/oauth2/token", requestContent).Result;
                // Get the response content.
                HttpContent responseContent = response.Content;
                // Get the stream of the content.
                using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
                {
                    var output = reader.ReadToEndAsync().Result;
                    refreshToken = JsonConvert.DeserializeObject<RefreshToken>(output);
                }
                if (!(refreshToken == null))
                {
                    if(refreshToken.access_token != null)
                    {
                        lock (RefreshTokenLock)
                        {
                            RefreshToken = refreshToken;
                            Logger.Info("Refresh Token Updated");
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
            }
        }
        public async Task<CandleList> GetLatestMinuteBars(string symbol, DateTime date, bool extendedhours)
        {
            var unixTime = new DateTimeOffset(date).ToUnixTimeMilliseconds();
            CandleList candleList = null;

            try
            {
                var url = $"https://api.tdameritrade.com/v1/marketdata/{symbol}/pricehistory?apikey={TDAmeritradeApiKey}&periodType=day&frequencyType=minute&frequency=1&endDate={unixTime}&startDate={unixTime}&needExtendedHoursData={extendedhours}";
                var client = new HttpClient();
                HttpResponseMessage response = null;
                lock (RefreshTokenLock)
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", RefreshToken.access_token);
                    // Get the response.
                    response = client.GetAsync(url).Result;
                }
                // Get the response content.
                HttpContent responseContent = response.Content;
                // Get the stream of the content.
                using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
                {
                    var output = reader.ReadToEndAsync().Result;
                    candleList = JsonConvert.DeserializeObject<CandleList>(output);
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
            }
            return candleList;
        }
    }
    public class RefreshToken
    {
        public string access_token { get; set; }
        public string scope { get; set; }
        public long expires_in { get; set; }
        public string token_type { get; set; }
    }
    public class AccessToken
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public string scope { get; set; }
        public long expires_in { get; set; }
        public long refresh_token_expires_in { get; set; }
        public string token_type { get; set; }
    }
    public class CandleList
    {
        public List<candles> candles { get; set; }
        public bool empty { get; set; }
        public string symbol { get; set; }
    }
    public class candles
    {
        public string close { get; set; }
        public long datetime { get; set; }
        public string high { get; set; }
        public string low { get; set; }
        public string open { get; set; }
        public string volume { get; set; }
    }
}
