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
        public int ExpiredDays;
        public string TargetStockID;
        public string TargetStockName;
        // 執行比例 0.001 = 1
        public uint UsageRatio;
        public uint StrikePrice;
        public Warrant(string id, string name, uint refPrice) : base(id, name, refPrice) { }
        public Warrant(string id, string name) : base(id, name) { }
        public uint LimitHigh(Stock stock)
        {
            uint change_ = stock.LimitHighChange();
            if (Reference <= 1000)
                return GetFloor(1);
            else if (Reference <= 5000)
                return GetFloor(5);
            else if (Reference <= 10000)
                return GetFloor(10);
            else if (Reference <= 50000)
                return GetFloor(50);
            else if (Reference <= 100000)
                return GetFloor(100);
            return GetFloor(500);
        }
        public uint LimitLow(Stock stock)
        {
            if (Reference <= 500)
                return GetCeiling(1);
            else if (Reference <= 1000)
                return GetCeiling(5);
            else if (Reference <= 5000)
                return GetCeiling(10);
            else if (Reference <= 10000)
                return GetCeiling(50);
            else if (Reference <= 50000)
                return GetCeiling(100);
            return GetCeiling(500);
        }
    }
}
