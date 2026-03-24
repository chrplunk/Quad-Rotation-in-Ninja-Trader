// Christopher Plunkett
// QR Strategy - Based on Quad Rotation + Keltner + MFI + MACD

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
        private const int K1 = 9,  D1 = 3;
        private const int K2 = 14, D2 = 3;
        private const int K3 = 40, D3 = 4;
        private const int K4 = 60, D4 = 10, SmoothK4 = 1;

        private Series<double> rawK1, smoothK1Series, dSeries1;
        private Series<double> rawK2, smoothK2Series, dSeries2;
        private Series<double> rawK3, smoothK3Series, dSeries3;
        private Series<double> rawK4, smoothK4Series2, dSeries4;

        private Series<double> mfiSeries;

        private NinjaTrader.NinjaScript.Indicators.KeltnerChannel keltner;
        private NinjaTrader.NinjaScript.Indicators.MACD macdInd;
        private NinjaTrader.NinjaScript.Indicators.EMA ema20;

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

                Quantity            = 2;
                MinScore            = 2;
                QRMinCount          = 3;

                UseKeltner          = true;
                KeltnerPeriod       = 20;
                KeltnerMultiplier   = 2.0;
                KeltnerLookback     = 6;

                UseMfi              = true;
                MfiPeriod           = 60;
                MfiMultiplier       = 150;
                MfiConsecutiveBars  = 5;

                UseMacd             = true;
                MacdFast            = 12;
                MacdSlow            = 26;
                MacdSignal          = 9;
                MacdConsecutiveBars = 5;

                StopPoints          = 16;

                TargetType          = ProfitTargetType.RR1to1_5;
                TargetPoints        = 24;
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

                mfiSeries = new Series<double>(this);

                keltner = KeltnerChannel(KeltnerMultiplier, KeltnerPeriod);
                macdInd = MACD(MacdFast, MacdSlow, MacdSignal);
                ema20   = EMA(20);
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
            // Compute MFI
            // =====================
            double candleRange = High[0] - Low[0];
            mfiSeries[0] = candleRange < 1e-10 ? 0 :
                ((Close[0] - Open[0]) / candleRange) * MfiMultiplier;

            bool mfiConsistentUp   = true;
            bool mfiConsistentDown = true;
            for (int i = 0; i < MfiConsecutiveBars && CurrentBar - i >= 1; i++)
            {
                double mfiCur  = SMA(mfiSeries, MfiPeriod)[i];
                double mfiPrev = SMA(mfiSeries, MfiPeriod)[i + 1];
                if (mfiCur <= mfiPrev) mfiConsistentUp   = false;
                if (mfiCur >= mfiPrev) mfiConsistentDown = false;
            }

            // =====================
            // MACD consecutive check
            // =====================
            bool macdConsistentUp   = true;
            bool macdConsistentDown = true;
            for (int i = 0; i < MacdConsecutiveBars && CurrentBar - i >= 1; i++)
            {
                double histCur  = macdInd[i]     - macdInd.Avg[i];
                double histPrev = macdInd[i + 1] - macdInd.Avg[i + 1];
                if (histCur <= histPrev) macdConsistentUp   = false;
                if (histCur >= histPrev) macdConsistentDown = false;
            }

            // =====================
            // EMA 20
            // =====================
            double ema20Val = ema20[0];

            // =====================
            // Keltner lookback
            // =====================
            bool keltnerTouchedLow  = false;
            bool keltnerTouchedHigh = false;
            if (UseKeltner)
            {
                for (int i = 0; i < KeltnerLookback && CurrentBar - i >= 0; i++)
                {
                    if (Low[i]  < keltner.Lower[i]) keltnerTouchedLow  = true;
                    if (High[i] > keltner.Upper[i]) keltnerTouchedHigh = true;
                    if (keltnerTouchedLow && keltnerTouchedHigh) break;
                }
            }

            // =====================
            // LONG criteria
            // =====================
            bool s1Up  = (s1 - s1Prev) > 0 && s1 <= 50;
            bool s2Up  = (s2 - s2Prev) > 0 && s2 <= 50;
            bool s3Up  = (s3 - s3Prev) > 0 && s3 < 20;
            bool s4Up  = (s4 - s4Prev) > 0 && s4 < 20;
            int qrUpCount = (s1Up ? 1 : 0) + (s2Up ? 1 : 0) + (s3Up ? 1 : 0) + (s4Up ? 1 : 0);
            bool c1Long = qrUpCount >= QRMinCount;
            bool c2Long = !UseKeltner || keltnerTouchedLow;
            bool c3Long = UseMfi  && mfiConsistentUp;
            bool c4Long = UseMacd && macdConsistentUp;

            int longScore = (c3Long ? 1 : 0)
                          + (c4Long ? 1 : 0);

            // =====================
            // SHORT criteria
            // =====================
            bool s1Down  = (s1 - s1Prev) < 0 && s1 >= 50;
            bool s2Down  = (s2 - s2Prev) < 0 && s2 >= 50;
            bool s3Down  = (s3 - s3Prev) < 0 && s3 > 80;
            bool s4Down  = (s4 - s4Prev) < 0 && s4 > 80;
            int qrDownCount = (s1Down ? 1 : 0) + (s2Down ? 1 : 0) + (s3Down ? 1 : 0) + (s4Down ? 1 : 0);
            bool c1Short = qrDownCount >= QRMinCount;
            bool c2Short = !UseKeltner || keltnerTouchedHigh;
            bool c3Short = UseMfi  && mfiConsistentDown;
            bool c4Short = UseMacd && macdConsistentDown;

            int shortScore = (c3Short ? 1 : 0)
                           + (c4Short ? 1 : 0);

            // =====================
            // EXITS
            // =====================
            if (Position.MarketPosition == MarketPosition.Long)
            {
                if (Close[0] <= stopPrice)
                    ExitLong("StopLoss", "EnterLong");

                if (TargetType == ProfitTargetType.Stoch4 && s4 >= 80)
                    ExitLong("ExitLong_Stoch4", "EnterLong");

                if (TargetType == ProfitTargetType.KeltnerOtherSide && Close[0] >= keltner.Upper[0])
                    ExitLong("ExitLong_Keltner", "EnterLong");

                if (TargetType == ProfitTargetType.EMA20 && Close[0] >= ema20Val)
                    ExitLong("ExitLong_EMA20", "EnterLong");

                if (TargetType == ProfitTargetType.RR1to1   ||
                    TargetType == ProfitTargetType.RR1to1_5 ||
                    TargetType == ProfitTargetType.RR1to2   ||
                    TargetType == ProfitTargetType.RR1to3   ||
                    TargetType == ProfitTargetType.FixedPoints)
                    if (Close[0] >= targetPrice)
                        ExitLong("ProfitTarget", "EnterLong");
            }
            else if (Position.MarketPosition == MarketPosition.Short)
            {
                if (Close[0] >= stopPrice)
                    ExitShort("StopLoss", "EnterShort");

                if (TargetType == ProfitTargetType.Stoch4 && s4 <= 20)
                    ExitShort("ExitShort_Stoch4", "EnterShort");

                if (TargetType == ProfitTargetType.KeltnerOtherSide && Close[0] <= keltner.Lower[0])
                    ExitShort("ExitShort_Keltner", "EnterShort");

                if (TargetType == ProfitTargetType.EMA20 && Close[0] <= ema20Val)
                    ExitShort("ExitShort_EMA20", "EnterShort");

                if (TargetType == ProfitTargetType.RR1to1   ||
                    TargetType == ProfitTargetType.RR1to1_5 ||
                    TargetType == ProfitTargetType.RR1to2   ||
                    TargetType == ProfitTargetType.RR1to3   ||
                    TargetType == ProfitTargetType.FixedPoints)
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
                    entryPrice  = Close[0];
                    stopPrice   = Close[0] - StopPoints * TickSize;
                    double risk = Math.Abs(Close[0] - stopPrice);
                    targetPrice = CalculateTarget(true, entryPrice, risk);
                    EnterLong(Quantity, "EnterLong");
                }
                else if (c1Short && c2Short && shortScore >= MinScore)
                {
                    entryPrice  = Close[0];
                    stopPrice   = Close[0] + StopPoints * TickSize;
                    double risk = Math.Abs(Close[0] - stopPrice);
                    targetPrice = CalculateTarget(false, entryPrice, risk);
                    EnterShort(Quantity, "EnterShort");
                }
            }

            // =====================
            // DEBUG
            // =====================
            Print(string.Format("{0} | LONG  C1(qr={1}/{2}) C2(kc={3}) Score={4}/2 C3={5} C4={6}",
                Time[0], qrUpCount, QRMinCount, c2Long, longScore, c3Long, c4Long));
            Print(string.Format("{0} | SHORT C1(qr={1}/{2}) C2(kc={3}) Score={4}/2 C3={5} C4={6}",
                Time[0], qrDownCount, QRMinCount, c2Short, shortScore, c3Short, c4Short));
        }

        private double CalculateTarget(bool isLong, double entry, double risk)
        {
            switch (TargetType)
            {
                case ProfitTargetType.RR1to1:
                    return isLong ? entry + risk       : entry - risk;
                case ProfitTargetType.RR1to1_5:
                    return isLong ? entry + risk * 1.5 : entry - risk * 1.5;
                case ProfitTargetType.RR1to2:
                    return isLong ? entry + risk * 2.0 : entry - risk * 2.0;
                case ProfitTargetType.RR1to3:
                    return isLong ? entry + risk * 3.0 : entry - risk * 3.0;
                case ProfitTargetType.FixedPoints:
                    return isLong
                        ? entry + TargetPoints * TickSize
                        : entry - TargetPoints * TickSize;
                default:
                    return isLong ? entry + risk : entry - risk;
            }
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

        public enum ProfitTargetType { Stoch4, KeltnerOtherSide, EMA20, RR1to1, RR1to1_5, RR1to2, RR1to3, FixedPoints }

        // =====================
        // Properties
        // =====================

        [NinjaScriptProperty]
        [Range(1, 40)]
        [Display(Name = "Quantity (contracts)", Order = 1, GroupName = "General")]
        public int Quantity { get; set; }

        [NinjaScriptProperty]
        [Range(0, 2)]
        [Display(Name = "Min Optional Score (0-2)", Order = 2, GroupName = "General")]
        public int MinScore { get; set; }

        [NinjaScriptProperty]
        [Range(1, 4)]
        [Display(Name = "QR Min Stochastics Count (1-4)", Order = 3, GroupName = "General")]
        public int QRMinCount { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Keltner Channel (C2)", Order = 1, GroupName = "Keltner Channel")]
        public bool UseKeltner { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Keltner Period", Order = 2, GroupName = "Keltner Channel")]
        public int KeltnerPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, 10.0)]
        [Display(Name = "Keltner Multiplier", Order = 3, GroupName = "Keltner Channel")]
        public double KeltnerMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(1, 20)]
        [Display(Name = "Keltner Lookback Bars", Order = 4, GroupName = "Keltner Channel")]
        public int KeltnerLookback { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Money Flow (C3)", Order = 1, GroupName = "Money Flow")]
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
        [Display(Name = "MFI Consecutive Bars", Order = 4, GroupName = "Money Flow")]
        public int MfiConsecutiveBars { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use MACD (C4)", Order = 1, GroupName = "MACD")]
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
        [Range(1, 20)]
        [Display(Name = "MACD Consecutive Bars Curving", Order = 5, GroupName = "MACD")]
        public int MacdConsecutiveBars { get; set; }

        [NinjaScriptProperty]
        [Range(1, 10000)]
        [Display(Name = "Fixed Stop (ticks)", Order = 1, GroupName = "Stop Loss")]
        public int StopPoints { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Profit Target Type", Order = 1, GroupName = "Profit Target")]
        public ProfitTargetType TargetType { get; set; }

        [NinjaScriptProperty]
        [Range(1, 10000)]
        [Display(Name = "Fixed Target (ticks)", Order = 2, GroupName = "Profit Target")]
        public int TargetPoints { get; set; }
    }
}