using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Intelligence;
using Package;

namespace Comfup
{
    class StocksTable
    {
        public StocksTable(QuoteCom quote)
        {
            quoteCom = quote;
            Stocks = new Dictionary<string, Stock>();
        }
        public int UpdateReferenceByMarket(List<string> lst, Dictionary<string, Warrant> warrants)
        {
            Console.WriteLine("All TSC/OTC Size: " + lst.Count);
            for (int i = 0; i < lst.Count; ++i)
            {
                string[] substring = lst[i].Split('|');
                Warrant targetWarrant;
                if (!warrants.TryGetValue(substring[0], out targetWarrant))
                {
                    PI30001 stock = quoteCom.GetProductSTOCK(substring[0]);
                    Stock st = new Stock(stock.StockNo, stock.StockName, Convert.ToUInt32(stock.Ref_Price));
                    Stocks.Add(stock.StockNo, st);
                }
            }
            Console.WriteLine("Stock Size: " + Stocks.Count);
            return 0;
        }
		public Stock GetStock(string id)
		{
            Stock stock;
			if(Stocks.TryGetValue(id, out stock))
			{
				return stock;
			}
			return null; // stock == null
		}
        public Dictionary<string, Stock> Stocks;
//        public List<Stock> OTCStocks; // not sure if needed
        private QuoteCom quoteCom;
    }
}
