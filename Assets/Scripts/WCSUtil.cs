using System;

namespace MariusSoft.WCSTools
{

    /* WDP 1/97: change the name of the routine from 'worldpos' to 'ffwldp' */

    /*  worldpos.c -- WCS Algorithms from Classic AIPS.
        Copyright (C) 1994
        Associated Universities, Inc. Washington DC, USA.
   
        This library is free software; you can redistribute it and/or modify it
        under the terms of the GNU Library General Public License as published by
        the Free Software Foundation; either version 2 of the License, or (at your
        option) any later version.
   
        This library is distributed in the hope that it will be useful, but WITHOUT
        ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
        FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Library General Public
        License for more details.
   
        You should have received a copy of the GNU Library General Public License
        along with this library; if not, write to the Free Software Foundation,
        Inc., 675 Massachusetts Ave, Cambridge, MA 02139, USA.
   
        Correspondence concerning AIPS should be addressed as follows:
               Internet email: aipsmail@nrao.edu
               Postal address: AIPS Group
                               National Radio Astronomy Observatory
                               520 Edgemont Road
                               Charlottesville, VA 22903-2475 USA

                     -=-=-=-=-=-=-

        These two ANSI C functions, worldpos() and xypix(), perform
        forward and reverse WCS computations for 8 types of projective
        geometries ("-SIN", "-TAN", "-ARC", "-NCP", "-GLS", "-MER", "-AIT"
        and "-STG"):

            worldpos() converts from pixel location to RA,Dec 
            xypix()    converts from RA,Dec         to pixel location   

        where "(RA,Dec)" are more generically (long,lat). These functions
        are based on the WCS implementation of Classic AIPS, an
        implementation which has been in production use for more than ten
        years. See the two memos by Eric Greisen

            ftp://fits.cv.nrao.edu/fits/documents/wcs/aips27.ps.Z
        ftp://fits.cv.nrao.edu/fits/documents/wcs/aips46.ps.Z

        for descriptions of the 8 projective geometries and the
        algorithms.  Footnotes in these two documents describe the
        differences between these algorithms and the 1993-94 WCS draft
        proposal (see URL below). In particular, these algorithms support
        ordinary field rotation, but not skew geometries (CD or PC matrix
        cases). Also, the MER and AIT algorithms work correctly only for
        CRVALi=(0,0). Users should note that GLS projections with yref!=0
        will behave differently in this code than in the draft WCS
        proposal.  The NCP projection is now obsolete (it is a special
        case of SIN).  WCS syntax and semantics for various advanced
        features is discussed in the draft WCS proposal by Greisen and
        Calabretta at:
    
            ftp://fits.cv.nrao.edu/fits/documents/wcs/wcs.all.ps.Z
    
                    -=-=-=-

        The original version of this code was Emailed to D.Wells on
        Friday, 23 September by Bill Cotton <bcotton@gorilla.cv.nrao.edu>,
        who described it as a "..more or less.. exact translation from the
        AIPSish..". Changes were made by Don Wells <dwells@nrao.edu>
        during the period October 11-13, 1994:
        1) added GNU license and header comments
        2) added testpos.c program to perform extensive circularity tests
        3) changed float-->double to get more than 7 significant figures
        4) testpos.c circularity test failed on MER and AIT. B.Cotton
           found that "..there were a couple of lines of code [in] the wrong
           place as a result of merging several Fortran routines." 
        5) testpos.c found 0h wraparound in xypix() and worldpos().
        6) E.Greisen recommended removal of various redundant if-statements,
           and addition of a 360d difference test to MER case of worldpos(). 
     */

