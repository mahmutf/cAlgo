﻿using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.Lib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class ArbitrageHedge_Metals : Robot
    {
        [Parameter("INIT_Volume", DefaultValue = 1, MinValue = 1)]
        public double Init_Volume { get; set; }

        [Parameter(DefaultValue = "XAUUSD")]
        public string FirstSymbol { get; set; }

        [Parameter(DefaultValue = "XAGUSD")]
        public string SecondSymbol { get; set; }

        [Parameter(DefaultValue = 120)]
        public int Period { get; set; }

        [Parameter(DefaultValue = 30)]
        public int Distance { get; set; }

        [Parameter(DefaultValue = 1)]
        public int timer { get; set; }

        [Parameter(DefaultValue = 80)]
        public double Ratio { get; set; }

        [Parameter(DefaultValue = 5)]
        public double Magnify { get; set; }

        [Parameter(DefaultValue = false)]
        public bool IsTrade { get; set; }

        private Currency_Highlight currency;
        private Currency_Sub_Highlight currency_sub;
        private bool AboveCross;
        private bool BelowCross;
        private string AboveLabel, BelowLabel;
        private Symbol _firstsymbol, _secondsymbol;
        private List<string> list_mark = new List<string>();
        private List<string> _metalssymbol = new List<string>();
        private List<string> _oilsymbol = new List<string>();
        private Colors PCorel, NCorel, NoCorel;
        private OrderParams initBuyF, initBuyS, initSellF, initSellS;

        protected override void OnStart()
        {
            // Currency_Highlight has two public parameters that were BarsAgo and _ratio.
            currency = Indicators.GetIndicator<Currency_Highlight>(FirstSymbol, SecondSymbol, Period, Distance, Ratio, Magnify);
            // Currency_Sub_Highlight has three public parameters that they were SIG, BarsAgo_Sub and Mark.
            currency_sub = Indicators.GetIndicator<Currency_Sub_Highlight>(FirstSymbol, SecondSymbol, Period, Distance, Ratio, Magnify);

            AboveCross = false;
            BelowCross = false;

            string _currencysymbol = (FirstSymbol.Substring(0, 3) == "USD" ? FirstSymbol.Substring(3, 3) : FirstSymbol.Substring(0, 3)) + (SecondSymbol.Substring(0, 3) == "USD" ? SecondSymbol.Substring(3, 3) : SecondSymbol.Substring(0, 3));
            Print("The currency of the current transaction is : " + _currencysymbol + ".");
            AboveLabel = "Above" + "-" + _currencysymbol + "-" + MarketSeries.TimeFrame.ToString();
            BelowLabel = "Below" + "-" + _currencysymbol + "-" + MarketSeries.TimeFrame.ToString();

            _firstsymbol = MarketData.GetSymbol(FirstSymbol);
            _secondsymbol = MarketData.GetSymbol(SecondSymbol);

            _metalssymbol.Add("XAUUSD");
            _metalssymbol.Add("XAGUSD");
            _oilsymbol.Add("XBRUSD");
            _oilsymbol.Add("XTIUSD");

            PCorel = Colors.Lime;
            NCorel = Colors.OrangeRed;
            NoCorel = Colors.Gray;

            #region OrderParams
            initBuyF = new OrderParams(TradeType.Buy, _firstsymbol, Init_Volume, null, null, null, null, null, null, new System.Collections.Generic.List<double> 
            {
                            });
            initSellF = new OrderParams(TradeType.Sell, _firstsymbol, Init_Volume, null, null, null, null, null, null, new System.Collections.Generic.List<double> 
            {
                            });
            initBuyS = new OrderParams(TradeType.Buy, _secondsymbol, Init_Volume, null, null, null, null, null, null, new System.Collections.Generic.List<double> 
            {
                            });
            initSellS = new OrderParams(TradeType.Sell, _secondsymbol, Init_Volume, null, null, null, null, null, null, new System.Collections.Generic.List<double> 
            {
                            });
            #endregion
        }

        protected override void OnTick()
        {
            #region Parameter
            var UR = currency.Result.LastValue;
            var UA = currency.Average.LastValue;
            var SR = currency_sub.Result.LastValue;
            var SA = currency_sub.Average.LastValue;

            List<Position> Pos_above = new List<Position>(this.GetPositions(AboveLabel));
            List<Position> Pos_below = new List<Position>(this.GetPositions(BelowLabel));
            Pos_above.Reverse();
            Pos_below.Reverse();
            #endregion

            #region Cross
            if (Pos_above.Count == 0)
                AboveCross = true;
            else
            {
                if (SR > SA)
                    AboveCross = true;
            }
            if (Pos_below.Count == 0)
                BelowCross = true;
            else
            {
                if (SR < SA)
                    BelowCross = true;
            }
            #endregion

            #region Close
            if (Pos_above.Count != 0)
            {
                if (GetClose(AboveLabel))
                {
                    if (SR <= 10)
                        this.closeAllLabel(AboveLabel);
                }
                else
                {
                    if (SR <= 0)
                        this.closeAllLabel(AboveLabel);
                }
            }
            if (Pos_below.Count != 0)
            {
                if (GetClose(BelowLabel))
                {
                    if (SR >= -10)
                        this.closeAllLabel(BelowLabel);
                }
                else
                {
                    if (SR >= 0)
                        this.closeAllLabel(BelowLabel);
                }
            }

            #endregion

            #region Close_OverBreak
            if (Pos_above.Count() != 0)
            {
                if (UR - Convert.ToInt32(this.FirstPosition(AboveLabel).Comment.Substring(0, 6)) > GetBreak(AboveLabel) - 100 && GetBreakCount(AboveLabel) != 0)
                {
                    this.closeAllLabel(AboveLabel);
                }
            }
            if (Pos_below.Count() != 0)
            {
                if (Convert.ToInt32(this.FirstPosition(BelowLabel).Comment.Substring(0, 6)) - UR > GetBreak(BelowLabel) - 100 && GetBreakCount(BelowLabel) != 0)
                {
                    this.closeAllLabel(BelowLabel);
                }
            }
            #endregion

            #region Mark
            if (Pos_above.Count != 0)
                foreach (var p in Pos_above)
                {
                    if (!list_mark.Contains(p.Comment.Substring(15)))
                        list_mark.Add(p.Comment.Substring(15));
                }
            if (Pos_below.Count != 0)
                foreach (var p in Pos_below)
                {
                    if (!list_mark.Contains(p.Comment.Substring(15)))
                        list_mark.Add(p.Comment.Substring(15));
                }
            #endregion

            Chart();

            if (IsTrade)
            {
                #region Above
                if (OpenSignal() == "above")
                {
                    initSellF.Volume = _firstsymbol.NormalizeVolume(Init_Volume * Math.Pow(2, Math.Floor((double)Pos_above.Count / 2)), RoundingMode.ToNearest);
                    initSellF.Label = AboveLabel;
                    initSellF.Comment = string.Format("{0:000000}", Math.Round(UR)) + "-" + string.Format("{0:000}", CrossAgo(Pos_above)) + "-" + string.Format("{0:000}", Pos_above.Count + 1) + "-" + currency_sub.Mark;
                    this.executeOrder(initSellF);
                    initBuyS.Volume = _secondsymbol.NormalizeVolume(Init_Volume * Ratio * Math.Pow(2, Math.Floor((double)Pos_above.Count / 2)), RoundingMode.ToNearest);
                    initBuyS.Label = AboveLabel;
                    initBuyS.Comment = string.Format("{0:000000}", Math.Round(UR)) + "-" + string.Format("{0:000}", CrossAgo(Pos_above)) + "-" + string.Format("{0:000}", Pos_above.Count + 2) + "-" + currency_sub.Mark;
                    this.executeOrder(initBuyS);
                    AboveCross = false;
                }
                if (OpenSignal() == "aboveB")
                {
                    initSellF.Volume = _firstsymbol.NormalizeVolume(Init_Volume);
                    initSellF.Label = AboveLabel;
                    initSellF.Comment = "777" + string.Format("{0:000}", GetBreak(AboveLabel) + 100) + "-" + string.Format("{0:000}", CrossAgo(Pos_above)) + "-" + string.Format("{0:000}", Pos_above.Count + 1) + "-" + currency_sub.Mark;
                    this.executeOrder(initSellF);
                    initBuyS.Volume = _secondsymbol.NormalizeVolume(Init_Volume * Ratio);
                    initBuyS.Label = AboveLabel;
                    initBuyS.Comment = "777" + string.Format("{0:000}", GetBreak(AboveLabel) + 100) + "-" + string.Format("{0:000}", CrossAgo(Pos_above)) + "-" + string.Format("{0:000}", Pos_above.Count + 2) + "-" + currency_sub.Mark;
                    this.executeOrder(initBuyS);
                }
                #endregion
                #region Below
                if (OpenSignal() == "below")
                {
                    initBuyF.Volume = _firstsymbol.NormalizeVolume(Init_Volume * Math.Pow(2, Math.Floor((double)Pos_below.Count / 2)), RoundingMode.ToNearest);
                    initBuyF.Label = BelowLabel;
                    initBuyF.Comment = string.Format("{0:000000}", Math.Round(UR)) + "-" + string.Format("{0:000}", CrossAgo(Pos_below)) + "-" + string.Format("{0:000}", Pos_below.Count + 1) + "-" + currency_sub.Mark;
                    this.executeOrder(initBuyF);
                    initSellS.Volume = _secondsymbol.NormalizeVolume(Init_Volume * Ratio * Math.Pow(2, Math.Floor((double)Pos_below.Count / 2)), RoundingMode.ToNearest);
                    initSellS.Label = BelowLabel;
                    initSellS.Comment = string.Format("{0:000000}", Math.Round(UR)) + "-" + string.Format("{0:000}", CrossAgo(Pos_below)) + "-" + string.Format("{0:000}", Pos_below.Count + 2) + "-" + currency_sub.Mark;
                    this.executeOrder(initSellS);
                    BelowCross = false;
                }
                if (OpenSignal() == "belowB")
                {
                    initBuyF.Volume = _firstsymbol.NormalizeVolume(Init_Volume);
                    initBuyF.Label = BelowLabel;
                    initBuyF.Comment = "777" + string.Format("{0:000}", GetBreak(BelowLabel) + 100) + "-" + string.Format("{0:000}", CrossAgo(Pos_below)) + "-" + string.Format("{0:000}", Pos_below.Count + 1) + "-" + currency_sub.Mark;
                    this.executeOrder(initBuyF);
                    initSellS.Volume = _secondsymbol.NormalizeVolume(Init_Volume * Ratio);
                    initSellS.Label = BelowLabel;
                    initSellS.Comment = "777" + string.Format("{0:000}", GetBreak(BelowLabel) + 100) + "-" + string.Format("{0:000}", CrossAgo(Pos_below)) + "-" + string.Format("{0:000}", Pos_below.Count + 2) + "-" + currency_sub.Mark;
                    this.executeOrder(initSellS);
                }
                #endregion
            }
        }

        private string OpenSignal()
        {
            #region Parameter
            string signal = null;
            var UR = currency.Result.LastValue;
            var UA = currency.Average.LastValue;
            var SR = currency_sub.Result.LastValue;
            var SA = currency_sub.Average.LastValue;
            var sig = currency_sub.SIG;

            List<Position> Pos_above = new List<Position>(this.GetPositions(AboveLabel));
            List<Position> Pos_below = new List<Position>(this.GetPositions(BelowLabel));
            Pos_above.Reverse();
            Pos_below.Reverse();

            if (SR > GetBreak(AboveLabel))
            {
                return "aboveB";
            }
            if (SR < -GetBreak(BelowLabel))
            {
                return "belowB";
            }

            if (sig == null)
            {
                return signal;
            }

            var now = DateTime.UtcNow;
            List<DateTime> lastPosTime = new List<DateTime>();
            if (Pos_above.Count != 0)
            {
                lastPosTime.Add(Pos_above[0].EntryTime.AddHours(timer));
            }
            if (Pos_below.Count != 0)
            {
                lastPosTime.Add(Pos_below[0].EntryTime.AddHours(timer));
            }
            var Pos_LastTime = lastPosTime.Count == 0 ? DateTime.UtcNow.AddHours(-timer) : lastPosTime.Max();
            #endregion

            if (DateTime.Compare(now, Pos_LastTime) > 0 && !list_mark.Contains(currency_sub.Mark))
            {
                if (sig == "above" && AboveCross)
                {
                    signal = "above";
                    if (Pos_above.Count != 0)
                    {
                        if (UR - CrossAgo(Pos_above) < Convert.ToDouble(Pos_above[0].Comment.Substring(0, 6)))
                            signal = null;
                    }
                }
                if (sig == "below" && BelowCross)
                {
                    signal = "below";
                    if (Pos_below.Count != 0)
                    {
                        if (UR + CrossAgo(Pos_below) > Convert.ToDouble(Pos_below[0].Comment.Substring(0, 6)))
                            signal = null;
                    }
                }
            }
            return signal;
        }

        private void Chart()
        {
            #region Parameter
            var UR = currency.Result.LastValue;
            var UA = currency.Average.LastValue;
            var SR = currency_sub.Result.LastValue;
            var SA = currency_sub.Average.LastValue;

            List<Position> Pos_eurchfabove = new List<Position>(this.GetPositions(AboveLabel));
            List<Position> Pos_eurchfbelow = new List<Position>(this.GetPositions(BelowLabel));
            Pos_eurchfabove.Reverse();
            Pos_eurchfbelow.Reverse();

            List<DateTime> lastPosTime = new List<DateTime>();
            if (Pos_eurchfabove.Count != 0)
                lastPosTime.Add(Pos_eurchfabove[0].EntryTime.AddHours(timer));
            if (Pos_eurchfbelow.Count != 0)
                lastPosTime.Add(Pos_eurchfbelow[0].EntryTime.AddHours(timer));
            var Pos_LastTime = lastPosTime.Count == 0 ? DateTime.UtcNow.AddHours(-timer) : lastPosTime.Max();
            #endregion

            double marginlevel = 0;
            if (this.Account.MarginLevel.HasValue)
                marginlevel = Math.Round((double)this.Account.MarginLevel);
            List<string> _currency = new List<string>();
            if (Positions.Count != 0)
                foreach (var pos in Positions)
                {
                    if (pos.Label == null)
                        continue;
                    if (!_currency.Contains(pos.SymbolCode + ": " + ((_oilsymbol.Contains(pos.SymbolCode) || _metalssymbol.Contains(pos.SymbolCode)) ? this.TotalLots(pos.Label, MarketData.GetSymbol(pos.SymbolCode)) : MarketData.GetSymbol(pos.SymbolCode).VolumeToQuantity(this.TotalLots(pos.Label, MarketData.GetSymbol(pos.SymbolCode))))))
                        _currency.Add(pos.SymbolCode + ": " + ((_oilsymbol.Contains(pos.SymbolCode) || _metalssymbol.Contains(pos.SymbolCode)) ? this.TotalLots(pos.Label, MarketData.GetSymbol(pos.SymbolCode)) : MarketData.GetSymbol(pos.SymbolCode).VolumeToQuantity(this.TotalLots(pos.Label, MarketData.GetSymbol(pos.SymbolCode)))));
                }
            ChartObjects.RemoveAllObjects();
            ChartObjects.DrawText("info1", "\t\t" + this.Account.Number + " - " + Pos_LastTime, StaticPosition.TopLeft, NoCorel);
            ChartObjects.DrawText("info2/1/1", "\n\t\tB:", StaticPosition.TopLeft, NoCorel);
            ChartObjects.DrawText("info2/1/2", "\n\t\t     " + Math.Round(this.Account.Balance), StaticPosition.TopLeft, GetColors(Math.Round(this.Account.Balance)));
            ChartObjects.DrawText("info2/2/1", "\n\t\t\t\tE:", StaticPosition.TopLeft, NoCorel);
            ChartObjects.DrawText("info2/2/2", "\n\t\t\t\t     " + Math.Round(this.Account.Equity), StaticPosition.TopLeft, GetColors(Math.Round(this.Account.Equity)));
            ChartObjects.DrawText("info2/3/1", "\n\t\t\t\t\t\tN:", StaticPosition.TopLeft, NoCorel);
            ChartObjects.DrawText("info2/3/2", "\n\t\t\t\t\t\t     " + Math.Round(this.Account.UnrealizedNetProfit), StaticPosition.TopLeft, GetColors(Math.Round(this.Account.UnrealizedNetProfit)));
            ChartObjects.DrawText("info2/4/1", "\n\t\t\t\t\t\t\t\tM:", StaticPosition.TopLeft, NoCorel);
            ChartObjects.DrawText("info2/4/2", "\n\t\t\t\t\t\t\t\t     " + Math.Round(this.Account.Margin), StaticPosition.TopLeft, GetColors(Math.Round(this.Account.Margin)));
            //ChartObjects.DrawText("info3", "\n\n\t\tSR: " + Math.Round(SR) + "\t\tSA: " + Math.Round(SA) + "\t\tSIG: " + currency_sub.SIG + "\t\tRatio: " + Ratio + "\t\tMagnify: " + Magnify, StaticPosition.TopLeft, Colors.White);
            ChartObjects.DrawText("info3/1/1", "\n\n\t\tSR:", StaticPosition.TopLeft, NoCorel);
            ChartObjects.DrawText("info3/1/2", "\n\n\t\t       " + Math.Round(SR), StaticPosition.TopLeft, GetColors(Math.Round(SR)));
            ChartObjects.DrawText("info3/2/1", "\n\n\t\t\t\tSA:", StaticPosition.TopLeft, NoCorel);
            ChartObjects.DrawText("info3/2/2", "\n\n\t\t\t\t       " + Math.Round(SA), StaticPosition.TopLeft, GetColors(Math.Round(SA)));
            if (currency_sub.SIG == null)
                ChartObjects.DrawText("info3/3", "\n\n\t\t\t\t\t\tSIG: " + "null", StaticPosition.TopLeft, NoCorel);
            else
                ChartObjects.DrawText("info3/3", "\n\n\t\t\t\t\t\tSIG: " + currency_sub.SIG, StaticPosition.TopLeft, NoCorel);
            ChartObjects.DrawText("info3/4", "\n\n\t\t\t\t\t\t\t\tRatio: " + Ratio, StaticPosition.TopLeft, NoCorel);
            ChartObjects.DrawText("info3/5", "\n\n\t\t\t\t\t\t\t\t\t\tMagnify: " + Magnify, StaticPosition.TopLeft, NoCorel);
            int i = 0;
            string si = null;
            string t = null;
            string tt = "\t\t";
            foreach (string c in _currency)
            {
                i++;
                si = "_C" + i.ToString();
                if (i <= 10)
                    ChartObjects.DrawText(si, "\n\n\n\t\t" + t + c, StaticPosition.TopLeft, NoCorel);
                if (i == 11)
                {
                    t = null;
                    ChartObjects.DrawText(si, "\n\n\n\n\t\t" + t + c, StaticPosition.TopLeft, NoCorel);
                }
                if (i > 11)
                {
                    ChartObjects.DrawText(si, "\n\n\n\n\t\t" + t + c, StaticPosition.TopLeft, NoCorel);
                }
                t += tt;
            }
        }

        private int CrossAgo(List<Position> pos)
        {
            int cross = (int)Math.Round((double)currency.BarsAgo / 24) * 5;
            int crossago = Distance;
            if (pos.Count == 0)
                if (cross > crossago)
                    crossago = cross;
            if (pos.Count != 0)
            {
                var c = 0;
                foreach (var p in pos)
                {
                    if (c == 0)
                        c = Convert.ToInt16(p.Comment.Substring(7, 3)) + 5;
                    if (c < Convert.ToInt16(p.Comment.Substring(7, 3)) + 5)
                        c = Convert.ToInt16(p.Comment.Substring(7, 3)) + 5;
                }
                crossago = c;
            }
            //return crossago;
            return Distance;
        }

        private Colors GetColors(double dou)
        {
            Colors col = Colors.White;
            //color1 = (Result1.LastValue > 0.8) ? PCorel : (Result1.LastValue < -0.8) ? NCorel : NoCorel;
            if (dou >= 0)
                col = PCorel;
            else
                col = NCorel;
            return col;
        }

        private bool GetClose(string label)
        {
            var poss = this.GetPositions(label, _firstsymbol);
            if (poss.Count() != 0)
            {
                double maxlot = 0;
                double minlot = 0;
                foreach (var p in poss)
                {
                    if (maxlot == 0)
                        maxlot = p.Volume;
                    if (minlot == 0)
                        minlot = p.Volume;
                    if (maxlot < p.Volume)
                        maxlot = p.Volume;
                    if (minlot > p.Volume)
                        minlot = p.Volume;
                }
                TimeSpan ts = DateTime.UtcNow - this.FirstPosition(label).EntryTime;
                if (ts.Days >= 1 || maxlot != minlot)
                    return true;
            }
            return false;
        }

        private int GetBreak(string label)
        {
            var poss = this.GetPositions(label);
            var br = 200;
            if (poss.Count() != 0)
            {
                foreach (var p in poss)
                {
                    if (p.Comment.Substring(0, 3) == "777")
                    {
                        if (br < Convert.ToInt16(p.Comment.Substring(3, 3)))
                            br = Convert.ToInt16(p.Comment.Substring(3, 3));
                    }
                }
            }
            return br;
        }
        private double GetBreakCount(string label)
        {
            var poss = this.GetPositions(label);
            double br = 0;
            int i = 0;
            if (poss.Count() != 0)
            {
                foreach (var p in poss)
                {
                    if (p.Comment.Substring(0, 3) == "777")
                    {
                        i++;
                    }
                }
                if (i > 0)
                    br = Math.Floor((double)i / 4) + 1;
            }
            else
                return br;
            return br;
        }
    }
}