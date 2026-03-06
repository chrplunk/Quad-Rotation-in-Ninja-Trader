#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.NinjaScript;
using NinjaTrader.NinjaScript;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
	public class QR_RotationStrategy : Strategy
	{
		// =========================
		// Fixed Stoch settings (static / non-editable)
		// =========================
		private const int K1 = 9,  D1 = 3;
		private const int K2 = 14, D2 = 3;
		private const int K3 = 40, D3 = 4;
		private const int K4 = 60, D4 = 10;
		private const int SmoothK4 = 1;

		// =========================
		// Inputs (editable)
		// =========================

		[NinjaScriptProperty]
		[Range(1, 4)]
		[Display(Name = "Min # of Stochastics for Rotation", Order = 1, GroupName = "Signals")]
		public int MinCount { get; set; }

		// Execution
		[NinjaScriptProperty]
		[Display(Name = "Only enter on NEW rotation (edge trigger)", Order = 1, GroupName = "Execution")]
		public bool OnlyEnterOnNewRotation { get; set; }

		public enum EntryTimingMode { NextBarConfirm, SameBarAsSignal }
		[NinjaScriptProperty]
		[Display(Name = "Entry Timing", Order = 2, GroupName = "Execution")]
		public EntryTimingMode EntryTiming { get; set; }

		// Risk
		[NinjaScriptProperty]
		[Range(1, 20)]
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

		// Exits
		[NinjaScriptProperty]
		[Display(Name = "Use Rotation Exit (Stoch4 other side + curve)", Order = 1, GroupName = "Exits")]
		public bool UseRotationExit { get; set; }

		[NinjaScriptProperty]
		[Range(1, 99)]
		[Display(Name = "Exit Long at >= (e.g., 80)", Order = 2, GroupName = "Exits")]
		public int ExitLongAt { get; set; }

		[NinjaScriptProperty]
		[Range(1, 99)]
		[Display(Name = "Exit Short at <= (e.g., 20)", Order = 3, GroupName = "Exits")]
		public int ExitShortAt { get; set; }

		// =========================
		// Internals
		// =========================
		private Series<double> rawK1, smoothK1, dSeries1;
		private Series<double> rawK2, smoothK2, dSeries2;
		private Series<double> rawK3, smoothK3, dSeries3;
		private Series<double> rawK4, smoothK4Series, dSeries4;

		private bool prevGreenRotation;
		private bool prevRedRotation;

		private bool pendingLong;
		private bool pendingShort;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "QR_RotationStrategy";
				Description = "Enters long on Green Quad Rotation and short on Red Quad Rotation (based on 4 fixed stochastics).";
				Calculate = Calculate.OnBarClose;

				// Signals
				MinCount = 4;

				// Execution
				OnlyEnterOnNewRotation = true;
				EntryTiming = EntryTimingMode.NextBarConfirm;

				// Risk
				Contracts = 1;
				StopLossTicks = 20;
				ProfitTargetTicks = 40;

				// Exits
				UseRotationExit = true;
				ExitLongAt = 80;
				ExitShortAt = 20;

				// basic safety defaults
				EntriesPerDirection = 1;
				EntryHandling = EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy = true;
				ExitOnSessionCloseSeconds = 30;
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
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 2)
				return;

			// Attach bracket orders
			SetStopLoss(CalculationMode.Ticks, StopLossTicks);
			SetProfitTarget(CalculationMode.Ticks, ProfitTargetTicks);

			// Compute stoch %D values (fixed parameters)
			double s1 = ComputeStochD(K1, 1, D1, rawK1, smoothK1, dSeries1);
			double s2 = ComputeStochD(K2, 1, D2, rawK2, smoothK2, dSeries2);
			double s3 = ComputeStochD(K3, 1, D3, rawK3, smoothK3, dSeries3);
			double s4 = ComputeStochD(K4, SmoothK4, D4, rawK4, smoothK4Series, dSeries4);

			// Previous values from internal series
			double s1Prev = dSeries1[1];
			double s2Prev = dSeries2[1];
			double s3Prev = dSeries3[1];
			double s4Prev = dSeries4[1];

			// Quad rotation slope logic (same as indicator)
			bool s1Down = (s1 - s1Prev) < 0 && s1 >= 50;
			bool s2Down = (s2 - s2Prev) < 0 && s2 >= 50;
			bool s3Down = (s3 - s3Prev) <= 0 && s3 > 80;
			bool s4Down = (s4 - s4Prev) <= 0 && s4 > 80;

			bool s1Up = (s1 - s1Prev) > 0 && s1 <= 50;
			bool s2Up = (s2 - s2Prev) > 0 && s2 <= 50;
			bool s3Up = (s3 - s3Prev) >= 0 && s3 < 20;
			bool s4Up = (s4 - s4Prev) >= 0 && s4 < 20;

			int downCount = (s1Down ? 1 : 0) + (s2Down ? 1 : 0) + (s3Down ? 1 : 0) + (s4Down ? 1 : 0);
			int upCount   = (s1Up   ? 1 : 0) + (s2Up   ? 1 : 0) + (s3Up   ? 1 : 0) + (s4Up   ? 1 : 0);

			bool redRotation   = downCount >= MinCount;
			bool greenRotation = upCount   >= MinCount;

			// Edge trigger
			bool newGreen = greenRotation && (!prevGreenRotation);
			bool newRed   = redRotation   && (!prevRedRotation);

			if (!OnlyEnterOnNewRotation)
			{
				newGreen = greenRotation;
				newRed   = redRotation;
			}

			// Reset opposite pending
			if (newGreen) pendingShort = false;
			if (newRed)   pendingLong  = false;

			// --- Entries ---
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				if (EntryTiming == EntryTimingMode.SameBarAsSignal)
				{
					if (newGreen) EnterLong(Contracts, "QR_Long");
					if (newRed)   EnterShort(Contracts, "QR_Short");
				}
				else
				{
					// Next bar confirm: arm on signal bar, fire on next bar open (historical = next bar)
					if (newGreen) pendingLong = true;
					if (newRed)   pendingShort = true;

					if (pendingLong && BarsSinceEntryExecution() > 0)
					{
						EnterLong(Contracts, "QR_Long");
						pendingLong = false;
					}
					if (pendingShort && BarsSinceEntryExecution() > 0)
					{
						EnterShort(Contracts, "QR_Short");
						pendingShort = false;
					}
				}
			}

			// --- Optional rotation-based exits using Stoch4 ---
			if (UseRotationExit)
			{
				// "Other side of range and starts to curve"
				bool s4CurvingDown = (s4 - s4Prev) < 0;
				bool s4CurvingUp   = (s4 - s4Prev) > 0;

				if (Position.MarketPosition == MarketPosition.Long)
				{
					if (s4 >= ExitLongAt && s4CurvingDown)
						ExitLong("QR_RotExit", "QR_Long");
				}
				else if (Position.MarketPosition == MarketPosition.Short)
				{
					if (s4 <= ExitShortAt && s4CurvingUp)
						ExitShort("QR_RotExit", "QR_Short");
				}
			}

			prevGreenRotation = greenRotation;
			prevRedRotation   = redRotation;
		}

		private double ComputeStochD(int kLen, int smoothKLen, int dLen,
			Series<double> rawK, Series<double> smoothK, Series<double> dSeries)
		{
			double hh = MAX(High, kLen)[0];
			double ll = MIN(Low, kLen)[0];
			double denom = hh - ll;

			double k = (Math.Abs(denom) < 1e-10) ? 50.0 : 100.0 * (Close[0] - ll) / denom;
			rawK[0] = k;

			smoothK[0] = SMA(rawK, smoothKLen)[0];
			dSeries[0] = SMA(smoothK, dLen)[0];

			return dSeries[0];
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		private QR_RotationStrategy[] cacheQR_RotationStrategy;
		public QR_RotationStrategy QR_RotationStrategy(int minCount, bool onlyEnterOnNewRotation, QR_RotationStrategy.EntryTimingMode entryTiming, int contracts, int stopLossTicks, int profitTargetTicks, bool useRotationExit, int exitLongAt, int exitShortAt)
		{
			return QR_RotationStrategy(Input, minCount, onlyEnterOnNewRotation, entryTiming, contracts, stopLossTicks, profitTargetTicks, useRotationExit, exitLongAt, exitShortAt);
		}

		public QR_RotationStrategy QR_RotationStrategy(ISeries<double> input, int minCount, bool onlyEnterOnNewRotation, QR_RotationStrategy.EntryTimingMode entryTiming, int contracts, int stopLossTicks, int profitTargetTicks, bool useRotationExit, int exitLongAt, int exitShortAt)
		{
			if (cacheQR_RotationStrategy != null)
				for (int idx = 0; idx < cacheQR_RotationStrategy.Length; idx++)
					if (cacheQR_RotationStrategy[idx] != null
						&& cacheQR_RotationStrategy[idx].MinCount == minCount
						&& cacheQR_RotationStrategy[idx].OnlyEnterOnNewRotation == onlyEnterOnNewRotation
						&& cacheQR_RotationStrategy[idx].EntryTiming == entryTiming
						&& cacheQR_RotationStrategy[idx].Contracts == contracts
						&& cacheQR_RotationStrategy[idx].StopLossTicks == stopLossTicks
						&& cacheQR_RotationStrategy[idx].ProfitTargetTicks == profitTargetTicks
						&& cacheQR_RotationStrategy[idx].UseRotationExit == useRotationExit
						&& cacheQR_RotationStrategy[idx].ExitLongAt == exitLongAt
						&& cacheQR_RotationStrategy[idx].ExitShortAt == exitShortAt
						&& cacheQR_RotationStrategy[idx].EqualsInput(input))
						return cacheQR_RotationStrategy[idx];
			return CacheStrategy<QR_RotationStrategy>(new QR_RotationStrategy()
			{
				MinCount = minCount,
				OnlyEnterOnNewRotation = onlyEnterOnNewRotation,
				EntryTiming = entryTiming,
				Contracts = contracts,
				StopLossTicks = stopLossTicks,
				ProfitTargetTicks = profitTargetTicks,
				UseRotationExit = useRotationExit,
				ExitLongAt = exitLongAt,
				ExitShortAt = exitShortAt
			}, input, ref cacheQR_RotationStrategy);
		}
	}
}

#endregion
