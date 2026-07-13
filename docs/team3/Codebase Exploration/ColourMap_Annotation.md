# Colour Map System — Annotation
**Files covered:**
- `Assets/Scripts/Tools/ColorMapEnum.cs`
- `Assets/Scripts/UI/Colorbar.cs`
- Colour map usage in `Assets/Shaders/Volumes/BasicVolume.cginc` (lines 456–491)

**Sub-team 3 — Rendering Engine — Sprint 1**

---

## What Is a Colour Map?

A colour map is a **lookup table that converts a single number into a colour**. After the ray marcher calculates a brightness value for a pixel (a float between 0 and 1), it uses the colour map to decide what colour that pixel should be.

```
brightness 0.0  →  black  (no signal)
brightness 0.5  →  orange (medium signal)
brightness 1.0  →  yellow (peak signal)
```

Different colour maps suit different scientific purposes. `Inferno` and `Viridis` are perceptually uniform (equal brightness steps = equal visual steps) and are preferred for scientific publication. `Jet` is common but misleading because it creates false visual boundaries. Astronomers switch maps depending on what feature they are trying to highlight in the data.

---

## The 80 Colour Maps — ColorMapEnum.cs

**Location:** `Assets/Scripts/Tools/ColorMapEnum.cs`

```csharp
public enum ColorMapEnum
{
    Accent, Afmhot, Autumn, Binary, Blues, Bone, BrBg, Brg, BuGn, BuPu, Bwr,
    CmRmap, Cool, Coolwarm, Copper, Cubehelix, Dark2, Flag, GistEarth, GistGray,
    GistHeat, GistNcar, GistRainbow, GistStern, GistYarg, GnBu, Gnuplot, Gnuplot2,
    Gray, Greens, Greys, Hot, Hsv, Inferno, Jet, Magma, NipySpectral, Ocean,
    Oranges, OrRd, Paired, Pastel1, Pastel2, Pink, PiYg, Plasma, PrGn, Prism,
    PuBu, PuBuGn, PuOr, PuRd, Purples, Rainbow, RdBu, RdGy, RdPu, RdYlBu,
    RdYlGn, Reds, Seismic, Set1, Set2, Set3, Spectral, Spring, Summer, Tab10,
    Tab20, Tab20B, Tab20C, Terrain, Viridis, Winter, Wistia, YlGn, YlGnBu,
    YlOrBr, YlOrRd, Turbo, None
}
```

80 named maps plus a `None` sentinel value at the end. The `None` entry is excluded from the count via:

```csharp
public static int NumColorMaps
{
    get { return Enum.GetNames(typeof(ColorMapEnum)).Length - 1; }  // 80
}
```

The integer value of each enum entry (its `GetHashCode()`) is its **index into the texture atlas** — `Inferno` is index 33, `Viridis` is index 69, etc. This is the same integer pushed to the `_ColorMapIndex` shader uniform.

`ColorMapUtils.FromHashCode(int)` provides a reverse lookup — given an integer, return the matching enum value. It falls back to `Accent` (index 0) if the integer is unrecognised.

---

## How the Texture Atlas Works

All 80 colour maps are packed into a single 2D texture (`_ColorMap`) as **horizontal strips stacked vertically**:

```
Y = 79/80  ──────────────────────────  ← Accent    (index 0)
Y = 78/80  ──────────────────────────  ← Afmhot    (index 1)
           ...
Y = 47/80  ──────────────────────────  ← Inferno   (index 33)
           ...
Y = 11/80  ──────────────────────────  ← Viridis   (index 69)
Y =  0/80  ──────────────────────────  ← Turbo     (index 78)

           dark ───────────────► bright
           x=0.0               x=1.0
```

The shader calculates which vertical strip to sample:

```hlsl
float colorMapOffset = 1.0 - (0.5 + _ColorMapIndex) / _NumColorMaps;
```

This converts the integer index into a normalised Y coordinate in the atlas, with `0.5` added to sample the centre of each strip rather than its edge (avoiding bleed between adjacent maps).

---

## How the Shader Applies the Colour Map

**Location:** `BasicVolume.cginc`, lines ~456–491

The full pipeline after the ray march loop completes:

### Step 1 — Normalise the raw voxel value
```hlsl
rayValue = (rayValue - _ScaleMin) / (_ScaleMax - _ScaleMin);
rayValue = clamp(rayValue, _ThresholdMin, _ThresholdMax);
```
Maps the raw FITS float (e.g. `-0.003` to `1.24` Jy/beam) into `[0, 1]` and clips to the visible threshold range.

### Step 2 — Remap within the threshold window
```hlsl
float x = (rayValue - _ThresholdMin) / (_ThresholdMax - _ThresholdMin);
```
Stretches the visible range to fill the full `[0, 1]` colour map width.