    /// <summary>
    /// World Coordinate System Utilities
    /// </summary>
    public class WCSUtil
    {
        /// <summary>
        /// routine to determine accurate position for pixel coordinates          
        /// returns 0 if successful otherwise:                                    
        /// 1 = angle too large for projection;                                   
        /// (WDP 1/97: changed the return value to 501 instead of 1)              
        /// does: -SIN, -TAN, -ARC, -NCP, -GLS, -MER, -AIT projections            
        /// anything else is linear (== -CAR)                                     
        /// </summary>
        /// <param name="xpix">x pixel number  (RA or long without rotation)</param>
        /// <param name="ypix">y pixel number  (dec or lat without rotation)</param>
        /// <param name="xref">x reference coordinate value (deg)</param>
        /// <param name="yref">y reference coordinate value (deg)</param>
        /// <param name="xrefpix">x reference pixel</param>
        /// <param name="yrefpix">y reference pixel</param>
        /// <param name="xinc">x coordinate increment (deg)</param>
        /// <param name="yinc">y coordinate increment (deg)</param>
        /// <param name="rot">rotation (deg)  (from N through E)</param>
        /// <param name="type">projection type code e.g. "-SIN";</param>
        /// <param name="xpos">x (RA) coordinate (deg)</param>
        /// <param name="ypos">y (dec) coordinate (deg)  </param>
        /// <returns></returns>
        public static int ffwldp(double xpix, double ypix, double xref, double yref,
            double xrefpix, double yrefpix, double xinc, double yinc, double rot,
            string type, out double xpos, out double ypos)
        {
            double cosr, sinr, dx, dy, dz, temp, x, y, z;
            double sins, coss, dect, rat, dt, l, m, mg, da, dd, cos0, sin0;
            double dec0, ra0, decout, raout;
            double geo1, geo2, geo3;
            double cond2r = 1.745329252e-2;
            double twopi = 6.28318530717959, deps = 1.0e-5;
            int i, itype;
            string[] types = { "-CAR", "-SIN", "-TAN", "-ARC", "-NCP", "-GLS", "-MER", "-AIT", "-STG" };


            /*   Offset from ref pixel  */
            dx = (xpix - xrefpix) * xinc;
            dy = (ypix - yrefpix) * yinc;
            /*   Take out rotation  */
            cosr = Math.Cos(rot * cond2r);
            sinr = Math.Sin(rot * cond2r);
            if (rot != 0.0)
            {
                temp = dx * cosr - dy * sinr;
                dy = dy * cosr + dx * sinr;
                dx = temp;
            }
            /*  find type  */
            /* WDP 1/97: removed support for default type for better error checking */
            /*  itype = 0;   default type is linear */
            itype = -1; /* no default type */
            for (i = 0; i < 9; i++)
            {
                if (type == types[i])
                {
                    itype = i;
                }
            }

            /* default, linear result for error return  */
            xpos = xref + dx;
            ypos = yref + dy;
            /* convert to radians  */
            ra0 = xref * cond2r;
            dec0 = yref * cond2r;
            l = dx * cond2r;
            m = dy * cond2r;
            sins = l * l + m * m;
            cos0 = Math.Cos(dec0);
            sin0 = Math.Sin(dec0);

            /* process by case  */
            switch (itype)
            {
                case 0: /* linear -CAR */
                    rat = ra0 + l;
                    dect = dec0 + m;
                    break;
                case 1: /* -SIN sin*/
                    if (sins > 1.0) return 501;
                    coss = Math.Sqrt(1.0 - sins);
                    dt = sin0 * coss + cos0 * m;
                    if ((dt > 1.0) || (dt < -1.0)) return 501;
                    dect = Math.Asin(dt);
                    rat = cos0 * coss - sin0 * m;
                    if ((rat == 0.0) && (l == 0.0)) return 501;

                    rat = Math.Atan2(l, rat) + ra0;
                    break;
                case 2: /* -TAN tan */
                    x = cos0 * Math.Cos(ra0) - l * Math.Sin(ra0) - m * Math.Cos(ra0) * sin0;
                    y = cos0 * Math.Sin(ra0) + l * Math.Cos(ra0) - m * Math.Sin(ra0) * sin0;
                    z = sin0 + m * cos0;
                    rat = Math.Atan2(y, x);
                    dect = Math.Atan(z / Math.Sqrt(x * x + y * y));
                    break;
                case 3: /* -ARC Arc*/
                    if (sins >= twopi * twopi / 4.0) return 501;
                    sins = Math.Sqrt(sins);
                    coss = Math.Cos(sins);
                    if (sins != 0.0) sins = Math.Sin(sins) / sins;
                    else
                        sins = 1.0;
                    dt = m * cos0 * sins + sin0 * coss;
                    if ((dt > 1.0) || (dt < -1.0)) return 501;
                    dect = Math.Asin(dt);
                    da = coss - dt * sin0;
                    dt = l * sins * cos0;
                    if ((da == 0.0) && (dt == 0.0)) return 501;
                    rat = ra0 + Math.Atan2(dt, da);
                    break;
                case 4: /* -NCP North celestial pole*/
                    dect = cos0 - m * sin0;
                    if (dect == 0.0) return 501;
                    rat = ra0 + Math.Atan2(l, dect);
                    dt = Math.Cos(rat - ra0);
                    if (dt == 0.0) return 501;
                    dect = dect / dt;
                    if ((dect > 1.0) || (dect < -1.0)) return 501;
                    dect = Math.Acos(dect);
                    if (dec0 < 0.0) dect = -dect;
                    break;
                case 5: /* -GLS global sinusoid */
                    dect = dec0 + m;
                    if (Math.Abs(dect) > twopi / 4.0) return 501;
                    coss = Math.Cos(dect);
                    if (Math.Abs(l) > twopi * coss / 2.0) return 501;
                    rat = ra0;
                    if (coss > deps) rat = rat + l / coss;
                    break;
                case 6: /* -MER mercator*/
                    dt = yinc * cosr + xinc * sinr;
                    if (dt == 0.0) dt = 1.0;
                    dy = (yref / 2.0 + 45.0) * cond2r;
                    dx = dy + dt / 2.0 * cond2r;
                    dy = Math.Log(Math.Tan(dy));
                    dx = Math.Log(Math.Tan(dx));
                    geo2 = dt * cond2r / (dx - dy);
                    geo3 = geo2 * dy;
                    geo1 = Math.Cos(yref * cond2r);
                    if (geo1 <= 0.0) geo1 = 1.0;
                    rat = l / geo1 + ra0;
                    if (Math.Abs(rat - ra0) > twopi) return 501; /* added 10/13/94 DCW/EWG */
                    dt = 0.0;
                    if (geo2 != 0.0) dt = (m + geo3) / geo2;
                    dt = Math.Exp(dt);
                    dect = 2.0 * Math.Atan(dt) - twopi / 4.0;
                    break;
                case 7: /* -AIT Aitoff*/
                    dt = yinc * cosr + xinc * sinr;
                    if (dt == 0.0) dt = 1.0;
                    dt = dt * cond2r;
                    dy = yref * cond2r;
                    dx = Math.Sin(dy + dt) / Math.Sqrt((1.0 + Math.Cos(dy + dt)) / 2.0) -
                         Math.Sin(dy) / Math.Sqrt((1.0 + Math.Cos(dy)) / 2.0);
                    if (dx == 0.0) dx = 1.0;
                    geo2 = dt / dx;
                    dt = xinc * cosr - yinc * sinr;
                    if (dt == 0.0) dt = 1.0;
                    dt = dt * cond2r;
                    dx = 2.0 * Math.Cos(dy) * Math.Sin(dt / 2.0);
                    if (dx == 0.0) dx = 1.0;
                    geo1 = dt * Math.Sqrt((1.0 + Math.Cos(dy) * Math.Cos(dt / 2.0)) / 2.0) / dx;
                    geo3 = geo2 * Math.Sin(dy) / Math.Sqrt((1.0 + Math.Cos(dy)) / 2.0);
                    rat = ra0;
                    dect = dec0;
                    if ((l == 0.0) && (m == 0.0)) break;
                    dz = 4.0 - l * l / (4.0 * geo1 * geo1) - ((m + geo3) / geo2) * ((m + geo3) / geo2);
                    if ((dz > 4.0) || (dz < 2.0)) return 501;
                    dz = 0.5 * Math.Sqrt(dz);
                    dd = (m + geo3) * dz / geo2;
                    if (Math.Abs(dd) > 1.0) return 501;
                    dd = Math.Asin(dd);
                    if (Math.Abs(Math.Cos(dd)) < deps) return 501;
                    da = l * dz / (2.0 * geo1 * Math.Cos(dd));
                    if (Math.Abs(da) > 1.0) return 501;
                    da = Math.Asin(da);
                    rat = ra0 + 2.0 * da;
                    dect = dd;
                    break;
                case 8: /* -STG Sterographic*/
                    dz = (4.0 - sins) / (4.0 + sins);
                    if (Math.Abs(dz) > 1.0) return 501;
                    dect = dz * sin0 + m * cos0 * (1.0 + dz) / 2.0;
                    if (Math.Abs(dect) > 1.0) return 501;
                    dect = Math.Asin(dect);
                    rat = Math.Cos(dect);
                    if (Math.Abs(rat) < deps) return 501;
                    rat = l * (1.0 + dz) / (2.0 * rat);
                    if (Math.Abs(rat) > 1.0) return 501;
                    rat = Math.Asin(rat);
                    mg = 1.0 + Math.Sin(dect) * sin0 + Math.Cos(dect) * cos0 * Math.Cos(rat);
                    if (Math.Abs(mg) < deps) return 501;
                    mg = 2.0 * (Math.Sin(dect) * cos0 - Math.Cos(dect) * sin0 * Math.Cos(rat)) / mg;
                    if (Math.Abs(mg - m) > deps) rat = twopi / 2.0 - rat;
                    rat = ra0 + rat;
                    break;

                default:
                    /* fall through to here on error */
                    return 504;
            }

            /*  return ra in range  */
            raout = rat;
            decout = dect;
            if (raout - ra0 > twopi / 2.0) raout = raout - twopi;
            if (raout - ra0 < -twopi / 2.0) raout = raout + twopi;
            if (raout < 0.0) raout += twopi; /* added by DCW 10/12/94 */

            /*  correct units back to degrees  */
            xpos = raout / cond2r;
            ypos = decout / cond2r;
            return 0;
        }

