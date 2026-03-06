#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript;
#endregion

// NOTE:
// - This file intentionally does NOT include any "NinjaScript generated code" block.
//   NinjaTrader will generate that automatically.
// - Stochastic parameters are hard-coded (static) as requested.

namespace NinjaTrader.NinjaScript.Strategies
{
	public class QR_RotationStrategy : Strategy
	{
		// =========================
		// User inputs
		// =========================

		public enum QREntryTiming
		{
			EnterOnSignalBarClose = 0,  // "as it fires" (on the bar that triggers the rotation; order is submitted at bar close)
			EnterNextBarOpen      = 1   // next-bar confirmation timing
		}

		[NinjaScriptProperty]
		[Display(Name = "Entry timing", Order = 1, GroupName = "Execution")]
		public QREntryTiming EntryTiming { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Only enter on NEW rotation (edge trigger)", Order = 2, GroupName = "Execution")]
		public bool OnlyEnterOnNewRotation { get; set; }

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

		[NinjaScriptProperty]
		[Display(Name = "Use Rotation Exit (Stoch4 other side + curve)", Order = 1, GroupName = "Exits")]
		public bool UseRotationExit { get; set; }

		[NinjaScriptProperty]
		[Range(1, 99)]
		[Display(Name = "Exit Long at ≥ (e.g., 80)", Order = 2, GroupName = "Exits")]
		public int ExitLongAt { get; set; }

		[NinjaScriptProperty]
		[Range(1, 99)]
		[Display(Name = "Exit Short at ≤ (e.g., 20)", Order = 3, GroupName = "Exits")]
		public int ExitShortAt { get; set; }

		// Optional: keep MinCount adjustable (logic-level input, not stoch parameters)
		[NinjaScriptProperty]
		[Range(1, 4)]
		[Display(Name = "Min # of Stochastics for Rotation", Order = 1, GroupName = "Signals")]
		public int MinCount { get; set; }

		// =========================
		// Hard-coded stochastic settings (STATIC)
		// =========================
		private const int K1 = 9;
		private const int D1 = 3;

		private const int K2 = 14;
		private const int D2 = 3;

		private const int K3 = 40;
		private const int D3 = 4;

		private const int K4 = 60;
		private const int D4 = 10;
		private const int SmoothK4 = 1;

		// =========================
		// Internals
		// =========================
		private Series<double> rawK1, smoothK1, dSeries1;
		private Series<double> rawK2, smoothK2, dSeries2;
		private Series<double> rawK3, smoothK3, dSeries3;
		private Series<double> rawK4, smoothK4Series, dSeries4;

		private bool prevBgGreen;
		private bool prevBgRed;

		private bool pendingLong;
		private bool pendingShort;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name					= "QR_RotationStrategy";
				Description				= "Strategy based on Quad Rotation (4 stochastics). Enters long on GREEN rotation and short on RED rotation.";
				Calculate				= Calculate.OnBarClose;
				EntriesPerDirection		= 1;
				EntryHandling			= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy = true;
				ExitOnSessionCloseSeconds	 = 30;
				IsInstantiatedOnEachOptimizationIteration = false;

				// Defaults
				EntryTiming				= QREntryTiming.EnterNextBarOpen;
				OnlyEnterOnNewRotation	= true;

				Contracts				= 1;
				StopLossTicks			= 20;
				ProfitTargetTicks		= 40;

				UseRotationExit			= true;
				ExitLongAt				= 80;
				ExitShortAt				= 20;

				MinCount				= 4;
			}
			else if (State == State.Configure)
			{
				// Attach fixed SL/TP (ticks)
				// (These apply to the named entry signals below.)
				SetStopLoss("QR_Long",  CalculationMode.Ticks, StopLossTicks, false);
				SetStopLoss("QR_Short", CalculationMode.Ticks, StopLossTicks, false);

				SetProfitTarget("QR_Long",  CalculationMode.Ticks, ProfitTargetTicks);
				SetProfitTarget("QR_Short", CalculationMode.Ticks, ProfitTargetTicks);
			}
			else if (State == State.DataLoaded)
			{
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
				prevBgRed   = false;

				pendingLong = false;
				pendingShort = false;
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 2)
				return;

			// 1) Compute the 4 %D values (same math as the indicator)
			double s1, s2, s3, s4;
			ComputeStochD(K1, 1, D1, rawK1, smoothK1, dSeries1, out s1);
			ComputeStochD(K2, 1, D2, rawK2, smoothK2, dSeries2, out s2);
			ComputeStochD(K3, 1, D3, rawK3, smoothK3, dSeries3, out s3);
			ComputeStochD(K4, SmoothK4, D4, rawK4, smoothK4Series, dSeries4, out s4);

