﻿using cAlgo.API;
using cAlgo.API.Internals;
using System;
using System.Text;
using System.Net.Mail;
using System.Linq;
using System.Collections.Generic;
using cAlgo.Strategies;
using System.Threading;

namespace cAlgo.Lib
{
    public static class RobotExtensions
    {
        public static string botName(this Robot robot)
        {
            return robot.ToString();
        }

        public static double moneyManagement(this Robot robot, double risk, double? stopLoss, bool isPips = true)
        {
            if (!(stopLoss.HasValue) || stopLoss.Value <= 0)
                return 0;
            else
            {
                double actualMaximumLossInQuoteCurrency = robot.potentialLoss();
                double moneyToInvestInDepositCurrency = (robot.Account.Balance * risk / 100.0);

                double moneyToInvestInQuoteCurrency = (moneyToInvestInDepositCurrency * robot.Symbol.Mid()) - actualMaximumLossInQuoteCurrency;
                double volumeToInvestInQuoteCurrency = moneyToInvestInQuoteCurrency / (stopLoss.Value * (isPips ? robot.Symbol.PipSize : 1));

                return Math.Max(0, volumeToInvestInQuoteCurrency);
            }
        }

        public static bool existPositions(this Robot robot, TradeType tradeType, string label = null)
        {
            return (robot.Positions.Find(label, robot.Symbol, tradeType) != null);
        }

        public static bool existBuyPositions(this Robot robot, string label = null)
        {
            return robot.existPositions(TradeType.Buy, label);
        }

        public static bool existSellPositions(this Robot robot, string label = null)
        {
            return robot.existPositions(TradeType.Sell, label);
        }

        public static bool existBuyAndSellPositions(this Robot robot, string label = null)
        {
            return robot.existBuyPositions(label) && robot.existSellPositions(label);
        }

        public static bool isNoPositions(this Robot robot, string label = null)
        {
            return !(robot.existBuyPositions(label)) && !(robot.existSellPositions(label));
        }

        public static double potentialGain(this Robot robot, string label = null)
        {
            double potential = 0;

            foreach (Position position in robot.Positions)
            {
                if (label != null && position.Label == label)
                {
                    double? positionPotential = position.potentialProfit();
                    potential += positionPotential.HasValue ? positionPotential.Value : 0;
                }

            }

            return potential;
        }

        public static double potentialLoss(this Robot robot, string label = null)
        {
            double potential = 0;

            foreach (Position position in robot.Positions)
            {
                if (label == null || label != null && position.Label == label)
                {
                    double? positionPotential = position.potentialLoss();
                    potential += positionPotential.HasValue ? positionPotential.Value : 0;
                }
            }

            return potential;
        }

        public static TradeResult closePosition(this Robot robot, Position position, double volume)
        {

            TradeResult result;

            if (volume == 0)
                result = robot.ClosePosition(position);
            else
                result = robot.ClosePosition(position, robot.Symbol.NormalizeVolumeInUnits(volume, RoundingMode.ToNearest));

            return result;
        }

        public static void closePosition(this Robot robot, Position position)
        {
            var result = robot.ClosePosition(position);

            if (!result.IsSuccessful)
                robot.Print("error : {0}", result.Error);
        }
        public static void closeAllLabel(this Robot robot, string label = "")
        {
            if (string.IsNullOrEmpty(label))
                foreach (Position position in robot.Positions)
                    robot.closePosition(position);
            else
                foreach (Position position in robot.Positions.FindAll(label))
                    robot.closePosition(position);
        }

        public static void closeAllPositions(this Robot robot, TradeType tradeType, string label = "")
        {
            foreach (Position position in robot.Positions.FindAll(label, robot.Symbol, tradeType))
                robot.closePosition(position);

        }

        public static void closeAllBuyPositions(this Robot robot, string label = "")
        {
            closeAllPositions(robot, TradeType.Buy, label);
        }

        public static void closeAllSellPositions(this Robot robot, string label = "")
        {
            closeAllPositions(robot, TradeType.Sell, label);
        }

        public static void closeAllPositions(this Robot robot, string label = "")
        {
            robot.closeAllBuyPositions(label);
            robot.closeAllSellPositions(label);
        }

        public static void cancelAllPendingOrders(this Robot robot, TradeType tradeType, string label = "")
        {
            foreach (PendingOrder order in robot.PendingOrders)
                if (order.Label == label && order.SymbolCode == robot.Symbol.Code && order.TradeType == tradeType)
                    robot.CancelPendingOrderAsync(order);
        }

        public static void cancelAllPendingBuyOrders(this Robot robot, string label = "")
        {
            cancelAllPendingOrders(robot, TradeType.Buy, label);
        }

