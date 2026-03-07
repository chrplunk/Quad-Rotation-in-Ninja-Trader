#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
#endregion

// QR_RotationStrategyPlus
// - STATIC Quad Rotation stochastic parameters (requested)
// - Adds Keltner "road" filter: Length 20, Mult 2.0, ATR Len 9, Source Close, EMA (TV-like)
// - Adds Money Flow (VuManChu-style f_rsimfi formula) and requires it to agree with trade direction
// - Adds WaveTrend dot (VuManChu/Cipher-B style) as proxy for Market Cipher dot
// - Optional VWAP band filter: can be required (A+ only) or just tags entries as A+
//
// IMPORTANT NOTE (Market Cipher dots):
// Market Cipher B is proprietary. This implements the common VuManChu/Cipher-B WaveTrend dot logic
// (WT cross + OB/OS), which matches the buySignal/sellSignal concept in the VMC script you pasted.

namespace NinjaTrader.NinjaScript.Strategies
{
	public class QR_RotationStrategyPlus : Strategy
	{
		// =========================
		// Enums
		// =========================
		public enum EntryTimingMode
		{
			EnterOnSignalBarClose = 0, // "as it fires" (bar close when condition becomes true)
			EnterNextBar          = 1  // next-bar confirmation (pending)
		}

		public enum VwapMode
		{
			Off = 0,
			Band2Only = 2,
			Band2Or3 = 3
		}

		// =========================
		// User Inputs - Execution
		// =========================
		[NinjaScriptProperty]
		[Display(Name = "Entry timing", Order = 1, GroupName = "Execution")]
		public EntryTimingMode EntryTiming { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Only enter on NEW rotation (edge trigger)", Order = 2, GroupName = "Execution")]
		public bool OnlyEnterOnNewRotation { get; set; }

		// =========================
		// User Inputs - Risk
		// =========================
		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Contracts (MES)", Order = 1, GroupName = "Risk")]
		public int Contracts { get; set; }

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Stop Loss (ticks)", Order = 2, GroupName = "Risk")]
		public int StopLossTicks { get; set; }

		[NinjaScriptProperty]
		[Range(1, 2000)]
		[Display(Name = "Take Profit (ticks)", Order = 3, GroupName = "Risk")]
		public int ProfitTargetTicks { get; set; }

		// =========================
		// User Inputs - Signals (Quad)
		// =========================
		[NinjaScriptProperty]
		[Range(1, 4)]
		[Display(Name = "Min # of Stochastics for Rotation", Order = 1, GroupName = "Signals - Quad")]
		public int MinCount { get; set; }

		// =========================
		// User Inputs - Keltner Road (TV settings)
		// =========================
		[NinjaScriptProperty]
		[Range(1, 200)]
		[Display(Name = "KC Length", Order = 1, GroupName = "Signals - Keltner Road")]
		public int KcLength { get; set; }

		[NinjaScriptProperty]
		[Range(0.1, 10.0)]
		[Display(Name = "KC Multiplier", Order = 2, GroupName = "Signals - Keltner Road")]
		public double KcMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(1, 200)]
		[Display(Name = "KC ATR Length", Order = 3, GroupName = "Signals - Keltner Road")]
		public int KcAtrLength { get; set; }

		[NinjaScriptProperty]
		[Range(1, 20)]
		[Display(Name = "# closes outside KC", Order = 4, GroupName = "Signals - Keltner Road")]
		public int KcOutsideCloses { get; set; }

