/*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 Inter-University Institute for Data Intensive Astronomy
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
// ZScale algorithm, adapted from Client Display Library (CDL) source code (Mike Fitzpatrick NOAO/IRAF Project)

#define INDEF -999

void cdl_zscale(unsigned char* im, /* image data to be sampled		*/
                int nx, int ny,    /* image dimensions			*/
                int bitpix,        /* bits per pixel			*/
                float* z1,
                float* z2,      /* output min and max greyscale values	*/
                float contrast, /* adj. to slope of transfer function	*/
                int opt_size,   /* desired number of pixels in sample	*/
                int len_stdline /* optimal number of pixels per line	*/
);
