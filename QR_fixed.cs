#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;

using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

// QR (Indicator) - NT8.1.6.3 friendly
// - NO SharpDX (avoids Brush/Color ambiguity + ToDxBrush issues)
// - TV-style 20-80 zone fill implemented via Draw.Rectangle with DateTime anchors (single updating tag)
// - Quad rotation BG coloring (editable colors/opacities)
// - ABCD shield X markers
// - SUPER arrows (when MinCount != 4 and all 4 align)
// - Continuation "zingers"/warnings (your Pine section 9): vertical line markers + optional alerts
// - Optional hide Stoch2/Stoch3 plots visually (logic unchanged)
//
// IMPORTANT:
// Do NOT paste any "NinjaScript generated code" block. NT regenerates that automatically.

namespace NinjaTrader.NinjaScript.Indicators
{
	public class QR : Indicator
	{
		// =========================
		// Inputs (logic)
		// =========================

		[NinjaScriptProperty]
		[Range(1, 4)]
		[Display(Name = "Min # of Stochastics for BG Coloring", Order = 1, GroupName = "Signals")]
		public int MinCount { get; set; }

		// Alerts
		[NinjaScriptProperty]
		[Display(Name = "Enable Alerts", Order = 1, GroupName = "Alerts")]
		public bool EnableAlerts { get; set; }

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

		// ABCD shield (stoch4 stuck in extreme zone)
		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Bars Since Above 90 (Long)", Order = 1, GroupName = "ABCD Shield")]
		public int AbcdBars90 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Bars Since Below 10 (Short)", Order = 2, GroupName = "ABCD Shield")]
		public int AbcdBars10 { get; set; }

		// =========================
		// Inputs (visuals / colors)
		// =========================

