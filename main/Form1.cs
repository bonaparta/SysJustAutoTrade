using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Intelligence;
using Package;

namespace Comfup {
    delegate void SetAddInfoCallBack(string text);
    public partial class Form1: Form {
        Intelligence.QuoteCom quoteCom;
        Dictionary<string, int> RecoverMap = new Dictionary<string, int>();
        private UTF8Encoding encoding = new System.Text.UTF8Encoding();
        public Form1() {
            InitializeComponent();
            #region Comfup Add
            InitializeComfup();
            #endregion
            quoteCom = new Intelligence.QuoteCom(cbHostStk.Text, 8000, tbSID.Text, tbToken.Text);
            quoteCom.OnRcvMessage += OnQuoteRcvMessage;
            quoteCom.OnGetStatus += OnQuoteGetStatus;
            quoteCom.OnRecoverStatus += OnRecoverStatus;
            quoteCom.QDebugLog = false;
            quoteCom.FQDebugLog = false;
            this.Text = "QuoteCom 範例程式 FOR 證券 [ Version : " + quoteCom.version + " ]";  //2014.6.19 ADD
        }

        private void AddInfo(string msg) {
            if (this.txtMsg.InvokeRequired) {
                SetAddInfoCallBack d = new SetAddInfoCallBack(AddInfo);
                this.Invoke(d, new object[] { msg });
            } else {
                string fMsg = String.Format("[{0}] {1} {2}", DateTime.Now.ToString("hh:mm:ss:ffff"), msg, Environment.NewLine);
                try {
                    txtMsg.AppendText(fMsg);
                } catch { };
            }
        }

        #region QuoteCom API 事件
        private void OnQuoteGetStatus(object sender, COM_STATUS staus, byte[] msg) {
            QuoteCom com = (QuoteCom)sender;
            string smsg = null;
            switch (staus) {
                case COM_STATUS.LOGIN_READY:
                    AddInfo(String.Format("LOGIN_READY:[{0}]", encoding.GetString(msg)));
                    break;
                case COM_STATUS.LOGIN_FAIL:
                    AddInfo(String.Format("LOGIN FAIL:[{0}]", encoding.GetString(msg)));
                    break;
                case COM_STATUS.LOGIN_UNKNOW:
                    AddInfo(String.Format("LOGIN UNKNOW:[{0}]", encoding.GetString(msg)));
                    break;
                case COM_STATUS.CONNECT_READY:
                    //quoteCom.Login(tfcom.Main_ID, tfcom.Main_PWD, tfcom.Main_CENTER);
                    smsg = "QuoteCom: [" + encoding.GetString(msg) + "] MyIP=" + quoteCom.MyIP;
                    AddInfo(smsg);
                    break;
                case COM_STATUS.CONNECT_FAIL:
                    smsg = encoding.GetString(msg);
                    AddInfo("CONNECT_FAIL:" + smsg);
                    break;
                case COM_STATUS.DISCONNECTED:
                    smsg = encoding.GetString(msg);
                    AddInfo("DISCONNECTED:" + smsg);
                    break;
                case COM_STATUS.SUBSCRIBE:
                    smsg = encoding.GetString(msg, 0, msg.Length - 1);
                    AddInfo(String.Format("SUBSCRIBE:[{0}]", smsg));
                    //txtQuoteList.AppendText(String.Format("SUBSCRIBE:[{0}]", smsg));  //2012.02.16 LYNN TEMPORARY ;
                    break;
                case COM_STATUS.UNSUBSCRIBE:
                    smsg = encoding.GetString(msg, 0, msg.Length - 1);
                    AddInfo(String.Format("UNSUBSCRIBE:[{0}]", smsg));
                    break;
                case COM_STATUS.ACK_REQUESTID:
                    long RequestId = BitConverter.ToInt64(msg, 0);
                    byte status = msg[8];
                    AddInfo("Request Id BACK: " + RequestId + " Status=" + status);
                    break;
                case COM_STATUS.RECOVER_DATA:
                    smsg = encoding.GetString(msg, 1, msg.Length - 1);
                    if (!RecoverMap.ContainsKey(smsg))
                        RecoverMap.Add(smsg, 0);

                    if (msg[0] == 0) {
                        RecoverMap[smsg] = 0;
                        AddInfo(String.Format("開始回補 Topic:[{0}]", smsg));
                    }

                    if (msg[0] == 1) {
                        AddInfo(String.Format("結束回補 Topic:[{0} 筆數:{1}]", smsg, RecoverMap[smsg]));
                    }
                    break;
            }
            com.Processed();
        }

