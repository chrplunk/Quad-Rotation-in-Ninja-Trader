// Christopher Plunkett
// QR Strategy - Based on Quad Rotation + VuManChu + Keltner + VWAP + MACD

#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    public class QRStrategy : Strategy
    {
        // Stochastic series
        private Series<double> rawK1, smoothK1Series, dSeries1;
        private Series<double> rawK2, smoothK2Series, dSeries2;
        private Series<double> rawK3, smoothK3Series, dSeries3;
        private Series<double> rawK4, smoothK4Series2, dSeries4;

        // WaveTrend series
        private Series<double> wtEsa, wtDe, wtCi, wt1Series, wt2Series;

        // MFI series
        private Series<double> mfiSeries;

        // VWAP manual calculation
        private Series<double> vwapSeries;
        private Series<double> vwapSumPV;
        private Series<double> vwapSumV;
        private Series<double> vwapSumPV2;

        // Indicators
        private NinjaTrader.NinjaScript.Indicators.KeltnerChannel keltner;
        private NinjaTrader.NinjaScript.Indicators.MACD macdInd;
        private NinjaTrader.NinjaScript.Indicators.ATR atrInd;

        // Stop loss tracking
        private double entryPrice;
        private double stopPrice;
        private double targetPrice;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name                            = "QRStrategy";
                Description                     = "Quad Rotation Strategy - Multi-criteria scoring entry system.";
                Calculate                       = Calculate.OnBarClose;
                EntriesPerDirection             = 1;
                EntryHandling                   = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy    = true;
                ExitOnSessionCloseSeconds       = 30;

                // General
                Quantity            = 1;
                MinScore            = 2;

                // Stochastics
                K1 = 9;  D1 = 3;
                K2 = 14; D2 = 3;
                K3 = 40; D3 = 4;
                K4 = 60; D4 = 10; SmoothK4 = 1;

                // Keltner
                KeltnerPeriod       = 20;
                KeltnerMultiplier   = 2.0;
                KeltnerCandleCount  = 4;

                // VWAP
                VwapBandMultiplier  = 2.0;

                // WaveTrend (optional)
                UseWaveTrend        = true;
                WtChannelLen        = 9;
                WtAverageLen        = 12;
                WtMALen             = 3;

                // MFI (optional)
                UseMfi              = true;
                MfiPeriod           = 60;
                MfiMultiplier       = 150;
                MfiLookback         = 5;

                // MACD (optional)
                UseMacd             = true;
                MacdFast            = 12;
                MacdSlow            = 26;
                MacdSignal          = 9;

                // Stop Loss
                StopType            = StopLossType.Fixed;
                StopPoints          = 10;
                AtrMultiplier       = 1.5;
                AtrPeriod           = 14;
                SwingLookback       = 10;

                // Profit Target
                TargetType          = ProfitTargetType.RR1to2;
                TargetPoints        = 20;
            }
            else if (State == State.DataLoaded)
            {
                rawK1          = new Series<double>(this);
                smoothK1Series = new Series<double>(this);
                dSeries1       = new Series<double>(this);

                rawK2          = new Series<double>(this);
                smoothK2Series = new Series<double>(this);
                dSeries2       = new Series<double>(this);

                rawK3          = new Series<double>(this);
                smoothK3Series = new Series<double>(this);
                dSeries3       = new Series<double>(this);

                rawK4           = new Series<double>(this);
                smoothK4Series2 = new Series<double>(this);
                dSeries4        = new Series<double>(this);

                wtEsa      = new Series<double>(this);
                wtDe       = new Series<double>(this);
                wtCi       = new Series<double>(this);
                wt1Series  = new Series<double>(this);
                wt2Series  = new Series<double>(this);

                mfiSeries  = new Series<double>(this);

                vwapSeries  = new Series<double>(this);
                vwapSumPV   = new Series<double>(this);
                vwapSumV    = new Series<double>(this);
                vwapSumPV2  = new Series<double>(this);

                keltner = KeltnerChannel(KeltnerMultiplier, KeltnerPeriod);
                macdInd = MACD(MacdFast, MacdSlow, MacdSignal);
                atrInd  = ATR(AtrPeriod);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 100) return;

            // =====================
            // Compute Stochastics
            // =====================
            ComputeStoch(K1, 1,        D1, rawK1, smoothK1Series, dSeries1,   out double s1);
            ComputeStoch(K2, 1,        D2, rawK2, smoothK2Series, dSeries2,   out double s2);
            ComputeStoch(K3, 1,        D3, rawK3, smoothK3Series, dSeries3,   out double s3);
            ComputeStoch(K4, SmoothK4, D4, rawK4, smoothK4Series2, dSeries4,  out double s4);

            double s1Prev = dSeries1[1];
            double s2Prev = dSeries2[1];
            double s3Prev = dSeries3[1];
            double s4Prev = dSeries4[1];

            // =====================
            // Manual VWAP calculation (resets each session)
            // =====================
            double typicalPrice = (High[0] + Low[0] + Close[0]) / 3.0;

            if (Bars.IsFirstBarOfSession)
            {
                vwapSumPV[0]  = typicalPrice * Volume[0];
                vwapSumV[0]   = Volume[0];
                vwapSumPV2[0] = typicalPrice * typicalPrice * Volume[0];
            }
            else
            {
                vwapSumPV[0]  = vwapSumPV[1]  + typicalPrice * Volume[0];
                vwapSumV[0]   = vwapSumV[1]   + Volume[0];
                vwapSumPV2[0] = vwapSumPV2[1] + typicalPrice * typicalPrice * Volume[0];
            }

            double vwap = vwapSumV[0] > 0 ? vwapSumPV[0] / vwapSumV[0] : typicalPrice;
            double vwapVariance = vwapSumV[0] > 0 ? (vwapSumPV2[0] / vwapSumV[0]) - (vwap * vwap) : 0;
            double vwapStdDev = vwapVariance > 0 ? Math.Sqrt(vwapVariance) : 0;
            double vwapUpperBand2 = vwap + VwapBandMultiplier * vwapStdDev;
            double vwapLowerBand2 = vwap - VwapBandMultiplier * vwapStdDev;

            // =====================
            // Compute WaveTrend
            // =====================
            double hlc3Val = typicalPrice;
            wtEsa[0]  = EMA(Typical, WtChannelLen)[0];
            double absVal = Math.Abs(hlc3Val - wtEsa[0]);

            if (CurrentBar == 0)
                wtDe[0] = absVal;
            else
                wtDe[0] = wtDe[1] + 2.0 / (WtChannelLen + 1) * (absVal - wtDe[1]);

            wtCi[0]      = wtDe[0] < 1e-10 ? 0 : (hlc3Val - wtEsa[0]) / (0.015 * wtDe[0]);
            wt1Series[0] = EMA(wtCi, WtAverageLen)[0];
            wt2Series[0] = SMA(wt1Series, WtMALen)[0];

            double wt1     = wt1Series[0];
            double wt2     = wt2Series[0];
            double wt2Prev = wt2Series[1];
            double wt1Prev = wt1Series[1];

            bool wtCrossUp    = wt1Prev < wt2Prev && wt1 >= wt2;
            bool wtCrossDown  = wt1Prev > wt2Prev && wt1 <= wt2;
            bool wtOversold   = wt2 <= -53;
            bool wtOverbought = wt2 >= 53;

            // =====================
            // Compute MFI
            // =====================
            double candleRange = High[0] - Low[0];
            mfiSeries[0] = candleRange < 1e-10 ? 0 :
                ((Close[0] - Open[0]) / candleRange) * MfiMultiplier;
            double mfi        = SMA(mfiSeries, MfiPeriod)[0];
            double mfiAvg5    = 0;
            for (int i = 1; i <= MfiLookback && CurrentBar - i >= 0; i++)
                mfiAvg5 += SMA(mfiSeries, MfiPeriod)[i];
            mfiAvg5 /= MfiLookback;

            // =====================
            // MACD values
            // =====================
            double macdVal      = macdInd[0];
            double macdAvg      = macdInd.Avg[0];
            double macdValPrev  = macdInd[1];
            double macdAvgPrev  = macdInd.Avg[1];
            double macdHist     = macdVal - macdAvg;
            double macdHistPrev = macdValPrev - macdAvgPrev;

            // =====================
            // LONG criteria
            // =====================

            // C1 REQUIRED: QR Green - all 4 stochs rotating up
            bool s1Up   = (s1 - s1Prev) > 0  && s1 <= 50;
            bool s2Up   = (s2 - s2Prev) > 0  && s2 <= 50;
            bool s3Up   = (s3 - s3Prev) >= 0 && s3 < 20;
            bool s4Up   = (s4 - s4Prev) >= 0 && s4 < 20;
            bool c1Long = s1Up && s2Up && s3Up && s4Up;

            // C2 REQUIRED: Keltner - N candles below lower band
            bool c2Long = true;
            for (int i = 0; i < KeltnerCandleCount; i++)
                if (Close[i] >= keltner.Lower[i]) { c2Long = false; break; }

            // C3 OPTIONAL: VWAP - price below lower band 2
            bool c3Long = Close[0] <= vwapLowerBand2;

            // C4 OPTIONAL: MFI sloping up vs avg of last N bars
            bool c4Long = UseMfi && mfi > mfiAvg5;

            // C5 OPTIONAL: WaveTrend green dot
            bool c5Long = UseWaveTrend && wtCrossUp && wtOversold;

            // C6 OPTIONAL: MACD histogram negative but contracting
            bool c6Long = UseMacd && macdHist < 0 && macdHist > macdHistPrev;

            int longScore = (c3Long ? 1 : 0)
                          + (c4Long ? 1 : 0)
                          + (c5Long ? 1 : 0)
                          + (c6Long ? 1 : 0);

            // =====================
            // SHORT criteria
            // =====================

            // C1 REQUIRED: QR Red - all 4 stochs rotating down
            bool s1Down  = (s1 - s1Prev) < 0  && s1 >= 50;
            bool s2Down  = (s2 - s2Prev) < 0  && s2 >= 50;
            bool s3Down  = (s3 - s3Prev) <= 0 && s3 > 80;
            bool s4Down  = (s4 - s4Prev) <= 0 && s4 > 80;
            bool c1Short = s1Down && s2Down && s3Down && s4Down;

            // C2 REQUIRED: Keltner - N candles above upper band
            bool c2Short = true;
            for (int i = 0; i < KeltnerCandleCount; i++)
                if (Close[i] <= keltner.Upper[i]) { c2Short = false; break; }

            // C3 OPTIONAL: VWAP - price above upper band 2
            bool c3Short = Close[0] >= vwapUpperBand2;

            // C4 OPTIONAL: MFI sloping down vs avg of last N bars
            bool c4Short = UseMfi && mfi < mfiAvg5;

            // C5 OPTIONAL: WaveTrend red dot
            bool c5Short = UseWaveTrend && wtCrossDown && wtOverbought;

            // C6 OPTIONAL: MACD histogram positive but contracting
            bool c6Short = UseMacd && macdHist > 0 && macdHist < macdHistPrev;

            int shortScore = (c3Short ? 1 : 0)
                           + (c4Short ? 1 : 0)
                           + (c5Short ? 1 : 0)
                           + (c6Short ? 1 : 0);

            // =====================
            // DEBUG
            // =====================
            Print(string.Format("{0} | LONG  C1(req)={1} C2(req)={2} Score={3}/4 C3={4} C4={5} C5={6} C6={7}",
                Time[0], c1Long, c2Long, longScore, c3Long, c4Long, c5Long, c6Long));
            Print(string.Format("{0} | SHORT C1(req)={1} C2(req)={2} Score={3}/4 C3={4} C4={5} C5={6} C6={7}",
                Time[0], c1Short, c2Short, shortScore, c3Short, c4Short, c5Short, c6Short));

            // =====================
            // EXITS
            // =====================
            if (Position.MarketPosition == MarketPosition.Long)
            {
                // Stoch 4 exit
                if (TargetType == ProfitTargetType.Stoch4 && s4 >= 80)
                    ExitLong("ExitLong_Stoch4", "EnterLong");

                // Keltner other side exit
                if (TargetType == ProfitTargetType.KeltnerOtherSide && Close[0] >= keltner.Upper[0])
                    ExitLong("ExitLong_Keltner", "EnterLong");

                // Stop loss check
                if (Close[0] <= stopPrice)
                    ExitLong("StopLoss", "EnterLong");

                // Target check for fixed point / RR targets
                if (TargetType != ProfitTargetType.Stoch4 && TargetType != ProfitTargetType.KeltnerOtherSide)
                    if (Close[0] >= targetPrice)
                        ExitLong("ProfitTarget", "EnterLong");
            }
            else if (Position.MarketPosition == MarketPosition.Short)
            {
                // Stoch 4 exit
                if (TargetType == ProfitTargetType.Stoch4 && s4 <= 20)
                    ExitShort("ExitShort_Stoch4", "EnterShort");

                // Keltner other side exit
                if (TargetType == ProfitTargetType.KeltnerOtherSide && Close[0] <= keltner.Lower[0])
                    ExitShort("ExitShort_Keltner", "EnterShort");

                // Stop loss check
                if (Close[0] >= stopPrice)
                    ExitShort("StopLoss", "EnterShort");

                // Target check for fixed point / RR targets
                if (TargetType != ProfitTargetType.Stoch4 && TargetType != ProfitTargetType.KeltnerOtherSide)
                    if (Close[0] <= targetPrice)
                        ExitShort("ProfitTarget", "EnterShort");
            }

            // =====================
            // ENTRIES
            // =====================
            if (Position.MarketPosition == MarketPosition.Flat)
            {
                if (c1Long && c2Long && longScore >= MinScore)
                {
                    double stop = CalculateStop(true);
                    double risk = Math.Abs(Close[0] - stop);
                    entryPrice  = Close[0];
                    stopPrice   = stop;
                    targetPrice = CalculateTarget(true, entryPrice, risk);
                    EnterLong(Quantity, "EnterLong");
                }
                else if (c1Short && c2Short && shortScore >= MinScore)
                {
                    double stop = CalculateStop(false);
                    double risk = Math.Abs(Close[0] - stop);
                    entryPrice  = Close[0];
                    stopPrice   = stop;
                    targetPrice = CalculateTarget(false, entryPrice, risk);
                    EnterShort(Quantity, "EnterShort");
                }
            }
        }

        private double CalculateStop(bool isLong)
        {
            switch (StopType)
            {
                case StopLossType.Fixed:
                    return isLong ? Close[0] - StopPoints * TickSize : Close[0] + StopPoints * TickSize;

                case StopLossType.ATR:
                    return isLong
                        ? Close[0] - AtrMultiplier * atrInd[0]
                        : Close[0] + AtrMultiplier * atrInd[0];

                case StopLossType.SwingHL:
                    if (isLong)
                    {
                        double swingLow = Low[0];
                        for (int i = 1; i <= SwingLookback && CurrentBar - i >= 0; i++)
                            swingLow = Math.Min(swingLow, Low[i]);
                        return swingLow - TickSize;
                    }
                    else
                    {
                        double swingHigh = High[0];
                        for (int i = 1; i <= SwingLookback && CurrentBar - i >= 0; i++)
                            swingHigh = Math.Max(swingHigh, High[i]);
                        return swingHigh + TickSize;
                    }

                default:
                    return isLong ? Close[0] - StopPoints * TickSize : Close[0] + StopPoints * TickSize;
            }
        }

        private double CalculateTarget(bool isLong, double entry, double risk)
        {
            double multiplier = 1.0;
            switch (TargetType)
            {
                case ProfitTargetType.RR1to1:   multiplier = 1.0;  break;
                case ProfitTargetType.RR1to1_5: multiplier = 1.5;  break;
                case ProfitTargetType.RR1to2:   multiplier = 2.0;  break;
                case ProfitTargetType.RR1to3:   multiplier = 3.0;  break;
                case ProfitTargetType.FixedPoints:
                    return isLong ? entry + TargetPoints * TickSize : entry - TargetPoints * TickSize;
                default:
                    return isLong ? entry + risk : entry - risk;
            }
            return isLong ? entry + risk * multiplier : entry - risk * multiplier;
        }

        private void ComputeStoch(int kLen, int smoothKLen, int dLen,
            Series<double> rawK, Series<double> smoothK, Series<double> dSeries,
            out double dValue)
        {
            double hh    = MAX(High, kLen)[0];
            double ll    = MIN(Low,  kLen)[0];
            double denom = hh - ll;
            double k     = Math.Abs(denom) < 1e-10 ? 50.0 : 100.0 * (Close[0] - ll) / denom;
            rawK[0]    = k;
            smoothK[0] = SMA(rawK,    Math.Max(1, smoothKLen))[0];
            dSeries[0] = SMA(smoothK, Math.Max(1, dLen))[0];
            dValue     = dSeries[0];
        }

        // =====================
        // Enums
        // =====================
        public enum StopLossType   { Fixed, ATR, SwingHL }
        public enum ProfitTargetType { Stoch4, KeltnerOtherSide, RR1to1, RR1to1_5, RR1to2, RR1to3, FixedPoints }

        // =====================
        // Properties
        // =====================

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Quantity", Order = 1, GroupName = "General")]
        public int Quantity { get; set; }

        [NinjaScriptProperty]
        [Range(0, 4)]
        [Display(Name = "Min Optional Score (0-4)", Order = 2, GroupName = "General")]
        public int MinScore { get; set; }

        [NinjaScriptProperty]
        [Range(1, 300)]
        [Display(Name = "%K Length (Stoch 1)", Order = 1, GroupName = "Stochastics")]
        public int K1 { get; set; }

        [NinjaScriptProperty]
        [Range(1, 300)]
        [Display(Name = "%D Smoothing (Stoch 1)", Order = 2, GroupName = "Stochastics")]
        public int D1 { get; set; }

        [NinjaScriptProperty]
        [Range(1, 300)]
        [Display(Name = "%K Length (Stoch 2)", Order = 1, GroupName = "Stochastics")]
        public int K2 { get; set; }

        [NinjaScriptProperty]
        [Range(1, 300)]
        [Display(Name = "%D Smoothing (Stoch 2)", Order = 2, GroupName = "Stochastics")]
        public int D2 { get; set; }

        [NinjaScriptProperty]
        [Range(1, 600)]
        [Display(Name = "%K Length (Stoch 3)", Order = 1, GroupName = "Stochastics")]
        public int K3 { get; set; }

        [NinjaScriptProperty]
        [Range(1, 600)]
        [Display(Name = "%D Smoothing (Stoch 3)", Order = 2, GroupName = "Stochastics")]
        public int D3 { get; set; }

        [NinjaScriptProperty]
        [Range(1, 1000)]
        [Display(Name = "%K Length (Stoch 4)", Order = 1, GroupName = "Stochastics")]
        public int K4 { get; set; }

        [NinjaScriptProperty]
        [Range(1, 1000)]
        [Display(Name = "%D Smoothing (Stoch 4)", Order = 2, GroupName = "Stochastics")]
        public int D4 { get; set; }

        [NinjaScriptProperty]
        [Range(1, 50)]
        [Display(Name = "Smoothing (Stoch 4)", Order = 3, GroupName = "Stochastics")]
        public int SmoothK4 { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Keltner Period", Order = 1, GroupName = "Keltner Channel")]
        public int KeltnerPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, 10.0)]
        [Display(Name = "Keltner Multiplier", Order = 2, GroupName = "Keltner Channel")]
        public double KeltnerMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(1, 20)]
        [Display(Name = "Candles Outside Keltner", Order = 3, GroupName = "Keltner Channel")]
        public int KeltnerCandleCount { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, 10.0)]
        [Display(Name = "VWAP Band Multiplier", Order = 1, GroupName = "VWAP")]
        public double VwapBandMultiplier { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use WaveTrend (C5)", Order = 1, GroupName = "VuManChu WaveTrend")]
        public bool UseWaveTrend { get; set; }

        [NinjaScriptProperty]
        [Range(1, 50)]
        [Display(Name = "WT Channel Length", Order = 2, GroupName = "VuManChu WaveTrend")]
        public int WtChannelLen { get; set; }

        [NinjaScriptProperty]
        [Range(1, 50)]
        [Display(Name = "WT Average Length", Order = 3, GroupName = "VuManChu WaveTrend")]
        public int WtAverageLen { get; set; }

        [NinjaScriptProperty]
        [Range(1, 20)]
        [Display(Name = "WT MA Length", Order = 4, GroupName = "VuManChu WaveTrend")]
        public int WtMALen { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Money Flow (C4)", Order = 1, GroupName = "Money Flow")]
        public bool UseMfi { get; set; }

        [NinjaScriptProperty]
        [Range(1, 200)]
        [Display(Name = "MFI Period", Order = 2, GroupName = "Money Flow")]
        public int MfiPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, 500)]
        [Display(Name = "MFI Multiplier", Order = 3, GroupName = "Money Flow")]
        public double MfiMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(1, 20)]
        [Display(Name = "MFI Lookback Bars", Order = 4, GroupName = "Money Flow")]
        public int MfiLookback { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use MACD (C6)", Order = 1, GroupName = "MACD")]
        public bool UseMacd { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "MACD Fast", Order = 2, GroupName = "MACD")]
        public int MacdFast { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "MACD Slow", Order = 3, GroupName = "MACD")]
        public int MacdSlow { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "MACD Signal", Order = 4, GroupName = "MACD")]
        public int MacdSignal { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Stop Loss Type", Order = 1, GroupName = "Stop Loss")]
        public StopLossType StopType { get; set; }

        [NinjaScriptProperty]
        [Range(1, 10000)]
        [Display(Name = "Fixed Stop (ticks)", Order = 2, GroupName = "Stop Loss")]
        public int StopPoints { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, 10.0)]
        [Display(Name = "ATR Multiplier", Order = 3, GroupName = "Stop Loss")]
        public double AtrMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "ATR Period", Order = 4, GroupName = "Stop Loss")]
        public int AtrPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Swing Lookback Bars", Order = 5, GroupName = "Stop Loss")]
        public int SwingLookback { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Profit Target Type", Order = 1, GroupName = "Profit Target")]
        public ProfitTargetType TargetType { get; set; }

        [NinjaScriptProperty]
        [Range(1, 10000)]
        [Display(Name = "Fixed Target (ticks)", Order = 2, GroupName = "Profit Target")]
        public int TargetPoints { get; set; }
    }
}