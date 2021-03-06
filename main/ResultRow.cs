﻿using System;
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

            isCallnPut = (warrantID.EndsWith("P") || warrantID.EndsWith("B") || warrantID.EndsWith("Y")) ? false : true;
		}
		
        public string WarrantID { get; }
        public int ExpiredDays { get; }
        public string UnderlyingID { get; }
        public string UnderlyingName { get; }
        // 報價 0.01 = 1
        public UInt64 Quote { get; set; }
        public UInt64 Low { get; set; }
        public uint Volume { get; set; }
		private UInt64 Reference;
        private UInt64 StrikePrice { get; }
        // 執行比例 0.001 = 1
        public UInt32 ConvertibleRatio { get; }
        // 報價 0.01 = 1
        public UInt64 UnderlyingQuote { get; }
        public UInt64 UnderlyingReference { get; }
        // Up / Down // rise / fall // higher / lower
        public float PLDay { get; }

        public UInt64 GetLimitLow()
        {
            return Stock.ValidCeiling(Reference - Convert.ToUInt64(Convert.ToDecimal(UnderlyingReference) * Convert.ToDecimal(1 - Stock.kMaxDown) * Convert.ToDecimal(ConvertibleRatio) / Convert.ToDecimal(Stock.kLotSize)));
        }
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
        public float GetLimitUpGain()
        {
            decimal NetIncome = Decimal.MinValue;
            decimal gain = Decimal.MinValue;
            decimal WarrantCost = Convert.ToDecimal(Quote) * Stock.kLotSize / ConvertibleRatio * Convert.ToDecimal(1 + Warrant.kHandleFee);
            if (isCallnPut)
            {
                UInt64 underlyingLimitHigh = Stock.ValidFloor(Convert.ToUInt64(Convert.ToDecimal(UnderlyingReference) * Convert.ToDecimal(Stock.kMaxUp)));
                // [ 股票賣價(Revenue) - 權證價值(Expense) - 執行賣價(Cost) ](單股獲利)
                NetIncome = Convert.ToDecimal(underlyingLimitHigh) * (1 - Convert.ToDecimal(Stock.kTradeTax)) - WarrantCost -
                    Convert.ToDecimal(StrikePrice) * Convert.ToDecimal((1 + Stock.kHandleFee));
            }
            else
            {
                UInt64 underlyingLimitLow = Stock.ValidCeiling(Convert.ToUInt64(Convert.ToDecimal(UnderlyingReference) * Convert.ToDecimal(Stock.kMaxDown)));
                // [ 執行賣價(Revenue) - 權證價值(Expense) - 股票賣價(Cost) ](單股獲利)
                NetIncome = Convert.ToDecimal(StrikePrice) * (1 - Convert.ToDecimal(Stock.kTradeTax + Stock.kHandleFee)) - WarrantCost -
                    Convert.ToDecimal(underlyingLimitLow);
            }
            gain = NetIncome / WarrantCost;
            return (float)gain;
        }
        public float GetLimitDownGain()
        {
            decimal NetIncome = Decimal.MinValue;
            decimal gain = Decimal.MinValue;
            decimal WarrantCost = Convert.ToDecimal(Quote) * Stock.kLotSize / ConvertibleRatio * Convert.ToDecimal(1 + Warrant.kHandleFee);
            if (isCallnPut)
            {
                UInt64 underlyingLimitLow = Stock.ValidCeiling(Convert.ToUInt64(Convert.ToDecimal(UnderlyingReference) * Convert.ToDecimal(Stock.kMaxDown)));
                // [ 股票賣價(Revenue) - 權證價值(Expense) - 執行賣價(Cost) ](單股獲利)
                NetIncome = Convert.ToDecimal(underlyingLimitLow) * (1 - Convert.ToDecimal(Stock.kTradeTax)) - WarrantCost -
                    Convert.ToDecimal(StrikePrice) * Convert.ToDecimal((1 + Stock.kHandleFee));
            }
            else
            {
                UInt64 underlyingLimitHigh = Stock.ValidFloor(Convert.ToUInt64(Convert.ToDecimal(UnderlyingReference) * Convert.ToDecimal(Stock.kMaxUp)));
                // [ 執行賣價(Revenue) - 權證價值(Expense) - 股票賣價(Cost) ](單股獲利)
                NetIncome = Convert.ToDecimal(StrikePrice) * (1 - Convert.ToDecimal(Stock.kTradeTax + Stock.kHandleFee)) - WarrantCost -
                    Convert.ToDecimal(underlyingLimitHigh);
            }
            gain = NetIncome / WarrantCost;
            return (float)gain;
        }
        public float GetArbitrageInterest()
        {
            // (warrants[wi].stockPrice / (sRealTrade + sTradeFee + sShortFee) - warrants[wi].buyingPrice - warrants[wi].exePrice * sHandleFee) * unit / warrants[wi].totalCost * 365 / warrants[wi].daysFromExpiration;
            return 0;
        }
        public UInt64 GetPercentGainCost(int percent2Gain)
        {
            decimal warrantValue = Decimal.MinValue;
            if (isCallnPut)
            {
                // [ 股票賣價(Revenue) - 執行賣價(Cost) ](單股獲利)
                warrantValue = Convert.ToDecimal(UnderlyingQuote) * (1 - Convert.ToDecimal(Stock.kTradeTax)) -
                    Convert.ToDecimal(StrikePrice) * Convert.ToDecimal((1 + Stock.kHandleFee));
            }
            else
            {
                // [ 執行賣價(Revenue) - 股票賣價(Cost) ](單股獲利)
                warrantValue = Convert.ToDecimal(StrikePrice) * (1 - Convert.ToDecimal(Stock.kTradeTax + Stock.kHandleFee)) -
                    Convert.ToDecimal(UnderlyingQuote);
            }
            if (warrantValue <= 0)
                return 0;
            // [ 權證價值(Cost) x 執行比例 x 預期報酬價(1 - %) ](單股獲利)
            return Warrant.ValidFloor(Convert.ToUInt64(warrantValue / Convert.ToDecimal(1 + Warrant.kHandleFee) * ConvertibleRatio / Stock.kLotSize * (1M - Convert.ToDecimal(percent2Gain) / 100M)));
        }
        public float GetLeverage()
        {
            decimal warrantCost = Warrant.WarrantCost(Quote, ConvertibleRatio);
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
        private bool isCallnPut;
    }
}
