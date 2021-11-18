using ByteTraderPoller.Services.QuoteStreaming;
using ByteTraderPoller.Tables;
using Dapper;
using NLog;
using NLog.Web;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ByteTraderPoller.Connections
{
    public class ByteTraderRepository : DbContext
    {
        public Logger Logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();


        public async Task<List<AssetTrackerSetup>> GetAssetTrackerSetup()
        {
            List<AssetTrackerSetup> appUsers;
            try
            {
                var sqlQuery = "SELECT * FROM AssetTrackerSetup WHERE IsActiveFlag = 'Y'";
                using (IDbConnection cn = Connection)
                {
                    cn.Open();
                    var result = cn.QueryAsync<AssetTrackerSetup>(sqlQuery).GetAwaiter().GetResult();
                    cn.Close();
                    appUsers = result.ToList();
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
                appUsers = null;
            }
            return appUsers;
        }

        public async Task InsertRealtimeQuote(content content, double timestamp)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Timestamp", timestamp);
                parameters.Add("@Symbol", content.key);
                parameters.Add("@key", content.key);
                parameters.Add("@delayed", content.delayed.ToString());
                parameters.Add("@assetMainType", content.assetMainType);
                parameters.Add("@cusip", content.cusip);
                parameters.Add("@BidPrice", content.BidPrice);
                parameters.Add("@AskPrice", content.AskPrice);
                parameters.Add("@LastPrice", content.LastPrice);
                parameters.Add("@BidSize", content.BidSize);
                parameters.Add("@AskSize", content.AskSize);
                parameters.Add("@AskId", content.AskId);
                parameters.Add("@BidId", content.BidId);
                parameters.Add("@TotalVolume", content.TotalVolume);
                parameters.Add("@LastSize", content.LastSize);
                parameters.Add("@TradeTime", content.TradeTime);
                parameters.Add("@QuoteTime", content.QuoteTime);
                parameters.Add("@HighPrice", content.HighPrice);
                parameters.Add("@LowPrice", content.LowPrice);
                parameters.Add("@BidTick", content.BidTick);
                parameters.Add("@ClosePrice", content.ClosePrice);
                parameters.Add("@QuoteTimeMs", content.QuoteTimeMs);
                parameters.Add("@TradeTimeMs", content.TradeTimeMs);
                parameters.Add("@DateTimeKey", DateTimeOffset.FromUnixTimeMilliseconds((long)timestamp).DateTime);
                
                var sqlCommand = $"INSERT INTO dbo.RealtimeQuotes (Timestamp, Symbol, [key], delayed, assetMainType, cusip, BidPrice, AskPrice, LastPrice, BidSize, AskSize, AskId, BidId, TotalVolume, LastSize, TradeTime, QuoteTime, HighPrice, LowPrice, BidTick, ClosePrice, QuoteTimeMs, TradeTimeMs, DateTimeKey) " +
                    $"VALUES (@Timestamp, @Symbol, @key, @delayed, @assetMainType, @cusip, @BidPrice, @AskPrice, @LastPrice, @BidSize, @AskSize, @AskId, @BidId, @TotalVolume, @LastSize, @TradeTime, @QuoteTime, @HighPrice, @LowPrice, @BidTick, @ClosePrice, @QuoteTimeMs, @TradeTimeMs, @DateTimeKey);";
                using (IDbConnection cn = Connection)
                {
                    try
                    {
                        cn.Open();
                        cn.Execute(sqlCommand, parameters);
                        cn.Close();
                    }
                    catch (Exception exc)
                    {
                        Logger.Info(exc.ToString());
                    }
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
            }
        }

        public async Task InsertBuySignal(string Symbol, int Upticks, int Downticks, int SequentialUpticks, decimal BidAskRatio, string MidDayVolumeSurge, DateTime SignalTimestamp)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Symbol", Symbol);
                parameters.Add("@Upticks", Upticks);
                parameters.Add("@Downticks", Downticks);
                parameters.Add("@SequentialUpticks", SequentialUpticks);
                parameters.Add("@BidAskRatio", BidAskRatio);
                parameters.Add("@MidDayVolumeSurge", MidDayVolumeSurge);
                parameters.Add("@SignalTimestamp", SignalTimestamp);

                var sqlCommand = $"INSERT INTO dbo.LogBuySignal (Symbol, Upticks, Downticks, SequentialUpticks, BidAskRatio, MidDayVolumeSurge, SignalTimestamp) " +
                    $"VALUES (@Symbol, @Upticks, @Downticks, @SequentialUpticks, @BidAskRatio, @MidDayVolumeSurge, @SignalTimestamp);";
                using (IDbConnection cn = Connection)
                {
                    try
                    {
                        cn.Open();
                        cn.Execute(sqlCommand, parameters);
                        cn.Close();
                    }
                    catch (Exception exc)
                    {
                        Logger.Info(exc.ToString());
                    }
                }
            }
            catch(Exception exc)
            {

            }
        }
        public async Task InsertAssetTracker(int SymbolId, string Symbol, decimal? StopLoss, decimal? StrikePrice, DateTime Expiration, string IsActiveFlag, string HasBeenNotified, string UserToAlert, string? Notes, long ShareQuantity)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@SymbolId", SymbolId);
                parameters.Add("@Symbol", Symbol);
                parameters.Add("@StopLoss", StopLoss);
                parameters.Add("@StrikePrice", StrikePrice);
                parameters.Add("@Expiration", Expiration);
                parameters.Add("@IsActiveFlag", IsActiveFlag);
                parameters.Add("@HasBeenNotified", HasBeenNotified);
                parameters.Add("@UserToAlert", UserToAlert);
                parameters.Add("@Notes", Notes);
                parameters.Add("@ShareQuantity", ShareQuantity);

                var sqlCommand = $"INSERT INTO dbo.AssetTrackerSetup (SymbolId, Symbol, StopLoss, StrikePrice, Expiration, IsActiveFlag, HasBeenNotified, UserToAlert, Notes, ShareQuantity) " +
                    $"VALUES (@SymbolId, @Symbol, @StopLoss, @StrikePrice, @Expiration, @IsActiveFlag, @HasBeenNotified, @UserToAlert, @Notes, @ShareQuantity);";
                using (IDbConnection cn = Connection)
                {
                    try
                    {
                        cn.Open();
                        cn.Execute(sqlCommand, parameters);
                        cn.Close();
                    }
                    catch (Exception exc)
                    {
                        Logger.Info(exc.ToString());
                    }
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
            }
        }

        public async Task SetTrackerInactive(int TrackerId, string IsActiveFlag, string HasBeenNotified, string Notes)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@TrackerId", TrackerId);
                parameters.Add("@IsActiveFlag", IsActiveFlag);
                parameters.Add("@HasBeenNotified", HasBeenNotified);
                parameters.Add("@Notes", Notes);

                var sqlCommand = $"UPDATE dbo.AssetTrackerSetup SET IsActiveFlag = @IsActiveFlag, HasBeenNotified = @HasBeenNotified, Notes = @Notes " +
                    $"WHERE TrackerId = @TrackerId;";
                using (IDbConnection cn = Connection)
                {
                    try
                    {
                        cn.Open();
                        cn.Execute(sqlCommand, parameters);
                        cn.Close();
                    }
                    catch (Exception exc)
                    {
                        Logger.Info(exc.ToString());
                    }
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
            }
        }
        public async Task UpdateTradeUser(int UserId, int RemainingDayTrades, string LockTrading)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", UserId);
            parameters.Add("@RemainingDayTrades", RemainingDayTrades);
            parameters.Add("@LockTrading", LockTrading);

            var sqlCommand = $"UPDATE dbo.TradeUser SET RemainingDayTrades = @RemainingDayTrades, LockTrading = @LockTrading " +
                $"WHERE UserId = @UserId;";
            using (IDbConnection cn = Connection)
            {
                try
                {
                    cn.Open();
                    cn.Execute(sqlCommand, parameters);
                    cn.Close();
                }
                catch (Exception exc)
                {
                    Logger.Info(exc.ToString());
                }
            }
        }
        public async Task<DailyFundamentalData> GetNewestFundamentals(string Symbol)
        {
            DailyFundamentalData stockSymbols;
            var parameters = new DynamicParameters();
            try
            {
                parameters.Add("@Symbol", Symbol);

                var sqlQuery = $"SELECT TOP 1 * FROM DailyFundamentalData WHERE Symbol = @Symbol Order by DateTimeKey Desc;";
                using (IDbConnection cn = Connection)
                {
                    cn.Open();
                    var result = cn.QueryAsync<DailyFundamentalData>(sqlQuery, parameters).Result;
                    cn.Close();
                    stockSymbols = result.FirstOrDefault();
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
                stockSymbols = null;
            }
            return stockSymbols;
        }
        //QuotesWatchList

        public async Task<List<string>> GetQuotesWatchList()
        {
            var sqlQuery = "SELECT * FROM dbo.QuotesWatchList;";
            List<string> appUsers;
            try
            {
                using (IDbConnection cn = Connection)
                {
                    cn.Open();
                    var result = cn.QueryAsync<string>(sqlQuery).GetAwaiter().GetResult();
                    cn.Close();
                    appUsers = result.ToList();
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
                appUsers = null;
            }
            return appUsers;
        }
        public async Task<TradeUser> GetTradeUser(int UserId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", UserId);
            var sqlQuery = "SELECT * FROM dbo.TradeUser WHERE UserId = @UserId;";
            TradeUser appUsers;
            try
            {
                using (IDbConnection cn = Connection)
                {
                    cn.Open();
                    var result = cn.QueryAsync<TradeUser>(sqlQuery, parameters).GetAwaiter().GetResult();
                    cn.Close();
                    appUsers = result.FirstOrDefault();
                }
            }
            catch (Exception exc)
            {
                Logger.Info(exc.ToString());
                appUsers = null;
            }
            return appUsers;
        }

        public async Task<SystemDefaults> GetSystemDefault(string attributeName)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@AttributeName", attributeName);
            SystemDefaults index;
            var sqlQuery = @"SELECT * FROM SystemDefaults WHERE AttributeName = @AttributeName;";
            try
            {
                using (IDbConnection cn = Connection)
                {
                    cn.Open();
                    var result = cn.QueryAsync<SystemDefaults>(sqlQuery, parameters).GetAwaiter().GetResult();
                    cn.Close();
                    index = result.FirstOrDefault();
                }
            }
            catch (Exception exc)
            {
                index = null;
                Logger.Info(exc.ToString());
            }
            return index;
        }
    }
}
