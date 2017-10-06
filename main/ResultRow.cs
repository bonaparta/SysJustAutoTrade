using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comfup
{
    public class ResultRow
    {
		public ResultRow(string WaID, int ExpDays, uint Quo, uint Lo, uint Ref, uint StriPr, uint ConRa, uint FiPa, string StID, string StNam,
                uint SpotQuo, uint StRef)
		{
            WarrantID = WaID;
            ExpiredDays = ExpDays;
            Quote = Quo;
            Low = Lo;
            Reference = Ref;
            StrikePrice = StriPr;
            ConvertibleRatio = ConRa;
            FixPercentReference = FiPa;
            UnderlyingID = StID;
            UnderlyingName = StNam;
            UnderlyingQuote = SpotQuo;
            UnderlyingReference = StRef;

            isCallnPut = true;
		}
		
        public string WarrantID { get; }
        public int ExpiredDays { get; }
        public string UnderlyingID { get; }
        public string UnderlyingName { get; }
        public float UnchangeGain()
        {
            decimal NetIncome = Decimal.MinValue;
            decimal gain = Decimal.MinValue;
            decimal WarrantCost = Convert.ToDecimal(Quote) * Stock.kLotSize / ConvertibleRatio * Convert.ToDecimal(1 + Warrant.kHandleFee);
            if (isCallnPut)
            {
                // [ 股票賣價(Revenue) - 權證價值(Expense) - 執行賣價(Cost) ](單股獲利)
                NetIncome = Convert.ToDecimal(UnderlyingQuote) * (1 - Convert.ToDecimal(Stock.kTradeTax)) - WarrantCost -
                    Convert.ToDecimal(StrikePrice) * Convert.ToDecimal((1 + Stock.kHandleFee));
            }
            else
            {
                // [ 執行賣價(Revenue) - 權證價值(Expense) - 股票賣價(Cost) ](單股獲利)
                NetIncome = Convert.ToDecimal(StrikePrice) * (1 - Convert.ToDecimal(Stock.kTradeTax + Stock.kHandleFee)) - WarrantCost -
                    Convert.ToDecimal(UnderlyingQuote);
            }
            gain = NetIncome / WarrantCost;
            return (float)gain;
        }
        public float LimitUpGain { get; }
        public float LimitDownGain { get; }
        // 報價 0.01 = 1
        public UInt64 Quote { get; set; }
        public UInt64 Low { get; set; }
        public uint Volume { get; set; }
        public uint LimitLow { get; set; }
        public uint FixPercentReference { get; set; }
		private UInt64 Reference;
        private UInt64 StrikePrice { get; }
        // 執行比例 0.001 = 1
        public UInt32 ConvertibleRatio { get; }
        public float leverage { get; }
        // 報價 0.01 = 1
        public UInt64 UnderlyingQuote { get; }
		private uint TargetStockLimitLow { get; }
        private UInt64 UnderlyingReference { get; }
        // Up / Down // rise / fall // higher / lower
        public float PLDay { get; }
        private bool isCallnPut;
    }
}
