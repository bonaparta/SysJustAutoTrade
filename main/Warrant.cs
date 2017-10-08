using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comfup
{
    // ###### #####P 認購 認售
    // #####F #####Q 國外證券 認購 認售
    // #####C #####B 牛證 熊證
    // #####X #####Y 展延型牛證 展延型熊證
    // #####K        ETF            外幣
    // #####L        ETF 槓桿型
    // #####M        ETF 槓桿型     外幣
    // #####R        ETF 反向型
    // #####S        ETF 反向型     外幣
    // #####U        ETF 期貨(債券)
    // #####V        ETF 期貨(債券) 外幣
    // #####T        受益證券
    class Warrant : Stock
    {
        public new const double kTradeTax = 0.001;
        public new const double kHandleFee = 0.001425;

        private static UInt64[] kTickLevel = { 500, 1000, 5000, 10000, 50000 };
        private static UInt64[] kTickMin = { 1, 5, 10, 50, 100, 500 };

        public int ExpiredDays;
        public string TargetStockID;
        public string TargetStockName;
        // 執行比例 0.001 = 1
        public uint UsageRatio;
        public uint StrikePrice;
        private Stock TargetStock;
        public Warrant(string id, string name, uint refPrice) : base(id, name, refPrice) { }
        public Warrant(string id, string name) : base(id, name) { }
        public void SetStock(Stock stock) { this.TargetStock = stock; }
        public override UInt64 LimitHigh()
        {
            return LimitAll(true);
        }
        public override UInt64 LimitLow()
        {
            return LimitAll(false);
        }

        public static decimal GetWarrantCost(UInt64 quote, UInt32 convertibllRatio)
        {
            return Convert.ToDecimal(quote) * Stock.kLotSize / convertibllRatio * Convert.ToDecimal(1 + Warrant.kHandleFee);
        }
        public static UInt64 GetValidFloor(UInt64 price)
        {
            if (price <= kTickLevel[0])
                return price;
            else if (price <= kTickLevel[1])
                return Stock.GetFloor(price, kTickMin[1]);
            else if (price <= kTickLevel[2])
                return Stock.GetFloor(price, kTickMin[2]);
            else if (price <= kTickLevel[3])
                return Stock.GetFloor(price, kTickMin[3]);
            else if (price <= kTickLevel[4])
                return Stock.GetFloor(price, kTickMin[4]);
            return Stock.GetFloor(price, kTickMin[5]);
        }
        public static UInt64 GetValidCeiling(UInt64 price)
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

        private UInt64 MaxChange(bool isHigh)
        {
            UInt64 StockChange_ = isHigh? TargetStock.LimitHighChange() : TargetStock.LimitLowChange();
            return Convert.ToUInt32(StockChange_ * UsageRatio / Limits.PerStock);
        }
        private UInt64 LimitAll(bool isHigh)
        {
            UInt64 LimitChange_ = 0;
            if (isHigh)
                LimitChange_ = Reference + MaxChange(isHigh);
            else
            {
                UInt64 MinChange_ = MaxChange(isHigh);
                LimitChange_ = (Reference <= MinChange_) ? Limits.MinPriceInCent: Reference - MinChange_;
            }
            if (LimitChange_ <= 500)
                return LimitChange_;
            else if (LimitChange_ <= 1000)
                return isHigh ? GetFloor(LimitChange_, 5) : GetCeiling(LimitChange_, 5);
            else if (LimitChange_ <= 5000)
                return isHigh ? GetFloor(LimitChange_, 10) : GetCeiling(LimitChange_, 10);
            else if (LimitChange_ <= 10000)
                return isHigh ? GetFloor(LimitChange_, 50) : GetCeiling(LimitChange_, 50);
            else if (LimitChange_ <= 50000)
                return isHigh ? GetFloor(LimitChange_, 100) : GetCeiling(LimitChange_, 100);
            return isHigh ? GetFloor(LimitChange_, 500) : GetCeiling(LimitChange_, 500);
        }
    }
}
