using ByteTraderPoller.Connections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ByteTraderPoller.Services.QuoteStreaming
{
    public class QuoteStreamingManager
    {
        public bool EnableMonitor = true;
        public ByteTraderRepository Repo = new ByteTraderRepository();
        public AmeritradeWebsocketWrapper TdaWebsocket { get; set; }
        public AmeritradeApiWrapper ApiWrapper = new AmeritradeApiWrapper();
        public QuoteStreamingManager()
        {
            
        }


        public async Task InitializeManager()
        {
            EnableMonitor = true;
            var apiKey = await Repo.GetSystemDefault("TDA Api Key");
            var accessToken = await Repo.GetSystemDefault("TDA Access Token");
            var accessTokenObj = JsonConvert.DeserializeObject<AccessToken>(accessToken.AttributeValue);
            var alpacaKeys = await Repo.GetSystemDefault("Alpaca Keys");
            var alpacaKeyObj = JsonConvert.DeserializeObject<AlpacaKeys>(alpacaKeys.AttributeValue);
            ApiWrapper.InitializeApiWrapper(accessTokenObj, apiKey.AttributeValue);
            var userPrinciples = await ApiWrapper.GetUserPrinciples();
            dynamic userPrincipalsResponse = JsonConvert.DeserializeObject(userPrinciples, typeof(object));
            //var testResponse = JObject.Parse(userPrinciples);
            TdaWebsocket = new AmeritradeWebsocketWrapper(alpacaKeyObj);
            var quotes = await Repo.GetQuotesWatchList();
            TdaWebsocket.StreamQuotes(userPrincipalsResponse, quotes);
        }
    }
}
