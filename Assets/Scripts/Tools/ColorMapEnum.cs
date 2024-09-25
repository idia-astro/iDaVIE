using System;

// @formatter:off
public enum ColorMapEnum
{
    Accent, Afmhot, Autumn, Binary, Blues, Bone, BrBg, Brg, BuGn, BuPu, Bwr, CmRmap, Cool, Coolwarm,
    Copper, Cubehelix, Dark2, Flag, GistEarth, GistGray, GistHeat, GistNcar, GistRainbow, GistStern, GistYarg,
    GnBu, Gnuplot, Gnuplot2, Gray, Greens, Greys, Hot, Hsv, Inferno, Jet, Magma, NipySpectral, Ocean, Oranges,
    OrRd, Paired, Pastel1, Pastel2, Pink, PiYg, Plasma, PrGn, Prism, PuBu, PuBuGn, PuOr, PuRd, Purples, Rainbow,
    RdBu, RdGy, RdPu, RdYlBu, RdYlGn, Reds, Seismic, Set1, Set2, Set3, Spectral, Spring, Summer, Tab10, Tab20,
    Tab20B, Tab20C, Terrain, Viridis, Winter, Wistia, YlGn, YlGnBu, YlOrBr, YlOrRd, Turbo, None
}
// @formatter:on

public static class ColorMapUtils
{
    public static ColorMapEnum FromHashCode(int hashCode)
    {
        foreach (var colorMap in Enum.GetValues(typeof(ColorMapEnum)))
        {
            if (colorMap.GetHashCode() == hashCode)
            {
                return (ColorMapEnum) colorMap;
            }
        }


        // If we can't find a color map, return the default one
        return ColorMapEnum.Accent;
    }

    public static int NumColorMaps
    {
        // Subtract 1 because the last enum value is "None"; 
        get { return Enum.GetNames(typeof(ColorMapEnum)).Length -1; }
    }
}