        private void OnRecoverStatus(object sender, string Topic, RECOVER_STATUS status, uint RecoverCount) {
            if (this.InvokeRequired) {
                OnRecover_EvenHandler d = new OnRecover_EvenHandler(OnRecoverStatus);
                this.Invoke(d, new object[] { sender, Topic, status, RecoverCount });
                return;
            }
            QuoteCom com = (QuoteCom)sender;
            switch (status) {
                case RECOVER_STATUS.RS_DONE:        //回補資料結束
                    AddInfo(String.Format("結束回補 Topic:[{0}]{1}", Topic, RecoverCount));
                    break;
                case RECOVER_STATUS.RS_BEGIN:       //開始回補資料
                    AddInfo(String.Format("開始回補 Topic:[{0}]", Topic));
                    break;
            }
        }

        private void OnQuoteRcvMessage(object sender, PackageBase package) {
            if (package.TOPIC != null)
                if (RecoverMap.ContainsKey(package.TOPIC))
                    RecoverMap[package.TOPIC]++;

            StringBuilder sb;

            switch (package.DT) {
                case (ushort)DT.LOGIN:
                    P001503 _p001503 = (P001503)package;
                    if (_p001503.Code == 0) {
                        AddInfo("可註冊檔數：" + _p001503.Qnum);
                        if (quoteCom.QuoteFuture) AddInfo("可註冊期貨報價");
                        if (quoteCom.QuoteStock) AddInfo("可註冊證券報價");
                    }
                    break;

                case (ushort)DT.QUOTE_STOCK_MATCH1:   //上市成交
                case (ushort)DT.QUOTE_STOCK_MATCH2:   //上櫃成交
                    PI31001 pi31001 = (PI31001)package;
                    if (!cbShow.Checked) break ;
                    sb = new StringBuilder(Environment.NewLine);
                    sb.Append((package.DT == (ushort)DT.QUOTE_STOCK_MATCH1) ? "上市 " : "上櫃 ");
                    if (pi31001.Status == 0) sb.Append("<試撮>");
                    sb.Append("商品代號: ").Append(pi31001.StockNo).Append("  更新時間: ").Append(pi31001.Match_Time).Append(Environment.NewLine);
                    sb.Append(" 成交價: ").Append(pi31001.Match_Price).Append("  單量: ").Append(pi31001.Match_Qty);
                    sb.Append(" 總量: ").Append(pi31001.Total_Qty).Append("  來源: ").Append(pi31001.Source ).Append(Environment.NewLine);
                    sb.Append("=========================================");
                    AddInfo(sb.ToString());
                    break;

                case (ushort)DT.QUOTE_STOCK_DEPTH1: //上市五檔
                case (ushort)DT.QUOTE_STOCK_DEPTH2: //上櫃五檔
                    PI31002 i31002 = (PI31002)package;
                    if (!cbShow.Checked) break;
                    sb = new StringBuilder(Environment.NewLine);
                    sb.Append((package.DT == (ushort)DT.QUOTE_STOCK_DEPTH1) ? "上市 " : "上櫃 ");
                    if (i31002.Status == 0) sb.Append("<試撮> ");
                    sb.Append("商品代號: ").Append(i31002.StockNo).Append(" 更新時間: ").Append(i31002.Match_Time).Append("  來源: ").Append(i31002.Source ).Append(Environment.NewLine);
                    for (int i = 0; i < 5; i++)
                        sb.Append(String.Format("五檔[{0}] 買[價:{1:N} 量:{2:N}]    賣[價:{3:N} 量:{4:N}]", i + 1, i31002.BUY_DEPTH[i].PRICE, i31002.BUY_DEPTH[i].QUANTITY, i31002.SELL_DEPTH[i].PRICE, i31002.SELL_DEPTH[i].QUANTITY)).Append(Environment.NewLine);
                    sb.Append("=========================================");

                    AddInfo(sb.ToString());
                    break;
                case (ushort)DT.QUOTE_LAST_PRICE_STOCK:
                    PI30026 pi30026 = (PI30026)package;
                    #region Comfup Add
                    UpdateStockComfup(pi30026);
                    #endregion
                    sb = new StringBuilder(Environment.NewLine);
                    sb.Append("商品代號:").Append(pi30026.StockNo).Append(" 最後價格:").Append(pi30026.LastMatchPrice).Append(Environment.NewLine);
                    sb.Append("當日最高成交價格:").Append(pi30026.DayHighPrice).Append(" 當日最低成交價格:").Append(pi30026.DayLowPrice);
                    sb.Append("開盤價:").Append(pi30026.FirstMatchPrice).Append(" 開盤量:").Append(pi30026.FirstMatchQty).Append(Environment.NewLine);
                    sb.Append("參考價:").Append(pi30026.ReferencePrice).Append(Environment.NewLine);
                    sb.Append("成交單量:").Append(pi30026.LastMatchQty).Append(Environment.NewLine);
                    sb.Append("成交總量:").Append(pi30026.TotalMatchQty).Append(Environment.NewLine);
                    for (int i = 0; i < 5; i++)
                        sb.Append(String.Format("五檔[{0}] 買[價:{1:N} 量:{2:N}]    賣[價:{3:N} 量:{4:N}]", i + 1, pi30026.BUY_DEPTH[i].PRICE, pi30026.BUY_DEPTH[i].QUANTITY, pi30026.SELL_DEPTH[i].PRICE, pi30026.SELL_DEPTH[i].QUANTITY)).Append(Environment.NewLine);
                    sb.Append("==============================================");
                    AddInfo(sb.ToString());
                    break;
                case (ushort)DT.QUOTE_STOCK_INDEX1:  //上市指數
                    PI31011 pi31011 = (PI31011)package;
                    sb = new StringBuilder(Environment.NewLine);
                    sb.Append("[上市指數]更新時間：").Append(pi31011.Match_Time).Append("   筆數: ").Append(pi31011.COUNT).Append(Environment.NewLine);
                    for (int i = 0; i < pi31011.COUNT; i++)
                        sb.Append(" [" + (i + 1) + "] ").Append(pi31011.IDX[i].VALUE);
                    sb.Append("==============================================");
                    AddInfo(sb.ToString());
                    break;
                case (ushort)DT.QUOTE_STOCK_INDEX2:  //上櫃指數
                    PI31011 pi32011 = (PI31011)package;
                    sb = new StringBuilder(Environment.NewLine);
                    sb.Append("[上櫃指數]更新時間：").Append(pi32011.Match_Time).Append("   筆數: ").Append(pi32011.COUNT).Append(Environment.NewLine);
                    for (int i = 0; i < pi32011.COUNT; i++)
                        sb.Append(" [" + (i + 1) + "]").Append(pi32011.IDX[i].VALUE);
                    sb.Append("==============================================");
                    AddInfo(sb.ToString());
                    break;
                case (ushort)DT.QUOTE_STOCK_NEWINDEX1:  //上市新編指數
                    PI31021 pi31021 = (PI31021)package;
                    sb = new StringBuilder(Environment.NewLine);
                    sb.Append("上市新編指數[").Append(pi31021.IndexNo).Append("] 時間:").Append(pi31021.IndexTime);
                    sb.Append("指數:  ").Append(pi31021.LatestIndex).Append(Environment.NewLine);
                    AddInfo(sb.ToString());
                    break;
                case (ushort)DT.QUOTE_STOCK_NEWINDEX2:  //上櫃新編指數
                    PI31021 pi32021 = (PI31021)package;
                    sb = new StringBuilder(Environment.NewLine);
                    sb.Append("上櫃新編指數[").Append(pi32021.IndexNo).Append("] 時間:").Append(pi32021.IndexTime);
                    sb.Append("最新指數: ").Append(pi32021.LatestIndex).Append(Environment.NewLine);
                    AddInfo(sb.ToString());
                    break;
                case (ushort)DT.QUOTE_LAST_INDEX1:  //上市最新指數查詢
                    PI31026 pi31026 = (PI31026)package;
                    sb = new StringBuilder(Environment.NewLine);
                    sb.Append("  最新上市指數  筆數: ").Append(pi31026.COUNT).Append(Environment.NewLine);
                    for (int i = 0; i < pi31026.COUNT; i++) {
                        sb.Append(" [" + (i + 1) + "] ").Append(" 昨日收盤指數:").Append(pi31026.IDX[i].RefIndex);
                        sb.Append(" 開盤指數:").Append(pi31026.IDX[i].FirstIndex).Append(" 最新指數:").Append(pi31026.IDX[i].LastIndex);
                        sb.Append(" 最高指數:").Append(pi31026.IDX[i].DayHighIndex).Append(" 最低指數:").Append(pi31026.IDX[i].DayLowIndex).Append(Environment.NewLine);
                        sb.Append("==============================================");
                    }
                    AddInfo(sb.ToString());
                    break;
                case (ushort)DT.QUOTE_LAST_INDEX2:  //上櫃最新指數查詢
                    PI31026 pi32026 = (PI31026)package;
                    sb = new StringBuilder(Environment.NewLine);
                    sb.Append("  最新上櫃指數  筆數: ").Append(pi32026.COUNT).Append(Environment.NewLine);
                    for (int i = 0; i < pi32026.COUNT; i++) {
                        sb.Append(" [" + (i + 1) + "] ").Append(" 昨日收盤指數:").Append(pi32026.IDX[i].RefIndex);
                        sb.Append(" 開盤指數:").Append(pi32026.IDX[i].FirstIndex).Append(" 最新指數:").Append(pi32026.IDX[i].LastIndex);
                        sb.Append(" 最高指數:").Append(pi32026.IDX[i].DayHighIndex).Append(" 最低指數:").Append(pi32026.IDX[i].DayLowIndex).Append(Environment.NewLine);
                        sb.Append("==============================================");
                    }
                    AddInfo(sb.ToString());
                    break;
                case (ushort)DT.QUOTE_STOCK_AVGINDEX:  //加權平均指數 2014.8.6 ADD ;
                    PI31022 pi31022 = (PI31022)package;
                    sb = new StringBuilder(Environment.NewLine);
                    sb.Append("加權平均指數[").Append(pi31022.IndexNo).Append("] 時間:").Append(pi31022.IndexTime);
                    sb.Append("最新指數: ").Append(pi31022.LatestIndex).Append(Environment.NewLine);
                    AddInfo(sb.ToString());
                    break;
            }
        }
        #endregion

