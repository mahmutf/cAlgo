﻿using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.Lib;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class MACS_Oil : Robot
    {
        #region Parameter
        private double _initvolume;
        private int _timer;
        private double _break;
        private double _distance;
        private bool _istrade;
        private bool _isbreak;
        private bool _breakfirst;
        private int _resultperiods;
        private int _averageperiods;
        private double _magnify;
        private double _sub;
        #endregion
        private string _datadir;
        private string _filename;
        private Oil_MAC _mac;
        private Oil_MAS _mas;
        private Symbol _xbrsymbol;
        private Symbol _xtisymbol;
        private bool _abovecross;
        private bool _belowcross;
        private bool _risk;
        private string _abovelabel, _belowlabel;
        private List<string> _marklist = new List<string>();
        private OrderParams _init;

        private void SetParams()
        {
            DataTable dt = new DataTable();
            if (!File.Exists(_filename))
                Thread.Sleep(1000);
            if (File.Exists(_filename))
                dt = CSVLib.CsvParsingHelper.CsvToDataTable(_filename, true);
            foreach (DataRow dr in dt.Rows)
            {
                if (dr["symbol"].ToString() == "XBRXTI")
                {
                    if (_initvolume != Convert.ToDouble(dr["initvolume"]))
                    {
                        _initvolume = Convert.ToDouble(dr["initvolume"]);
                        Print("Init_Volume: " + _initvolume.ToString() + "-" + _initvolume.GetType().ToString());
                    }
                    if (_timer != Convert.ToInt32(dr["tmr"]))
                    {
                        _timer = Convert.ToInt32(dr["tmr"]);
                        Print("Timer: " + _timer.ToString() + "-" + _timer.GetType().ToString());
                    }
                    if (_break != Convert.ToDouble(dr["brk"]))
                    {
                        _break = Convert.ToDouble(dr["brk"]);
                        Print("Break: " + _break.ToString() + "-" + _break.GetType().ToString());
                    }
                    if (_distance != Convert.ToDouble(dr["distance"]))
                    {
                        _distance = Convert.ToDouble(dr["distance"]);
                        Print("Distance: " + _distance.ToString() + "-" + _distance.GetType().ToString());
                    }
                    if (_istrade != Convert.ToBoolean(dr["istrade"]))
                    {
                        _istrade = Convert.ToBoolean(dr["istrade"]);
                        Print("IsTrade: " + _istrade.ToString() + "-" + _istrade.GetType().ToString());
                    }
                    if (_isbreak != Convert.ToBoolean(dr["isbreak"]))
                    {
                        _isbreak = Convert.ToBoolean(dr["isbreak"]);
                        Print("IsBreak: " + _isbreak.ToString() + "-" + _isbreak.GetType().ToString());
                    }
                    if (_breakfirst != Convert.ToBoolean(dr["breakfirst"]))
                    {
                        _breakfirst = Convert.ToBoolean(dr["breakfirst"]);
                        Print("BreakFirst: " + _breakfirst.ToString() + "-" + _breakfirst.GetType().ToString());
                    }
                    if (_resultperiods != Convert.ToInt32(dr["resultperiods"]))
                    {
                        _resultperiods = Convert.ToInt32(dr["resultperiods"]);
                        Print("ResultPeriods: " + _resultperiods.ToString() + "-" + _resultperiods.GetType().ToString());
                    }
                    if (_averageperiods != Convert.ToInt32(dr["averageperiods"]))
                    {
                        _averageperiods = Convert.ToInt32(dr["averageperiods"]);
                        Print("AveragePeriods: " + _averageperiods.ToString() + "-" + _averageperiods.GetType().ToString());
                    }
                    if (_magnify != Convert.ToDouble(dr["magnify"]))
                    {
                        _magnify = Convert.ToDouble(dr["magnify"]);
                        Print("Magnify: " + _magnify.ToString() + "-" + _magnify.GetType().ToString());
                    }
                    if (_sub != Convert.ToDouble(dr["sub"]))
                    {
                        _sub = Convert.ToDouble(dr["sub"]);
                        Print("Sub: " + _sub.ToString() + "-" + _sub.GetType().ToString());
                    }
                    break;
                }
            }
        }

        protected override void OnStart()
        {
            _datadir = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\cAlgo\\cbotset\\";
            _filename = _datadir + "\\" + "cBotSet.csv";
            Print("fiName=" + _filename);
            SetParams();
            if (_magnify != 1)
            {
                Print("Please choose the MACS_Magnify.");
                this.Stop();
            }
            Positions.Opened += OnPositionsOpened;
            Positions.Closed += OnPositionsClosed;
            _mac = Indicators.GetIndicator<Oil_MAC>(_resultperiods, _averageperiods, _sub);
            _mas = Indicators.GetIndicator<Oil_MAS>(_resultperiods, _averageperiods, _sub);
            _xbrsymbol = MarketData.GetSymbol("XBRUSD");
            _xtisymbol = MarketData.GetSymbol("XTIUSD");
            _abovecross = true;
            _belowcross = true;
            _risk = false;
            _abovelabel = "Above" + "-" + "XBRXTI" + "-" + MarketSeries.TimeFrame.ToString();
            _belowlabel = "Below" + "-" + "XBRXTI" + "-" + MarketSeries.TimeFrame.ToString();
            _init = new OrderParams(null, null, null, null, null, null, null, null, null, new System.Collections.Generic.List<double> 
            {
                            });

            #region Get Mark
            Position[] pos_above = this.GetPositions(_abovelabel);
            Position[] pos_below = this.GetPositions(_belowlabel);
            var poss = pos_above.Length == 0 ? pos_below : pos_above;
            if (poss.Length != 0)
                foreach (var p in poss)
                {
                    var idx = p.Comment.IndexOf("M_") + 2;
                    if (!_marklist.Contains(p.Comment.Substring(idx, 13)))
                        _marklist.Add(p.Comment.Substring(idx, 13));
                }
            if (_marklist.Count != 0)
            {
                foreach (var mar in _marklist)
                    Print(mar);
            }
            #endregion
            Print("Done OnStart()");
        }

        private void OnPositionsOpened(PositionOpenedEventArgs obj)
        {
            Position pos = obj.Position;
            if (pos.Label != _abovelabel && pos.Label != _belowlabel)
                return;
            var idx = pos.Comment.IndexOf("M_") + 2;
            _marklist.Add(pos.Comment.Substring(idx, 13));
            Print("It's successful to add a mark for XBRXTI.");
        }

        private void OnPositionsClosed(PositionClosedEventArgs obj)
        {
            Position pos = obj.Position;
            if (pos.Label != _abovelabel && pos.Label != _belowlabel)
                return;
            var idx = pos.Comment.IndexOf("M_") + 2;
            if (_marklist.Remove(pos.Comment.Substring(idx, 13)))
                Print("It's successful to remove a mark for XBRXTI.");
        }

        protected override void OnTick()
        {
            #region Parameter
            GetRisk();
            SetParams();
            var cr = _mac.Result.LastValue;
            var ca = _mac.Average.LastValue;
            var sr = _mas.Result.LastValue;
            var sa = _mas.Average.LastValue;
            Position[] pos_above = this.GetPositions(_abovelabel);
            Position[] pos_below = this.GetPositions(_belowlabel);
            var poss = pos_above.Length == 0 ? pos_below : pos_above;
            List<Position> poss_xbr = new List<Position>();
            List<Position> poss_xti = new List<Position>();
            if (poss.Length != 0)
            {
                foreach (var p in poss)
                {
                    if (p.SymbolCode == _xbrsymbol.Code)
                        poss_xbr.Add(p);
                    if (p.SymbolCode == _xtisymbol.Code)
                        poss_xti.Add(p);
                }
                poss_xbr.OrderBy(p => p.EntryTime);
                poss_xti.OrderBy(p => p.EntryTime);
            }
            #endregion

            #region Cross
            if (pos_above.Length == 0)
                _abovecross = true;
            else
            {
                if (sr > sa)
                    _abovecross = true;
            }
            if (pos_below.Length == 0)
                _belowcross = true;
            else
            {
                if (sr < sa)
                    _belowcross = true;
            }
            #endregion

            #region Close
            //Risk
            if (_risk)
            {
                Print("There is a risk for the current symbol.");
                if (poss_xbr.Count >= 2 && poss_xti.Count >= 2)
                {
                    var first_xbr = poss_xbr[0];
                    var second_xbr = poss_xbr[1];
                    var first_xti = poss_xti[0];
                    var second_xti = poss_xti[1];
                    var last0_xbr = poss_xbr.OrderByDescending(p => p.EntryTime).ToArray()[0];
                    var last1_xbr = poss_xbr.OrderByDescending(p => p.EntryTime).ToArray()[1];
                    var last0_xti = poss_xti.OrderByDescending(p => p.EntryTime).ToArray()[0];
                    var last1_xti = poss_xti.OrderByDescending(p => p.EntryTime).ToArray()[1];
                    var first_net = first_xbr.NetProfit + first_xti.NetProfit;
                    var second_net = second_xbr.NetProfit + second_xti.NetProfit;
                    var last0_net = last0_xbr.NetProfit + last0_xti.NetProfit;
                    var last1_net = last1_xbr.NetProfit + last1_xti.NetProfit;
                    if (last1_net < 0 && first_net + last0_net > 0)
                    {
                        this.ClosePosition(last0_xbr);
                        this.ClosePosition(last0_xti);
                        this.ClosePosition(first_xbr);
                        this.ClosePosition(first_xti);
                        _risk = false;
                        return;
                    }
                    else if (last1_net > 0)
                    {
                        this.ClosePosition(last0_xbr);
                        this.ClosePosition(last0_xti);
                        _risk = false;
                        return;
                    }
                }
            }
            if (pos_above.Length != 0)
            {
                if (GetClose(_abovelabel))
                {
                    if (sr <= _sub / 5)
                    {
                        this.closeAllLabel(_abovelabel);
                        _risk = false;
                    }
                }
                else
                {
                    if (sr <= 0)
                    {
                        this.closeAllLabel(_abovelabel);
                        _risk = false;
                    }
                }
            }
            if (pos_below.Length != 0)
            {
                if (GetClose(_belowlabel))
                {
                    if (sr >= -_sub / 5)
                    {
                        this.closeAllLabel(_belowlabel);
                        _risk = false;
                    }
                }
                else
                {
                    if (sr >= 0)
                    {
                        this.closeAllLabel(_belowlabel);
                        _risk = false;
                    }
                }
            }
            #endregion

            if (_istrade)
            {
                #region Open
                #region Above
                if (GetOpen() == "above")
                {
                    var volume = GetOpenVolume(GetOpen());
                    _init.TradeType = TradeType.Sell;
                    _init.Symbol = _xbrsymbol;
                    _init.Volume = Symbol.NormalizeVolume(volume, RoundingMode.ToNearest);
                    _init.Label = _abovelabel;
                    _init.Comment = "CR_" + string.Format("{0:000000}", Math.Round(cr)) + "<";
                    _init.Comment += "BR_000" + "<";
                    _init.Comment += "D_" + string.Format("{0:000}", _distance) + "<";
                    _init.Comment += "S_" + string.Format("{0:000}", _sub) + "<";
                    _init.Comment += "B_" + string.Format("{0:000}", _break) + "<";
                    _init.Comment += "P_" + string.Format("{0:000}", poss_xbr.Count + 1) + "<";
                    _init.Comment += "M_" + _mas.Mark + "<";
                    this.executeOrder(_init);
                    if (this.LastResult.IsSuccessful)
                    {
                        _init.TradeType = TradeType.Buy;
                        _init.Symbol = _xtisymbol;
                        _init.Volume = _init.Volume;
                        this.executeOrder(_init);
                    }
                    _abovecross = false;
                }
                if (GetOpen() == "above_br" && _isbreak)
                {
                    var volume = GetOpenVolume(GetOpen());
                    _init.TradeType = TradeType.Sell;
                    _init.Symbol = _xbrsymbol;
                    _init.Volume = Symbol.NormalizeVolume(volume, RoundingMode.ToNearest);
                    _init.Label = _abovelabel;
                    _init.Comment = "CR_" + string.Format("{0:000000}", Math.Round(cr)) + "<";
                    _init.Comment += "BR_" + string.Format("{0:000}", GetBreak(_abovelabel) + _distance) + "<";
                    _init.Comment += "D_" + string.Format("{0:000}", _distance) + "<";
                    _init.Comment += "S_" + string.Format("{0:000}", _sub) + "<";
                    _init.Comment += "B_" + string.Format("{0:000}", _break) + "<";
                    _init.Comment += "P_" + string.Format("{0:000}", poss_xbr.Count + 1) + "<";
                    _init.Comment += "M_" + _mas.Mark + "<";
                    this.executeOrder(_init);
                    if (this.LastResult.IsSuccessful)
                    {
                        _init.TradeType = TradeType.Buy;
                        _init.Symbol = _xtisymbol;
                        _init.Volume = _init.Volume;
                        this.executeOrder(_init);
                    }
                }
                #endregion
                #region Below
                if (GetOpen() == "below")
                {
                    var volume = GetOpenVolume(GetOpen());
                    _init.TradeType = TradeType.Buy;
                    _init.Symbol = _xbrsymbol;
                    _init.Volume = Symbol.NormalizeVolume(volume, RoundingMode.ToNearest);
                    _init.Label = _belowlabel;
                    _init.Comment = "CR_" + string.Format("{0:000000}", Math.Round(cr)) + "<";
                    _init.Comment += "BR_000" + "<";
                    _init.Comment += "D_" + string.Format("{0:000}", _distance) + "<";
                    _init.Comment += "S_" + string.Format("{0:000}", _sub) + "<";
                    _init.Comment += "B_" + string.Format("{0:000}", _break) + "<";
                    _init.Comment += "P_" + string.Format("{0:000}", poss_xbr.Count + 1) + "<";
                    _init.Comment += "M_" + _mas.Mark + "<";
                    this.executeOrder(_init);
                    if (this.LastResult.IsSuccessful)
                    {
                        _init.TradeType = TradeType.Sell;
                        _init.Symbol = _xtisymbol;
                        _init.Volume = _init.Volume;
                        this.executeOrder(_init);
                    }
                    _belowcross = false;
                }
                if (GetOpen() == "below_br" && _isbreak)
                {
                    var volume = GetOpenVolume(GetOpen());
                    _init.TradeType = TradeType.Buy;
                    _init.Symbol = _xbrsymbol;
                    _init.Volume = Symbol.NormalizeVolume(volume, RoundingMode.ToNearest);
                    _init.Label = _belowlabel;
                    _init.Comment = "CR_" + string.Format("{0:000000}", Math.Round(cr)) + "<";
                    _init.Comment += "BR_" + string.Format("{0:000}", GetBreak(_belowlabel) + _distance) + "<";
                    _init.Comment += "D_" + string.Format("{0:000}", _distance) + "<";
                    _init.Comment += "S_" + string.Format("{0:000}", _sub) + "<";
                    _init.Comment += "B_" + string.Format("{0:000}", _break) + "<";
                    _init.Comment += "P_" + string.Format("{0:000}", poss_xbr.Count + 1) + "<";
                    _init.Comment += "M_" + _mas.Mark + "<";
                    this.executeOrder(_init);
                    if (this.LastResult.IsSuccessful)
                    {
                        _init.TradeType = TradeType.Sell;
                        _init.Symbol = _xtisymbol;
                        _init.Volume = _init.Volume;
                        this.executeOrder(_init);
                    }
                }
                #endregion
                #endregion
            }
        }

        private string GetOpen()
        {
            if (!GetTradeTime())
                return null;
            #region Parameter
            string signal = null;
            Position[] pos_above = this.GetPositions(_abovelabel);
            Position[] pos_below = this.GetPositions(_belowlabel);
            var poss = pos_above.Length == 0 ? pos_below : pos_above;
            var cr = _mac.Result.LastValue;
            var ca = _mac.Average.LastValue;
            var sr = _mas.Result.LastValue;
            var sa = _mas.Average.LastValue;
            var nowtime = DateTime.UtcNow;
            List<DateTime> lastpostime = new List<DateTime>();
            if (poss.Length != 0)
            {
                lastpostime.Add(this.LastPosition(poss).EntryTime.AddHours(_timer));
            }
            var pos_lasttime = lastpostime.Count == 0 ? DateTime.UtcNow.AddHours(-_timer) : lastpostime.Max();
            #endregion

            if (DateTime.Compare(nowtime, pos_lasttime) < 0)
                return null;
            if ((_isbreak && poss.Length != 0) || (_isbreak && _breakfirst))
            {
                if (sr >= GetBreak(_abovelabel))
                    return signal = "above_br";
                if (sr <= -GetBreak(_belowlabel))
                    return signal = "below_br";
            }
            var sig = _mas.SignalOne;
            if (sig == null)
            {
                return null;
            }

            if (!_marklist.Contains(_mas.Mark))
            {
                if (sig == "above" && _abovecross)
                {
                    signal = "above";
                    if (pos_above.Length != 0)
                    {
                        var idx = this.LastPosition(pos_above).Comment.IndexOf("CR_") + 3;
                        if (cr - GetDistance() < Convert.ToDouble(this.LastPosition(pos_above).Comment.Substring(idx, 6)))
                            signal = null;
                    }
                }
                if (sig == "below" && _belowcross)
                {
                    signal = "below";
                    if (pos_below.Length != 0)
                    {
                        var idx = this.LastPosition(pos_below).Comment.IndexOf("CR_") + 3;
                        if (cr + GetDistance() > Convert.ToDouble(this.LastPosition(pos_below).Comment.Substring(idx, 6)))
                            signal = null;
                    }
                }
            }
            return signal;
        }

        private double GetOpenVolume(string opensignal)
        {
            double volume = 0;
            if (opensignal == null)
                return _initvolume;
            string label = opensignal.Substring(0, 1).ToUpper() + opensignal.Substring(1, 4);
            label = label + "-" + "XBRXTI" + "-" + MarketSeries.TimeFrame.ToString();
            var poss = this.GetPositions(label, Symbol);
            if (poss.Length == 0)
                return _initvolume;
            List<Position> list_poss = new List<Position>();
            var cr = _mac.Result.LastValue;
            var ca = _mac.Average.LastValue;
            var sr = _mas.Result.LastValue;
            var sa = _mas.Average.LastValue;
            foreach (var p in poss)
            {
                var idx = p.Comment.IndexOf("CR_") + 3;
                double pcr = Convert.ToDouble(p.Comment.Substring(idx, 6));
                if (pcr < ca && sr > 0)
                    list_poss.Add(p);
                if (pcr > ca & sr < 0)
                    list_poss.Add(p);
            }
            if (list_poss.Count > 0)
            {
                foreach (var p in list_poss)
                {
                    volume += p.Volume * 2;
                }
            }

            if (this.LastPosition(poss).Volume > volume)
                volume = this.LastPosition(poss).Volume;
            if (_initvolume > volume)
                volume = _initvolume;
            return volume;
        }

        private double GetDistance()
        {
            return _distance;
        }

        private bool GetClose(string label)
        {
            var poss = this.GetPositions(label, Symbol);
            if (poss.Length != 0)
            {
                int barsago = MarketSeries.barsAgo(this.FirstPosition(poss));
                if (barsago > 24 || poss.Length > 1)
                    return true;
            }
            return false;
        }

        private double GetBreak(string label)
        {
            var poss = this.GetPositions(label, Symbol);
            var sr = Math.Abs(_mas.Result.LastValue);
            double br = _break;
            if (br < sr)
                br = Math.Floor(sr);
            if (poss.Length != 0)
            {
                foreach (var p in poss)
                {
                    var idx = p.Comment.IndexOf("BR_") + 3;
                    if (br < Convert.ToDouble(p.Comment.Substring(idx, 3)))
                        br = Convert.ToDouble(p.Comment.Substring(idx, 3));
                }
            }
            return br;
        }

        private bool GetTradeTime()
        {
            if (Symbol.Spread / Symbol.PipSize <= 5)
                return true;
            var now = DateTime.UtcNow;
            var hour = now.Hour;
            if (hour >= 20)
                return false;
            return true;
        }

        private void GetRisk()
        {
            Position[] pos_above = this.GetPositions(_abovelabel, Symbol);
            Position[] pos_below = this.GetPositions(_belowlabel, Symbol);
            var poss = pos_above.Length == 0 ? pos_below : pos_above;
            if (poss.Length == 0)
            {
                _risk = false;
                return;
            }

            List<Position> list_poss = new List<Position>();
            var cr = _mac.Result.LastValue;
            var ca = _mac.Average.LastValue;
            var sr = _mas.Result.LastValue;
            var sa = _mas.Average.LastValue;
            foreach (var p in poss)
            {
                var idx = p.Comment.IndexOf("CR_") + 3;
                double pcr = Convert.ToDouble(p.Comment.Substring(idx, 6));
                if (pcr < ca && sr > 0)
                    list_poss.Add(p);
                if (pcr > ca & sr < 0)
                    list_poss.Add(p);
            }
            if (list_poss.Count > 1)
            {
                _risk = true;
            }
            else
            {
                _risk = false;
            }
        }
    }
}
