using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Intelligence;
using Package;

namespace Comfup
{    
    class WarrantsTable
    {
        public WarrantsTable(QuoteCom quote)
        {
            quoteCom = quote;
            Warrants = new Dictionary<string, Warrant>();
            WarrantsAll = new Dictionary<string, Warrant>();
        }
        public int BuildWarrantsAndUpdateExpiredDateByMarket()
        {
            // Update Expired Date
            // Load from TSEWarrantInfo.xml or quoteCom.GetWarrantList(Security_Market.SM_TWSE)
            UpdateExpiredDateByMarket(quoteCom.GetWarrantList(Security_Market.SM_TWSE));
            UpdateExpiredDateByMarket(quoteCom.GetWarrantList(Security_Market.SM_GTSM));
            return 0;
        }
        public int UpdateReferenceByMarket(List<string> lst)
        {
            Console.WriteLine("All TSC/OTC Size(Warrant): " + lst.Count);
            for (int i = 0; i < lst.Count; ++i)
            {
                string[] substring = lst[i].Split('|');
                PI30001 stock = quoteCom.GetProductSTOCK(substring[0]);
                Warrant targetWarrant;
                if(Warrants.TryGetValue(stock.StockNo, out targetWarrant))
                {
                    targetWarrant.Reference = Convert.ToUInt32(stock.Ref_Price);
                }
            }
            Console.WriteLine("Warrant Size: " + Warrants.Count);
            return 0;
        }
        private int UpdateExpiredDateByMarket(List<PI30002> lst)
        {
            Console.WriteLine("All Warrant TSC/OTC Size: " + lst.Count);
            foreach (PI30002 warrantSource in lst)
            {
                Warrant warrantTarget = new Warrant(warrantSource.WarrantID, warrantSource.WarrantAbbr);
                warrantTarget.TargetStockID = warrantSource.TargetStockNo;
                warrantTarget.TargetStockName = warrantSource.TargetStockNm;
                warrantTarget.UsageRatio = Convert.ToUInt32(warrantSource.UsageRatio * 100);
                warrantTarget.StrikePrice = Convert.ToUInt32(warrantSource.StrikePrice * 100);
                if (!warrantSource.ExpiredDate.Equals("") && !warrantTarget.TargetStockID.Equals(""))
                {
                    warrantTarget.ExpiredDays = DateTime.ParseExact(warrantSource.ExpiredDate, "yyyymmdd", null).Date.Subtract(DateTime.Now.Date).Days;
                    Warrants.Add(warrantTarget.ID, warrantTarget);
                }
                WarrantsAll.Add(warrantTarget.ID, warrantTarget);
            }
            Console.WriteLine("Warrant Size: " + Warrants.Count);
            return 0;
        }
        public Dictionary<string, Warrant> Warrants;
        public Dictionary<string, Warrant> WarrantsAll;
        //        public List<Stock> OTCWarrants; // not sure if needed
        private QuoteCom quoteCom;
    }
}
