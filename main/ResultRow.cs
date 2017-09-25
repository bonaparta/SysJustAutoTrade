using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comfup
{
    class ResultRow
    {
		ResultRow(string WaID, int ExpDays, uint Quo, uint Ref, uint UsaRa, uint FiPa, string StID, string StNam,
                uint SpotQuo, uint StRef)
		{
            WarrantID = WaID;
            ExpiredDays = ExpDays;
            Quote = Quo;
            ReferencePrice = Ref;
            UsageRatio = UsaRa;
            FixPercentReference = FiPa;
            SpotID = StID;
            SpotName = StNam;
            SpotQuote = SpotQuo;
            SpotReference = StRef;

            SpotLimitHigh = 0;
		}
		
        public string WarrantID { get; }
        public int ExpiredDays { get; }
        public string SpotID { get; }
        public string SpotName { get; }
        public float UnchangeGain { get; }
        public float LimitUpGain { get; }
        public float LimitDownGain { get; }
        // 報價 0.01 = 1
        public uint Quote { get; set; }
        public uint Volume { get; set; }
        public uint LimitLow { get; set; }
        public uint FixPercentReference { get; set; }
		private uint ReferencePrice;
        // 執行比例 0.001 = 1
        public uint UsageRatio;
        public float leverage { get; }
        // 報價 0.01 = 1
        public uint SpotQuote { get; }
		private uint SpotLimitHigh { get; }
		private uint TargetStockLimitLow { get; }
        private uint SpotReference { get; }
        // Up / Down // rise / fall // higher / lower
        public float PLDay { get; }
    }
}