        public static void cancelAllPendingSellOrders(this Robot robot, string label = "")
        {
            cancelAllPendingOrders(robot, TradeType.Sell, label);
        }

        public static void cancelAllPendingOrders(this Robot robot, string label = "")
        {
            robot.cancelAllPendingBuyOrders(label);
            robot.cancelAllPendingSellOrders(label);
        }

        public static void notifyMessage(this Robot robot, string headMessage, string message, MailAddress mailAddress)
        {
            robot.Notifications.SendEmail("cAlgoBot@cAlgoLib.com", mailAddress.Address, headMessage, message);
        }

        public static void notifyError(this Robot robot, Error error, MailAddress mailAddress)
        {
            string errorText = robot.errorString(error);

            if (error.Code == ErrorCode.NoMoney || error.Code == ErrorCode.TechnicalError)
                robot.notifyMessage("Robot Error : ", errorText, mailAddress);
            else
                if (error.Code == ErrorCode.MarketClosed)
                    robot.notifyMessage("End of week report : ", errorText, mailAddress);

        }

        public static string errorString(this Robot robot, Error error)
        {
            string errorText = "";

            switch (error.Code)
            {
                case ErrorCode.BadVolume: errorText = "Bad volume";
                    break;
                case ErrorCode.TechnicalError: errorText = "Technical Error";
                    break;
                case ErrorCode.NoMoney: errorText = "No Money";
                    break;
                case ErrorCode.Disconnected: errorText = "Disconnected";
                    break;
                case ErrorCode.MarketClosed: errorText = "Market Closed";
                    break;
            }


            if (error.Code == ErrorCode.BadVolume || error.Code == ErrorCode.Disconnected)
                errorText = "Error:" + errorText;

            if (error.Code == ErrorCode.MarketClosed)
            {
                StringBuilder report = new StringBuilder("End of trading week report for the week of:");
                report.Append(DateTime.Now);
                report.Append("\n");
                report.Append("Account Balance:");
                report.Append(robot.Account.Balance);
                report.Append("\n");

                return report.ToString();
            }

            return errorText;
        }

        public static void partialClose(this Robot robot, string label = "")
        {
            foreach (var position in robot.Positions.FindAll(label, robot.Symbol))
            {
                if (position.TakeProfit.HasValue && position.StopLoss.HasValue)
                {
                    string ident = position.Comment.Substring(position.Comment.Length - 1, 1);
                    double? percentLoss = position.percentLoss();

                    if (percentLoss.HasValue && (((percentLoss <= -0.33) && (ident == "1")) || ((percentLoss <= -0.66) && (ident == "2"))))
                        robot.closePosition(position);

                }
            }
        }

        public static void executeOrder(this Robot robot, OrderParams op)
        {
            if (op == null)
                throw new System.ArgumentException(String.Format("parameter 'op' must be non null", op));
            if (op.Volume <= 0)
                throw new System.ArgumentException(String.Format("parameter 'op.Volume' must be strictly positive", op.Volume));
            if (!op.TradeType.HasValue)
                throw new System.ArgumentException(String.Format("parameter 'op.TradeType' must have a value", op.TradeType));


            // it is necessary that the volume is a multiple of "microvolume".
            //long v = robot.Symbol.NormalizeVolume(op.Volume.Value, RoundingMode.ToNearest);
            long v = Convert.ToInt64(op.Volume);

            if (v > 0)
            {
                var result = robot.ExecuteMarketOrder(op.TradeType.Value, op.Symbol, v, op.Label, op.StopLoss, op.TakeProfit, op.Slippage, op.Comment);
                if (!result.IsSuccessful)
                    robot.Print("error : {0}, {1}", result.Error, v);
            }
        }

        public static void splitAndExecuteOrder(this Robot robot, OrderParams op)
        {
            if (op == null)
                throw new System.ArgumentException(String.Format("parameter 'op' must be non null", op));
            if (op.Volume <= 0)
                throw new System.ArgumentException(String.Format("parameter 'op.Volume' must be strictly positive", op.Volume));

            double sum = op.Parties.Sum(x => Math.Abs(x));
            OrderParams partialOP = new OrderParams(op);
            List<double> l = new List<double>(op.Parties);
            l.Sort();

            for (int i = l.Count - 1; i >= 0; i--)
            {
                partialOP.Volume = op.Volume.Value * l[i] / sum;
                partialOP.Comment = string.Format("{0}-{1}", partialOP.Comment, i);

                robot.executeOrder(partialOP);
            }

        }

