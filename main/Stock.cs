using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comfup
{
    class Stock
    {
        public const double kTradeTax = 0.003;
        public const double kHandleFee = 0.001425;
        public const double kShortFee = 0.0008;

        public static readonly double kMaxUp = 1.1;
        public static readonly double kMaxDown = 0.9;

        public static readonly double kShortRate = 0.9;

        public const UInt32 kLotSize = 1000;

        private static UInt64[] kTickLevel = { 1000, 5000, 10000, 50000, 100000 };
        private static UInt64[] kTickMin = { 1, 5, 10, 50, 100, 500 };

        public string ID;
        public string Name;
        public uint Reference;
        public uint Open;
        public uint High;
        public uint Low;
        public uint Close;
        public uint Volume;

        public Stock(string id, string name, uint refPrice)
        {
            ID = id;
            Name = name;
            Reference = refPrice;
        }
        public Stock(string id, string name)
        {
            ID = id;
            Name = name;
        }
        public virtual UInt64 LimitHigh()
        {
            return LimitAll(true);
        }
        public virtual UInt64 LimitLow()
        {
            return LimitAll(false);
        }
        public UInt64 LimitHighChange()
        {
            return LimitHigh() - Reference;
        }
        public UInt64 LimitLowChange()
        {
            return Reference - LimitLow();
        }

        public static decimal GetStockCost(UInt64 quote)
        {
            return Convert.ToDecimal(quote) * Convert.ToDecimal(1 + Stock.kHandleFee);
        }

        private uint MaxChange()
        {
            return Convert.ToUInt32(Reference * Limits.LimitFloat);
        }
        private UInt64 LimitAll(bool isHigh)
        {
            UInt64 LimitChange_ = isHigh ? Reference + MaxChange() : Reference - MaxChange();
            if (LimitChange_ <= 1000)
                return isHigh? FloorWithMinTick(LimitChange_, 1) : CeilingWithMinTick(LimitChange_, 1);
            else if (LimitChange_ <= 5000)
                return isHigh ? FloorWithMinTick(LimitChange_, 5) : CeilingWithMinTick(LimitChange_, 5);
            else if (LimitChange_ <= 10000)
                return isHigh ? FloorWithMinTick(LimitChange_, 10) : CeilingWithMinTick(LimitChange_, 10);
            else if (LimitChange_ <= 50000)
                return isHigh ? FloorWithMinTick(LimitChange_, 50) : CeilingWithMinTick(LimitChange_, 50);
            else if (LimitChange_ <= 100000)
                return isHigh ? FloorWithMinTick(LimitChange_, 100) : CeilingWithMinTick(LimitChange_, 100);
            return isHigh ? FloorWithMinTick(LimitChange_, 500) : CeilingWithMinTick(LimitChange_, 500);
        }
        public static UInt64 FloorWithMinTick(UInt64 LimitChange, UInt64 TickInCent)
        {
            return Convert.ToUInt64(LimitChange / TickInCent) * TickInCent;
        }
        public static UInt64 CeilingWithMinTick(UInt64 LimitChange, UInt64 TickInCent)
        {
            return (LimitChange % TickInCent) == 0 ? LimitChange : FloorWithMinTick(LimitChange, TickInCent) + TickInCent;
        }
        public static UInt64 ValidFloor(UInt64 price)
        {
            if (price <= kTickLevel[0])
                return price;
            else if (price <= kTickLevel[1])
                return Stock.FloorWithMinTick(price, kTickMin[1]);
            else if (price <= kTickLevel[2])
                return Stock.FloorWithMinTick(price, kTickMin[2]);
            else if (price <= kTickLevel[3])
                return Stock.FloorWithMinTick(price, kTickMin[3]);
            else if (price <= kTickLevel[4])
                return Stock.FloorWithMinTick(price, kTickMin[4]);
            return Stock.FloorWithMinTick(price, kTickMin[5]);
        }
        public static UInt64 ValidCeiling(UInt64 price)
        {
            if (price <= kTickLevel[0])
                return price;
            else if (price <= kTickLevel[1])
                return (price % kTickMin[1]) == 0 ? price : (price / kTickMin[1]) + kTickMin[1];
            else if (price <= kTickLevel[2])
                return (price % kTickMin[2]) == 0 ? price : (price / kTickMin[2]) + kTickMin[2];
            else if (price <= kTickLevel[3])
                return (price % kTickMin[3]) == 0 ? price : (price / kTickMin[3]) + kTickMin[3];
            else if (price <= kTickLevel[4])
                return (price % kTickMin[4]) == 0 ? price : (price / kTickMin[4]) + kTickMin[4];
            return (price % kTickMin[5]) == 0 ? price : (price / kTickMin[5]) + kTickMin[5];
        }
    }
}