        private void button3_Click(object sender, EventArgs e) {
            quoteCom.Logout();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e) {
            quoteCom.Dispose();
        }

        private void button1_Click(object sender, EventArgs e) {
            string host = cbHostStk.Text;
            ushort port = ushort.Parse(tbPortStk.Text);
            string id = tbIDStk.Text;
            string pwd = tbPWDStk.Text;
            char area = ' ';
            quoteCom.SourceId = tbSID.Text;
            quoteCom.Connect2Quote(host, port, id, pwd, area, "" );
        }

        private void btnSubTSEC_Click(object sender, EventArgs e) {
            short istatus;
            if (cbMatch.Checked) {
                istatus = quoteCom.SubQuotesMatch(txtTSEC.Text);
                if (istatus < 0)   //
                    AddInfo("成交:" + quoteCom.GetSubQuoteMsg(istatus));
            }
            if (cbDepth.Checked) {
                istatus = quoteCom.SubQuotesDepth(txtTSEC.Text);
                if (istatus < 0)   //
                    AddInfo("五檔:" + quoteCom.GetSubQuoteMsg(istatus));
            }
        }

        private void btnGetT30_Click(object sender, EventArgs e) {
            short istatus = quoteCom.RetriveProductTSE();
            if (istatus < 0)
                AddInfo(quoteCom.GetSubQuoteMsg(istatus));
            else AddInfo("上市商品檔下載完成");
            #region Comfup Add
            GetAllListComfup(sender, e);
            UpdateListComfup();
            AddInfo("更新完成");
            #endregion
        }

