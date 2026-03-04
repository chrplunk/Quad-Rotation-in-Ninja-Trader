#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;

using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public class QR : Indicator
	{
		// ===== Inputs =====
		[NinjaScriptProperty]
		[Range(1, 4)]
		[Display(Name = "Min # of Stochastics for BG Coloring", Order = 1, GroupName = "Signals")]
		public int MinCount { get; set; }

		// Stoch 1
		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "%K Length (Stochastic 1)", Order = 1, GroupName = "Stoch 1")]
		public int K1 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "%D Smoothing (Stochastic 1)", Order = 2, GroupName = "Stoch 1")]
		public int D1 { get; set; }

		// Stoch 2
		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "%K Length (Stochastic 2)", Order = 1, GroupName = "Stoch 2")]
		public int K2 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "%D Smoothing (Stochastic 2)", Order = 2, GroupName = "Stoch 2")]
		public int D2 { get; set; }

		// Stoch 3
		[NinjaScriptProperty]
		[Range(1, 600)]
		[Display(Name = "%K Length (Stochastic 3)", Order = 1, GroupName = "Stoch 3")]
		public int K3 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 600)]
		[Display(Name = "%D Smoothing (Stochastic 3)", Order = 2, GroupName = "Stoch 3")]
		public int D3 { get; set; }

		// Stoch 4
		[NinjaScriptProperty]
		[Range(1, 1000)]
		[Display(Name = "%K Length (Stochastic 4)", Order = 1, GroupName = "Stoch 4")]
		public int K4 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 1000)]
		[Display(Name = "%D Smoothing (Stochastic 4)", Order = 2, GroupName = "Stoch 4")]
		public int D4 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 50)]
		[Display(Name = "Smoothing (Stochastic 4)", Order = 3, GroupName = "Stoch 4")]
		public int SmoothK4 { get; set; }

		// ABCD shield
		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Bars Since Above 90 (Long)", Order = 1, GroupName = "ABCD Shield")]
		public int AbcdBars90 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Bars Since Below 10 (Short)", Order = 2, GroupName = "ABCD Shield")]
		public int AbcdBars10 { get; set; }

		// Alerts
		[NinjaScriptProperty]
		[Display(Name = "Enable Alerts", Order = 1, GroupName = "Alerts")]
		public bool EnableAlerts { get; set; }

		// ===== Internals =====
		private Series<double> rawK1, smoothK1, dSeries1;
		private Series<double> rawK2, smoothK2, dSeries2;
		private Series<double> rawK3, smoothK3, dSeries3;
		private Series<double> rawK4, smoothK4Series, dSeries4;

		private int barsSinceStoch4Le90;
		private int barsSinceStoch4Ge10;

		private bool prevBgRed, prevBgGreen, prevSuperDown, prevSuperUp, prevBearCont, prevBullCont, prevAbcdBull, prevAbcdBear;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "QR";
				Description = "Quad Rotation - 4 Stochastics Overlay (NT8.1.6.3 safe build: NO Draw.Line/Draw.Region).";
				IsOverlay = false;
				Calculate = Calculate.OnBarClose;

				MinCount = 4;

				K1 = 9;  D1 = 3;
				K2 = 14; D2 = 3;
				K3 = 40; D3 = 4;
				K4 = 60; D4 = 10; SmoothK4 = 1;

				AbcdBars90 = 5;
				AbcdBars10 = 5;

				EnableAlerts = true;

				AddPlot(Brushes.Gold,   "Stoch1D");
				AddPlot(Brushes.Orange, "Stoch2D");
				AddPlot(Brushes.Gray,   "Stoch3D");
				AddPlot(Brushes.White,  "Stoch4D");

				Plots[0].Width = 2;
				Plots[1].Width = 1;
				Plots[2].Width = 1;
				Plots[3].Width = 3;

				AddLine(Brushes.White, 80, "Overbought80");
				AddLine(Brushes.White, 20, "Oversold20");
				AddLine(Brushes.White, 50, "Midline50");
				AddLine(Brushes.White, 90, "ExtremeOB90");
				AddLine(Brushes.White, 10, "ExtremeOS10");
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

				barsSinceStoch4Le90 = 0;
				barsSinceStoch4Ge10 = 0;
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 1)
			{
				Values[0][0] = 50;
				Values[1][0] = 50;
				Values[2][0] = 50;
				Values[3][0] = 50;
				return;
			}

			ComputeStochD(K1, 1, D1, rawK1, smoothK1, dSeries1, out double s1);
			ComputeStochD(K2, 1, D2, rawK2, smoothK2, dSeries2, out double s2);
			ComputeStochD(K3, 1, D3, rawK3, smoothK3, dSeries3, out double s3);
			ComputeStochD(K4, SmoothK4, D4, rawK4, smoothK4Series, dSeries4, out double s4);

			Values[0][0] = s1;
			Values[1][0] = s2;
			Values[2][0] = s3;
			Values[3][0] = s4;

			// Base faint blue tint
			BackBrushes[0] = new SolidColorBrush(Color.FromArgb(35, 30, 144, 255));

			// Sloping logic (matches your Pine intent)
			bool s1Down = (s1 - Values[0][1]) < 0 && s1 >= 50;
			bool s2Down = (s2 - Values[1][1]) < 0 && s2 >= 50;
			bool s3Down = (s3 - Values[2][1]) <= 0 && s3 > 80;
			bool s4Down = (s4 - Values[3][1]) <= 0 && s4 > 80;

			bool s1Up = (s1 - Values[0][1]) > 0 && s1 <= 50;
			bool s2Up = (s2 - Values[1][1]) > 0 && s2 <= 50;
			bool s3Up = (s3 - Values[2][1]) >= 0 && s3 < 20;
			bool s4Up = (s4 - Values[3][1]) >= 0 && s4 < 20;

			int downCount = (s1Down ? 1 : 0) + (s2Down ? 1 : 0) + (s3Down ? 1 : 0) + (s4Down ? 1 : 0);
			int upCount   = (s1Up ? 1 : 0) + (s2Up ? 1 : 0) + (s3Up ? 1 : 0) + (s4Up ? 1 : 0);

			bool bgRed = downCount >= MinCount;
			bool bgGreen = upCount >= MinCount;

			if (bgRed)
				BackBrushes[0] = new SolidColorBrush(Color.FromArgb(90, 255, 0, 0));
			else if (bgGreen)
				BackBrushes[0] = new SolidColorBrush(Color.FromArgb(90, 0, 255, 0));

			if (EnableAlerts)
			{
				if (bgRed && !prevBgRed)
					Alert("BG_RED", Priority.Medium, "Red BG Triggered", "Alert1.wav", 0, Brushes.Red, Brushes.White);
				if (bgGreen && !prevBgGreen)
					Alert("BG_GREEN", Priority.Medium, "Green BG Triggered", "Alert1.wav", 0, Brushes.LimeGreen, Brushes.Black);
			}
			prevBgRed = bgRed;
			prevBgGreen = bgGreen;

			// ABCD shield counters (barssince equivalents)
			if (s4 <= 90) barsSinceStoch4Le90 = 0; else barsSinceStoch4Le90++;
			if (s4 >= 10) barsSinceStoch4Ge10 = 0; else barsSinceStoch4Ge10++;

			bool abcdBull = barsSinceStoch4Le90 > AbcdBars90;
			bool abcdBear = barsSinceStoch4Ge10 > AbcdBars10;

			if (abcdBull)
				Draw.Dot(this, "ABCD_BULL_" + CurrentBar, false, 0, 98, Brushes.LimeGreen);
			if (abcdBear)
				Draw.Dot(this, "ABCD_BEAR_" + CurrentBar, false, 0, 2, Brushes.Red);

			if (EnableAlerts)
			{
				if (abcdBull && !prevAbcdBull)
					Alert("ABCD_LONG", Priority.Medium, "ABCD Long detected", "Alert2.wav", 0, Brushes.LimeGreen, Brushes.Black);
				if (abcdBear && !prevAbcdBear)
					Alert("ABCD_SHORT", Priority.Medium, "ABCD Short detected", "Alert2.wav", 0, Brushes.Red, Brushes.White);
			}
			prevAbcdBull = abcdBull;
			prevAbcdBear = abcdBear;

			// Super signals (all 4)
			bool superDown = s1Down && s2Down && s3Down && s4Down;
			bool superUp   = s1Up && s2Up && s3Up && s4Up;

			if (MinCount != 4 && superDown)
				Draw.ArrowDown(this, "SUPER_DN_" + CurrentBar, false, 0, 98, Brushes.Red);

			if (MinCount != 4 && superUp)
				Draw.ArrowUp(this, "SUPER_UP_" + CurrentBar, false, 0, 2, Brushes.LimeGreen);

			if (EnableAlerts)
			{
				if (superDown && !prevSuperDown)
					Alert("SUPER_DOWN", Priority.High, "SUPER Down", "Alert3.wav", 0, Brushes.Red, Brushes.White);
				if (superUp && !prevSuperUp)
					Alert("SUPER_UP", Priority.High, "SUPER Up", "Alert3.wav", 0, Brushes.LimeGreen, Brushes.Black);
			}
			prevSuperDown = superDown;
			prevSuperUp = superUp;

			// Continuation logic
			bool s1CrossAbove80 = CrossAbove(Values[0], 80, 1);
			bool s1CrossBelow20 = CrossBelow(Values[0], 20, 1);

			bool s4Below30 = s4 < 30;
			bool s4Above70 = s4 > 70;

			int bearShieldCount = 0;
			int bullShieldCount = 0;
			for (int i = 0; i < 10 && CurrentBar - i >= 0; i++)
			{
				if (Values[3][i] < 10) bearShieldCount++;
				if (Values[3][i] > 90) bullShieldCount++;
			}

			bool bearCont = s4Below30 && s1CrossAbove80 && bearShieldCount >= 3;
			bool bullCont = s4Above70 && s1CrossBelow20 && bullShieldCount >= 3;

			if (bearCont)
				Draw.TriangleDown(this, "CONT_SHORT_" + CurrentBar, false, 0, 95, Brushes.Red);

			if (bullCont)
				Draw.TriangleUp(this, "CONT_LONG_" + CurrentBar, false, 0, 5, Brushes.LimeGreen);

			if (EnableAlerts)
			{
				if (bearCont && !prevBearCont)
					Alert("CONT_SHORT", Priority.Medium, "Look for Short Soon (continuation)", "Alert4.wav", 0, Brushes.Red, Brushes.White);

				if (bullCont && !prevBullCont)
					Alert("CONT_LONG", Priority.Medium, "Look for Long Soon (continuation)", "Alert4.wav", 0, Brushes.LimeGreen, Brushes.Black);
			}
			prevBearCont = bearCont;
			prevBullCont = bullCont;
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

			smoothK[0] = SMA(rawK, smoothKLen)[0];
			dSeries[0] = SMA(smoothK, dLen)[0];

			dValue = dSeries[0];
		}
	}
}