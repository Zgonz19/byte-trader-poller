using System;
using System.Collections.Generic;
using System.Text;

namespace ByteTraderPoller.Tables
{
    public class AssetTrackerSetup
    {
        public int TrackerId { get; set; }
        public int SymbolId { get; set; }
        public string Symbol { get; set; }
        public decimal StopLoss { get; set; }
        public decimal StrikePrice { get; set; }
        public DateTime Expiration { get; set; }
        public string IsActiveFlag { get; set; }
        public string HasBeenNotified { get; set; }
        public string UserToAlert { get; set; }
        public string Notes { get; set; }
    }
}