			double s1Prev = dSeries1[1];
			double s2Prev = dSeries2[1];
			double s3Prev = dSeries3[1];
			double s4Prev = dSeries4[1];

			// 2) Rotation slope logic (matches the indicator's intent)
			bool s1Down = (s1 - s1Prev) < 0 && s1 >= 50;
			bool s2Down = (s2 - s2Prev) < 0 && s2 >= 50;
			bool s3Down = (s3 - s3Prev) <= 0 && s3 > 80;
			bool s4Down = (s4 - s4Prev) <= 0 && s4 > 80;

			bool s1Up = (s1 - s1Prev) > 0 && s1 <= 50;
			bool s2Up = (s2 - s2Prev) > 0 && s2 <= 50;
			bool s3Up = (s3 - s3Prev) >= 0 && s3 < 20;
			bool s4Up = (s4 - s4Prev) >= 0 && s4 < 20;

			int downCount = (s1Down ? 1 : 0) + (s2Down ? 1 : 0) + (s3Down ? 1 : 0) + (s4Down ? 1 : 0);
			int upCount   = (s1Up ? 1 : 0) + (s2Up ? 1 : 0) + (s3Up ? 1 : 0) + (s4Up ? 1 : 0);

			bool bgRed   = downCount >= MinCount;
			bool bgGreen = upCount   >= MinCount;

			bool newGreen = bgGreen && (!prevBgGreen);
			bool newRed   = bgRed   && (!prevBgRed);

			// 3) Execute pending next-bar entries (if any)
			if (EntryTiming == QREntryTiming.EnterNextBarOpen)
			{
				// We submit the order at the open of THIS bar (after the prior bar set pending flags)
				// Since we're Calculate.OnBarClose, we approximate "open of this bar" by using IsFirstBarOfSession check? No.
				// In OnBarClose mode, orders fire after bar close; this still gives you a clean "next bar" behavior in backtests.
				if (pendingLong)
				{
					TryEnterLong();
					pendingLong = false;
				}
				if (pendingShort)
				{
					TryEnterShort();
					pendingShort = false;
				}
			}

			// 4) Entry signals
			bool allowEnterLong  = bgGreen;
			bool allowEnterShort = bgRed;

			if (OnlyEnterOnNewRotation)
			{
				allowEnterLong  = newGreen;
				allowEnterShort = newRed;
			}

			if (EntryTiming == QREntryTiming.EnterOnSignalBarClose)
			{
				if (allowEnterLong)
					TryEnterLong();
				if (allowEnterShort)
					TryEnterShort();
			}
			else // EnterNextBarOpen (pending)
			{
				if (allowEnterLong)
					pendingLong = true;
				if (allowEnterShort)
					pendingShort = true;
			}

			// 5) Optional Rotation Exit (Stoch4 reaches other side AND curves back)
			if (UseRotationExit)
			{
				// "curve back" logic:
				// - Long: when s4 is high (≥ ExitLongAt) and starts to slope down
				// - Short: when s4 is low (≤ ExitShortAt) and starts to slope up
				bool s4CurvingDown = (s4 - s4Prev) < 0;
				bool s4CurvingUp   = (s4 - s4Prev) > 0;

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
			prevBgRed   = bgRed;
		}

		private void TryEnterLong()
		{
			if (Position.MarketPosition == MarketPosition.Long)
				return;

			// If currently short, reverse (optional). Here we just exit and re-enter.
			if (Position.MarketPosition == MarketPosition.Short)
				ExitShort("QR_FlipToLong", "QR_Short");

			EnterLong(Contracts, "QR_Long");
		}

		private void TryEnterShort()
		{
			if (Position.MarketPosition == MarketPosition.Short)
				return;

			if (Position.MarketPosition == MarketPosition.Long)
				ExitLong("QR_FlipToShort", "QR_Long");

			EnterShort(Contracts, "QR_Short");
		}

		private void ComputeStochD(int kLen, int smoothKLen, int dLen,
			Series<double> rawK, Series<double> smoothK, Series<double> dSeries,
			out double dValue)
		{
			double hh = MAX(High, kLen)[0];
			double ll = MIN(Low, kLen)[0];
			double denom = hh - ll;

			double k = (Math.Abs(denom) < 1e-10) ? 50.0 : 100.0 * (Close[0] - ll) / denom;
			rawK[0] = k;

			// Guard against smoothKLen==0 (shouldn't happen)
			int sk = Math.Max(1, smoothKLen);
			int dl = Math.Max(1, dLen);

			smoothK[0] = SMA(rawK, sk)[0];
			dSeries[0] = SMA(smoothK, dl)[0];

			dValue = dSeries[0];
		}
	}
}
