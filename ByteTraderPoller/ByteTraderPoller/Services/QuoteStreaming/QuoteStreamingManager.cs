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
        public AmeritradeWebsocketWrapper TdaWebsocket = new AmeritradeWebsocketWrapper();
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
            ApiWrapper.InitializeApiWrapper(accessTokenObj, apiKey.AttributeValue);
            var userPrinciples = await ApiWrapper.GetUserPrinciples();
            dynamic userPrincipalsResponse = JsonConvert.DeserializeObject(userPrinciples, typeof(object));
            var testResponse = JObject.Parse(userPrinciples);
            //dynamic data = Json.Decode(userPrinciples);


            var stockList = new List<string>
            {
                "XELA",
                "AMD",
                "OSH"
            };

            TdaWebsocket.StreamQuotes(userPrincipalsResponse, stockList);
        }
    }
}
