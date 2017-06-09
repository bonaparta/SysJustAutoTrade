using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comfup
{
    class Stock
    {
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
        public virtual uint LimitHigh()
        {
            return LimitAll(true);
        }
        public virtual uint LimitLow()
        {
            return LimitAll(false);
        }
        public uint LimitHighChange()
        {
            return LimitHigh() - Reference;
        }
        public uint LimitLowChange()
        {
            return Reference - LimitLow();
        }
        private uint MaxChange()
        {
            return Convert.ToUInt32(Reference * Limits.LimitFloat);
        }
        private uint LimitAll(bool isHigh)
        {
            uint LimitChange_ = isHigh ? Reference + MaxChange() : Reference - MaxChange();
            if (LimitChange_ <= 1000)
                return isHigh? GetFloor(LimitChange_, 1) : GetCeiling(LimitChange_, 1);
            else if (LimitChange_ <= 5000)
                return isHigh ? GetFloor(LimitChange_, 5) : GetCeiling(LimitChange_, 5);
            else if (LimitChange_ <= 10000)
                return isHigh ? GetFloor(LimitChange_, 10) : GetCeiling(LimitChange_, 10);
            else if (LimitChange_ <= 50000)
                return isHigh ? GetFloor(LimitChange_, 50) : GetCeiling(LimitChange_, 50);
            else if (LimitChange_ <= 100000)
                return isHigh ? GetFloor(LimitChange_, 100) : GetCeiling(LimitChange_, 100);
            return isHigh ? GetFloor(LimitChange_, 500) : GetCeiling(LimitChange_, 500);
        }
        protected internal uint GetFloor(uint LimitChange, uint TickInCent)
        {
            return Convert.ToUInt32((Reference + MaxChange()) / TickInCent) * TickInCent;
        }
        protected internal uint GetCeiling(uint LimitChange, uint TickInCent)
        {
            return (LimitChange % TickInCent) == 0 ? LimitChange : GetFloor(LimitChange, TickInCent) + TickInCent;
        }
    }
}