        private void btnShowT30_Click(object sender, EventArgs e) {
            List<string> listT30 = new List<string>();
            listT30 = quoteCom.GetProductListTSC();
            listBox1.Items.Clear();
            if (listT30 == null) {
                listBox1.Items.Add("無法取得上市商品列表,可能未連線/未下載!!");
                return;
            }
            listBox1.Items.Add("上市商品列表");
            listBox1.Items.Add("===============");
            for (int i = 0; i < listT30.Count; i++)
                listBox1.Items.Add(listT30[i]);
        }

        private void btnGetOT30_Click(object sender, EventArgs e) {
            short istatus = quoteCom.RetriveProductOTC();
            if (istatus < 0)
                AddInfo(quoteCom.GetSubQuoteMsg(istatus));
            else AddInfo("上櫃商品檔下載完成");
        }

        private void btnShowOT30_Click(object sender, EventArgs e) {
            List<string> listT30 = new List<string>();
            listT30 = quoteCom.GetProductListOTC();
            listBox1.Items.Clear();
            if (listT30 == null) {
                listBox1.Items.Add("無法取得櫃市商品列表,可能未連線/未下載!!");
                return;
            }
            listBox1.Items.Add("上櫃商品列表");
            listBox1.Items.Add("===============");
            for (int i = 0; i < listT30.Count; i++)
                listBox1.Items.Add(listT30[i]);
        }

