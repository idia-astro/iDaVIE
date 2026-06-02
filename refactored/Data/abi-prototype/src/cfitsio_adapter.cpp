#include "cfitsio_adapter.h"

#include <fitsio.h>

#include <algorithm>
#include <cstring>

namespace idv {

namespace {

FitsAdapterError ok()
{
    return {};
}

FitsAdapterError fail(int cfitsio_status, const char* message)
{
    return {map_cfitsio_status(cfitsio_status), cfitsio_status, message ? message : ""};
}

bool validate_region(const idv_region_request_t& request, const FitsImageInfo& info)
{
    if (request.part_index != 0 || request.axis_count != info.axis_count || request.axis_count == 0 || request.axis_count > IDV_MAX_AXES) {
        return false;
    }
    for (uint32_t axis = 0; axis < request.axis_count; ++axis) {
        if (request.start[axis] < 0 || request.end[axis] < request.start[axis] || request.end[axis] >= info.axis_lengths[axis]) {
            return false;
        }
    }
    return true;
}

int64_t region_element_count(const idv_region_request_t& request)
{
    int64_t count = 1;
    for (uint32_t axis = 0; axis < request.axis_count; ++axis) {
        count *= request.end[axis] - request.start[axis] + 1;
    }
    return count;
}

} // namespace

CfitsioFile::~CfitsioFile()
{
    close();
}

FitsAdapterError CfitsioFile::open(const char* path_utf8, int mode, uint32_t selected_part)
{
    if (!path_utf8) {
        return {IDV_STATUS_INVALID_ARGUMENT, 0, "FITS path is null"};
    }
    close();

    int status = 0;
    if (fits_open_file(&file_, const_cast<char*>(path_utf8), mode, &status) != 0) {
        file_ = nullptr;
        return fail(status, "CFITSIO could not open file");
    }

    if (selected_part > 0) {
        int hdu_type = 0;
        if (fits_movabs_hdu(file_, static_cast<int>(selected_part + 1), &hdu_type, &status) != 0) {
            close();
            return fail(status, "CFITSIO could not move to selected HDU");
        }
    }

    return ok();
}

FitsAdapterError CfitsioFile::close()
{
    if (!file_) {
        return ok();
    }
    int status = 0;
    fitsfile* file = file_;
    file_ = nullptr;
    if (fits_close_file(file, &status) != 0) {
        return fail(status, "CFITSIO could not close file");
    }
    return ok();
}

FitsAdapterError CfitsioFile::image_info(FitsImageInfo* out_info) const
{
    if (!file_ || !out_info) {
        return {IDV_STATUS_INVALID_ARGUMENT, 0, "Invalid FITS image info request"};
    }

    int status = 0;
    int hdu_count = 0;
    int hdu_type = 0;
    int bitpix = 0;
    int axis_count = 0;
    long axes[IDV_MAX_AXES] = {0};

    if (fits_get_num_hdus(file_, &hdu_count, &status) != 0) {
        return fail(status, "CFITSIO could not count HDUs");
    }
    if (fits_get_hdu_type(file_, &hdu_type, &status) != 0) {
        return fail(status, "CFITSIO could not read HDU type");
    }
    if (hdu_type != IMAGE_HDU) {
        return {IDV_STATUS_UNSUPPORTED, status, "Current HDU is not an image HDU"};
    }
    if (fits_get_img_type(file_, &bitpix, &status) != 0) {
        return fail(status, "CFITSIO could not read image type");
    }
    if (fits_get_img_dim(file_, &axis_count, &status) != 0) {
        return fail(status, "CFITSIO could not read image axis count");
    }
    if (axis_count <= 0 || axis_count > IDV_MAX_AXES) {
        return {IDV_STATUS_FORMAT_ERROR, status, "Unsupported FITS image dimensionality"};
    }
    if (fits_get_img_size(file_, IDV_MAX_AXES, axes, &status) != 0) {
        return fail(status, "CFITSIO could not read image dimensions");
    }

    *out_info = {};
    out_info->kind = axis_count >= 3 ? IDV_DATASET_VOLUME : IDV_DATASET_IMAGE;
    out_info->scalar_type = scalar_type_from_bitpix(bitpix);
    out_info->axis_count = static_cast<uint32_t>(axis_count);
    out_info->part_count = static_cast<uint32_t>(std::max(0, hdu_count));
    for (int axis = 0; axis < axis_count; ++axis) {
        out_info->axis_lengths[axis] = axes[axis];
    }

    return ok();
}

FitsAdapterError CfitsioFile::header_cards(std::vector<unsigned char>* out_cards) const
{
    if (!file_ || !out_cards) {
        return {IDV_STATUS_INVALID_ARGUMENT, 0, "Invalid FITS header request"};
    }

    int status = 0;
    char* header = nullptr;
    int nkeys = 0;
    if (fits_hdr2str(file_, 1, nullptr, 0, &header, &nkeys, &status) != 0) {
        return fail(status, "CFITSIO could not serialise FITS header");
    }

    const size_t byte_count = static_cast<size_t>(std::max(0, nkeys)) * 80;
    out_cards->assign(reinterpret_cast<unsigned char*>(header), reinterpret_cast<unsigned char*>(header) + byte_count);
    fits_free_memory(header, &status);
    if (status != 0) {
        return fail(status, "CFITSIO could not free header memory");
    }
    return ok();
}

FitsAdapterError CfitsioFile::read_region_f32(const idv_region_request_t& request, std::vector<float>* out_voxels) const
{
    if (!out_voxels) {
        return {IDV_STATUS_INVALID_ARGUMENT, 0, "Invalid FITS region output"};
    }
    FitsImageInfo info;
    auto info_result = image_info(&info);
    if (info_result.status != IDV_STATUS_OK) {
        return info_result;
    }
    if (!validate_region(request, info)) {
        return {IDV_STATUS_INVALID_ARGUMENT, 0, "FITS region is outside image bounds"};
    }

    const int64_t count = region_element_count(request);
    out_voxels->assign(static_cast<size_t>(count), 0.0f);

    int status = 0;
    int anynul = 0;
    float nulval = 0.0f;
    const long first_pixel = 1;
    const long nelements = static_cast<long>(count);

    if (fits_read_img(file_, TFLOAT, first_pixel, nelements, &nulval, out_voxels->data(), &anynul, &status) != 0) {
        out_voxels->clear();
        return fail(status, "CFITSIO could not read FITS image as float32");
    }
    return ok();
}

FitsAdapterError CfitsioFile::read_region_typed(const idv_region_request_t& request, idv_scalar_type_t type, std::vector<unsigned char>* out_bytes) const
{
    if (!out_bytes) {
        return {IDV_STATUS_INVALID_ARGUMENT, 0, "Invalid FITS typed region output"};
    }
    if (type == IDV_SCALAR_FLOAT32) {
        std::vector<float> floats;
        auto result = read_region_f32(request, &floats);
        if (result.status != IDV_STATUS_OK) {
            return result;
        }
        const auto byte_count = floats.size() * sizeof(float);
        out_bytes->resize(byte_count);
        std::memcpy(out_bytes->data(), floats.data(), byte_count);
        return ok();
    }

    FitsImageInfo info;
    auto info_result = image_info(&info);
    if (info_result.status != IDV_STATUS_OK) {
        return info_result;
    }
    if (!validate_region(request, info)) {
        return {IDV_STATUS_INVALID_ARGUMENT, 0, "FITS region is outside image bounds"};
    }

    switch (type) {
    case IDV_SCALAR_FLOAT32: {
        std::vector<float> temp;
        auto result = read_region_f32(request, &temp);
        if (result.status != IDV_STATUS_OK) {
            return result;
        }
        out_bytes->resize(temp.size() * sizeof(float));
        std::memcpy(out_bytes->data(), temp.data(), out_bytes->size());
        return ok();
    }
    case IDV_SCALAR_FLOAT64: {
        std::vector<float> temp;
        auto result = read_region_f32(request, &temp);
        if (result.status != IDV_STATUS_OK) {
            return result;
        }
        std::vector<double> converted(temp.size());
        std::transform(temp.begin(), temp.end(), converted.begin(), [](float value) { return static_cast<double>(value); });
        out_bytes->resize(converted.size() * sizeof(double));
        std::memcpy(out_bytes->data(), converted.data(), out_bytes->size());
        return ok();
    }
    case IDV_SCALAR_INT16: {
        std::vector<float> temp;
        auto result = read_region_f32(request, &temp);
        if (result.status != IDV_STATUS_OK) {
            return result;
        }
        std::vector<int16_t> converted(temp.size());
        std::transform(temp.begin(), temp.end(), converted.begin(), [](float value) { return static_cast<int16_t>(value); });
        out_bytes->resize(converted.size() * sizeof(int16_t));
        std::memcpy(out_bytes->data(), converted.data(), out_bytes->size());
        return ok();
    }
    case IDV_SCALAR_INT32: {
        std::vector<float> temp;
        auto result = read_region_f32(request, &temp);
        if (result.status != IDV_STATUS_OK) {
            return result;
        }
        std::vector<int32_t> converted(temp.size());
        std::transform(temp.begin(), temp.end(), converted.begin(), [](float value) { return static_cast<int32_t>(value); });
        out_bytes->resize(converted.size() * sizeof(int32_t));
        std::memcpy(out_bytes->data(), converted.data(), out_bytes->size());
        return ok();
    }
    case IDV_SCALAR_UINT8: {
        std::vector<float> temp;
        auto result = read_region_f32(request, &temp);
        if (result.status != IDV_STATUS_OK) {
            return result;
        }
        std::vector<unsigned char> converted(temp.size());
        std::transform(temp.begin(), temp.end(), converted.begin(), [](float value) { return static_cast<unsigned char>(value); });
        *out_bytes = std::move(converted);
        return ok();
    }
    case IDV_SCALAR_INT64: {
        std::vector<float> temp;
        auto result = read_region_f32(request, &temp);
        if (result.status != IDV_STATUS_OK) {
            return result;
        }
        std::vector<int64_t> converted(temp.size());
        std::transform(temp.begin(), temp.end(), converted.begin(), [](float value) { return static_cast<int64_t>(value); });
        out_bytes->resize(converted.size() * sizeof(int64_t));
        std::memcpy(out_bytes->data(), converted.data(), out_bytes->size());
        return ok();
    }
    case IDV_SCALAR_INT8: {
        std::vector<float> temp;
        auto result = read_region_f32(request, &temp);
        if (result.status != IDV_STATUS_OK) {
            return result;
        }
        std::vector<int8_t> converted(temp.size());
        std::transform(temp.begin(), temp.end(), converted.begin(), [](float value) { return static_cast<int8_t>(value); });
        out_bytes->resize(converted.size() * sizeof(int8_t));
        std::memcpy(out_bytes->data(), converted.data(), out_bytes->size());
        return ok();
    }
    default:
        return {IDV_STATUS_UNSUPPORTED, 0, "Unsupported FITS output scalar type"};
    }
}

FitsAdapterError CfitsioFile::write_mask_subcube(
    const idv_region_request_t& region,
    const int16_t* mask_data,
    int64_t mask_voxel_count)
{
    if (!file_ || !mask_data || mask_voxel_count < 0) {
        return {IDV_STATUS_INVALID_ARGUMENT, 0, "Invalid FITS mask write request"};
    }

    FitsImageInfo info;
    auto info_result = image_info(&info);
    if (info_result.status != IDV_STATUS_OK) {
        return info_result;
    }
    if (!validate_region(region, info)) {
        return {IDV_STATUS_INVALID_ARGUMENT, 0, "FITS mask region is outside image bounds"};
    }

    const int64_t expected = region_element_count(region);
    if (mask_voxel_count != expected) {
        return {IDV_STATUS_INVALID_ARGUMENT, 0, "FITS mask voxel count does not match region"};
    }

    long first[IDV_MAX_AXES] = {1};
    long last[IDV_MAX_AXES] = {1};
    for (uint32_t axis = 0; axis < region.axis_count; ++axis) {
        first[axis] = static_cast<long>(region.start[axis] + 1);
        last[axis] = static_cast<long>(region.end[axis] + 1);
    }

    int status = 0;
    if (fits_write_subset(file_, TSHORT, first, last, const_cast<int16_t*>(mask_data), &status) != 0) {
        return fail(status, "CFITSIO could not write int16 FITS mask subcube");
    }
    if (fits_flush_file(file_, &status) != 0) {
        return fail(status, "CFITSIO could not flush FITS mask subcube");
    }
    return ok();
}

idv_status_t map_cfitsio_status(int cfitsio_status)
{
    if (cfitsio_status == 0) {
        return IDV_STATUS_OK;
    }
    switch (cfitsio_status) {
    case FILE_NOT_OPENED:
    case FILE_NOT_CREATED:
    case READ_ERROR:
    case WRITE_ERROR:
        return IDV_STATUS_IO_ERROR;
    case BAD_FILEPTR:
    case BAD_HDU_NUM:
    case BAD_COL_NUM:
        return IDV_STATUS_INVALID_ARGUMENT;
    case MEMORY_ALLOCATION:
        return IDV_STATUS_OUT_OF_MEMORY;
    default:
        return IDV_STATUS_FORMAT_ERROR;
    }
}

idv_scalar_type_t scalar_type_from_bitpix(int bitpix)
{
    switch (bitpix) {
    case BYTE_IMG:
        return IDV_SCALAR_UINT8;
    case SHORT_IMG:
        return IDV_SCALAR_INT16;
    case LONG_IMG:
        return IDV_SCALAR_INT32;
    case LONGLONG_IMG:
        return IDV_SCALAR_INT64;
    case FLOAT_IMG:
        return IDV_SCALAR_FLOAT32;
    case DOUBLE_IMG:
        return IDV_SCALAR_FLOAT64;
    default:
        return IDV_SCALAR_UINT8;
    }
}

int cfitsio_type_from_scalar(idv_scalar_type_t type)
{
    switch (type) {
    case IDV_SCALAR_FLOAT32:
        return TFLOAT;
    case IDV_SCALAR_FLOAT64:
        return TDOUBLE;
    case IDV_SCALAR_INT16:
        return TSHORT;
    case IDV_SCALAR_INT32:
        return TINT;
    case IDV_SCALAR_UINT8:
        return TBYTE;
    case IDV_SCALAR_INT64:
        return TLONGLONG;
    case IDV_SCALAR_INT8:
        return TSBYTE;
    default:
        return 0;
    }
}

int64_t scalar_size_bytes(idv_scalar_type_t type)
{
    switch (type) {
    case IDV_SCALAR_FLOAT32:
        return 4;
    case IDV_SCALAR_FLOAT64:
        return 8;
    case IDV_SCALAR_INT16:
        return 2;
    case IDV_SCALAR_INT32:
        return 4;
    case IDV_SCALAR_UINT8:
        return 1;
    case IDV_SCALAR_INT64:
        return 8;
    case IDV_SCALAR_INT8:
        return 1;
    default:
        return 0;
    }
}

} // namespace idv
