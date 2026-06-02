#ifndef IDV_CFITSIO_ADAPTER_H
#define IDV_CFITSIO_ADAPTER_H

#include "idv_plugin_abi.h"

#include <fitsio.h>
#include <cstdint>
#include <string>
#include <vector>

namespace idv {

struct FitsAdapterError {
    idv_status_t status = IDV_STATUS_OK;
    int cfitsio_status = 0;
    std::string message;
};

struct FitsImageInfo {
    idv_dataset_kind_t kind = IDV_DATASET_IMAGE;
    idv_scalar_type_t scalar_type = IDV_SCALAR_FLOAT32;
    uint32_t axis_count = 0;
    int64_t axis_lengths[IDV_MAX_AXES] = {0};
    uint32_t part_count = 0;
};

class CfitsioFile {
public:
    CfitsioFile() = default;
    CfitsioFile(const CfitsioFile&) = delete;
    CfitsioFile& operator=(const CfitsioFile&) = delete;
    ~CfitsioFile();

    fitsfile* raw() const { return file_; }
    FitsAdapterError open(const char* path_utf8, int mode, uint32_t selected_part);
    FitsAdapterError close();
    FitsAdapterError image_info(FitsImageInfo* out_info) const;
    FitsAdapterError header_cards(std::vector<unsigned char>* out_cards) const;
    FitsAdapterError read_region_f32(const idv_region_request_t& request, std::vector<float>* out_voxels) const;
    FitsAdapterError read_region_typed(const idv_region_request_t& request, idv_scalar_type_t type, std::vector<unsigned char>* out_bytes) const;
    FitsAdapterError write_mask_subcube(const idv_region_request_t& region, const int16_t* mask_data, int64_t mask_voxel_count);

private:
    fitsfile* file_ = nullptr;
};

idv_status_t map_cfitsio_status(int cfitsio_status);
idv_scalar_type_t scalar_type_from_bitpix(int bitpix);
int cfitsio_type_from_scalar(idv_scalar_type_t type);
int64_t scalar_size_bytes(idv_scalar_type_t type);

} // namespace idv

#endif
