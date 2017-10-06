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
            ResultRow rr = new ResultRow("064696", 120, 111, 110, 111, 1100000, 10, 10, "TSE.TW", "加權指數",
                1029660, 1032492);

            Assert.AreEqual("064696", rr.WarrantID);
        }
    }
}
