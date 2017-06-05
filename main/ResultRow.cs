using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comfup
{
    class ResultRow
    {
		ResultRow(string WaID, int ExpDays, string StID, string StNam, uint Quo, uint Lo, uint FiPa, uint Ref, uint UsaRa,
                uint StQuo, uint StRef)
		{
            WarrantID = WaID;
            ExpiredDays = ExpDays;
            TargetStockID = StID;
            TargetStockName = StNam;
            Quote = Quo;
            Low = Lo;
            FixPercentReference = FiPa;
            ReferencePrice = Ref;
            UsageRatio = UsaRa;
            TargetStockQuote = StQuo;
            TargetStockReference = StRef;

            TargetStockLimitHigh = 0;
		}
		
        public string WarrantID { get; }
        public int ExpiredDays { get; }
        public string TargetStockID { get; }
        public string TargetStockName { get; }
        public float UnchangeGain { get; }
        public float LimitUpGain { get; }
        public float LimitDownGain { get; }
        // 報價 0.01 = 1
        public uint Quote { get; set; }
        public uint Volume { get; set; }
        public uint Low { get; set; }
        public uint LimitLow { get; set; }
        public uint FixPercentReference { get; set; }
		private uint ReferencePrice;
        // 執行比例 0.001 = 1
        public uint UsageRatio;
        public float leverage { get; }
        // 報價 0.01 = 1
        public uint TargetStockQuote { get; }
		private uint TargetStockLimitHigh { get; }
		private uint TargetStockLimitLow { get; }
        private uint TargetStockReference { get; }
        // Up / Down // rise / fall // higher / lower
        public float PLDay { get; }
    }
}
