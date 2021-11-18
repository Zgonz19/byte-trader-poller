using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Alpaca;
using Alpaca.Markets;

namespace ByteTraderPoller.Connections
{
    public class AlpacaTradingWrapper
    {
        public string ApiKey { get; set; }
        public string ApiSecretKey { get; set; }

        public AlpacaTradingWrapper(string apiKey, string apiSecretKey)
        {
            ApiKey = apiKey;
            ApiSecretKey = apiSecretKey;
        }

        public async Task<SellOrderStatus> ExecuteFullBalanceSell(string symbol, int quantity, decimal targetprice)
        {
            SellOrderStatus orderStatus = null;
            try
            {
                var tradeClient = Alpaca.Markets.Environments.Live.GetAlpacaTradingClient(new SecretKey(ApiKey, ApiSecretKey));
                var orderRequest = new NewOrderRequest(symbol, quantity, OrderSide.Sell, OrderType.Market, TimeInForce.Gtc);
                var orderResult = tradeClient.PostOrderAsync(orderRequest).GetAwaiter().GetResult();
                if (orderResult.OrderStatus == OrderStatus.Accepted)
                {
                    orderStatus = new SellOrderStatus();
                    orderStatus.SellPrice = targetprice;
                    var account = tradeClient.GetAccountAsync().GetAwaiter().GetResult();
                    orderStatus.AccountBalance = account.TradableCash;
                    orderStatus.MarketValue = account.LongMarketValue;
                }
            }
            catch (Exception exc)
            {

            }

            return orderStatus;
        }
        public async Task<BuyOrderStatus> ExecuteFullBalanceBuy(string symbol, decimal LastPrice)
        {
            BuyOrderStatus orderStatus = null;
            try
            {
                var tradeClient = Alpaca.Markets.Environments.Live.GetAlpacaTradingClient(new SecretKey(ApiKey, ApiSecretKey));
                var account = tradeClient.GetAccountAsync().GetAwaiter().GetResult();
                var shareCount = (int)(account.TradableCash / LastPrice);
                //shareCount = shareCount - 1;
                var orderRequest = new NewOrderRequest(symbol, shareCount, OrderSide.Buy, OrderType.Market, TimeInForce.Gtc);
                var orderResult = tradeClient.PostOrderAsync(orderRequest).GetAwaiter().GetResult();
                if (orderResult.OrderStatus == OrderStatus.Accepted)
                {
                    
                    orderStatus = new BuyOrderStatus
                    {
                        SharesPurchased = shareCount,
                        PurchasePrice = LastPrice
                    };
                    var accountBal = await tradeClient.GetAccountAsync();
                    orderStatus.AccountBalance = accountBal.TradableCash;
                    orderStatus.MarketValue = accountBal.LongMarketValue;
                }
            }
            catch (Exception exc)
            {

            }

            return orderStatus;
        }


    }

    public class AlpacaKeys
    {
        public string AlpacaApiKey { get; set; }
        public string AlpacaSecretKey { get; set; }
    }
    public class BuyOrderStatus
    {
        public long SharesPurchased { get; set; }
        public decimal? PurchasePrice { get; set; }
        public decimal? AccountBalance { get; set; }
        public decimal? MarketValue { get; set; }
    }

    public class SellOrderStatus
    {
        public decimal? SellPrice { get; set; }
        public decimal? AccountBalance { get; set; }
        public decimal? MarketValue { get; set; }
    }
}
