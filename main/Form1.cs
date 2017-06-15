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
using Smart;

namespace Comfup {
    delegate void SetAddInfoCallBack(string text);
    public partial class Form1: Form
    {
        TaiFexCom tfcom;
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

            //先Create , 尚未正式連線,  IP 在正式連線時再輸入即可 ;
            tfcom = new TaiFexCom("10.4.99.71", 8000, "API");
            tfcom.OnRcvMessage += OnTradeRcvMessage;          //資料接收事件
            tfcom.OnGetStatus += OnTradeGetStatus;               //狀態通知事件
            tfcom.OnRcvServerTime += OnRcvServerTime;   //接收主機時間
            tfcom.OnRecoverStatus += OnRecoverStatus;   //回補狀態通知
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
                Intelligence.OnRecover_EvenHandler d = new Intelligence.OnRecover_EvenHandler(OnRecoverStatus);
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
        #region TaiFexCom API 事件
        private void OnTradeRcvMessage(object sender, PackageBase package)
        {
            if (this.InvokeRequired)
            {
                Smart.OnRcvMessage_EventHandler d = new Smart.OnRcvMessage_EventHandler(OnTradeRcvMessage);
                this.Invoke(d, new object[] { sender, package });
                return;
            }
            StringBuilder sbtmp = new StringBuilder();
            switch ((DT)package.DT)
            {
                case DT.LOGIN:  // Bona登入
                    P001503 p1503 = (P001503)package;
                    if (p1503.Code != 0)
                        AddInfo("登入失敗 CODE = " + p1503.Code + " " + tfcom.GetMessageMap(p1503.Code));
                    else
                    {
                        AddInfo("登入成功 ");
                        AddInfo("Yo:" + p1503.p001503_2[0].BrokeId + ":" + p1503.p001503_2[0].Account);
                    }
                    break;
                case DT.SECU_ALLOWANCE_RPT:   //子帳額度控管: 回補
                    PT05002 p5002 = (PT05002)package;
                    AddInfo(p5002.ToLog());
                    break;
                case DT.SECU_ALLOWANCE:
                    PT05003 p5003 = (PT05003)package;
                    AddInfo(p5003.ToLog());
                    break;
                #region 證券下單回報
                case DT.SECU_ORDER_ACK:   //下單第二回覆
//                    if (!cbShowUI.Checked) break;
                    PT04002 p4002 = (PT04002)package;
                    AddInfo(p4002.ToLog() + "訊息:" + tfcom.GetMessageMap(p4002.ErrorCode));
                    break;
                case DT.SECU_ORDER_RPT: // Bona委託回報
//                    if (!cbShowUI.Checked) break;
                    PT04010 p4010 = (PT04010)package;
                    AddInfo("RCV 4010 [" + p4010.CNT + "," + p4010.OrderNo + "]");
                    // "委託型態", "分公司代號", "帳號", "綜合帳戶", "營業員代碼",                                                                                     "委託書號",            "交易日期",             "回報時間",           "委託日期時間",            "商品代號", "下單序號",             "委託來源別",                     "市場別",                        "買賣",                    "委託別",                         "委託種類",                      "委託價格",    "改量前數量",              "改量後數量",    "錯誤代碼", "錯誤訊息"
                    string[] row4010 = { p4010.OrderFunc.ToString(), p4010.BrokerId, p4010.Account, p4010.SubAccount, p4010.OmniAccount, p4010.AgentId, p4010.OrderNo, p4010.TradeDate, p4010.ReportTime, p4010.ClientOrderTime, p4010.StockID, p4010.CNT, p4010.Channel.ToString(), p4010.Market.ToString(), p4010.Side.ToString(), p4010.OrdLot.ToString(), p4010.OrdClass.ToString(), p4010.Price, p4010.BeforeQty, p4010.AfterQty, p4010.ErrCode, p4010.ErrMsg };
//                    dgv4010.Rows.Add(row4010);
                    break;
                case DT.SECU_DEAL_RPT:   //成交回報
//                    if (!cbShowUI.Checked) break;
                    PT04011 p4011 = (PT04011)package;
                    AddInfo("RCV 4011 [" + p4011.CNT + "]");
                    //                            "委託型態",                             "分公司代號", "帳號",              "綜合帳戶", "營業員代碼",                  "委託書號",              "交易日期",               "回報時間", "電子單號",     "來源別",                          "市場別",                         "商品代碼",            "買賣別",             "委託別",                            ",委託種類",                          "成交價格", "成交數量", "市場成交序號"
                    string[] row4011 = { p4011.OrderFunc.ToString(), p4011.BrokerId, p4011.Account, p4011.SubAccount, p4011.OmniAccount, p4011.AgentId, p4011.OrderNo, p4011.TradeDate, p4011.ReportTime, p4011.CNT, p4011.Channel.ToString(), p4011.Market.ToString(), p4011.StockID, p4011.Side.ToString(), p4011.OrdLot.ToString(), p4011.OrdClass.ToString(), p4011.Price, p4011.DealQty, p4011.MarketNo };
//                    dgv4011.Rows.Add(row4011);
                    break;
                #endregion

                #region 證券複委託回報  <2015.11 Add >
                case DT.SECU_SUBORDER_ACK:   //下單第二回覆
//                    if (!cbShowUI.Checked) break;
                    PT04102 p4102 = (PT04102)package;
                    sbtmp.Append("[RCV 4102]").Append(" RequestId=").Append("" + p4102.RequestId)
                             .Append(";CNT=").Append(p4102.CNT)
                             .Append(";WEBID=").Append(p4102.WEBID)
                             .Append(";Code=").Append("" + p4102.Code)
                             //.Append(";CustomerID=").Append(p4102.CustomerID)
                             .Append(";SeqNum=").Append(p4102.SeqNum)
                             .Append(";OrdgSeqNum=").Append(p4102.OrdgSeqNum)
                             .Append(";OrderNo=").Append(p4102.OrderNo)
                             .Append(";ErrCode=").Append(p4102.ErrCode)
                              .Append(";ErrMsg=").Append(p4102.ErrMsg);
                    //AddInfo("RCV 4102 [" + p4102.RequestId + "," + p4102.WEBID + "," +  "]");
                    AddInfo(sbtmp.ToString());
                    break;
                case DT.SECU_SUBORDER_RPT: //委託回報
//                    if (!cbShowUI.Checked) break;
                    PT04110 p4110 = (PT04110)package;
                    AddInfo("RCV 4110 [ SeqNo=" + p4110.SeqNo + " OrderNO=" + p4110.OrderNo + " OrgSeqNo=" + p4110.OrgSeqNo + "]");
                    //"委託型態",            "客戶帳號",             "客戶姓名",                "委託書號",           "交易日期",           "市場別",         "商品代號",           "商品名稱",          "買賣別",                   "幣別",            "價格",          "數量",        "成交/改單數量", "成交狀態",        "成交數量",      "委託狀態",       " 委託序號",   "原始委託序號",       "接單方式",                "錯誤代碼",            "錯誤訊息",         "下單IP", "下單管道",         "通路別",            "建立日期"
                    string[] row4110 = { p4110.OrderFunc, p4110.CustomerId, p4110.CustomerName, p4110.OrderNo, p4110.TradeDate, p4110.Market, p4110.Symbol, p4110.SymbolName, p4110.BS.ToString(), p4110.Currency, p4110.Price, p4110.Qty, p4110.Qty2, p4110.ExeStatus, p4110.ExeQty, p4110.Status, p4110.SeqNo, p4110.OrgSeqNo, p4110.OrderMethod, p4110.ErrorCode, p4110.ErrorMsg, p4110.IP, p4110.SystemID, p4110.Channel, p4110.CreateDate };
//                    dgv4110.Rows.Add(row4110);
                    break;
                case DT.SECU_SUBDEAL_RPT:   //成交回報
//                    if (!cbShowUI.Checked) break;
                    PT04111 p4111 = (PT04111)package;
                    AddInfo("RCV 4111 [" + p4111.OrderNo + "]");
                    //                            "客戶帳號",               "客戶姓名",               "交易日期",                "市場別",        "商品代碼",       "商品名稱",             "委託價格",    "委託數量", "成交價格",             "成交數量",         "委託書號",         "成交序號",         "下單管道",         "通路別"
                    string[] row4111 = { p4111.CustomerId, p4111.CustomerName, p4111.TradeDate, p4111.Market, p4111.Symbol, p4111.SymbolName, p4111.Price, p4111.Qty, p4111.MatchPrice, p4111.MatchQty, p4111.OrderNo, p4111.MatchNo, p4111.SystemID, p4111.Channel };
//                    dgv4111.Rows.Add(row4111);
                    break;
                #endregion
                #region 複委託帳務查詢
                case DT.FINANCIAL_RCORDER:                //4113. 複委託委託查詢
                    PT04113 p4113 = (PT04113)package;
                    AddInfo("RCV 4113 [" + p4113.Code + "][" + p4113.CodeDesc + "][ 筆數:" + p4113.Rows + "]");
                    if (p4113.Code == 0)
                    {
//                        dgvRCQuery.DataSource = p4113.Detail;
//                        SetGridHeader(4113);
                    }
                    break;
                case DT.FINANCIAL_RCMATCHSUM:         //4115. 複委託成交彙總查詢

                    PT04115 p4115 = (PT04115)package;
                    AddInfo("RCV 4115 [" + p4115.Code + "][" + p4115.CodeDesc + "][ 筆數:" + p4115.Rows + "]");
                    if (p4115.Code == 0)
                    {
//                        dgvRCQuery.DataSource = p4115.Detail;
//                        SetGridHeader(4115);
                    }
                    break;
                case DT.FINANCIAL_RCMATCHDETAIL:      //4117. 複委託成交明細查詢
                    PT04117 p4117 = (PT04117)package;
                    AddInfo("RCV 4117 [" + p4117.Code + "][" + p4117.CodeDesc + "][ 筆數:" + p4117.Rows + "]");
                    if (p4117.Code == 0)
                    {
//                        dgvRCQuery.DataSource = p4117.Detail;
//                        SetGridHeader(4117);
                    }
                    break;
                case DT.FINANCIAL_RCPOSITIONSUM:     //4119. 複委託整戶部位明細查詢

                    PT04119 p4119 = (PT04119)package;
                    AddInfo("RCV 4119 [" + p4119.Code + "][" + p4119.CodeDesc + "][ 筆數:" + p4119.Rows + "]");
                    if (p4119.Code == 0)
                    {
//                        dgvRCQuery.DataSource = p4119.Detail;
//                        SetGridHeader(4119);
                    }
                    break;
                case DT.FINANCIAL_RCCURRENCY:          //4121. 複委託單一幣別帳務查詢
                    PT04121 p4121 = (PT04121)package;
                    AddInfo("RCV 4121 [" + p4121.Code + "][" + p4121.CodeDesc + "][ 筆數:" + p4121.Rows + "]");
                    if (p4121.Code == 0)
                    {
//                        dgvRCQuery.DataSource = p4121.Detail;
//                        SetGridHeader(4121);
                    }
                    break;
                case DT.FINANCIAL_RCSTOCKPOSITION:  //4123. 複委託股票庫存部位查詢 ;

                    PT04123 p4123 = (PT04123)package;
                    AddInfo("RCV 4123 [" + p4123.Code + "][" + p4123.CodeDesc + "][ 筆數:" + p4123.Rows + "]");
                    if (p4123.Code == 0)
                    {
//                        dgvRCQuery.DataSource = p4123.Detail;
//                        SetGridHeader(4123);
                    }
                    break;
                case DT.FINANCIAL_RCDELIVERY:           //4125. 複委託交割金額試算 ; 交割:Delivery ;
                    PT04125 p4125 = (PT04125)package;
                    AddInfo("RCV 4125 [" + p4125.Code + "][" + p4125.CodeDesc + "][ 筆數:" + p4125.Rows + "]");
                    if (p4125.Code == 0)
                    {
//                        dgvRCQuery.DataSource = p4125.Detail;
//                        SetGridHeader(4125);
                    }
                    break;

                #endregion

                #region 帳務中台WebSerivce 查詢
                case DT.FINANCIAL_WSSETAMTTRIAL:  //當日交割金額試算查詢
                    PT04302 p4302 = (PT04302)package;
                    AddInfo("RCV 4302 [" + p4302.Code + "][" + p4302.CodeDesc + "][ 筆數:" + p4302.Rows1 + " , " + p4302.Rows2 + "]");
//                    lbResult.Text = "4302[ " + p4302.Rows1 + "]筆";
//                    lbResult1.Text = "4302_2[ " + p4302.Rows2 + "]筆";
                    if (p4302.Code == 0)
                    {
//                        dgvWSDetail1.DataSource = p4302.Detail1;
//                        dgvWSDetail2.DataSource = p4302.Detail2;
//                        SetWSGridHeader(4302);
                    }
                    break;
                case DT.FINANCIAL_WSSETAMTDETAIL: //當日交割金額_沖銷明細(非當沖)查詢
                    PT04304 p4304 = (PT04304)package;
                    AddInfo("RCV 4304 [" + p4304.Code + "][" + p4304.CodeDesc + "][ 筆數:" + p4304.Rows + "]");
//                    lbResult.Text = "4304[ " + p4304.Rows + "]筆";
                    if (p4304.Code == 0)
                    {
//                        dgvWSDetail1.DataSource = p4304.Detail;
//                        SetWSGridHeader(4304);
                    }
                    break;
                case DT.FINANCIAL_WSSETTLEAMT://交割金額查詢(3日)查詢
                    PT04306 p4306 = (PT04306)package;
                    AddInfo("RCV 4306 [" + p4306.Code + "][" + p4306.CodeDesc + "][ 筆數:" + p4306.Rows + "]");
//                    lbResult.Text = "4304[ " + p4306.Rows + "]筆";
                    if (p4306.Code == 0)
                    {
//                        dgvWSDetail1.DataSource = p4306.Detail;
//                        SetWSGridHeader(4306);
                    }
                    break;
                case DT.FINANCIAL_WSINVENTORY://庫存損益及即時維持率試算查詢
                    PT04308 p4308 = (PT04308)package;
                    AddInfo("RCV 4308 [" + p4308.Code + "][" + p4308.CodeDesc + "][ 筆數:" + p4308.Rows + "]");
//                    lbResult.Text = "4308[ " + p4308.Rows + "]筆";
                    if (p4308.Code == 0)
                    {
//                        dgvWSDetail1.DataSource = p4308.Detail;
//                        SetWSGridHeader(4308);
                    }
                    break;
                case DT.FINANCIAL_WSINVENTORYSUM://證券庫存彙總查詢
                    PT04310 p4310 = (PT04310)package;
                    AddInfo("RCV 4310 [" + p4310.Code + "][" + p4310.CodeDesc + "][ 筆數:" + p4310.Rows + "]");
//                    lbResult.Text = "4310[ " + p4310.Rows + "]筆";
                    if (p4310.Code == 0)
                    {
//                        dgvWSDetail1.DataSource = p4310.Detail;
//                        SetWSGridHeader(4310);
                    }
                    break;
                case DT.FINANCIAL_WSBALANCESTATEMENT://證券對帳單查詢
                    PT04312 p4312 = (PT04312)package;
                    AddInfo("RCV 4312 [" + p4312.Code + "][" + p4312.CodeDesc + "][ 筆數:" + p4312.Rows + "]");

//                    lbResult.Text = "4312[ " + p4312.Rows + "]筆";
                    if (p4312.Code == 0)
                    {
//                        dgvWSDetail1.DataSource = p4312.Detail;
//                        SetWSGridHeader(4312);
                    }

                    break;

                #endregion

                case DT.SECU_EARMARK_SET: //圈存 ;
                    //string[] header4031 = { "Requestid", "webid", "1.圈/D.解圈", "分公司代號", "帳號", "證券代號", "申請張數", "回覆張數", "申請日期" ,"申請時間","回覆時間","序號","代碼","訊息"};
                    P004031 p4031 = (P004031)package;
                    //MessageBox.Show("EarMark Set:" + p4031.ErrCode);
                    string[] row4031 = { p4031.RequestId.ToString(), p4031.Webid, p4031.TCode.ToString(), p4031.BrokerId, p4031.Account, p4031.StockNO, p4031.ApplyQTY, p4031.ReplyQTY, p4031.ApplyDate, p4031.ApplyTime, p4031.ReplyTime, p4031.Seqno, p4031.ErrCode, p4031.ErrMsg };
//                    dgv4031.Rows.Add(row4031);
                    break;
            }
        }
        private void OnTradeGetStatus(object sender, COM_STATUS staus, byte[] msg)
        {
            TaiFexCom com = (TaiFexCom)sender;
            if (this.InvokeRequired)
            {
                Smart.OnGetStatus_EventHandler d = new Smart.OnGetStatus_EventHandler(OnTradeGetStatus);
                this.Invoke(d, new object[] { sender, staus, msg });
                return;
            }
//            OnGetStatusUpdateUI(sender, staus, msg);
        }
        private void OnRcvServerTime(Object sender, DateTime serverTime, int ConnQuality)
        {
            if (this.InvokeRequired)
            {
                Smart.OnRcvServerTime_EventHandler d = new Smart.OnRcvServerTime_EventHandler(OnRcvServerTime);
                this.Invoke(d, new object[] { sender, serverTime, ConnQuality });
                return;
            }
            //ConnQuality : 本次與上次 HeatBeat 之時間差(milliseconds)
//            if (ConnQuality > 100)
//                pbConnQuality.Value = 0;
//            else
//                pbConnQuality.Value = 100 - ConnQuality;
            //labelServerTime.Text = String.Format("{0:yyyy/MM/dd hh:mm:ss.fff}", serverTime);
//            labelServerTime.Text = String.Format("{0:hh:mm:ss.fff}", serverTime);
//            labelConnQuality.Text = "[" + ConnQuality + "]";
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
