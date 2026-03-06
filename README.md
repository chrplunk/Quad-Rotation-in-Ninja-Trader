# Quad Rotation in NinjaTrader (QR)

A NinjaTrader 8 indicator that implements a **Quad Rotation** concept using **4 stochastic %D series**, with visual cues and alerts inspired by a TradingView workflow.

This repo contains the `QR.cs` NinjaScript indicator.

---

## What it does

### Core idea

* Computes **4 stochastic %D** lines (independent parameter sets).
* Detects “rotation” conditions based on slope + zones (above/below thresholds).
* Highlights the background when enough stochastics agree (**MinCount**, 1–4).

### Visuals

* **Optional filled 20–80 zone** (TradingView-style band).
* Background highlight for “quad red” / “quad green” conditions.
* Optional visibility toggles to hide Stoch 2 and Stoch 3 lines (while logic still runs).
* Editable line and background colors (via indicator properties in NT).

### Alerts

* Background trigger alerts (quad red / quad green)
* “ABCD shield” signals
* “Super” signals (all 4 aligned)
* Continuation signals (“look for long/short soon” style events)

---

## Installation (NinjaTrader 8)

1. Download `QR.cs` from this repo.
2. In NinjaTrader:

   * **New → NinjaScript Editor**
   * Right-click **Indicators** → **Add New Item** (or open an existing `QR.cs`)
   * Paste the full contents of `QR.cs`
3. Press **Compile**.

Then apply it to a chart:

* **Chart → Indicators → QR**

---

## Recommended starting settings

Defaults are set in code, but typical usage:

* **MinCount = 4** (strict quad)
* Stoch sets (defaults):

  * Stoch1: K=9,  D=3
  * Stoch2: K=14, D=3
  * Stoch3: K=40, D=4
  * Stoch4: K=60, D=10 (SmoothK4=1)
* Enable 20–80 zone fill: **On**
* Adjust quad background opacity to taste (lighter red/green)

---

## Notes / Design choices

* Some display toggles (like hiding Stoch 2/3 lines) use `double.NaN` so plots disappear visually.
* Logic is designed to remain consistent even when those plots are hidden.

---

## Disclaimer

This indicator is for research and informational purposes only. It does not constitute trading advice. Futures and leveraged products carry significant risk.

---