        private void btnGetTRange_Click(object sender, EventArgs e) {
            #region Comfup Add
            UpdatePriceComfup();
            #endregion
            short istatus = quoteCom.RetriveLastPriceStock(txtStkno.Text.Trim());
            if (istatus < 0)   //
                AddInfo(quoteCom.GetSubQuoteMsg(istatus));
        }

        private void btnUnSubTSEC_Click(object sender, EventArgs e) {
            if (cbMatch.Checked)
                quoteCom.UnSubQuotesMatch(txtTSEC.Text);
            if (cbDepth.Checked)
                quoteCom.UnSubQuotesDepth(txtTSEC.Text);
        }

        private void button5_Click(object sender, EventArgs e) {
            #region Comfup Add
            GenerateReport();
            #endregion
            PI30001 i30001 = new PI30001();
            i30001 = quoteCom.GetProductSTOCK(txtStkno.Text);
            if (i30001 == null) {
                AddInfo("無法取得該商品明細,可能商品檔未下載或該商品不存在!!");
                return;
            }

            StringBuilder sb;
            sb = new StringBuilder(Environment.NewLine);
            sb.Append("    股票代碼:").Append(i30001.StockNo);
            sb.Append("    股票名稱:").Append(i30001.StockName);
            sb.Append("    市場別:").Append(i30001.Market);
            sb.Append("    漲停價:").Append(i30001.Bull_Price);
            sb.Append("    參考價:").Append(i30001.Ref_Price);
            sb.Append("    跌停價:").Append(i30001.Bear_Price);
            sb.Append("上次交易日:").Append(i30001.LastTradeDate).Append(Environment.NewLine);
            sb.Append("==========================================");
            AddInfo(sb.ToString());
        }

        private void button2_Click(object sender, EventArgs e) {
            short istatus = quoteCom.SubQuotesIndex();
            if (istatus < 0)   //
                AddInfo(quoteCom.GetSubQuoteMsg(istatus));
        }

