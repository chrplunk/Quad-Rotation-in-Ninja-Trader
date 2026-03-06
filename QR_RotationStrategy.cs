#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
	public class QR_RotationStrategy : Strategy
	{
		// =========================
		// Enums
		// =========================
		public enum EntryTimingMode
		{
			NextBarConfirm,   // OnBarClose logic (classic backtest)
			IntrabarAsFires   // As soon as rotation condition flips (requires Calculate.OnEachTick; best with Tick Replay)
		}

		// =========================
		// Execution
		// =========================

		[NinjaScriptProperty]
		[Display(Name = "Entry Timing", Order = 1, GroupName = "Execution")]
		public EntryTimingMode EntryTiming { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Only enter on NEW rotation (edge trigger)", Order = 2, GroupName = "Execution")]
		public bool OnlyEnterOnNewRotation { get; set; }

		// =========================
		// Exits
		// =========================

		[NinjaScriptProperty]
		[Display(Name = "Use Rotation Exit (Stoch4 other side + curve)", Order = 1, GroupName = "Exits")]
		public bool UseRotationExit { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Exit Long at >= (e.g., 80)", Order = 2, GroupName = "Exits")]
		public int ExitLongAt { get; set; }

		[NinjaScriptProperty]
		[Range(0, 99)]
		[Display(Name = "Exit Short at <= (e.g., 20)", Order = 3, GroupName = "Exits")]
		public int ExitShortAt { get; set; }

		// =========================
		// Risk
		// =========================

		[NinjaScriptProperty]
		[Range(1, 50)]
		[Display(Name = "Contracts (MES)", Order = 1, GroupName = "Risk")]
		public int Contracts { get; set; }

		[NinjaScriptProperty]
		[Range(1, 2000)]
		[Display(Name = "Stop Loss (ticks)", Order = 2, GroupName = "Risk")]
		public int StopLossTicks { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Use Profit Target", Order = 3, GroupName = "Risk")]
		public bool UseProfitTarget { get; set; }

		[NinjaScriptProperty]
		[Range(1, 2000)]
		[Display(Name = "Profit Target (ticks)", Order = 4, GroupName = "Risk")]
		public int ProfitTargetTicks { get; set; }

		// =========================
		// Signals (rotation)
		// =========================

		[NinjaScriptProperty]
		[Range(1, 4)]
		[Display(Name = "Min # of Stochastics for Rotation", Order = 1, GroupName = "Signals")]
		public int MinCount { get; set; }

		// =========================
		// Stoch inputs (same as indicator)
		// =========================

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "Stoch1 K", Order = 1, GroupName = "Stoch 1")]
		public int K1 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "Stoch1 D", Order = 2, GroupName = "Stoch 1")]
		public int D1 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "Stoch2 K", Order = 1, GroupName = "Stoch 2")]
		public int K2 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "Stoch2 D", Order = 2, GroupName = "Stoch 2")]
		public int D2 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 600)]
		[Display(Name = "Stoch3 K", Order = 1, GroupName = "Stoch 3")]
		public int K3 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 600)]
		[Display(Name = "Stoch3 D", Order = 2, GroupName = "Stoch 3")]
		public int D3 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 1000)]
		[Display(Name = "Stoch4 K", Order = 1, GroupName = "Stoch 4")]
		public int K4 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 1000)]
		[Display(Name = "Stoch4 D", Order = 2, GroupName = "Stoch 4")]
		public int D4 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 50)]
		[Display(Name = "Stoch4 SmoothK", Order = 3, GroupName = "Stoch 4")]
		public int SmoothK4 { get; set; }

		// =========================
		// Internals (%D series so logic is stable)
		// =========================

		private Series<double> rawK1, smoothK1, d1Series;
		private Series<double> rawK2, smoothK2, d2Series;
		private Series<double> rawK3, smoothK3, d3Series;
		private Series<double> rawK4, smoothK4, d4Series;

		private bool prevBgGreen;
		private bool prevBgRed;

		// prevents multiple entries on same bar in Intrabar mode
		private int lastEntryBar = -1;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name							= "QR_RotationStrategy";
				Calculate						= Calculate.OnBarClose; // will be overridden based on EntryTiming
				EntriesPerDirection				= 1;
				EntryHandling					= EntryHandling.AllEntries;

				IsExitOnSessionCloseStrategy	= true;
				ExitOnSessionCloseSeconds		= 30;

				// Execution defaults
				EntryTiming						= EntryTimingMode.NextBarConfirm;
				OnlyEnterOnNewRotation			= true;

				// Exit defaults
				UseRotationExit					= true;
				ExitLongAt						= 80;
				ExitShortAt						= 20;

				// Risk defaults
				Contracts						= 1;
				StopLossTicks					= 40;

				UseProfitTarget					= true;
				ProfitTargetTicks				= 80;

				// Signal defaults
				MinCount						= 4;

				// Stoch defaults
				K1 = 9;   D1 = 3;
				K2 = 14;  D2 = 3;
				K3 = 40;  D3 = 4;
				K4 = 60;  D4 = 10; SmoothK4 = 1;
			}
			else if (State == State.Configure)
			{
				// Make Calculate match the requested behavior
				Calculate = (EntryTiming == EntryTimingMode.IntrabarAsFires)
					? Calculate.OnEachTick
					: Calculate.OnBarClose;
			}
			else if (State == State.DataLoaded)
			{
				rawK1		= new Series<double>(this);
				smoothK1	= new Series<double>(this);
				d1Series	= new Series<double>(this);

				rawK2		= new Series<double>(this);
				smoothK2	= new Series<double>(this);
				d2Series	= new Series<double>(this);

				rawK3		= new Series<double>(this);
				smoothK3	= new Series<double>(this);
				d3Series	= new Series<double>(this);

				rawK4		= new Series<double>(this);
				smoothK4	= new Series<double>(this);
				d4Series	= new Series<double>(this);

				prevBgGreen = false;
				prevBgRed   = false;
				lastEntryBar = -1;
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 2)
				return;

			// Apply SL/TP from UI (Strategy Analyzer param edits take effect)
			SetStopLoss("QR_Long",  CalculationMode.Ticks, StopLossTicks, false);
			SetStopLoss("QR_Short", CalculationMode.Ticks, StopLossTicks, false);

			if (UseProfitTarget)
			{
				SetProfitTarget("QR_Long",  CalculationMode.Ticks, ProfitTargetTicks);
				SetProfitTarget("QR_Short", CalculationMode.Ticks, ProfitTargetTicks);
			}

			// Compute 4 stoch %D values
			double s1 = ComputeStochD(K1, 1,        D1, rawK1, smoothK1, d1Series);
			double s2 = ComputeStochD(K2, 1,        D2, rawK2, smoothK2, d2Series);
			double s3 = ComputeStochD(K3, 1,        D3, rawK3, smoothK3, d3Series);
			double s4 = ComputeStochD(K4, SmoothK4, D4, rawK4, smoothK4, d4Series);

			double s1Prev = d1Series[1];
			double s2Prev = d2Series[1];
			double s3Prev = d3Series[1];
			double s4Prev = d4Series[1];

			// Rotation slope logic (matching your indicator)
			bool s1Down = (s1 - s1Prev) < 0  && s1 >= 50;
			bool s2Down = (s2 - s2Prev) < 0  && s2 >= 50;
			bool s3Down = (s3 - s3Prev) <= 0 && s3 >  80;
			bool s4Down = (s4 - s4Prev) <= 0 && s4 >  80;

			bool s1Up   = (s1 - s1Prev) > 0  && s1 <= 50;
			bool s2Up   = (s2 - s2Prev) > 0  && s2 <= 50;
			bool s3Up   = (s3 - s3Prev) >= 0 && s3 <  20;
			bool s4Up   = (s4 - s4Prev) >= 0 && s4 <  20;

			int downCount = (s1Down ? 1 : 0) + (s2Down ? 1 : 0) + (s3Down ? 1 : 0) + (s4Down ? 1 : 0);
			int upCount   = (s1Up   ? 1 : 0) + (s2Up   ? 1 : 0) + (s3Up   ? 1 : 0) + (s4Up   ? 1 : 0);

			bool bgRed   = downCount >= MinCount;
			bool bgGreen = upCount   >= MinCount;

			bool edgeGreen = bgGreen && !prevBgGreen;
			bool edgeRed   = bgRed   && !prevBgRed;

			// ===== Entries =====
			// - NextBarConfirm: evaluates on bar close and enters on next bar (standard OnBarClose behavior)
			// - IntrabarAsFires: evaluates each tick and enters immediately when the condition flips true
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				// prevent spamming entries multiple times on same bar in Intrabar mode
				if (EntryTiming == EntryTimingMode.IntrabarAsFires && lastEntryBar == CurrentBar)
				{
					// do nothing
				}
				else
				{
					bool wantLong  = OnlyEnterOnNewRotation ? edgeGreen : bgGreen;
					bool wantShort = OnlyEnterOnNewRotation ? edgeRed   : bgRed;

					if (wantLong)
					{
						EnterLong(Contracts, "QR_Long");
						lastEntryBar = CurrentBar;
					}
					else if (wantShort)
					{
						EnterShort(Contracts, "QR_Short");
						lastEntryBar = CurrentBar;
					}
				}
			}

			// ===== Optional rotation exit (Stoch4 other side + curve) =====
			if (UseRotationExit)
			{
				if (Position.MarketPosition == MarketPosition.Long)
				{
					// “Other side” for long = high zone, then start curving down
					bool exitLong = (s4 >= ExitLongAt) && (s4 < s4Prev);
					if (exitLong)
						ExitLong("QR_Long_RotationExit", "QR_Long");
				}
				else if (Position.MarketPosition == MarketPosition.Short)
				{
					// “Other side” for short = low zone, then start curving up
					bool exitShort = (s4 <= ExitShortAt) && (s4 > s4Prev);
					if (exitShort)
						ExitShort("QR_Short_RotationExit", "QR_Short");
				}
			}

			prevBgGreen = bgGreen;
			prevBgRed   = bgRed;
		}

		private double ComputeStochD(int kLen, int smoothKLen, int dLen,
			Series<double> rawK, Series<double> smoothK, Series<double> dSeries)
		{
			double hh = MAX(High, kLen)[0];
			double ll = MIN(Low,  kLen)[0];
			double denom = hh - ll;

			double k = (Math.Abs(denom) < 1e-10) ? 50.0 : 100.0 * (Close[0] - ll) / denom;
			rawK[0] = k;

			smoothK[0] = SMA(rawK, smoothKLen)[0];
			dSeries[0] = SMA(smoothK, dLen)[0];

			return dSeries[0];
		}
	}
}
