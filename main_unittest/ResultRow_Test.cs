using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Comfup;

namespace main_unittest
{
    [TestClass]
    public class ResultRowTest
    {
        [TestMethod]
        public void ResultRow_UnchangeGain_Call()
        {
            DateTime target = DateTime.Parse("2017/12/20");
            DateTime today = DateTime.Today;
            int days = (target - today).Days;
            ResultRow rr = new ResultRow("061790", (target - DateTime.Today).Days,
                570, 560, 570, 1000000, 10, 10, "TSE.TW", "加權指數", 1053274,
                1052237);
            float gain = rr.UnchangeGain();

            Assert.AreEqual("061790", rr.WarrantID);
        }

        [TestMethod]
        public void ResultRow_UnchangeGain_Put()
        {
            DateTime target = DateTime.Parse("2018/07/03");
            DateTime today = DateTime.Today;
            int days = (target - today).Days;
            ResultRow rr = new ResultRow("04144P", (target - DateTime.Today).Days,
                220, 215, 215, 11448, 156, 10, "2317.TW", "鴻海", 10575,
                10650);
            float gain = rr.UnchangeGain();

            Assert.AreEqual("04144P", rr.WarrantID);
        }
    }
}
