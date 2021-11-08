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
