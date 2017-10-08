using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comfup
{
    public class ResultRow
    {
		public ResultRow(string warrantID, string expirationDate, UInt64 close, UInt64 low, UInt64 open, UInt64 strikePrice, uint convertiableRatio, string underlyingID, string underlyingName,
                UInt64 underLyingClose, UInt64 underlyingOpen)
		{
            WarrantID = warrantID;
            DateTime target = DateTime.Parse(expirationDate);
            DateTime today = DateTime.Today;
            int days = (target - today).Days;
            ExpiredDays = days;
            Quote = close;
            Low = low;
            Reference = open;
            StrikePrice = strikePrice;
            ConvertibleRatio = convertiableRatio;
            UnderlyingID = underlyingID;
            UnderlyingName = underlyingName;
            UnderlyingQuote = underLyingClose;
            UnderlyingReference = underlyingOpen;

            isCallnPut = (warrantID.EndsWith("P") || warrantID.EndsWith("B")) ? false : true;
		}
		
        public string WarrantID { get; }
        public int ExpiredDays { get; }
        public string UnderlyingID { get; }
        public string UnderlyingName { get; }
        public float LimitUpGain { get; }
        public float LimitDownGain { get; }
        // 報價 0.01 = 1
        public UInt64 Quote { get; set; }
        public UInt64 Low { get; set; }
        public uint Volume { get; set; }
        public UInt64 LimitLow()
        {
            return 0;
        }
		private UInt64 Reference;
        private UInt64 StrikePrice { get; }
        // 執行比例 0.001 = 1
        public UInt32 ConvertibleRatio { get; }

        public float GetUnchangeGain()
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
        public UInt64 GetPercentGainCost(int percent2Gain)
        {
            // TODO(Bona): count accurately
            return Warrant.GetValidFloor(Convert.ToUInt64(Convert.ToDecimal(Quote) * (1 + percent2Gain / 100.0M) / Convert.ToDecimal(1 + GetUnchangeGain())));
        }
        public float GetLeverage()
        {
            decimal warrantCost = Warrant.GetWarrantCost(Quote, ConvertibleRatio);
            decimal stockCost = Decimal.MaxValue;
            if (isCallnPut)
            {
                stockCost = UnderlyingQuote * Convert.ToDecimal(Stock.kShortRate + Stock.kTradeTax + Stock.kHandleFee + Stock.kShortFee);
            }
            else
            {
                stockCost = Stock.GetStockCost(UnderlyingQuote);
            }
            return (float)(stockCost / warrantCost);
        }
        // 報價 0.01 = 1
        public UInt64 UnderlyingQuote { get; }
		private uint TargetStockLimitLow { get; }
        private UInt64 UnderlyingReference { get; }
        // Up / Down // rise / fall // higher / lower
        public float PLDay { get; }
        private bool isCallnPut;
    }
}
