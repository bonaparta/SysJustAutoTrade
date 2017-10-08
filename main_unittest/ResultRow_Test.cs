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
            ResultRow rr = new ResultRow("061790", "2017/12/20",
                570, 560, 570, 1000000, 10, "TSE.TW", "加權指數", 1053274,
                1052237);
            float gain = rr.GetUnchangeGain();

            Assert.AreEqual("061790", rr.WarrantID);
        }

        [TestMethod]
        public void ResultRow_UnchangeGain_Put()
        {
            ResultRow rr = new ResultRow("04144P", "2018/07/03",
                220, 215, 215, 11448, 156, "2317.TW", "鴻海", 10575,
                10650);
            float gain = rr.GetUnchangeGain();

            Assert.AreEqual("04144P", rr.WarrantID);
        }
    }
}
