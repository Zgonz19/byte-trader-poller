using System;
using System.Collections.Generic;
using System.Text;

namespace ByteTraderPoller.Tables
{
    public class TradeUser
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public int RemainingDayTrades { get; set; }
        public string LockTrading { get; set; }
    }
}