### Step 3 — Apply non-linear scaling curve
```hlsl
if      (ScaleType == SQUARE)  x = x * x;
else if (ScaleType == SQRT)    x = sqrt(x);
else if (ScaleType == LOG)     x = clamp(log(ScaleAlpha * x + 1.0) / log(ScaleAlpha), 0.0, 1.0);
else if (ScaleType == POWER)   x = (pow(ScaleAlpha, x) - 1.0) / ScaleAlpha;
else if (ScaleType == GAMMA)   x = pow(x, ScaleGamma);
// else: Linear (no change)
```
Astronomers use Log scaling most often — it reveals faint extended emission that would be invisible on a linear scale.

### Step 4 — Apply bias and contrast
```hlsl
x = clamp(x - ScaleBias, 0.0, 1.0);
x = clamp((x - 0.5) * ScaleContrast + 0.5, 0.0, 1.0);
```
Fine-grained manual adjustments on top of the scaling curve.

### Step 5 — Sample the colour map atlas
```hlsl
float4 colorMapColor = float4(tex2D(_ColorMap, float2(x, colorMapOffset)).xyz, x);
```
`x` = horizontal position in the strip (brightness → colour)  
`colorMapOffset` = vertical position in the atlas (which map to use)

### Step 6 — Highlight blend
```hlsl
float colorFraction = maxInHighlightBounds ? 1.0f : HighlightSaturateFactor;
float4 greyscaleColor = x;
float4 color = lerp(greyscaleColor, colorMapColor, colorFraction);
```
When a feature is highlighted, `HighlightSaturateFactor = 1.0` (full colour). Outside the highlight region it blends toward greyscale, making the highlighted source stand out visually.

---

## The Colorbar UI — Colorbar.cs

**Location:** `Assets/Scripts/UI/Colorbar.cs`

The `Colorbar` MonoBehaviour renders the colour scale indicator in the desktop GUI panel. It is a **pure UI concern** — it does not interact with the shader directly.

It loads colour map sprites from `Resources/allmaps_sprites` (pre-rendered PNG strips, one per map) and displays the currently active one as a `UnityEngine.UI.Image`. Tick labels are generated dynamically from `ScaleMin`, `ScaleMax`, and `NumTicks`.

```csharp
private void ApplyColormap()
{
    int index = ColorMap.GetHashCode();
    if (index >= 0 && index < _colormapSprites?.Length)
    {
        _colorbarImage.sprite = _colormapSprites[index];
    }
}
```

The same integer index used in the shader is used to pick the sprite — they stay in sync because both derive from `ColorMapEnum.GetHashCode()`.

---

## How Colour Map Switching Works End-to-End

```
User presses controller button
        ↓
VolumeCommandController.ShiftColorMap(delta)
        ↓
VolumeDataSetRenderer.ShiftColorMap(delta)
  → increments ColorMap enum by delta
  → calls OnColorMapChanged delegate
        ↓
VolumeMaterialBinder.SyncShaderState()   ← TARGET (currently inline in Update())
  → _materialInstance.SetFloat(MaterialID.ColorMapIndex, ColorMap.GetHashCode())
        ↓
BasicVolume.cginc reads _ColorMapIndex
  → samples correct horizontal strip from _ColorMap atlas
        ↓
Colorbar.cs reads ColorMap enum
  → swaps sprite to matching PNG
```

No material rebuild. No texture swap. One integer uniform update — the atlas approach is specifically designed for this zero-cost switching.

---

## Issues and Refactoring Notes

| Issue | Detail | Target Fix |
|---|---|---|
| `GetHashCode()` used as index | Fragile — enum insertion order determines the shader index. Reordering the enum breaks the colour map atlas alignment. | Use explicit `[int]` cast with a stable integer mapping |
| `ShiftColorMap()` inside `VolumeDataSetRenderer` | Colour map selection is a material concern, not a renderer concern | Move to `VolumeMaterialBinder.SetColorMap()` |
| `Colorbar.cs` calls `GetHashCode()` independently | Two places deriving the same index via the same fragile mechanism | Both should use a single `ColorMapUtils.GetIndex(ColorMapEnum)` helper |
| 80 colour maps hard-coded as enum values | Adding a new map requires editing the enum, the atlas texture, and the sprite sheet | Future: load map names from a config file; enum is a maintenance burden at 80 entries |
| `tex2D` sampling | Built-In RP texture sampling macro | Replace with `SAMPLE_TEXTURE2D` macro for URP compatibility |

---

## CK Metrics Relevance

The colour map system is currently spread across:
- `ColorMapEnum.cs` — data only
- `VolumeDataSetRenderer.ShiftColorMap()` — behaviour in wrong class
- `Colorbar.cs` — UI coupling to raw `GetHashCode()`
- `BasicVolume.cginc` — shader implementation

Post-refactor, `VolumeMaterialBinder` owns the colour map selection behaviour. `ColorMapUtils` is the single source of truth for index calculation. `Colorbar.cs` is a pure UI consumer with no knowledge of the shader.

---

*Annotation by Sub-team 3 — Rendering Engine — Team Alpha*