		// Plot visibility toggles
		[NinjaScriptProperty]
		[Display(Name = "Show Stoch 2 Line", Order = 1, GroupName = "Visual - Line Toggles")]
		public bool ShowStoch2Line { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Stoch 3 Line", Order = 2, GroupName = "Visual - Line Toggles")]
		public bool ShowStoch3Line { get; set; }

		// TradingView-style filled 20-80 zone
		[NinjaScriptProperty]
		[Display(Name = "Show 20-80 Zone Fill", Order = 1, GroupName = "Colors - Zone")]
		public bool ShowZoneFill { get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "20-80 Zone Opacity (0-100)", Order = 2, GroupName = "Colors - Zone")]
		public int ZoneOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "20-80 Zone Fill Color", Order = 3, GroupName = "Colors - Zone")]
		public Brush ZoneFillBrush { get; set; }

		[Browsable(false)]
		public string ZoneFillBrushSerialize
		{
			get { return BrushSerialization.ToString(ZoneFillBrush); }
			set { ZoneFillBrush = BrushSerialization.FromString(value); }
		}

		// Quad rotation background colors (editable)
		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Quad RED Opacity (0-100)", Order = 1, GroupName = "Colors - Quad BG")]
		public int QuadRedOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "Quad RED Color", Order = 2, GroupName = "Colors - Quad BG")]
		public Brush QuadRedBrush { get; set; }

		[Browsable(false)]
		public string QuadRedBrushSerialize
		{
			get { return BrushSerialization.ToString(QuadRedBrush); }
			set { QuadRedBrush = BrushSerialization.FromString(value); }
		}

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name = "Quad GREEN Opacity (0-100)", Order = 3, GroupName = "Colors - Quad BG")]
		public int QuadGreenOpacity { get; set; }

		[XmlIgnore]
		[Display(Name = "Quad GREEN Color", Order = 4, GroupName = "Colors - Quad BG")]
		public Brush QuadGreenBrush { get; set; }

		[Browsable(false)]
		public string QuadGreenBrushSerialize
		{
			get { return BrushSerialization.ToString(QuadGreenBrush); }
			set { QuadGreenBrush = BrushSerialization.FromString(value); }
		}

		// Stochastic line colors (editable)
		[XmlIgnore]
		[Display(Name = "Stoch 1 Color", Order = 1, GroupName = "Colors - Lines")]
		public Brush Stoch1Brush { get; set; }

		[Browsable(false)]
		public string Stoch1BrushSerialize
		{
			get { return BrushSerialization.ToString(Stoch1Brush); }
			set { Stoch1Brush = BrushSerialization.FromString(value); }
		}

		[XmlIgnore]
		[Display(Name = "Stoch 2 Color", Order = 2, GroupName = "Colors - Lines")]
		public Brush Stoch2Brush { get; set; }

		[Browsable(false)]
		public string Stoch2BrushSerialize
		{
			get { return BrushSerialization.ToString(Stoch2Brush); }
			set { Stoch2Brush = BrushSerialization.FromString(value); }
		}

		[XmlIgnore]
		[Display(Name = "Stoch 3 Color", Order = 3, GroupName = "Colors - Lines")]
		public Brush Stoch3Brush { get; set; }

		[Browsable(false)]
		public string Stoch3BrushSerialize
		{
			get { return BrushSerialization.ToString(Stoch3Brush); }
			set { Stoch3Brush = BrushSerialization.FromString(value); }
		}

		[XmlIgnore]
		[Display(Name = "Stoch 4 Color", Order = 4, GroupName = "Colors - Lines")]
		public Brush Stoch4Brush { get; set; }

		[Browsable(false)]
		public string Stoch4BrushSerialize
		{
			get { return BrushSerialization.ToString(Stoch4Brush); }
			set { Stoch4Brush = BrushSerialization.FromString(value); }
		}

		// Zingers / warning crosses (Pine section 9)
		[NinjaScriptProperty]
		[Display(Name = "Show Zingers (warning vertical bars)", Order = 1, GroupName = "Zingers")]
		public bool ShowZingers { get; set; }

		[XmlIgnore]
		[Display(Name = "Zinger Bearish Color", Order = 2, GroupName = "Zingers")]
		public Brush ZingerBearBrush { get; set; }

		[Browsable(false)]
		public string ZingerBearBrushSerialize
		{
			get { return BrushSerialization.ToString(ZingerBearBrush); }
			set { ZingerBearBrush = BrushSerialization.FromString(value); }
		}

		[XmlIgnore]
		[Display(Name = "Zinger Bullish Color", Order = 3, GroupName = "Zingers")]
		public Brush ZingerBullBrush { get; set; }

		[Browsable(false)]
		public string ZingerBullBrushSerialize
		{
			get { return BrushSerialization.ToString(ZingerBullBrush); }
			set { ZingerBullBrush = BrushSerialization.FromString(value); }
		}

		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Zinger Line Width", Order = 4, GroupName = "Zingers")]
		public int ZingerWidth { get; set; }

		// =========================
		// Internals
		// =========================

		private Series<double> rawK1, smoothK1, dSeries1;
		private Series<double> rawK2, smoothK2, dSeries2;
		private Series<double> rawK3, smoothK3, dSeries3;
		private Series<double> rawK4, smoothK4Series, dSeries4;

		private int barsSinceStoch4Le90;
		private int barsSinceStoch4Ge10;

		private bool prevBgRed, prevBgGreen, prevSuperDown, prevSuperUp, prevBearZ, prevBullZ, prevAbcdBull, prevAbcdBear;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "QR";
				Description = "Quad Rotation - 4 Stochastics Overlay (TV-style 20-80 fill + zingers + editable colors).";
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

				// Visual defaults
				ShowStoch2Line = true;
				ShowStoch3Line = true;

				ShowZoneFill  = true;
				ZoneOpacity   = 100;
				ZoneFillBrush = Brushes.DodgerBlue;

				QuadRedBrush     = Brushes.Red;
				QuadGreenBrush   = Brushes.LimeGreen;
				QuadRedOpacity   = 30;
				QuadGreenOpacity = 30;

				Stoch1Brush = Brushes.Gold;
				Stoch2Brush = Brushes.Orange;
				Stoch3Brush = Brushes.Gray;
				Stoch4Brush = Brushes.White;

				ShowZingers = true;
				ZingerBearBrush = Brushes.Red;
				ZingerBullBrush = Brushes.LimeGreen;
				ZingerWidth = 4;

				AddPlot(Stoch1Brush, "Stoch1D");
				AddPlot(Stoch2Brush, "Stoch2D");
				AddPlot(Stoch3Brush, "Stoch3D");
				AddPlot(Stoch4Brush, "Stoch4D");

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

				prevBgRed = prevBgGreen = false;
				prevSuperDown = prevSuperUp = false;
				prevBearZ = prevBullZ = false;
				prevAbcdBull = prevAbcdBear = false;
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 2)
			{
				Values[0][0] = 50;
				Values[1][0] = 50;
				Values[2][0] = 50;
				Values[3][0] = 50;
				return;
			}

			// Apply user-selected plot colors (UI wins)
			Plots[0].Brush = Stoch1Brush;
			Plots[1].Brush = Stoch2Brush;
			Plots[2].Brush = Stoch3Brush;
			Plots[3].Brush = Stoch4Brush;

			ComputeStochD(K1, 1, D1, rawK1, smoothK1, dSeries1, out double s1);
			ComputeStochD(K2, 1, D2, rawK2, smoothK2, dSeries2, out double s2);
			ComputeStochD(K3, 1, D3, rawK3, smoothK3, dSeries3, out double s3);
			ComputeStochD(K4, SmoothK4, D4, rawK4, smoothK4Series, dSeries4, out double s4);

			// Plot assignment (with visibility toggles)
			Values[0][0] = s1;
			Values[1][0] = ShowStoch2Line ? s2 : double.NaN;
			Values[2][0] = ShowStoch3Line ? s3 : double.NaN;
			Values[3][0] = s4;

			// Use internal %D series for previous values (stable even if plots hidden)
			double s1Prev = dSeries1[1];
			double s2Prev = dSeries2[1];
			double s3Prev = dSeries3[1];
			double s4Prev = dSeries4[1];

			// -------------------------
			// TV-style 20-80 zone fill (rectangle spanning visible history)
			// -------------------------
			if (ShowZoneFill && ZoneFillBrush != null)
			{
				DateTime startTime = Time[CurrentBar]; // oldest loaded bar
				DateTime endTime   = Time[0];          // current bar
				Brush fill = WithOpacity(ZoneFillBrush, ZoneOpacity);

				// Single tag so it updates, not spams objects
				Draw.Rectangle(this, "QR_ZONE_20_80", false,
					startTime, 80,
					endTime,   20,
					fill, null, 0);
			}

			// -------------------------
			// Quad rotation BG coloring
			// -------------------------
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

			bool bgRed   = downCount >= MinCount;
			bool bgGreen = upCount   >= MinCount;

			BackBrushes[0] = null;
			if (bgRed)
				BackBrushes[0] = WithOpacity(QuadRedBrush, QuadRedOpacity);
			else if (bgGreen)
				BackBrushes[0] = WithOpacity(QuadGreenBrush, QuadGreenOpacity);

			if (EnableAlerts)
			{
				if (bgRed && !prevBgRed)
					Alert("BG_RED", Priority.Medium, "Red BG Triggered", "Alert1.wav", 0, Brushes.Red, Brushes.White);

				if (bgGreen && !prevBgGreen)
					Alert("BG_GREEN", Priority.Medium, "Green BG Triggered", "Alert1.wav", 0, Brushes.LimeGreen, Brushes.Black);
			}
			prevBgRed = bgRed;
			prevBgGreen = bgGreen;

			// -------------------------
			// ABCD shield counters (barssince equivalents)
			// Pine: stoch4Above90ForBars = barssince(stoch4D <= 90) > abcdBars90
			//       stoch4Below10ForBars = barssince(stoch4D >= 10) > abcdBars10
			// -------------------------
			if (s4 <= 90) barsSinceStoch4Le90 = 0; else barsSinceStoch4Le90++;
			if (s4 >= 10) barsSinceStoch4Ge10 = 0; else barsSinceStoch4Ge10++;

			bool abcdBull = barsSinceStoch4Le90 > AbcdBars90;
			bool abcdBear = barsSinceStoch4Ge10 > AbcdBars10;

			if (abcdBull)
				Draw.Cross(this, "ABCD_BULL_" + CurrentBar, false, 0, 98, Brushes.LimeGreen);

			if (abcdBear)
				Draw.Cross(this, "ABCD_BEAR_" + CurrentBar, false, 0, 2, Brushes.Red);

			if (EnableAlerts)
			{
				if (abcdBull && !prevAbcdBull)
					Alert("ABCD_LONG", Priority.Medium, "ABCD Long detected", "Alert2.wav", 0, Brushes.LimeGreen, Brushes.Black);

				if (abcdBear && !prevAbcdBear)
					Alert("ABCD_SHORT", Priority.Medium, "ABCD Short detected", "Alert2.wav", 0, Brushes.Red, Brushes.White);
			}
			prevAbcdBull = abcdBull;
			prevAbcdBear = abcdBear;

			// -------------------------
			// SUPER signals (all 4 align)
			// -------------------------
			bool superDown = s1Down && s2Down && s3Down && s4Down;
			bool superUp   = s1Up   && s2Up   && s3Up   && s4Up;

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

			// -------------------------
			// Zingers / warning crosses (Pine section 9)
			// - bearishCondition = stoch4Below30 AND stoch1CrossAbove80 AND (stoch4 < 10 in >=3 of last 10 bars)
			// - bullishCondition = stoch4Above70 AND stoch1CrossBelow20 AND (stoch4 > 90 in >=3 of last 10 bars)
			// -------------------------
			bool s1CrossAbove80 = CrossAbove(dSeries1, 80, 1);
			bool s1CrossBelow20 = CrossBelow(dSeries1, 20, 1);

			bool s4Below30 = s4 < 30;
			bool s4Above70 = s4 > 70;

			int bearShieldCount = 0;
			int bullShieldCount = 0;
			for (int i = 0; i < 10 && CurrentBar - i >= 0; i++)
			{
				if (dSeries4[i] < 10) bearShieldCount++;
				if (dSeries4[i] > 90) bullShieldCount++;
			}

			bool bearishZinger = s4Below30 && s1CrossAbove80 && bearShieldCount >= 3;
			bool bullishZinger = s4Above70 && s1CrossBelow20 && bullShieldCount >= 3;

			if (ShowZingers)
			{
				if (bearishZinger)
					Draw.VerticalLine(this, "Z_BEAR_" + CurrentBar, 0, ZingerBearBrush);

				if (bullishZinger)
					Draw.VerticalLine(this, "Z_BULL_" + CurrentBar, 0, ZingerBullBrush);

				// Make them thicker by drawing a tiny second/third line (NT VerticalLine has fixed width in UI),
				// so instead we use a Text marker with big font? Keep simple: users can style in Draw Objects.
				// If you want "fat bars", tell me and I'll switch to Draw.Rectangle with a time window.
			}

			if (EnableAlerts)
			{
				if (bearishZinger && !prevBearZ)
					Alert("ZINGER_BEAR", Priority.Medium, "Zinger Bearish (Look for Short Soon)", "Alert4.wav", 0, Brushes.Red, Brushes.White);

				if (bullishZinger && !prevBullZ)
					Alert("ZINGER_BULL", Priority.Medium, "Zinger Bullish (Look for Long Soon)", "Alert4.wav", 0, Brushes.LimeGreen, Brushes.Black);
			}
			prevBearZ = bearishZinger;
			prevBullZ = bullishZinger;
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

			int sk = Math.Max(1, smoothKLen);
			int dl = Math.Max(1, dLen);

			smoothK[0] = SMA(rawK, sk)[0];
			dSeries[0] = SMA(smoothK, dl)[0];

			dValue = dSeries[0];
		}

		private Brush WithOpacity(Brush b, int opacityPct0to100)
		{
			if (b == null)
				return null;

			SolidColorBrush sb = b as SolidColorBrush;
			if (sb == null)
				return b;

			int pct = Math.Max(0, Math.Min(100, opacityPct0to100));
			byte a = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(255.0 * (pct / 100.0))));
			Color c = sb.Color;

			SolidColorBrush nb = new SolidColorBrush(Color.FromArgb(a, c.R, c.G, c.B));
			nb.Freeze();
			return nb;
		}
	}

	internal static class BrushSerialization
	{
		public static string ToString(Brush b)
		{
			if (b == null) return null;
			BrushConverter bc = new BrushConverter();
			return bc.ConvertToString(b);
		}

		public static Brush FromString(string s)
		{
			if (string.IsNullOrEmpty(s)) return null;
			BrushConverter bc = new BrushConverter();
			return (Brush)bc.ConvertFromString(s);
		}
	}
}
