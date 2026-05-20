#pragma once

#ifdef __cplusplus
extern "C" {
#endif

typedef struct fitsfile fitsfile;

#define READONLY 0
#define READWRITE 1
#define IMAGE_HDU 0
#define ASCII_TBL 1
#define BINARY_TBL 2
#define FLOAT_IMG -32
#define TFLOAT 42
#define TSHORT 21
#define TLONG 41
#define TSTRING 16
#define TDOUBLE 82
#define FLEN_STATUS 31
#define FLEN_VALUE 71
#define FLEN_CARD 81

int fits_open_file(...);
int fits_create_file(...);
int fits_close_file(...);
int fits_flush_file(...);
int fits_get_num_hdus(...);
int fits_get_hdu_num(...);
int fits_movabs_hdu(...);
int fits_get_hdu_type(...);
int fits_get_hdrspace(...);
int fits_get_num_rows(...);
int fits_get_num_cols(...);
int fits_make_keyn(...);
int fits_read_key(...);
int fits_read_key_str(...);
int fits_read_keyn(...);
int fits_read_keyword(...);
int fits_delete_key(...);
int fits_get_img_dim(...);
int fits_create_img(...);
int fits_copy_header(...);
int fits_copy_file(...);
int fits_copy_image_section(...);
int fits_write_pix(...);
int fits_write_subset(...);
int fits_write_history(...);
int fits_write_key(...);
int fits_update_key(...);
int fits_get_img_sizell(...);
int fits_read_col(...);
int fits_read_pixll(...);
int fits_read_subset(...);
int fits_hdr2str(...);
int fits_free_memory(...);
int fits_get_errstatus(...);
int fits_read_errmsg(...);

#ifdef __cplusplus
}
#endif
