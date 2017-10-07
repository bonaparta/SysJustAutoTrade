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
        public override uint LimitHigh()
        {
            return LimitAll(true);
        }
        public override uint LimitLow()
        {
            return LimitAll(false);
        }

        public static decimal GetWarrantCost(UInt64 quote, UInt32 convertibllRatio)
        {
            return Convert.ToDecimal(quote) * Stock.kLotSize / convertibllRatio * Convert.ToDecimal(1 + Warrant.kHandleFee);
        }

        private uint MaxChange(bool isHigh)
        {
            ulong StockChange_ = isHigh? TargetStock.LimitHighChange() : TargetStock.LimitLowChange();
            return Convert.ToUInt32(StockChange_ * UsageRatio / Limits.PerStock);
        }
        private uint LimitAll(bool isHigh)
        {
            uint LimitChange_ = 0;
            if (isHigh)
                LimitChange_ = Reference + MaxChange(isHigh);
            else
            {
                uint MinChange_ = MaxChange(isHigh);
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