        public static TradeType? signal(this Robot robot, List<Strategy> strategies, int? ceilSignal = null)
        {
            int ceil;
            if (ceilSignal.HasValue)
                ceil = ceilSignal.Value;
            else
                ceil = strategies.Count;

            int signal = 0;
            foreach (Strategy strategy in strategies)
                signal += strategy.signal().factor();

            if (signal >= ceil)
                return TradeType.Buy;
            else
                if (signal <= -ceil)
                    return TradeType.Sell;

            return null;
        }

        public static OrderParams martingale(this Robot robot, Position position, bool inversePosition = true, double martingaleCoeff = 1.5)
        {
            if ((position != null) && position.Pips < 0)
            {
                OrderParams op = new OrderParams(position);
                op.Comment = string.Format("{0}-{1}", position.Comment, "Mart");
                if (inversePosition)
                    op.TradeType = position.TradeType.inverseTradeType();
                op.Volume = position.VolumeInUnits * martingaleCoeff;

                return op;
            }
            else
                return null;
        }

        //LabelExtensions
        public static Position[] GetPositions(this Robot robot, string label, Symbol symbol = null)
        {
            if (symbol == null)
            {
                return robot.Positions.FindAll(label).OrderBy(p=>p.EntryTime).ToArray();
            }
            return robot.Positions.FindAll(label, symbol).OrderBy(p=>p.EntryTime).ToArray();
        }

        public static long TotalLots(this Robot robot, string label = null, Symbol symbol = null)
        {
            List<Position> poss = new List<Position>();
            if (label == null && symbol == null)
                poss.AddRange(robot.Positions);
            else if (symbol == null)
                poss.AddRange(robot.GetPositions(label));
            else if (label == null)
            {
                foreach (var p in robot.Positions)
                {
                    if (p.SymbolCode == symbol.Code)
                        poss.Add(p);
                }
            }
            else
                poss.AddRange(robot.GetPositions(label, symbol));
            if (poss.Count == 0)
                return 0;
            long totallots = 0;
            foreach (var pos in poss)
                totallots += pos.Volume;
            return totallots;
        }

        public static long MaxLot(this Robot robot, string label = null)
        {
            List<Position> poss = new List<Position>();
            if (label == null)
                poss.AddRange(robot.Positions);
            else
                poss.AddRange(robot.GetPositions(label));
            if (poss.Count == 0)
                return 0;
            long maxlot = (long)poss.OrderByDescending(p => p.VolumeInUnits).ToArray()[0].VolumeInUnits;
            return maxlot;
        }

        public static long MinLot(this Robot robot, string label = null)
        {
            List<Position> poss = new List<Position>();
            if (label == null)
                poss.AddRange(robot.Positions);
            else
                poss.AddRange(robot.GetPositions(label));
            if (poss.Count == 0)
                return 0;
            long minlot = (long)poss.OrderBy(p => p.VolumeInUnits).ToArray()[0].VolumeInUnits;
            return minlot;
        }

        public static long MartingaleLot(this Robot robot, string label, double goalprice)
        {
            List<Position> poss = new List<Position>();
            if (label == null)
                poss.AddRange(robot.Positions);
            else
                poss.AddRange(robot.GetPositions(label));
            if (poss.Count == 0)
                return 0;
            string IsBorS = "Nope";
            int buy = 0;
            int sell = 0;
            foreach (var pos in poss)
            {
                if (pos.TradeType == TradeType.Buy)
                    buy++;
                if (pos.TradeType == TradeType.Sell)
                    sell++;
            }
            if (buy > sell)
                IsBorS = "buy";
            if (sell > buy)
                IsBorS = "sell";
            long marlot = 0;
            if (IsBorS == "buy" && robot.AveragePrice(label) < goalprice)
                return 0;
            if (IsBorS == "sell" && robot.AveragePrice(label) > goalprice)
                return 0;
            marlot = (long)robot.Symbol.NormalizeVolumeInUnits((robot.AveragePrice(label) * robot.TotalLots(label) - goalprice * robot.TotalLots(label)) / (goalprice - robot.Symbol.Mid()), RoundingMode.ToNearest);
            return marlot;
        }

        public static double TotalProfits(this Robot robot, string label = null)
        {
            List<Position> poss = new List<Position>();
            if (label == null)
                poss.AddRange(robot.Positions);
            else
                poss.AddRange(robot.GetPositions(label));
            if (poss.Count == 0)
                return 0;
            double totalprofits = 0;
            foreach (var pos in poss)
                totalprofits += pos.NetProfit;
            return totalprofits;
        }