		// =========================
		// User Inputs - Money Flow (VuManChu-style)
		// =========================
		[NinjaScriptProperty]
		[Display(Name = "Require Money Flow agreement", Order = 1, GroupName = "Signals - Money Flow")]
		public bool RequireMoneyFlow { get; set; }

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "MF Period", Order = 2, GroupName = "Signals - Money Flow")]
		public int MfPeriod { get; set; }

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "MF Multiplier", Order = 3, GroupName = "Signals - Money Flow")]
		public double MfMultiplier { get; set; }

		[NinjaScriptProperty]
		[Range(-50, 50)]
		[Display(Name = "MF Y Offset (PosY)", Order = 4, GroupName = "Signals - Money Flow")]
		public double MfPosY { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "MF must be rising/falling too", Order = 5, GroupName = "Signals - Money Flow")]
		public bool MfRequireSlope { get; set; }

		// =========================
		// User Inputs - WaveTrend Dot
		// =========================
		[NinjaScriptProperty]
		[Display(Name = "Require WT dot (green/red)", Order = 1, GroupName = "Signals - WaveTrend Dot")]
		public bool RequireWaveTrendDot { get; set; }

		[NinjaScriptProperty]
		[Range(1, 200)]
		[Display(Name = "WT Channel Len", Order = 2, GroupName = "Signals - WaveTrend Dot")]
		public int WtChannelLen { get; set; }

		[NinjaScriptProperty]
		[Range(1, 200)]
		[Display(Name = "WT Average Len", Order = 3, GroupName = "Signals - WaveTrend Dot")]
		public int WtAverageLen { get; set; }

		[NinjaScriptProperty]
		[Range(1, 50)]
		[Display(Name = "WT MA Len", Order = 4, GroupName = "Signals - WaveTrend Dot")]
		public int WtMaLen { get; set; }

		[NinjaScriptProperty]
		[Range(-200, 200)]
		[Display(Name = "WT Oversold Level", Order = 5, GroupName = "Signals - WaveTrend Dot")]
		public double WtOversold { get; set; }

		[NinjaScriptProperty]
		[Range(-200, 200)]
		[Display(Name = "WT Overbought Level", Order = 6, GroupName = "Signals - WaveTrend Dot")]
		public double WtOverbought { get; set; }

		// =========================
		// User Inputs - VWAP Bands (optional / A+)
		// =========================
		[NinjaScriptProperty]
		[Display(Name = "VWAP filter mode", Order = 1, GroupName = "Signals - VWAP (A+)")]
		public VwapMode VwapFilterMode { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Require VWAP for entry (A+ only)", Order = 2, GroupName = "Signals - VWAP (A+)")]
		public bool RequireVwapForEntry { get; set; }

		[NinjaScriptProperty]
		[Range(0, 50)]
		[Display(Name = "VWAP buffer (ticks)", Order = 3, GroupName = "Signals - VWAP (A+)")]
		public int VwapBufferTicks { get; set; }

		// =========================
		// User Inputs - Rotation Exit (optional)
		// =========================
		[NinjaScriptProperty]
		[Display(Name = "Use Rotation Exit (Stoch4 other side + curve)", Order = 1, GroupName = "Exits")]
		public bool UseRotationExit { get; set; }

		[NinjaScriptProperty]
		[Range(1, 99)]
		[Display(Name = "Exit Long at ≥", Order = 2, GroupName = "Exits")]
		public int ExitLongAt { get; set; }

		[NinjaScriptProperty]
		[Range(1, 99)]
		[Display(Name = "Exit Short at ≤", Order = 3, GroupName = "Exits")]
		public int ExitShortAt { get; set; }

		// =========================
		// STATIC Quad Stoch Settings (requested)
		// =========================
		private const int QK1 = 9;
		private const int QD1 = 3;

		private const int QK2 = 14;
		private const int QD2 = 3;

		private const int QK3 = 40;
		private const int QD3 = 4;

		private const int QK4 = 60;
		private const int QD4 = 10;
		private const int QSmoothK4 = 1;

		// =========================
		// Internals - Quad Stoch
		// =========================
		private Series<double> rawK1, smoothK1, dSeries1;
		private Series<double> rawK2, smoothK2, dSeries2;
		private Series<double> rawK3, smoothK3, dSeries3;
		private Series<double> rawK4, smoothK4Series, dSeries4;

		private bool prevBgGreen;
		private bool prevBgRed;

		private bool pendingLong;
		private bool pendingShort;

		// =========================
		// Internals - Keltner Road
		// =========================
		private EMA emaKc;
		private ATR atrKc;

		// =========================
		// Internals - Money Flow
		// =========================
		private Series<double> mfRaw;
		private SMA mfSma;

		// =========================
		// Internals - WaveTrend
		// =========================
		private Series<double> wtSrc;     // HLC3
		private EMA wtEsa;
		private Series<double> wtAbsDev;
		private EMA wtDe;
		private Series<double> wtCi;
		private EMA wt1;
		private SMA wt2;

		// =========================
		// Internals - Session VWAP + stdev
		// =========================
		private double cumV, cumPV, cumP2V;
		private double vwap, vwapStd;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "QR_RotationStrategyPlus";
				Description = "Quad Rotation strategy with Keltner Road + Money Flow + WaveTrend dot + optional VWAP A+ filter.";
				Calculate = Calculate.OnBarClose;
				EntriesPerDirection = 1;
				EntryHandling = EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy = true;
				ExitOnSessionCloseSeconds = 30;
				IsInstantiatedOnEachOptimizationIteration = false;

				// Execution defaults
				EntryTiming = EntryTimingMode.EnterNextBar;
				OnlyEnterOnNewRotation = true;

				// Risk defaults
				Contracts = 1;
				StopLossTicks = 20;
				ProfitTargetTicks = 30;

				// Quad defaults
				MinCount = 4;

				// KC defaults (your TV screenshot)
				KcLength = 20;
				KcMultiplier = 2.0;
				KcAtrLength = 9;
				KcOutsideCloses = 4;

				// Money Flow defaults (from your VMC script)
				RequireMoneyFlow = true;
				MfPeriod = 60;
				MfMultiplier = 150.0;
				MfPosY = 2.5;
				MfRequireSlope = true;

				// WaveTrend defaults (from your VMC script)
				RequireWaveTrendDot = true;
				WtChannelLen = 9;
				WtAverageLen = 12;
				WtMaLen = 3;
				WtOversold = -53;
				WtOverbought = 53;

				// VWAP defaults (optional)
				VwapFilterMode = VwapMode.Off;
				RequireVwapForEntry = false; // if true, only A+ trades
				VwapBufferTicks = 2;

				// Rotation exit defaults
				UseRotationExit = false;
				ExitLongAt = 80;
				ExitShortAt = 20;
			}
			else if (State == State.Configure)
			{
				// Fixed SL/TP (ticks) on the named entry signals we use
				SetStopLoss("QR_Long",  CalculationMode.Ticks, StopLossTicks, false);
				SetStopLoss("QR_Short", CalculationMode.Ticks, StopLossTicks, false);

				SetProfitTarget("QR_Long",  CalculationMode.Ticks, ProfitTargetTicks);
				SetProfitTarget("QR_Short", CalculationMode.Ticks, ProfitTargetTicks);
			}
			else if (State == State.DataLoaded)
			{
				// Quad series
				rawK1 = new Series<double>(this);
				smoothK1 = new Series<double>(this);
				dSeries1 = new Series<double>(this);

				rawK2 = new Series<double>(this);
				smoothK2 = new Series<double>(this);
				dSeries2 = new Series<double>(this);

				rawK3 = new Series<double>(this);
				smoothK3 = new Series<double>(this);
				dSeries3 = new Series<double>(this);

				rawK4 = new Series<double>(this);
				smoothK4Series = new Series<double>(this);
				dSeries4 = new Series<double>(this);

				prevBgGreen = false;
				prevBgRed = false;
				pendingLong = false;
				pendingShort = false;

				// KC components
				emaKc = EMA(Close, KcLength);
				atrKc = ATR(KcAtrLength);

				// Money flow (custom series + SMA)
				mfRaw = new Series<double>(this);
				mfSma = SMA(mfRaw, MfPeriod);

				// WaveTrend
				wtSrc = new Series<double>(this);
				wtEsa = EMA(wtSrc, WtChannelLen);

				wtAbsDev = new Series<double>(this);
				wtDe = EMA(wtAbsDev, WtChannelLen);

				wtCi = new Series<double>(this);
				wt1 = EMA(wtCi, WtAverageLen);
				wt2 = SMA(wt1, WtMaLen);

				ResetSessionVwap();
			}
		}

		protected override void OnBarUpdate()
		{
			// Basic warmup
			int warm = Math.Max(60, Math.Max(KcLength, MfPeriod)) + 5;
			if (CurrentBar < warm)
				return;

			if (Bars.IsFirstBarOfSession)
				ResetSessionVwap();

			UpdateSessionVwap();

			// =========================
			// 1) Quad Rotation (bgGreen / bgRed)
			// =========================
			double s1, s2, s3, s4;
			ComputeStochD(QK1, 1, QD1, rawK1, smoothK1, dSeries1, out s1);
			ComputeStochD(QK2, 1, QD2, rawK2, smoothK2, dSeries2, out s2);
			ComputeStochD(QK3, 1, QD3, rawK3, smoothK3, dSeries3, out s3);
			ComputeStochD(QK4, QSmoothK4, QD4, rawK4, smoothK4Series, dSeries4, out s4);

			double s1Prev = dSeries1[1];
			double s2Prev = dSeries2[1];
			double s3Prev = dSeries3[1];
			double s4Prev = dSeries4[1];

			bool s1Down = (s1 - s1Prev) < 0 && s1 >= 50;
			bool s2Down = (s2 - s2Prev) < 0 && s2 >= 50;
			bool s3Down = (s3 - s3Prev) <= 0 && s3 > 80;
			bool s4Down = (s4 - s4Prev) <= 0 && s4 > 80;

			bool s1Up = (s1 - s1Prev) > 0 && s1 <= 50;
			bool s2Up = (s2 - s2Prev) > 0 && s2 <= 50;
			bool s3Up = (s3 - s3Prev) >= 0 && s3 < 20;
			bool s4Up = (s4 - s4Prev) >= 0 && s4 < 20;

			int downCount = (s1Down ? 1 : 0) + (s2Down ? 1 : 0) + (s3Down ? 1 : 0) + (s4Down ? 1 : 0);
			int upCount = (s1Up ? 1 : 0) + (s2Up ? 1 : 0) + (s3Up ? 1 : 0) + (s4Up ? 1 : 0);

			bool bgRed = downCount >= MinCount;
			bool bgGreen = upCount >= MinCount;

			bool newGreen = bgGreen && !prevBgGreen;
			bool newRed = bgRed && !prevBgRed;

			// =========================
			// 2) Keltner "Road" filter: N consecutive closes outside outer band
			// =========================
			bool kcLongOk = ConsecutiveClosesOutsideKC(lowerSide: true, bars: KcOutsideCloses);
			bool kcShortOk = ConsecutiveClosesOutsideKC(lowerSide: false, bars: KcOutsideCloses);

			// =========================
			// 3) Money Flow direction filter
			// =========================
			double mfNow = ComputeMoneyFlow();
			double mfPrev = mfSma[1];

			bool mfLongOk = true;
			bool mfShortOk = true;

			if (RequireMoneyFlow)
			{
				mfLongOk = mfNow > 0;
				mfShortOk = mfNow < 0;

				if (MfRequireSlope)
				{
					mfLongOk = mfLongOk && (mfNow > mfPrev);
					mfShortOk = mfShortOk && (mfNow < mfPrev);
				}
			}

			// =========================
			// 4) WaveTrend dot (buySignal / sellSignal)
			// =========================
			bool wtBuyDot = true;
			bool wtSellDot = true;

			if (RequireWaveTrendDot)
			{
				UpdateWaveTrend();

				double wt2v = wt2[0];
				bool wtOversoldNow = wt2v <= WtOversold;
				bool wtOverboughtNow = wt2v >= WtOverbought;

				bool crossUp = CrossAbove(wt1, wt2, 1);
				bool crossDown = CrossBelow(wt1, wt2, 1);

				wtBuyDot = crossUp && wtOversoldNow;
				wtSellDot = crossDown && wtOverboughtNow;
			}

			// =========================
			// 5) Optional VWAP A+ condition
			// =========================
			bool vwapLongOk = true;
			bool vwapShortOk = true;
			bool vwapIsAPlusLong = false;
			bool vwapIsAPlusShort = false;

			if (VwapFilterMode != VwapMode.Off && vwapStd > 0)
			{
				double buf = VwapBufferTicks * TickSize;

				double band2Upper = vwap + 2.0 * vwapStd;
				double band2Lower = vwap - 2.0 * vwapStd;

				double band3Upper = vwap + 3.0 * vwapStd;
				double band3Lower = vwap - 3.0 * vwapStd;

				bool longBand2 = Close[0] <= (band2Lower + buf);
				bool shortBand2 = Close[0] >= (band2Upper - buf);

				bool longBand3 = Close[0] <= (band3Lower + buf);
				bool shortBand3 = Close[0] >= (band3Upper - buf);

				if (VwapFilterMode == VwapMode.Band2Only)
				{
					vwapIsAPlusLong = longBand2;
					vwapIsAPlusShort = shortBand2;
				}
				else
				{
					vwapIsAPlusLong = longBand2 || longBand3;
					vwapIsAPlusShort = shortBand2 || shortBand3;
				}

				if (RequireVwapForEntry)
				{
					vwapLongOk = vwapIsAPlusLong;
					vwapShortOk = vwapIsAPlusShort;
				}
			}

			// =========================
			// 6) Final entry signals
			// =========================
			bool quadLong = OnlyEnterOnNewRotation ? newGreen : bgGreen;
			bool quadShort = OnlyEnterOnNewRotation ? newRed : bgRed;

			bool longSignal = quadLong && kcLongOk && mfLongOk && wtBuyDot && vwapLongOk;
			bool shortSignal = quadShort && kcShortOk && mfShortOk && wtSellDot && vwapShortOk;

			// =========================
			// 7) Entries (timing)
			// =========================
			if (EntryTiming == EntryTimingMode.EnterNextBar)
			{
				// Execute pending first
				if (pendingLong)
				{
					TryEnterLong(vwapIsAPlusLong);
					pendingLong = false;
				}
				if (pendingShort)
				{
					TryEnterShort(vwapIsAPlusShort);
					pendingShort = false;
				}

				// Set pending
				if (longSignal) pendingLong = true;
				if (shortSignal) pendingShort = true;
			}
			else
			{
				if (longSignal) TryEnterLong(vwapIsAPlusLong);
				if (shortSignal) TryEnterShort(vwapIsAPlusShort);
			}

			// =========================
			// 8) Optional Rotation Exit
			// =========================
			if (UseRotationExit)
			{
				bool s4CurvingDown = (s4 - s4Prev) < 0;
				bool s4CurvingUp = (s4 - s4Prev) > 0;

				if (Position.MarketPosition == MarketPosition.Long)
				{
					if (s4 >= ExitLongAt && s4CurvingDown)
						ExitLong("QR_RotationExit_Long", "QR_Long");
				}
				else if (Position.MarketPosition == MarketPosition.Short)
				{
					if (s4 <= ExitShortAt && s4CurvingUp)
						ExitShort("QR_RotationExit_Short", "QR_Short");
				}
			}

			prevBgGreen = bgGreen;
			prevBgRed = bgRed;
		}

		// =========================
		// Entry helpers (A+ tagging)
		// =========================
		private void TryEnterLong(bool aPlus)
		{
			if (Position.MarketPosition == MarketPosition.Long)
				return;

			if (Position.MarketPosition == MarketPosition.Short)
				ExitShort("QR_FlipToLong", "QR_Short");

			string signalName = aPlus ? "QR_Long_APlus" : "QR_Long";
			EnterLong(Contracts, signalName);
		}

		private void TryEnterShort(bool aPlus)
		{
			if (Position.MarketPosition == MarketPosition.Short)
				return;

			if (Position.MarketPosition == MarketPosition.Long)
				ExitLong("QR_FlipToShort", "QR_Long");

			string signalName = aPlus ? "QR_Short_APlus" : "QR_Short";
			EnterShort(Contracts, signalName);
		}

		// =========================
		// Keltner helper
		// =========================
		private bool ConsecutiveClosesOutsideKC(bool lowerSide, int bars)
		{
			for (int i = 0; i < bars; i++)
			{
				if (CurrentBar - i < 0) return false;

				double mid = emaKc[i];
				double atr = atrKc[i];
				double upper = mid + KcMultiplier * atr;
				double lower = mid - KcMultiplier * atr;

				if (lowerSide)
				{
					if (!(Close[i] < lower)) return false;
				}
				else
				{
					if (!(Close[i] > upper)) return false;
				}
			}
			return true;
		}

		// =========================
		// Money Flow (VuManChu f_rsimfi)
		// =========================
		private double ComputeMoneyFlow()
		{
			double hl = High[0] - Low[0];
			double ratio = (Math.Abs(hl) < 1e-10) ? 0.0 : (Close[0] - Open[0]) / hl;

			mfRaw[0] = ratio * MfMultiplier;
			return mfSma[0] - MfPosY;
		}

		// =========================
		// WaveTrend update
		// =========================
		private void UpdateWaveTrend()
		{
			double src = (High[0] + Low[0] + Close[0]) / 3.0;
			wtSrc[0] = src;

			double esa = wtEsa[0];
			wtAbsDev[0] = Math.Abs(src - esa);

			double de = wtDe[0];
			double ci = (Math.Abs(de) < 1e-10) ? 0.0 : (src - esa) / (0.015 * de);

			wtCi[0] = ci;
			// wt1/wt2 are indicator series built from wtCi
		}

		// =========================
		// Session VWAP + stdev (simple)
		// =========================
		private void ResetSessionVwap()
		{
			cumV = 0;
			cumPV = 0;
			cumP2V = 0;
			vwap = 0;
			vwapStd = 0;
		}

		private void UpdateSessionVwap()
		{
			double p = (High[0] + Low[0] + Close[0]) / 3.0;
			double v = Math.Max(Volume[0], 1);

			cumV += v;
			cumPV += p * v;
			cumP2V += (p * p) * v;

			vwap = cumPV / cumV;

			double meanP2 = cumP2V / cumV;
			double var = meanP2 - (vwap * vwap);
			if (var < 0) var = 0;

			vwapStd = Math.Sqrt(var);
		}

		// =========================
		// Quad stoch %D computation
		// =========================
		private void ComputeStochD(int kLen, int smoothKLen, int dLen,
			Series<double> rawK, Series<double> smoothK, Series<double> dSeries,
			out double dValue)
		{
			double hh = MAX(High, kLen)[0];
			double ll = MIN(Low, kLen)[0];
			double denom = hh - ll;

			double k = (Math.Abs(denom) < 1e-10) ? 50.0 : 100.0 * (Close[0] - ll) / denom;
			rawK[0] = k;

			int sk = Math.Max(1, smoothKLen);
			int dl = Math.Max(1, dLen);

			smoothK[0] = SMA(rawK, sk)[0];
			dSeries[0] = SMA(smoothK, dl)[0];

			dValue = dSeries[0];
		}
	}
}
