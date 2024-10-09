/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 IDIA, INAF-OACT
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms 
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with 
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party 
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 */
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