        private void button4_Click(object sender, EventArgs e) {
            quoteCom.UnSubQuotesIndex();
        }

        private void button6_Click(object sender, EventArgs e) {
            short istatus = quoteCom.RetriveLastIndex(IdxKind.IdxKind_List);
            if (istatus < 0)   //
                AddInfo(quoteCom.GetSubQuoteMsg(istatus));
        }

        private void button7_Click(object sender, EventArgs e) {
            short istatus = quoteCom.RetriveLastIndex(IdxKind.IdxKind_OTC);
            if (istatus < 0)   //
                AddInfo(quoteCom.GetSubQuoteMsg(istatus));

        }

        private void button9_Click(object sender, EventArgs e) {
            short istatus = quoteCom.SubQuotesNewIndex();
            if (istatus < 0)   //
                AddInfo(quoteCom.GetSubQuoteMsg(istatus));
        }

        private void button8_Click(object sender, EventArgs e) {
            quoteCom.UnSubQuotesNewIndex();
        }

        private void btnGetWarrInfo_Click(object sender, EventArgs e) {
            short istatus = quoteCom.RetriveWarrantInfo();
            if (istatus < 0)
                AddInfo(quoteCom.GetSubQuoteMsg(istatus));
        }

        private string Gen30002Msg(PI30002 p30002) {
            string rtn = String.Format("代碼:{0}, 市場別:{1}, 簡稱:{2}, 類型:{3}, 目標股={4}, 目標股名稱:{5}, 履約價:{6}, 漲停價:{7}, 跌停價:{8}, 行使比例:{9}, 最近交易日:{10}, 到期日:{11}, C/P:{12}, 發行餘額量:{13}, 上限價格:{14},下限價格{15}",
                p30002.WarrantID,   //0
                p30002.Market,         //1
                p30002.WarrantAbbr, //2
                p30002.WarrantType, //3
                p30002.TargetStockNo, //4
                p30002.TargetStockNm, //5
                p30002.StrikePrice,//6
                p30002.BullPrice,//7
                p30002.BearPrice, //8
                p30002.UsageRatio, //9
                p30002.LastTradeDate, //10
                p30002.ExpiredDate, //11
                p30002.WarrantVariety,
                p30002.IssuingBalVol ,
                p30002.LimitUpPrice,
                p30002.LimitDownPrice
            );
            return rtn;
        }
        private void btnWList_Click(object sender, EventArgs e) {
            lbWarrant.Items.Clear();
            List<PI30002> lst = null;
            if (rbTSE.Checked)
                lst = quoteCom.GetWarrantList(Security_Market.SM_TWSE);  //上市
            else lst = quoteCom.GetWarrantList(Security_Market.SM_GTSM) ;  //上櫃

            if (lst == null) {
                lbWarrant.Items.Add("查無資料,可能未連線/未下載/無此商品!!");
                return;
            }
            string msg = rbTSE.Checked ? "上市權證商品列表" : "上櫃權證商品列表";
            lbWarrant.Items.Add(msg);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lst.Count; i++){
                lbWarrant.Items.Add(Gen30002Msg(lst[i]));
            }
        }

        private void btnWInfo_Click(object sender, EventArgs e) {
            lbWarrant.Items.Clear();
            PI30002 p30002 = quoteCom.GetWarrantInfo(txtWid.Text);
            if (p30002 == null) {
                lbWarrant.Items.Add("查無資料,可能未連線/未下載/無此商品!!");
                return;
            }
            lbWarrant.Items.Add("權證:[" + txtWid.Text + "]");
            lbWarrant.Items.Add(Gen30002Msg(p30002));
        }