        /// <summary>
        ///  routine to determine accurate pixel coordinates for an RA and Dec     */
        ///  returns 0 if successful otherwise:                                    */
        ///  1 = angle too large for projection;                                   */
        ///  2 = bad values                                                        */
        ///  WDP 1/97: changed the return values to 501 and 502 instead of 1 and 2 */
        ///  does: -SIN, -TAN, -ARC, -NCP, -GLS, -MER, -AIT projections            */
        ///  anything else is linear                                               */
        /// </summary>
        /// <param name="xpos">x (RA) coordinate (deg)</param>
        /// <param name="ypos">y (dec) coordinate (deg)</param>
        /// <param name="xref">x reference coordinate value (deg)</param>
        /// <param name="yref">y reference coordinate value (deg)</param>
        /// <param name="xrefpix">x reference pixel</param>
        /// <param name="yrefpix">y reference pixel</param>
        /// <param name="xinc">x coordinate increment (deg)</param>
        /// <param name="yinc">y coordinate increment (deg)</param>
        /// <param name="rot">rotation (deg)  (from N through E)</param>
        /// <param name="type">projection type code e.g. "-SIN";</param>
        /// <param name="xpix"> x pixel number  (RA or long without rotation)</param>
        /// <param name="ypix">y pixel number  (dec or lat without rotation)</param>
        /// <returns></returns>
        public static int ffxypx(double xpos, double ypos, double xref, double yref,
            double xrefpix, double yrefpix, double xinc, double yinc, double rot,
            string type, out double xpix, out double ypix)
        {

            double dx, dy, dz, r, ra0, dec0, ra, dec, coss, sins, dt, da, dd, sint;
            double l, m, geo1, geo2, geo3, sinr, cosr, cos0, sin0;
            double cond2r = 1.745329252e-2, deps = 1.0e-5, twopi = 6.28318530717959;
            int i, itype;
            string[] types = { "-CAR", "-SIN", "-TAN", "-ARC", "-NCP", "-GLS", "-MER", "-AIT", "-STG" };



            /* 0h wrap-around tests added by D.Wells 10/12/94: */
            dt = (xpos - xref);
            if (dt > 180) xpos -= 360;
            if (dt < -180) xpos += 360;
            /* NOTE: changing input argument xpos is OK (call-by-value in C!) */

            /* default values - linear */
            dx = xpos - xref;
            dy = ypos - yref;
            /*  dz = 0.0; */
            /*  Correct for rotation */
            r = rot * cond2r;
            cosr = Math.Cos(r);
            sinr = Math.Sin(r);
            dz = dx * cosr + dy * sinr;
            dy = dy * cosr - dx * sinr;
            dx = dz;
            /*     check axis increments - bail out if either 0 */
            if ((xinc == 0.0) || (yinc == 0.0)) { xpix = 0.0; ypix = 0.0; return 502; }
            /*     convert to pixels  */
            xpix = dx / xinc + xrefpix;
            ypix = dy / yinc + yrefpix;

            /*  find type  */
            /* WDP 1/97: removed support for default type for better error checking */
            /*  itype = 0;   default type is linear */
            itype = -1;  /* no default type */
            for (i = 0; i < 9; i++)
            {
                if (type == types[i])
                {
                    itype = i;
                }
            }

            /* Non linear position */
            ra0 = xref * cond2r;
            dec0 = yref * cond2r;
            ra = xpos * cond2r;
            dec = ypos * cond2r;

            /* compute direction cosine */
            coss = Math.Cos(dec);
            sins = Math.Sin(dec);
            cos0 = Math.Cos(dec0);
            sin0 = Math.Sin(dec0);
            l = Math.Sin(ra - ra0) * coss;
            sint = sins * sin0 + coss * cos0 * Math.Cos(ra - ra0);

            /* process by case  */
            switch (itype)
            {
                case 1:   /* -SIN sin*/
                    if (sint < 0.0) return 501;
                    m = sins * Math.Cos(dec0) - coss * Math.Sin(dec0) * Math.Cos(ra - ra0);
                    break;
                case 2:   /* -TAN tan */
                    if (sint <= 0.0) return 501;
                    if (cos0 < 0.001)
                    {
                        /* Do a first order expansion around pole */
                        m = (coss * Math.Cos(ra - ra0)) / (sins * sin0);
                        m = (-m + cos0 * (1.0 + m * m)) / sin0;
                    }
                    else
                    {
                        m = (sins / sint - sin0) / cos0;
                    }
                    if (Math.Abs(Math.Sin(ra0)) < 0.3)
                    {
                        l = coss * Math.Sin(ra) / sint - cos0 * Math.Sin(ra0) + m * Math.Sin(ra0) * sin0;
                        l /= Math.Cos(ra0);
                    }
                    else
                    {
                        l = coss * Math.Cos(ra) / sint - cos0 * Math.Cos(ra0) + m * Math.Cos(ra0) * sin0;
                        l /= -Math.Sin(ra0);
                    }
                    break;
                case 3:   /* -ARC Arc*/
                    m = sins * Math.Sin(dec0) + coss * Math.Cos(dec0) * Math.Cos(ra - ra0);
                    if (m < -1.0) m = -1.0;
                    if (m > 1.0) m = 1.0;
                    m = Math.Acos(m);
                    if (m != 0)
                        m = m / Math.Sin(m);
                    else
                        m = 1.0;
                    l = l * m;
                    m = (sins * Math.Cos(dec0) - coss * Math.Sin(dec0) * Math.Cos(ra - ra0)) * m;
                    break;
                case 4:   /* -NCP North celestial pole*/
                    if (dec0 == 0.0)
                        return 501;  /* can't stand the equator */
                    else
                        m = (Math.Cos(dec0) - coss * Math.Cos(ra - ra0)) / Math.Sin(dec0);
                    break;
                case 5:   /* -GLS global sinusoid */
                    dt = ra - ra0;
                    if (Math.Abs(dec) > twopi / 4.0) return 501;
                    if (Math.Abs(dec0) > twopi / 4.0) return 501;
                    m = dec - dec0;
                    l = dt * coss;
                    break;
                case 6:   /* -MER mercator*/
                    dt = yinc * cosr + xinc * sinr;
                    if (dt == 0.0) dt = 1.0;
                    dy = (yref / 2.0 + 45.0) * cond2r;
                    dx = dy + dt / 2.0 * cond2r;
                    dy = Math.Log(Math.Tan(dy));
                    dx = Math.Log(Math.Tan(dx));
                    geo2 = dt * cond2r / (dx - dy);
                    geo3 = geo2 * dy;
                    geo1 = Math.Cos(yref * cond2r);
                    if (geo1 <= 0.0) geo1 = 1.0;
                    dt = ra - ra0;
                    l = geo1 * dt;
                    dt = dec / 2.0 + twopi / 8.0;
                    dt = Math.Tan(dt);
                    if (dt < deps) return 502;
                    m = geo2 * Math.Log(dt) - geo3;
                    break;
                case 7:   /* -AIT Aitoff*/
                    da = (ra - ra0) / 2.0;
                    if (Math.Abs(da) > twopi / 4.0) return 501;
                    dt = yinc * cosr + xinc * sinr;
                    if (dt == 0.0) dt = 1.0;
                    dt = dt * cond2r;
                    dy = yref * cond2r;
                    dx = Math.Sin(dy + dt) / Math.Sqrt((1.0 + Math.Cos(dy + dt)) / 2.0) -
                        Math.Sin(dy) / Math.Sqrt((1.0 + Math.Cos(dy)) / 2.0);
                    if (dx == 0.0) dx = 1.0;
                    geo2 = dt / dx;
                    dt = xinc * cosr - yinc * sinr;
                    if (dt == 0.0) dt = 1.0;
                    dt = dt * cond2r;
                    dx = 2.0 * Math.Cos(dy) * Math.Sin(dt / 2.0);
                    if (dx == 0.0) dx = 1.0;
                    geo1 = dt * Math.Sqrt((1.0 + Math.Cos(dy) * Math.Cos(dt / 2.0)) / 2.0) / dx;
                    geo3 = geo2 * Math.Sin(dy) / Math.Sqrt((1.0 + Math.Cos(dy)) / 2.0);
                    dt = Math.Sqrt((1.0 + Math.Cos(dec) * Math.Cos(da)) / 2.0);
                    if (Math.Abs(dt) < deps) return 503;
                    l = 2.0 * geo1 * Math.Cos(dec) * Math.Sin(da) / dt;
                    m = geo2 * Math.Sin(dec) / dt - geo3;
                    break;
                case 8:   /* -STG Sterographic*/
                    da = ra - ra0;
                    if (Math.Abs(dec) > twopi / 4.0) return 501;
                    dd = 1.0 + sins * Math.Sin(dec0) + coss * Math.Cos(dec0) * Math.Cos(da);
                    if (Math.Abs(dd) < deps) return 501;
                    dd = 2.0 / dd;
                    l = l * dd;
                    m = dd * (sins * Math.Cos(dec0) - coss * Math.Sin(dec0) * Math.Cos(da));
                    break;

                default:
                    /* fall through to here on error */
                    return 504;

            }  /* end of itype switch */

            /*   back to degrees  */
            dx = l / cond2r;
            dy = m / cond2r;
            /*  Correct for rotation */
            dz = dx * cosr + dy * sinr;
            dy = dy * cosr - dx * sinr;
            dx = dz;
            /*     convert to pixels  */
            xpix = dx / xinc + xrefpix;
            ypix = dy / yinc + yrefpix;
            return 0;
        }
    }
}