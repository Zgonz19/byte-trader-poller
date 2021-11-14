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
                parameters.Add("@delayed", content.delayed);
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
