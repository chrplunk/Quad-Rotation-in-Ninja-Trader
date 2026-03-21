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

		[NinjaScriptProperty]
		[Display(Name = "Show Regular Divergence Lines", Order = 1, GroupName = "Divergence")]
		public bool ShowDivergenceLines { get; set; }

		[NinjaScriptProperty]
		[Range(1, 10)]
		[Display(Name = "Pivot Strength", Order = 2, GroupName = "Divergence")]
		public int DivPivotStrength { get; set; }

		[NinjaScriptProperty]
		[Range(1, 100)]
		[Display(Name = "Min Bars Between Pivots", Order = 3, GroupName = "Divergence")]
		public int DivMinBars { get; set; }

		[NinjaScriptProperty]
		[Range(1, 200)]
		[Display(Name = "Max Bars Between Pivots", Order = 4, GroupName = "Divergence")]
		public int DivMaxBars { get; set; }

		[XmlIgnore]
		[Display(Name = "Bearish Divergence Color", Order = 5, GroupName = "Divergence")]
		public Brush BearDivBrush { get; set; }

		[Browsable(false)]
		public string BearDivBrushSerialize
		{
			get { return BrushSerialization.ToString(BearDivBrush); }
			set { BearDivBrush = BrushSerialization.FromString(value); }
		}

		[XmlIgnore]
		[Display(Name = "Bullish Divergence Color", Order = 6, GroupName = "Divergence")]
		public Brush BullDivBrush { get; set; }

		[Browsable(false)]
		public string BullDivBrushSerialize
		{
			get { return BrushSerialization.ToString(BullDivBrush); }
			set { BullDivBrush = BrushSerialization.FromString(value); }
		}

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
		[Display(Name = "Show Zingers (vertical lines)", Order = 1, GroupName = "Zingers")]
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

		private Series<double> rawK1, smoothK1, dSeries1;
		private Series<double> rawK2, smoothK2, dSeries2;
		private Series<double> rawK3, smoothK3, dSeries3;
		private Series<double> rawK4, smoothK4Series, dSeries4;

		private int barsSinceStoch4Le90;
		private int barsSinceStoch4Ge10;

		private bool prevBgRed, prevBgGreen, prevSuperDown, prevSuperUp, prevBearCont, prevBullCont, prevShieldAbove90, prevShieldBelow10;

		private int prevHighPivotBar = -1;
		private double prevHighPivotPrice = 0.0;
		private double prevHighPivotStoch = 0.0;

		private int prevLowPivotBar = -1;
		private double prevLowPivotPrice = 0.0;
		private double prevLowPivotStoch = 0.0;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "QR";
				Description = "Quad Rotation - 4 Stochastics Overlay (blue zone + regular Stoch1 divergence + correctly oriented shield markers + zingers).";
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

				ShowDivergenceLines = true;
				DivPivotStrength = 2;
				DivMinBars = 4;
				DivMaxBars = 30;
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

			bool bgRed = downCount >= MinCount;
			bool bgGreen = upCount >= MinCount;

			BackBrushes[0] = null;
			if (bgRed)
				BackBrushes[0] = WithOpacity(QuadRedBrush, QuadRedOpacity);
			else if (bgGreen)
				BackBrushes[0] = WithOpacity(QuadGreenBrush, QuadGreenOpacity);

			if (s4 <= 90) barsSinceStoch4Le90 = 0;
			else barsSinceStoch4Le90++;

			if (s4 >= 10) barsSinceStoch4Ge10 = 0;
			else barsSinceStoch4Ge10++;

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

			prevBgRed = bgRed;
			prevBgGreen = bgGreen;
			prevShieldAbove90 = shieldAbove90;
			prevShieldBelow10 = shieldBelow10;

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

			bool bearCont = s4Below30 && CrossBelow(dSeries1, 90, 1) && bearShieldCount >= 3;
			bool bullCont = s4Above70 && CrossAbove(dSeries1, 10, 1) && bullShieldCount >= 3;

			if (ShowZingers)
			{
				if (bearCont)
					Draw.VerticalLine(this, "Z_BEAR_" + CurrentBar, 0, ZingerBearBrush);
				if (bullCont)
					Draw.VerticalLine(this, "Z_BULL_" + CurrentBar, 0, ZingerBullBrush);
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

			if (ShowDivergenceLines && CurrentBar >= DivPivotStrength * 2 + 1)
				ProcessRegularDivergence();
		}

		private void ProcessRegularDivergence()
		{
			int s = DivPivotStrength;
			int pivotBarsAgo = s;
			int pivotAbsBar = CurrentBar - s;

			bool stochPivotHigh = IsPivotHigh(dSeries1, s);
			bool stochPivotLow  = IsPivotLow(dSeries1, s);
			bool pricePivotHigh = IsPivotHigh(High, s);
			bool pricePivotLow  = IsPivotLow(Low, s);

			if (stochPivotHigh && pricePivotHigh)
			{
				double currPriceHigh = High[pivotBarsAgo];
				double currStochHigh = dSeries1[pivotBarsAgo];

				if (prevHighPivotBar >= 0)
				{
					int barsBetween = pivotAbsBar - prevHighPivotBar;
					if (barsBetween >= DivMinBars && barsBetween <= DivMaxBars)
					{
						bool bearishDiv = currPriceHigh > prevHighPivotPrice && currStochHigh < prevHighPivotStoch;
						if (bearishDiv)
						{
							int prevBarsAgo = CurrentBar - prevHighPivotBar;
							int currBarsAgo = pivotBarsAgo;
							DateTime startTime = Times[0][prevBarsAgo];
							DateTime endTime   = Times[0][currBarsAgo];
							Draw.Line(this, "BEAR_DIV_" + pivotAbsBar, false, startTime, prevHighPivotStoch, endTime, currStochHigh, BearDivBrush, DashStyleHelper.Solid, 1);
						}
					}
				}
				prevHighPivotBar   = pivotAbsBar;
				prevHighPivotPrice = currPriceHigh;
				prevHighPivotStoch = currStochHigh;
			}

			if (stochPivotLow && pricePivotLow)
			{
				double currPriceLow = Low[pivotBarsAgo];
				double currStochLow = dSeries1[pivotBarsAgo];

				if (prevLowPivotBar >= 0)
				{
					int barsBetween = pivotAbsBar - prevLowPivotBar;
					if (barsBetween >= DivMinBars && barsBetween <= DivMaxBars)
					{
						bool bullishDiv = currPriceLow < prevLowPivotPrice && currStochLow > prevLowPivotStoch;
						if (bullishDiv)
						{
							int prevBarsAgo = CurrentBar - prevLowPivotBar;
							int currBarsAgo = pivotBarsAgo;
							DateTime startTime = Times[0][prevBarsAgo];
							DateTime endTime   = Times[0][currBarsAgo];
							Draw.Line(this, "BULL_DIV_" + pivotAbsBar, false, startTime, prevLowPivotStoch, endTime, currStochLow, BullDivBrush, DashStyleHelper.Solid, 1);
						}
					}
				}
				prevLowPivotBar   = pivotAbsBar;
				prevLowPivotPrice = currPriceLow;
				prevLowPivotStoch = currStochLow;
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
				float y80 = chartScale.GetYByValue(80);
				float y20 = chartScale.GetYByValue(20);
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
			int sk     = Math.Max(1, smoothKLen);
			int dl     = Math.Max(1, dLen);
			smoothK[0] = SMA(rawK,    sk)[0];
			dSeries[0] = SMA(smoothK, dl)[0];
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
