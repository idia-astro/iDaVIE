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
