using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comfup
{
    class StockWarrantUpdate
    {
        public static int UpdateRefereceXML(Intelligence.QuoteCom quoteCom, StocksTable stocks, WarrantsTable warrants)
        {
            List<string> twse = quoteCom.GetProductListTSC();
            List<string> otc = quoteCom.GetProductListOTC();

            warrants.BuildWarrantsAndUpdateExpiredDateByMarket();
            warrants.UpdateReferenceByMarket(twse);
            warrants.UpdateReferenceByMarket(otc);

            stocks.UpdateReferenceByMarket(twse, warrants.WarrantsAll);
            stocks.UpdateReferenceByMarket(otc, warrants.WarrantsAll);
            return 0;
        }
    }
}
