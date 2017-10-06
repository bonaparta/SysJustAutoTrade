using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Comfup;

namespace main_unittest
{
    [TestClass]
    public class ResultRowTest
    {
        [TestMethod]
        public void ResultRow_UnchangeGain()
        {
            DateTime target = DateTime.Parse("2017/12/20");
            DateTime today = DateTime.Today;
            int days = (target - today).Days;
            ResultRow rr = new ResultRow("061789", (target - DateTime.Today).Days, 520, 510, 520, 1050000, 20, 10, "TSE.TW", "加權指數",
                1030000, 1029000);
            float gain = rr.UnchangeGain();

            Assert.AreEqual("061789", rr.WarrantID);
        }
    }
}
