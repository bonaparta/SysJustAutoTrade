using System;
using System.Collections.Generic;
using Intelligence;
using Package;

namespace Comfup
{
    partial class Form1
    {
        private void GetAllListComfup(object sender, EventArgs e)
        {
            // T30.xml 開盤參考價
            // OT30.xml 開盤參考價
            btnGetOT30_Click(sender, e);
            // TSEWarrantInfo.xml 履約日 / 可轉換比率
            btnGetWarrInfo_Click(sender, e);
            // TSEWarrantPrice.xml
            btnGetWPrice_Click(sender, e);
            // TSEStockPrice.xml
            btnGetSPrice_Click(sender, e);
        }

        private void UpdateListComfup()
        {
            StocksTable = new StocksTable(quoteCom);
            WarrantsTable = new WarrantsTable(quoteCom);
            int ret_ = StockWarrantUpdate.UpdateRefereceXML(quoteCom, StocksTable, WarrantsTable);
        }

        private void UpdatePriceComfup()
        {
            HashSet<string> stockList = new HashSet<string>();
            foreach(KeyValuePair<string, Warrant> target in WarrantsTable.Warrants)
            {
                stockList.Add(target.Value.TargetStockID);
            }
            foreach (string targetStock in stockList)
            {
                short istatus = quoteCom.RetriveLastPriceStock(targetStock);
                if (istatus < 0)   //
                    AddInfo(quoteCom.GetSubQuoteMsg(istatus));
            }
            Console.WriteLine("Dic '{0}' size", stockList.Count);
        }

        private void UpdateStockComfup(PI30026 pi30026)
        {
            Stock target;
            if(StocksTable.Stocks.TryGetValue(pi30026.StockNo, out target))
            {
                target.Close = Convert.ToUInt32(pi30026.LastMatchPrice * 100);
                target.High = Convert.ToUInt32(pi30026.DayHighPrice * 100);
                target.Low = Convert.ToUInt32(pi30026.DayLowPrice * 100);
            }
        }

        private StocksTable StocksTable;
        private WarrantsTable WarrantsTable;
    }
}
