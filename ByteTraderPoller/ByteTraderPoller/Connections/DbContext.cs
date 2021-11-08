//using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;


namespace ByteTraderPoller.Connections
{
    public abstract class DbContext
    {
        internal IDbConnection Connection
        {
            get
            {
                return new SqlConnection(ConfigurationManager.AppSetting["BTConnectionString"]);
            }
        }
        internal SqlConnection SqlConnect
        {
            get
            {
                return new SqlConnection(ConfigurationManager.AppSetting["BTConnectionString"]);
            }
        }
    }
}
