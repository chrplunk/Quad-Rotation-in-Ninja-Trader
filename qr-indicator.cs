// Christopher Plunkett 

#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using System.Xml.Serialization;

using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
	public class QR : Indicator
	{
		[NinjaScriptProperty]
		[Range(1, 4)]
		[Display(Name = "Min # of Stochastics for BG Coloring", Order = 1, GroupName = "Signals")]
		public int MinCount { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Enable Alerts", Order = 1, GroupName = "Alerts")]
		public bool EnableAlerts { get; set; }

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "%K Length (Stochastic 1)", Order = 1, GroupName = "Stoch 1")]
		public int K1 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "%D Smoothing (Stochastic 1)", Order = 2, GroupName = "Stoch 1")]
		public int D1 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "%K Length (Stochastic 2)", Order = 1, GroupName = "Stoch 2")]
		public int K2 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 300)]
		[Display(Name = "%D Smoothing (Stochastic 2)", Order = 2, GroupName = "Stoch 2")]
		public int D2 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 600)]
		[Display(Name = "%K Length (Stochastic 3)", Order = 1, GroupName = "Stoch 3")]
		public int K3 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 600)]
		[Display(Name = "%D Smoothing (Stochastic 3)", Order = 2, GroupName = "Stoch 3")]
		public int D3 { get; set; }

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

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Bars Since Above 90 (Long)", Order = 1, GroupName = "ABCD Shield")]
		public int AbcdBars90 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 500)]
		[Display(Name = "Bars Since Below 10 (Short)", Order = 2, GroupName = "ABCD Shield")]
		public int AbcdBars10 { get; set; }

		// Divergence
		[NinjaScriptProperty]
		[Display(Name = "Show Divergence - Stoch 1", Order = 1, GroupName = "Divergence")]
		public bool ShowDivLines1 { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Divergence - Stoch 2", Order = 2, GroupName = "Divergence")]
		public bool ShowDivLines2 { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Divergence - Stoch 3", Order = 3, GroupName = "Divergence")]
		public bool ShowDivLines3 { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Divergence - Stoch 4", Order = 4, GroupName = "Divergence")]
		public bool ShowDivLines4 { get; set; }

		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Pivot Strength (Left & Right Bars)", Order = 5, GroupName = "Divergence")]
		public int DivPivotStrength { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Min Bars Between Pivots", Order = 6, GroupName = "Divergence")]
		public int DivMinBars { get; set; }

		[NinjaScriptProperty]
		[Range(1, 200)]
		[Display(Name = "Max Bars Between Pivots", Order = 7, GroupName = "Divergence")]
		public int DivMaxBars { get; set; }

		[XmlIgnore]
		[Display(Name = "Bearish Divergence Color", Order = 8, GroupName = "Divergence")]
		public Brush BearDivBrush { get; set; }

		[Browsable(false)]
		public string BearDivBrushSerialize
		{
			get { return BrushSerialization.ToString(BearDivBrush); }
			set { BearDivBrush = BrushSerialization.FromString(value); }
		}

		[XmlIgnore]
		[Display(Name = "Bullish Divergence Color", Order = 9, GroupName = "Divergence")]
		public Brush BullDivBrush { get; set; }

		[Browsable(false)]
		public string BullDivBrushSerialize
		{
			get { return BrushSerialization.ToString(BullDivBrush); }
			set { BullDivBrush = BrushSerialization.FromString(value); }
		}

		// Visuals
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

		[NinjaScriptProperty]
		[Display(Name = "Show Stoch 2 Line", Order = 1, GroupName = "Visual - Line Toggles")]
		public bool ShowStoch2Line { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Stoch 3 Line", Order = 2, GroupName = "Visual - Line Toggles")]
		public bool ShowStoch3Line { get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Show Zingers", Order = 1, GroupName = "Zingers")]
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
		[Display(Name = "Show Warning Markers", Order = 1, GroupName = "ABCD Markers")]
		public bool ShowWarningMarkers { get; set; }

		[XmlIgnore]
		[Display(Name = "Above 90 Warning Color", Order = 2, GroupName = "ABCD Markers")]
		public Brush WarnAbove90Brush { get; set; }

		[Browsable(false)]
		public string WarnAbove90BrushSerialize
		{
			get { return BrushSerialization.ToString(WarnAbove90Brush); }
			set { WarnAbove90Brush = BrushSerialization.FromString(value); }
		}

		[XmlIgnore]
		[Display(Name = "Below 10 Warning Color", Order = 3, GroupName = "ABCD Markers")]
		public Brush WarnBelow10Brush { get; set; }

		[Browsable(false)]
		public string WarnBelow10BrushSerialize
		{
			get { return BrushSerialization.ToString(WarnBelow10Brush); }
			set { WarnBelow10Brush = BrushSerialization.FromString(value); }
		}

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

		// Internals
		private Series<double> rawK1, smoothK1, dSeries1;
		private Series<double> rawK2, smoothK2, dSeries2;
		private Series<double> rawK3, smoothK3, dSeries3;
		private Series<double> rawK4, smoothK4Series, dSeries4;

		private int barsSinceStoch4Le90;
		private int barsSinceStoch4Ge10;

		private bool prevBgRed, prevBgGreen, prevSuperDown, prevSuperUp, prevBearCont, prevBullCont, prevShieldAbove90, prevShieldBelow10;

		private int prevHighBar1 = -1; private double prevHighPrice1; private double prevHighStoch1;
		private int prevLowBar1  = -1; private double prevLowPrice1;  private double prevLowStoch1;
		private int prevHighBar2 = -1; private double prevHighPrice2; private double prevHighStoch2;
		private int prevLowBar2  = -1; private double prevLowPrice2;  private double prevLowStoch2;
		private int prevHighBar3 = -1; private double prevHighPrice3; private double prevHighStoch3;
		private int prevLowBar3  = -1; private double prevLowPrice3;  private double prevLowStoch3;
		private int prevHighBar4 = -1; private double prevHighPrice4; private double prevHighStoch4;
		private int prevLowBar4  = -1; private double prevLowPrice4;  private double prevLowStoch4;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "QR";
				Description = "Quad Rotation - 4 Stochastics Overlay.";
				IsOverlay = false;
				DrawOnPricePanel = false;
				Calculate = Calculate.OnBarClose;

				MinCount = 4;
				K1 = 9;  D1 = 3;
				K2 = 14; D2 = 3;
				K3 = 40; D3 = 4;
				K4 = 60; D4 = 10; SmoothK4 = 1;
				AbcdBars90 = 5;
				AbcdBars10 = 5;
				EnableAlerts = true;

				ShowDivLines1 = true;
				ShowDivLines2 = true;
				ShowDivLines3 = true;
				ShowDivLines4 = true;
				DivPivotStrength = 5;
				DivMinBars = 5;
				DivMaxBars = 60;
				BearDivBrush = Brushes.Red;
				BullDivBrush = Brushes.LimeGreen;

				ShowZoneFill = true;
				ZoneOpacity = 38;
				ZoneFillBrush = Brushes.DodgerBlue;

				QuadRedBrush = Brushes.Red;
				QuadGreenBrush = Brushes.LimeGreen;
				QuadRedOpacity = 90;
				QuadGreenOpacity = 90;

				ShowStoch2Line = true;
				ShowStoch3Line = true;

				Stoch1Brush = Brushes.Gold;
				Stoch2Brush = Brushes.Orange;
				Stoch3Brush = Brushes.Gray;
				Stoch4Brush = Brushes.White;

				ShowZingers = true;
				ZingerBearBrush = Brushes.Red;
				ZingerBullBrush = Brushes.LimeGreen;

				ShowWarningMarkers = true;
				WarnAbove90Brush = Brushes.LimeGreen;
				WarnBelow10Brush = Brushes.Red;

				AddPlot(Stoch1Brush, "Stoch1D");
				AddPlot(Stoch2Brush, "Stoch2D");
				AddPlot(Stoch3Brush, "Stoch3D");
				AddPlot(Stoch4Brush, "Stoch4D");
				AddPlot(WarnAbove90Brush, "WarnAbove90");
				AddPlot(WarnBelow10Brush, "WarnBelow10");

				Plots[0].Width = 2;
				Plots[1].Width = 1;
				Plots[2].Width = 1;
				Plots[3].Width = 3;
				Plots[4].PlotStyle = PlotStyle.TriangleUp;
				Plots[4].Width = 6;
				Plots[5].PlotStyle = PlotStyle.TriangleDown;
				Plots[5].Width = 6;

				AddLine(Brushes.Firebrick, 80, "Overbought80");
				AddLine(Brushes.Green, 20, "Oversold20");
				AddLine(Brushes.White, 50, "Midline50");
				AddLine(Brushes.Firebrick, 90, "ExtremeOB90");
				AddLine(Brushes.Green, 10, "ExtremeOS10");
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
			if (CurrentBar < 2)
			{
				for (int i = 0; i < 6; i++)
					Values[i][0] = double.NaN;
				return;
			}

			Plots[0].Brush = Stoch1Brush;
			Plots[1].Brush = Stoch2Brush;
			Plots[2].Brush = Stoch3Brush;
			Plots[3].Brush = Stoch4Brush;
			Plots[4].Brush = WarnAbove90Brush;
			Plots[5].Brush = WarnBelow10Brush;

			ComputeStochD(K1, 1, D1, rawK1, smoothK1, dSeries1, out double s1);
			ComputeStochD(K2, 1, D2, rawK2, smoothK2, dSeries2, out double s2);
			ComputeStochD(K3, 1, D3, rawK3, smoothK3, dSeries3, out double s3);
			ComputeStochD(K4, SmoothK4, D4, rawK4, smoothK4Series, dSeries4, out double s4);

			Values[0][0] = s1;
			Values[1][0] = ShowStoch2Line ? s2 : double.NaN;
			Values[2][0] = ShowStoch3Line ? s3 : double.NaN;
			Values[3][0] = s4;

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
			int upCount   = (s1Up ? 1 : 0) + (s2Up ? 1 : 0) + (s3Up ? 1 : 0) + (s4Up ? 1 : 0);

			bool bgRed   = downCount >= MinCount;
			bool bgGreen = upCount   >= MinCount;

			BackBrushes[0] = null;
			if (bgRed)
				BackBrushes[0] = WithOpacity(QuadRedBrush, QuadRedOpacity);
			else if (bgGreen)
				BackBrushes[0] = WithOpacity(QuadGreenBrush, QuadGreenOpacity);

			if (s4 <= 90) barsSinceStoch4Le90 = 0;
			else          barsSinceStoch4Le90++;

			if (s4 >= 10) barsSinceStoch4Ge10 = 0;
			else          barsSinceStoch4Ge10++;

			bool shieldAbove90 = barsSinceStoch4Le90 >= AbcdBars90;
			bool shieldBelow10 = barsSinceStoch4Ge10 >= AbcdBars10;

			Values[4][0] = (ShowWarningMarkers && shieldAbove90) ? 53 : double.NaN;
			Values[5][0] = (ShowWarningMarkers && shieldBelow10) ? 47 : double.NaN;

			if (EnableAlerts)
			{
				if (bgRed && !prevBgRed)
					Alert("BG_RED", Priority.Medium, "Red BG Triggered", "Alert1.wav", 0, Brushes.Red, Brushes.White);
				if (bgGreen && !prevBgGreen)
					Alert("BG_GREEN", Priority.Medium, "Green BG Triggered", "Alert1.wav", 0, Brushes.LimeGreen, Brushes.Black);
				if (shieldAbove90 && !prevShieldAbove90)
					Alert("ABCD_ABOVE90", Priority.Medium, "ABCD Above 90 warning", "Alert2.wav", 0, Brushes.LimeGreen, Brushes.Black);
				if (shieldBelow10 && !prevShieldBelow10)
					Alert("ABCD_BELOW10", Priority.Medium, "ABCD Below 10 warning", "Alert2.wav", 0, Brushes.Red, Brushes.White);
			}

			prevBgRed         = bgRed;
			prevBgGreen       = bgGreen;
			prevShieldAbove90 = shieldAbove90;
			prevShieldBelow10 = shieldBelow10;

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
			prevSuperUp   = superUp;

			// Zinger logic - arrows in subpanel
			bool s1WasAbove90 = false;
			bool s1WasBelow10 = false;
			for (int i = 1; i <= 10 && CurrentBar - i >= 0; i++)
			{
				if (dSeries1[i] > 90) s1WasAbove90 = true;
				if (dSeries1[i] < 10) s1WasBelow10 = true;
			}

			int bearShieldCount = 0;
			int bullShieldCount = 0;
			for (int i = 0; i < 10 && CurrentBar - i >= 0; i++)
			{
				if (dSeries4[i] < 10) bearShieldCount++;
				if (dSeries4[i] > 90) bullShieldCount++;
			}

			bool s4Below30 = s4 < 30;
			bool s4Above70 = s4 > 70;

			bool bearCont = s4Below30 && CrossBelow(dSeries1, 80, 1) && s1WasAbove90 && bearShieldCount >= 3;
			bool bullCont = s4Above70 && CrossAbove(dSeries1, 20, 1) && s1WasBelow10 && bullShieldCount >= 3;

			if (ShowZingers)
			{
				if (bearCont)
					Draw.ArrowDown(this, "Z_BEAR_" + CurrentBar, false, 0, 98, ZingerBearBrush);
				if (bullCont)
					Draw.ArrowUp(this, "Z_BULL_" + CurrentBar, false, 0, 2, ZingerBullBrush);
			}

			if (EnableAlerts)
			{
				if (bearCont && !prevBearCont)
					Alert("CONT_SHORT", Priority.Medium, "Look for Short Soon (continuation)", "Alert4.wav", 0, Brushes.Red, Brushes.White);
				if (bullCont && !prevBullCont)
					Alert("CONT_LONG", Priority.Medium, "Look for Long Soon (continuation)", "Alert4.wav", 0, Brushes.LimeGreen, Brushes.Black);
			}

			prevBearCont = bearCont;
			prevBullCont = bullCont;

			int minBarsNeeded = DivPivotStrength * 2 + 1;
			if (CurrentBar >= minBarsNeeded)
			{
				if (ShowDivLines1) ProcessDivergence(dSeries1, "S1", BearDivBrush, BullDivBrush, ref prevHighBar1, ref prevHighPrice1, ref prevHighStoch1, ref prevLowBar1, ref prevLowPrice1, ref prevLowStoch1);
				if (ShowDivLines2) ProcessDivergence(dSeries2, "S2", BearDivBrush, BullDivBrush, ref prevHighBar2, ref prevHighPrice2, ref prevHighStoch2, ref prevLowBar2, ref prevLowPrice2, ref prevLowStoch2);
				if (ShowDivLines3) ProcessDivergence(dSeries3, "S3", BearDivBrush, BullDivBrush, ref prevHighBar3, ref prevHighPrice3, ref prevHighStoch3, ref prevLowBar3, ref prevLowPrice3, ref prevLowStoch3);
				if (ShowDivLines4) ProcessDivergence(dSeries4, "S4", BearDivBrush, BullDivBrush, ref prevHighBar4, ref prevHighPrice4, ref prevHighStoch4, ref prevLowBar4, ref prevLowPrice4, ref prevLowStoch4);
			}
		}

		private void ProcessDivergence(
			Series<double> stochSeries, string tag,
			Brush bearBrush, Brush bullBrush,
			ref int prevHighBar,   ref double prevHighPrice, ref double prevHighStoch,
			ref int prevLowBar,    ref double prevLowPrice,  ref double prevLowStoch)
		{
			int s            = DivPivotStrength;
			int pivotAbsBar  = CurrentBar - s;
			int pivotBarsAgo = s;

			if (IsPivotHigh(stochSeries, s) && IsPivotHigh(High, s))
			{
				double curPrice = High[pivotBarsAgo];
				double curStoch = stochSeries[pivotBarsAgo];

				if (prevHighBar >= 0)
				{
					int gap = pivotAbsBar - prevHighBar;
					if (gap >= DivMinBars && gap <= DivMaxBars)
					{
						if (curPrice > prevHighPrice && curStoch < prevHighStoch)
						{
							int startBarsAgo = CurrentBar - prevHighBar;
							int endBarsAgo   = pivotBarsAgo;
							Draw.Line(this, "BD_" + tag + "_" + pivotAbsBar, false,
								startBarsAgo, prevHighStoch,
								endBarsAgo,   curStoch,
								bearBrush, DashStyleHelper.Solid, 2);
						}
					}
				}
				prevHighBar   = pivotAbsBar;
				prevHighPrice = curPrice;
				prevHighStoch = curStoch;
			}

			if (IsPivotLow(stochSeries, s) && IsPivotLow(Low, s))
			{
				double curPrice = Low[pivotBarsAgo];
				double curStoch = stochSeries[pivotBarsAgo];

				if (prevLowBar >= 0)
				{
					int gap = pivotAbsBar - prevLowBar;
					if (gap >= DivMinBars && gap <= DivMaxBars)
					{
						if (curPrice < prevLowPrice && curStoch > prevLowStoch)
						{
							int startBarsAgo = CurrentBar - prevLowBar;
							int endBarsAgo   = pivotBarsAgo;
							Draw.Line(this, "BU_" + tag + "_" + pivotAbsBar, false,
								startBarsAgo, prevLowStoch,
								endBarsAgo,   curStoch,
								bullBrush, DashStyleHelper.Solid, 2);
						}
					}
				}
				prevLowBar   = pivotAbsBar;
				prevLowPrice = curPrice;
				prevLowStoch = curStoch;
			}
		}

		private bool IsPivotHigh(ISeries<double> series, int strength)
		{
			double candidate = series[strength];
			for (int i = 1; i <= strength; i++)
			{
				if (candidate <= series[strength + i]) return false;
				if (candidate <= series[strength - i]) return false;
			}
			return true;
		}

		private bool IsPivotLow(ISeries<double> series, int strength)
		{
			double candidate = series[strength];
			for (int i = 1; i <= strength; i++)
			{
				if (candidate >= series[strength + i]) return false;
				if (candidate >= series[strength - i]) return false;
			}
			return true;
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (ShowZoneFill && ZoneFillBrush != null && chartControl != null && chartScale != null && ChartPanel != null)
			{
				float y80    = chartScale.GetYByValue(80);
				float y20    = chartScale.GetYByValue(20);
				float top    = Math.Min(y80, y20);
				float bottom = Math.Max(y80, y20);
				float height = Math.Max(1, bottom - top);
				float x      = ChartPanel.X;
				float width  = ChartPanel.W;

				Brush wpf = WithOpacity(ZoneFillBrush, ZoneOpacity);
				var sb = wpf as SolidColorBrush;
				if (sb != null)
				{
					Color mc = sb.Color;
					var dxColor = new SharpDX.Color4(mc.R / 255f, mc.G / 255f, mc.B / 255f, mc.A / 255f);
					using (var dxBrush = new SharpDX.Direct2D1.SolidColorBrush(RenderTarget, dxColor))
					{
						var rect = new SharpDX.RectangleF(x, top, width, height);
						RenderTarget.FillRectangle(rect, dxBrush);
					}
				}
			}
			base.OnRender(chartControl, chartScale);
		}

		private void ComputeStochD(int kLen, int smoothKLen, int dLen,
			Series<double> rawK, Series<double> smoothK, Series<double> dSeries,
			out double dValue)
		{
			double hh    = MAX(High, kLen)[0];
			double ll    = MIN(Low,  kLen)[0];
			double denom = hh - ll;
			double k     = (Math.Abs(denom) < 1e-10) ? 50.0 : 100.0 * (Close[0] - ll) / denom;
			rawK[0]    = k;
			smoothK[0] = SMA(rawK,    Math.Max(1, smoothKLen))[0];
			dSeries[0] = SMA(smoothK, Math.Max(1, dLen))[0];
			dValue     = dSeries[0];
		}

		private Brush WithOpacity(Brush b, int opacityPct0to100)
		{
			if (b == null) return null;
			var sb = b as SolidColorBrush;
			if (sb == null) return b;
			int  pct = Math.Max(0, Math.Min(100, opacityPct0to100));
			byte a   = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(255.0 * (pct / 100.0))));
			Color c  = sb.Color;
			var nb   = new SolidColorBrush(Color.FromArgb(a, c.R, c.G, c.B));
			nb.Freeze();
			return nb;
		}
	}

	internal static class BrushSerialization
	{
		public static string ToString(Brush b)
		{
			if (b == null) return null;
			var bc = new BrushConverter();
			return bc.ConvertToString(b);
		}

		public static Brush FromString(string s)
		{
			if (string.IsNullOrEmpty(s)) return null;
			var bc = new BrushConverter();
			return (Brush)bc.ConvertFromString(s);
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private QR[] cacheQR;
		public QR QR(int minCount, bool enableAlerts, int k1, int d1, int k2, int d2, int k3, int d3, int k4, int d4, int smoothK4, int abcdBars90, int abcdBars10, bool showDivLines1, bool showDivLines2, bool showDivLines3, bool showDivLines4, int divPivotStrength, int divMinBars, int divMaxBars, bool showZoneFill, int zoneOpacity, int quadRedOpacity, int quadGreenOpacity, bool showStoch2Line, bool showStoch3Line, bool showZingers, bool showWarningMarkers)
		{
			return QR(Input, minCount, enableAlerts, k1, d1, k2, d2, k3, d3, k4, d4, smoothK4, abcdBars90, abcdBars10, showDivLines1, showDivLines2, showDivLines3, showDivLines4, divPivotStrength, divMinBars, divMaxBars, showZoneFill, zoneOpacity, quadRedOpacity, quadGreenOpacity, showStoch2Line, showStoch3Line, showZingers, showWarningMarkers);
		}

		public QR QR(ISeries<double> input, int minCount, bool enableAlerts, int k1, int d1, int k2, int d2, int k3, int d3, int k4, int d4, int smoothK4, int abcdBars90, int abcdBars10, bool showDivLines1, bool showDivLines2, bool showDivLines3, bool showDivLines4, int divPivotStrength, int divMinBars, int divMaxBars, bool showZoneFill, int zoneOpacity, int quadRedOpacity, int quadGreenOpacity, bool showStoch2Line, bool showStoch3Line, bool showZingers, bool showWarningMarkers)
		{
			if (cacheQR != null)
				for (int idx = 0; idx < cacheQR.Length; idx++)
					if (cacheQR[idx] != null && cacheQR[idx].MinCount == minCount && cacheQR[idx].EnableAlerts == enableAlerts && cacheQR[idx].K1 == k1 && cacheQR[idx].D1 == d1 && cacheQR[idx].K2 == k2 && cacheQR[idx].D2 == d2 && cacheQR[idx].K3 == k3 && cacheQR[idx].D3 == d3 && cacheQR[idx].K4 == k4 && cacheQR[idx].D4 == d4 && cacheQR[idx].SmoothK4 == smoothK4 && cacheQR[idx].AbcdBars90 == abcdBars90 && cacheQR[idx].AbcdBars10 == abcdBars10 && cacheQR[idx].ShowDivLines1 == showDivLines1 && cacheQR[idx].ShowDivLines2 == showDivLines2 && cacheQR[idx].ShowDivLines3 == showDivLines3 && cacheQR[idx].ShowDivLines4 == showDivLines4 && cacheQR[idx].DivPivotStrength == divPivotStrength && cacheQR[idx].DivMinBars == divMinBars && cacheQR[idx].DivMaxBars == divMaxBars && cacheQR[idx].ShowZoneFill == showZoneFill && cacheQR[idx].ZoneOpacity == zoneOpacity && cacheQR[idx].QuadRedOpacity == quadRedOpacity && cacheQR[idx].QuadGreenOpacity == quadGreenOpacity && cacheQR[idx].ShowStoch2Line == showStoch2Line && cacheQR[idx].ShowStoch3Line == showStoch3Line && cacheQR[idx].ShowZingers == showZingers && cacheQR[idx].ShowWarningMarkers == showWarningMarkers && cacheQR[idx].EqualsInput(input))
						return cacheQR[idx];
			return CacheIndicator<QR>(new QR(){ MinCount = minCount, EnableAlerts = enableAlerts, K1 = k1, D1 = d1, K2 = k2, D2 = d2, K3 = k3, D3 = d3, K4 = k4, D4 = d4, SmoothK4 = smoothK4, AbcdBars90 = abcdBars90, AbcdBars10 = abcdBars10, ShowDivLines1 = showDivLines1, ShowDivLines2 = showDivLines2, ShowDivLines3 = showDivLines3, ShowDivLines4 = showDivLines4, DivPivotStrength = divPivotStrength, DivMinBars = divMinBars, DivMaxBars = divMaxBars, ShowZoneFill = showZoneFill, ZoneOpacity = zoneOpacity, QuadRedOpacity = quadRedOpacity, QuadGreenOpacity = quadGreenOpacity, ShowStoch2Line = showStoch2Line, ShowStoch3Line = showStoch3Line, ShowZingers = showZingers, ShowWarningMarkers = showWarningMarkers }, input, ref cacheQR);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.QR QR(int minCount, bool enableAlerts, int k1, int d1, int k2, int d2, int k3, int d3, int k4, int d4, int smoothK4, int abcdBars90, int abcdBars10, bool showDivLines1, bool showDivLines2, bool showDivLines3, bool showDivLines4, int divPivotStrength, int divMinBars, int divMaxBars, bool showZoneFill, int zoneOpacity, int quadRedOpacity, int quadGreenOpacity, bool showStoch2Line, bool showStoch3Line, bool showZingers, bool showWarningMarkers)
		{
			return indicator.QR(Input, minCount, enableAlerts, k1, d1, k2, d2, k3, d3, k4, d4, smoothK4, abcdBars90, abcdBars10, showDivLines1, showDivLines2, showDivLines3, showDivLines4, divPivotStrength, divMinBars, divMaxBars, showZoneFill, zoneOpacity, quadRedOpacity, quadGreenOpacity, showStoch2Line, showStoch3Line, showZingers, showWarningMarkers);
		}

		public Indicators.QR QR(ISeries<double> input, int minCount, bool enableAlerts, int k1, int d1, int k2, int d2, int k3, int d3, int k4, int d4, int smoothK4, int abcdBars90, int abcdBars10, bool showDivLines1, bool showDivLines2, bool showDivLines3, bool showDivLines4, int divPivotStrength, int divMinBars, int divMaxBars, bool showZoneFill, int zoneOpacity, int quadRedOpacity, int quadGreenOpacity, bool showStoch2Line, bool showStoch3Line, bool showZingers, bool showWarningMarkers)
		{
			return indicator.QR(input, minCount, enableAlerts, k1, d1, k2, d2, k3, d3, k4, d4, smoothK4, abcdBars90, abcdBars10, showDivLines1, showDivLines2, showDivLines3, showDivLines4, divPivotStrength, divMinBars, divMaxBars, showZoneFill, zoneOpacity, quadRedOpacity, quadGreenOpacity, showStoch2Line, showStoch3Line, showZingers, showWarningMarkers);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.QR QR(int minCount, bool enableAlerts, int k1, int d1, int k2, int d2, int k3, int d3, int k4, int d4, int smoothK4, int abcdBars90, int abcdBars10, bool showDivLines1, bool showDivLines2, bool showDivLines3, bool showDivLines4, int divPivotStrength, int divMinBars, int divMaxBars, bool showZoneFill, int zoneOpacity, int quadRedOpacity, int quadGreenOpacity, bool showStoch2Line, bool showStoch3Line, bool showZingers, bool showWarningMarkers)
		{
			return indicator.QR(Input, minCount, enableAlerts, k1, d1, k2, d2, k3, d3, k4, d4, smoothK4, abcdBars90, abcdBars10, showDivLines1, showDivLines2, showDivLines3, showDivLines4, divPivotStrength, divMinBars, divMaxBars, showZoneFill, zoneOpacity, quadRedOpacity, quadGreenOpacity, showStoch2Line, showStoch3Line, showZingers, showWarningMarkers);
		}

		public Indicators.QR QR(ISeries<double> input, int minCount, bool enableAlerts, int k1, int d1, int k2, int d2, int k3, int d3, int k4, int d4, int smoothK4, int abcdBars90, int abcdBars10, bool showDivLines1, bool showDivLines2, bool showDivLines3, bool showDivLines4, int divPivotStrength, int divMinBars, int divMaxBars, bool showZoneFill, int zoneOpacity, int quadRedOpacity, int quadGreenOpacity, bool showStoch2Line, bool showStoch3Line, bool showZingers, bool showWarningMarkers)
		{
			return indicator.QR(input, minCount, enableAlerts, k1, d1, k2, d2, k3, d3, k4, d4, smoothK4, abcdBars90, abcdBars10, showDivLines1, showDivLines2, showDivLines3, showDivLines4, divPivotStrength, divMinBars, divMaxBars, showZoneFill, zoneOpacity, quadRedOpacity, quadGreenOpacity, showStoch2Line, showStoch3Line, showZingers, showWarningMarkers);
		}
	}
}

#endregion