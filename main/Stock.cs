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
        public uint LimitHigh()
        {
            return LimitAll(false);
        }
        public uint LimitLow()
        {
            return LimitAll(true);
        }
        public uint LimitHighChange()
        {
            return LimitHigh() - Reference;
        }
        public uint LimitLowChange()
        {
            return LimitLow() - Reference;
        }
        private uint GetChange()
        {
            return Convert.ToUInt32(Reference * Limits.LimitFloat);
        }
        private uint LimitAll(bool isCeiling)
        {
            if (Reference <= 1000)
                return isCeiling? GetCeiling(1) : GetFloor(1);
            else if (Reference <= 5000)
                return isCeiling ? GetCeiling(5) : GetFloor(5);
            else if (Reference <= 10000)
                return isCeiling ? GetCeiling(10) : GetFloor(10);
            else if (Reference <= 50000)
                return isCeiling ? GetCeiling(50) : GetFloor(50);
            else if (Reference <= 100000)
                return isCeiling ? GetCeiling(100) : GetFloor(100);
            return isCeiling ? GetCeiling(500) : GetFloor(500);
        }
        protected internal uint GetFloor(uint TickInCent)
        {
            return Convert.ToUInt32((Reference + GetChange()) / TickInCent) * TickInCent;
        }
        protected internal uint GetCeiling(uint TickInCent)
        {
            uint RetChange_ = Reference + GetChange();
            return (RetChange_ % TickInCent) == 0 ? RetChange_ : GetFloor(TickInCent) + TickInCent;
        }
    }
}