        private void btnWStk_Click(object sender, EventArgs e) {
            lbWarrant.Items.Clear();
            List<PI30002> lst = null;
            lst = quoteCom.GetWarrantTargetStock(txtTargetid.Text);

            if (lst == null) {
                lbWarrant.Items.Add("查無資料,可能未連線/未下載/無此商品!!");
                return;
            }
            string msg = "權證特定標的股 [" + txtTargetid.Text + "] 列表";
            lbWarrant.Items.Add(msg);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lst.Count; i++) {
                lbWarrant.Items.Add(Gen30002Msg(lst[i]));
            }
        }

        private void btnGetWPrice_Click(object sender, EventArgs e) {
            short istatus = quoteCom.RetriveWarrantPrice();
            if (istatus < 0)
                AddInfo(quoteCom.GetSubQuoteMsg(istatus));
        }

        private void btnWPrice_Click(object sender, EventArgs e) {
            lbWarrant.Items.Clear();
            PI30003 p30003 = quoteCom.GetWarrantPrice(txtWPrice.Text);
            if (p30003 == null) {
                lbWarrant.Items.Add("查無資料,可能未連線/未下載/無此商品!!");
                return;
            }
            StringBuilder sb = new StringBuilder() ;
            sb.Append("權證代碼:").Append(p30003.WarrantID).Append(" 交易日期:").Append(p30003.TradeDate).Append(" 總成交量:").Append(p30003.TradeVol);
            sb.Append(" 開盤價:").Append(p30003.OpenPrice).Append(" 最高價:").Append(p30003.HighPrice).Append(" 最低價:").Append(p30003.LowPrice);
            sb.Append(" 收盤價:").Append(p30003.LastPrice).Append(" 最後一盤買價:").Append(p30003.LastBidPrice).Append(" 最後一盤賣價:").Append(p30003.LastAskPrice) ;
            lbWarrant.Items.Add(sb.ToString());
        }

        private void btnGetSPrice_Click(object sender, EventArgs e) {
            short istatus = quoteCom.RetriveStockPrice();
            if (istatus < 0)
                AddInfo(quoteCom.GetSubQuoteMsg(istatus));
        }

        private void button10_Click(object sender, EventArgs e) {
            lbWarrant.Items.Clear();
            PI30003 p30003 = quoteCom.GetStockPrice(txtWPrice.Text);
            if (p30003 == null) {
                lbWarrant.Items.Add("查無資料,可能未連線/未下載/無此商品!!");
                return;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("權證代碼:").Append(p30003.WarrantID).Append(" 交易日期:").Append(p30003.TradeDate).Append(" 總成交量:").Append(p30003.TradeVol);
            sb.Append(" 開盤價:").Append(p30003.OpenPrice).Append(" 最高價:").Append(p30003.HighPrice).Append(" 最低價:").Append(p30003.LowPrice);
            sb.Append(" 收盤價:").Append(p30003.LastPrice).Append(" 最後一盤買價:").Append(p30003.LastBidPrice).Append(" 最後一盤賣價:").Append(p30003.LastAskPrice);
            lbWarrant.Items.Add(sb.ToString());
        }

        private void btnAvgIdx_Click(object sender, EventArgs e) {
            short istatus = quoteCom.SubQuotesAvgIndex();
            if (istatus < 0)   //
                AddInfo(quoteCom.GetSubQuoteMsg(istatus));
        }

        private void button11_Click(object sender, EventArgs e) {
            quoteCom.UnSubQuotesAvgIndex();
        }

        private void bnETF_Click(object sender, EventArgs e) {
            short istatus = quoteCom.RetriveETFStock();
            if (istatus < 0)
                AddInfo(quoteCom.GetSubQuoteMsg(istatus));
        }

        private void button12_Click(object sender, EventArgs e) {

            List<string > lst = null;
            lst = quoteCom.GetETFStocks(tbetfcode.Text);
            lbETF.Items.Clear();
            if (lst == null) {
                lbETF.Items.Add("查無資料,可能未連線/未下載/無此商品!!");
                return;
            }
            string msg = "ETF  [" + tbetfcode.Text + "] 成份股列表";
            lbETF.Items.Add(msg);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lst.Count; i++) {
                lbETF.Items.Add(lst[i]);
            }
        }
    }
}