        public static double MaxPrice(this Robot robot, string label = null)
        {
            List<Position> poss = new List<Position>();
            if (label == null)
                poss.AddRange(robot.Positions);
            else
                poss.AddRange(robot.GetPositions(label));
            if (poss.Count == 0)
                return 0;
            double maxprice = poss.OrderByDescending(p => p.EntryPrice).ToArray()[0].EntryPrice;
            return maxprice;
        }

        public static double MinPrice(this Robot robot, string label = null)
        {
            List<Position> poss = new List<Position>();
            if (label == null)
                poss.AddRange(robot.Positions);
            else
                poss.AddRange(robot.GetPositions(label));
            if (poss.Count == 0)
                return 0;
            double minprice = poss.OrderBy(p => p.EntryTime).ToArray()[0].EntryPrice;
            return minprice;
        }

        public static double AveragePrice(this Robot robot, string label = null)
        {
            List<Position> poss = new List<Position>();
            if (label == null)
                poss.AddRange(robot.Positions);
            else
                poss.AddRange(robot.GetPositions(label));
            if (poss.Count == 0)
                return 0;
            long totallots = 0;
            double lotsprice = 0;
            double averageprice = 0;
            foreach (var pos in poss)
            {
                totallots += pos.Volume;
                lotsprice += pos.Volume * pos.EntryPrice;
            }
            averageprice = lotsprice / totallots;
            return averageprice;
        }

        public static Position FirstPosition(this Robot robot, Position[] poss)
        {
            Position pos = null;
            if (poss.Count() == 0)
                return null;
            else
            {
                pos=poss.OrderBy(p=>p.EntryTime).ToArray()[0];
            }
            return pos;
        }

        public static Position LastPosition(this Robot robot, Position[] poss)
        {
            Position pos = null;
            if (poss.Count() == 0)
                return null;
            else
            {
                pos = poss.OrderByDescending(p => p.EntryTime).ToArray()[0];
            }
            return pos;
        }

        public static bool Risk(this Robot robot)
        {
            bool risk = false;
            if (robot.Positions.Count == 0)
                return risk;
            double lots = 0;
            List<Position> list_pos = new List<Position>();
            foreach (var p in robot.Positions)
            {
                if (p.SymbolCode == robot.Symbol.Code)
                {
                    lots += p.Volume;
                    list_pos.Add(p);
                }
            }
            if (list_pos.Count == 0)
                return risk;
            var freemargin = robot.Account.FreeMargin;
            var pips = freemargin / (lots * robot.Symbol.PipValue);
            double min = 0;
            double max = 0;
            double bars_pips = 0;
            var bars = robot.MarketSeries.Bars();
            for (int i = bars - 120; i < bars; i++)
            {
                if (min == 0)
                    min = robot.MarketSeries.Low[i];
                if (max == 0)
                    max = robot.MarketSeries.High[i];
                if (min > robot.MarketSeries.Low[i])
                    min = robot.MarketSeries.Low[i];
                if (max < robot.MarketSeries.High[i])
                    max = robot.MarketSeries.High[i];
            }
            bars_pips = (max - min) / robot.Symbol.PipSize;
            //robot.Print("Max: " + Math.Round(pips).ToString());
            //robot.Print("Present: " + Math.Round(bars_pips).ToString());
            if (pips < bars_pips)
                risk = true;
            return risk;
        }

        public static bool PreRisk(this Robot robot)
        {
            bool risk = false;
            if (robot.Positions.Count == 0)
                return risk;
            double lots = 0;
            List<Position> list_pos = new List<Position>();
            foreach (var p in robot.Positions)
            {
                if (p.SymbolCode == robot.Symbol.Code)
                {
                    lots += p.Volume;
                    list_pos.Add(p);
                }
            }
            if (list_pos.Count == 0)
                return risk;
            var pos = robot.LastPosition(list_pos.ToArray());
            lots += pos.Volume * 2;
            var freemargin = robot.Account.FreeMargin;
            var pips = freemargin / (lots * robot.Symbol.PipValue);
            double min = 0;
            double max = 0;
            double bars_pips = 0;
            var bars = robot.MarketSeries.Bars();
            for (int i = bars - 120; i < bars; i++)
            {
                if (min == 0)
                    min = robot.MarketSeries.Low[i];
                if (max == 0)
                    max = robot.MarketSeries.High[i];
                if (min > robot.MarketSeries.Low[i])
                    min = robot.MarketSeries.Low[i];
                if (max < robot.MarketSeries.High[i])
                    max = robot.MarketSeries.High[i];
            }
            bars_pips = (max - min) / robot.Symbol.PipSize;
            //robot.Print("Max: " + Math.Round(pips).ToString());
            //robot.Print("Present: " + Math.Round(bars_pips).ToString());
            if (pips < bars_pips)
                risk = true;
            return risk;
        }
    }
}
