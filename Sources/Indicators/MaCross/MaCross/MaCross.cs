﻿using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

namespace cAlgo
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class MaCross : Indicator
    {
        [Output("Result")]
        public IndicatorDataSeries Result { get; set; }

        [Output("Average")]
        public IndicatorDataSeries Average { get; set; }

        [Parameter("Result Periods", DefaultValue = 1)]
        public int ResultPeriods { get; set; }

        [Parameter("Average Periods", DefaultValue = 120)]
        public int AveragePeriods { get; set; }

        private SimpleMovingAverage _result;
        private SimpleMovingAverage _average;

        protected override void Initialize()
        {
            _result = Indicators.SimpleMovingAverage(MarketSeries.Close, ResultPeriods);
            _average = Indicators.SimpleMovingAverage(MarketSeries.Close, AveragePeriods);
        }

        public override void Calculate(int index)
        {
            Result[index] = _result.Result[index] / Symbol.PipSize;
            Average[index] = _average.Result[index] / Symbol.PipSize;
        }
    }
}
