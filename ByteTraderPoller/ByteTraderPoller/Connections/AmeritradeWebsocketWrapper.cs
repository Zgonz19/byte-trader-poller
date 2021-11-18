using ByteTraderPoller.Services.QuoteStreaming;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ByteTraderPoller.Connections
{
    public class AmeritradeWebsocketWrapper
    {
        public bool LoginSuccess = false;

        public AlpacaKeys Keys { get; set; }
        public QuoteDataManager QuoteManager { get; set; }
        public AmeritradeWebsocketWrapper(AlpacaKeys keys)
        {
            Keys = keys;
            QuoteManager = new QuoteDataManager(Keys);
        }



        public async void InitializeStreaming()
        {
            //var task = TestSocketStreaming();
            //task.Wait();
        }
        public async Task ProcessQuoteResponse(dynamic data)
        {

                
        }
        //function jsonToQueryString(json)
        //{
        //    return Object.keys(json).map(function(key) {
        //        return encodeURIComponent(key) + '=' +
        //            encodeURIComponent(json[key]);
        //    }).join('&');
        //}
        public string DictToUrlEncoded(Dictionary<string, dynamic> table)
        {
            string str = "";
            bool first = true;
            foreach(var item in table)
            {
                if (first)
                {
                    str = item.Key + "=" + HttpUtility.UrlEncode(item.Value.ToString());
                    first = false;
                }
                else
                {
                    str = str + "&" + item.Key + "=" + HttpUtility.UrlEncode(item.Value.ToString());
                }
            }
            return str;
        }
        public string LoginRequestString(dynamic userPrincipalsResponse)
        {
            try
            {
                var tokenTimeStampAsDateObj = (DateTime)userPrincipalsResponse.streamerInfo.tokenTimestamp;
                var tokenTimeStampAsMs = new DateTimeOffset(tokenTimeStampAsDateObj).ToUnixTimeMilliseconds();
                var credDictionary = new Dictionary<string, dynamic>();
                credDictionary.Add("userid", userPrincipalsResponse.accounts[0].accountId);
                credDictionary.Add("token", userPrincipalsResponse.streamerInfo.token);
                credDictionary.Add("company", userPrincipalsResponse.accounts[0].company);
                credDictionary.Add("segment", userPrincipalsResponse.accounts[0].segment);
                credDictionary.Add("cddomain", userPrincipalsResponse.accounts[0].accountCdDomainId);
                credDictionary.Add("usergroup", userPrincipalsResponse.streamerInfo.userGroup);
                credDictionary.Add("accesslevel", userPrincipalsResponse.streamerInfo.accessLevel);
                credDictionary.Add("authorized", "Y");
                credDictionary.Add("timestamp", tokenTimeStampAsMs);
                credDictionary.Add("appid", userPrincipalsResponse.streamerInfo.appId);
                credDictionary.Add("acl", userPrincipalsResponse.streamerInfo.acl);
                var credsEncoded = DictToUrlEncoded(credDictionary);
                var parameters = new parameters
                {
                    credential = credsEncoded,
                    token = userPrincipalsResponse.streamerInfo.token,
                    version = "1.0"
                };
                var request = new request
                {
                    service = "ADMIN",
                    command = "LOGIN",
                    requestid = 0,
                    account = userPrincipalsResponse.accounts[0].accountId,
                    source = userPrincipalsResponse.streamerInfo.appId,
                    parameters = parameters
                };
                string requestString = JsonConvert.SerializeObject(request);
                return requestString;
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
                return null;
            }
        }
        public string QuotesRequestString(List<string> symbols, dynamic userPrincipalsResponse)
        {
            var parameters = new
            {
                keys = String.Join(",", symbols),
                fields = "0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,50,51",
            };
            var request = new
            {
                service = "QUOTE",
                requestid = "1",
                command = "SUBS",
                account = userPrincipalsResponse.accounts[0].accountId,
                source = userPrincipalsResponse.streamerInfo.appId,
                parameters = parameters
            };
            string requestString = JsonConvert.SerializeObject(request);
            return requestString;
        }

        public async Task StreamQuotes(dynamic userPrincipalsResponse, List<string> quoteSymbols)
        {
            string requestString = LoginRequestString(userPrincipalsResponse);
            var socketUrl = "wss://" + userPrincipalsResponse.streamerInfo.streamerSocketUrl + "/ws";
            using (ClientWebSocket ws = new ClientWebSocket())
            {
                Uri serverUri = new Uri(socketUrl);
                await ws.ConnectAsync(serverUri, CancellationToken.None);
                if (ws.State == WebSocketState.Open)
                {
                    ArraySegment<byte> bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(requestString));
                    await ws.SendAsync(bytesToSend, WebSocketMessageType.Text, true, CancellationToken.None);
                    ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[1024]);
                    WebSocketReceiveResult result = await ws.ReceiveAsync(bytesReceived, CancellationToken.None);
                    var responseString = Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count);
                    if (!string.IsNullOrWhiteSpace(responseString))
                    {
                        var response = JObject.Parse(responseString);
                        if ((int)response["response"][0]["content"]["code"] == 0)
                        {
                            LoginSuccess = true;
                            string quotesRequestString = QuotesRequestString(quoteSymbols, userPrincipalsResponse);
                            ArraySegment<byte> quotesRequest = new ArraySegment<byte>(Encoding.UTF8.GetBytes(quotesRequestString));
                            await ws.SendAsync(quotesRequest, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                }
                else
                {
                    //log error, notify admins
                }
                while (ws.State == WebSocketState.Open && LoginSuccess)
                {
                    string copyresponse = "";
                    try
                    {
                        ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[5024]);
                        WebSocketReceiveResult result = await ws.ReceiveAsync(bytesReceived, CancellationToken.None);
                        var responseString = Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count);
                        copyresponse = responseString;
                        var data = JObject.Parse(responseString);
                        //dynamic data = JsonConvert.DeserializeObject(responseString, typeof(object));
                        QuoteManager.ReadStreamResponse(data);
                        //dynamic data = JsonConvert.DeserializeObject(responseString);
                    }
                    catch (Exception exc)
                    {
                        var test = copyresponse;

                    }
                }
                //send logout requestv
                //mySock.send(JSON.stringify(request));
            }
        }
    }
    public class request
    {
        public string service { get; set; }
        public string command { get; set; }
        public long requestid { get; set; }
        public string account { get; set; }
        public string source { get; set; }
        public parameters parameters { get; set; }
    }

    public class parameters
    {
        public string credential { get; set; }
        public string token { get; set; }
        public string version { get; set; }
    }
}
