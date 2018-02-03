﻿using cAlgo.API;
using cAlgo.API.Internals;

namespace cAlgo
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Oil_MAC : Indicator
    {
        [Output("Result")]
        public IndicatorDataSeries Result { get; set; }

        [Output("Average")]
        public IndicatorDataSeries Average { get; set; }

        [Output("Sig_Result_A", Color = Colors.DeepSkyBlue, PlotType = PlotType.Points, Thickness = 2)]
        public IndicatorDataSeries Sig_Result_A { get; set; }

        [Output("Sig_Result_B", Color = Colors.OrangeRed, PlotType = PlotType.Points, Thickness = 2)]
        public IndicatorDataSeries Sig_Result_B { get; set; }

        [Parameter("MA Type")]
        public MovingAverageType MAType { get; set; }

        [Parameter("SourceSeries")]
        public DataSeries SourceSeries { get; set; }

        [Parameter("Result Periods", DefaultValue = 1)]
        public int ResultPeriods { get; set; }

        [Parameter("Average Periods", DefaultValue = 120)]
        public int AveragePeriods { get; set; }

        [Parameter(DefaultValue = 30)]
        public double Sub { get; set; }

        //PCorel=Colors.Lime;NCorel=Colors.OrangeRed;NoCorel=Colors.Gray;
        public string _Signal;
        public int _BarsAgo;
        private Oil_MaCross _mac;
        private Oil_MaSub _mas;
        private Colors _nocorel;

        protected override void Initialize()
        {
            _mac = Indicators.GetIndicator<Oil_MaCross>(MAType, SourceSeries, ResultPeriods, AveragePeriods);
            _mas = Indicators.GetIndicator<Oil_MaSub>(MAType, SourceSeries, ResultPeriods, AveragePeriods);
            _nocorel = Colors.Gray;
        }

        public override void Calculate(int index)
        {
            Result[index] = _mac.Result[index];
            Average[index] = _mac.Average[index];
            string Sig = GetSignal(index);
            if (Sig == "above")
                Sig_Result_A[index] = _mac.Result[index];
            if (Sig == "below")
                Sig_Result_B[index] = _mac.Result[index];

            #region Chart
            _Signal = Sig;
            _BarsAgo = GetBarsAgo(index);
            ChartObjects.DrawText("barsago", "Cross_(" + _BarsAgo.ToString() + ")", StaticPosition.TopLeft, _nocorel);
            #endregion
        }

        private string GetSignal(int index)
        {
            double CR = _mac.Result[index];
            double CA = _mac.Average[index];
            double SR = _mas.Result[index];
            double SA = _mas.Average[index];
            if (-Sub > SR && SR > SA && CR < CA)
                return "below";
            if (Sub < SR && SR < SA && CR > CA)
                return "above";
            return null;
        }

        private int GetBarsAgo(int index)
        {
            double CR = _mac.Result[index];
            double CA = _mac.Average[index];
            if (CR > CA)
                for (int i = index - 1; i > 0; i--)
                {
                    if (_mac.Result[i] <= _mac.Average[i])
                        return index - i;
                }
            if (CR < CA)
                for (int i = index - 1; i > 0; i--)
                {
                    if (_mac.Result[i] >= _mac.Average[i])
                        return index - i;
                }
            return -1;
        }
    }
}