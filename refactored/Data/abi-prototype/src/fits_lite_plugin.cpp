#include "idv_plugin_abi.h"

#include <algorithm>
#include <cerrno>
#include <climits>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <string>
#include <vector>

struct idv_plugin_t {
    const idv_host_services_t* services;
};

struct idv_buffer_t {
    std::vector<unsigned char> bytes;
    idv_scalar_type_t scalar_type = IDV_SCALAR_UINT8;
    uint32_t axis_count = 1;
    int64_t axis_lengths[IDV_MAX_AXES] = {0};
};

struct fits_file_t {
    std::string path;
    std::vector<std::string> cards;
    std::string header_text;
    int bitpix = 0;
    uint32_t axis_count = 0;
    int64_t axis_lengths[IDV_MAX_AXES] = {0};
    int64_t data_offset = 0;
    std::string last_error;
};

namespace {

idv_status_t fail(fits_file_t* file, const std::string& message, idv_status_t status)
{
    if (file) {
        file->last_error = message;
    }
    return status;
}

std::string trim(const std::string& value)
{
    const auto first = value.find_first_not_of(' ');
    if (first == std::string::npos) {
        return "";
    }
    const auto last = value.find_last_not_of(' ');
    return value.substr(first, last - first + 1);
}

bool parse_int_card(const std::string& card, const char* key, long long* out_value)
{
    if (card.compare(0, std::strlen(key), key) != 0) {
        return false;
    }
    const auto equals = card.find('=');
    if (equals == std::string::npos) {
        return false;
    }
    auto value_part = card.substr(equals + 1);
    const auto slash = value_part.find('/');
    if (slash != std::string::npos) {
        value_part = value_part.substr(0, slash);
    }
    value_part = trim(value_part);
    if (value_part.empty()) {
        return false;
    }
    char* end = nullptr;
    errno = 0;
    const long long parsed = std::strtoll(value_part.c_str(), &end, 10);
    if (errno != 0 || end == value_part.c_str()) {
        return false;
    }
    *out_value = parsed;
    return true;
}

uint16_t read_be_u16(const unsigned char* p)
{
    return static_cast<uint16_t>((p[0] << 8) | p[1]);
}

uint32_t read_be_u32(const unsigned char* p)
{
    return (static_cast<uint32_t>(p[0]) << 24) |
           (static_cast<uint32_t>(p[1]) << 16) |
           (static_cast<uint32_t>(p[2]) << 8) |
           static_cast<uint32_t>(p[3]);
}

uint64_t read_be_u64(const unsigned char* p)
{
    uint64_t value = 0;
    for (int i = 0; i < 8; ++i) {
        value = (value << 8) | p[i];
    }
    return value;
}

double read_numeric_value(const unsigned char* p, int bitpix)
{
    switch (bitpix) {
    case 8:
        return static_cast<double>(*p);
    case 16:
        return static_cast<double>(static_cast<int16_t>(read_be_u16(p)));
    case 32:
        return static_cast<double>(static_cast<int32_t>(read_be_u32(p)));
    case 64:
        return static_cast<double>(static_cast<int64_t>(read_be_u64(p)));
    case -32: {
        const uint32_t bits = read_be_u32(p);
        float f = 0.0f;
        std::memcpy(&f, &bits, sizeof(float));
        return static_cast<double>(f);
    }
    default:
        return 0.0;
    }
}

size_t bytes_per_pixel(int bitpix)
{
    switch (bitpix) {
    case 8:
        return 1;
    case 16:
        return 2;
    case 32:
    case -32:
        return 4;
    case 64:
        return 8;
    default:
        return 0;
    }
}

idv_scalar_type_t scalar_for_bitpix(int bitpix)
{
    switch (bitpix) {
    case 8:
        return IDV_SCALAR_UINT8;
    case 16:
        return IDV_SCALAR_INT16;
    case 32:
        return IDV_SCALAR_INT32;
    case 64:
        return IDV_SCALAR_INT64;
    case -32:
        return IDV_SCALAR_FLOAT32;
    default:
        return IDV_SCALAR_UINT8;
    }
}

idv_status_t IDV_CALL open_file(
    idv_plugin_handle_t,
    const char* path,
    int32_t path_byte_length,
    const idv_open_options_t* options,
    idv_fits_file_handle_t* out_file)
{
    if (!path || path_byte_length < 0 || !out_file) {
        return IDV_STATUS_INVALID_ARGUMENT;
    }
    if (options && options->mode != IDV_OPEN_READ_ONLY) {
        return IDV_STATUS_UNSUPPORTED;
    }
    std::string path_string(path, static_cast<size_t>(path_byte_length));
    FILE* stream = std::fopen(path_string.c_str(), "rb");
    if (!stream) {
        return IDV_STATUS_IO_ERROR;
    }

    auto* file = new (std::nothrow) fits_file_t();
    if (!file) {
        std::fclose(stream);
        return IDV_STATUS_OUT_OF_MEMORY;
    }
    file->path = path_string;

    unsigned char card_buf[80];
    int64_t bytes_read = 0;
    bool found_end = false;
    while (std::fread(card_buf, 1, sizeof(card_buf), stream) == sizeof(card_buf)) {
        bytes_read += sizeof(card_buf);
        std::string card(reinterpret_cast<char*>(card_buf), sizeof(card_buf));
        file->cards.push_back(card);
        file->header_text += card;
        file->header_text += "\n";
        if (card.compare(0, 3, "END") == 0) {
            found_end = true;
            break;
        }
    }

    if (!found_end) {
        std::fclose(stream);
        delete file;
        return IDV_STATUS_FORMAT_ERROR;
    }

    file->data_offset = ((bytes_read + 2879) / 2880) * 2880;

    long long parsed = 0;
    for (const auto& card : file->cards) {
        if (parse_int_card(card, "BITPIX", &parsed)) {
            file->bitpix = static_cast<int>(parsed);
        } else if (parse_int_card(card, "NAXIS ", &parsed)) {
            file->axis_count = static_cast<uint32_t>(parsed);
        } else {
            for (uint32_t axis = 0; axis < IDV_MAX_AXES; ++axis) {
                char key[16];
                std::snprintf(key, sizeof(key), "NAXIS%u", axis + 1);
                if (parse_int_card(card, key, &parsed)) {
                    file->axis_lengths[axis] = static_cast<int64_t>(parsed);
                }
            }
        }
    }

    std::fclose(stream);
    if (file->axis_count == 0 || file->axis_count > IDV_MAX_AXES || bytes_per_pixel(file->bitpix) == 0) {
        delete file;
        return IDV_STATUS_FORMAT_ERROR;
    }

    *out_file = file;
    return IDV_STATUS_OK;
}

idv_status_t IDV_CALL close_file(idv_plugin_handle_t, idv_fits_file_handle_t file)
{
    if (!file) {
        return IDV_STATUS_INVALID_ARGUMENT;
    }
    delete static_cast<fits_file_t*>(file);
    return IDV_STATUS_OK;
}

idv_status_t IDV_CALL get_dataset_info(idv_plugin_handle_t, idv_fits_file_handle_t file_handle, idv_dataset_info_t* out_info)
{
    auto* file = static_cast<fits_file_t*>(file_handle);
    if (!file || !out_info) {
        return IDV_STATUS_INVALID_ARGUMENT;
    }
    std::memset(out_info, 0, sizeof(*out_info));
    out_info->struct_size = sizeof(*out_info);
    out_info->kind = file->axis_count >= 3 ? IDV_DATASET_VOLUME : IDV_DATASET_IMAGE;
    out_info->scalar_type = scalar_for_bitpix(file->bitpix);
    out_info->axis_count = file->axis_count;
    out_info->part_count = 1;
    for (uint32_t i = 0; i < file->axis_count; ++i) {
        out_info->axis_lengths[i] = file->axis_lengths[i];
    }
    return IDV_STATUS_OK;
}

idv_status_t IDV_CALL read_header(idv_plugin_handle_t, idv_fits_file_handle_t file_handle, idv_buffer_handle_t* out_header)
{
    auto* file = static_cast<fits_file_t*>(file_handle);
    if (!file || !out_header) {
        return IDV_STATUS_INVALID_ARGUMENT;
    }
    auto* buffer = new (std::nothrow) idv_buffer_t();
    if (!buffer) {
        return fail(file, "Could not allocate header buffer", IDV_STATUS_OUT_OF_MEMORY);
    }
    buffer->bytes.assign(file->header_text.begin(), file->header_text.end());
    buffer->bytes.push_back('\0');
    buffer->scalar_type = IDV_SCALAR_UINT8;
    buffer->axis_count = 1;
    buffer->axis_lengths[0] = buffer->bytes.size();
    *out_header = buffer;
    return IDV_STATUS_OK;
}

idv_status_t IDV_CALL read_region_typed(idv_plugin_handle_t, idv_fits_file_handle_t file_handle, const idv_region_request_t* request, idv_buffer_handle_t* out_buffer)
{
    auto* file = static_cast<fits_file_t*>(file_handle);
    if (!file || !request || !out_buffer) {
        return IDV_STATUS_INVALID_ARGUMENT;
    }
    if (request->part_index != 0 || request->axis_count != file->axis_count) {
        return fail(file, "Unsupported part index or axis count", IDV_STATUS_UNSUPPORTED);
    }
    if (request->output_type != IDV_SCALAR_FLOAT64) {
        return fail(file, "fits_lite only emits float64 region buffers", IDV_STATUS_UNSUPPORTED);
    }
    int64_t out_lengths[IDV_MAX_AXES] = {0};
    int64_t element_count = 1;
    for (uint32_t axis = 0; axis < file->axis_count; ++axis) {
        if (request->start[axis] > request->end[axis] || request->end[axis] >= file->axis_lengths[axis]) {
            return fail(file, "Region request is out of bounds", IDV_STATUS_INVALID_ARGUMENT);
        }
        out_lengths[axis] = request->end[axis] - request->start[axis] + 1;
        element_count *= out_lengths[axis];
    }

    FILE* stream = std::fopen(file->path.c_str(), "rb");
    if (!stream) {
        return fail(file, "Could not reopen FITS file for region read", IDV_STATUS_IO_ERROR);
    }

    auto* buffer = new (std::nothrow) idv_buffer_t();
    if (!buffer) {
        std::fclose(stream);
        return fail(file, "Could not allocate region buffer", IDV_STATUS_OUT_OF_MEMORY);
    }
    buffer->bytes.resize(element_count * sizeof(double));
    buffer->scalar_type = IDV_SCALAR_FLOAT64;
    buffer->axis_count = file->axis_count;
    for (uint32_t i = 0; i < file->axis_count; ++i) {
        buffer->axis_lengths[i] = out_lengths[i];
    }

    const size_t bpp = bytes_per_pixel(file->bitpix);
    const int64_t nx = file->axis_lengths[0];
    const int64_t ny = file->axis_count > 1 ? file->axis_lengths[1] : 1;
    double* out = reinterpret_cast<double*>(buffer->bytes.data());
    int64_t out_index = 0;
    std::vector<unsigned char> pixel(bpp);

    for (int64_t z = request->start[2]; z <= request->end[2]; ++z) {
        for (int64_t y = request->start[1]; y <= request->end[1]; ++y) {
            for (int64_t x = request->start[0]; x <= request->end[0]; ++x) {
                const int64_t pixel_index = x + y * nx + z * nx * ny;
                const int64_t offset = file->data_offset + pixel_index * static_cast<int64_t>(bpp);
                if (std::fseek(stream, static_cast<long>(offset), SEEK_SET) != 0 ||
                    std::fread(pixel.data(), 1, bpp, stream) != bpp) {
                    std::fclose(stream);
                    delete buffer;
                    return fail(file, "Could not read FITS pixel data", IDV_STATUS_IO_ERROR);
                }
                out[out_index++] = read_numeric_value(pixel.data(), file->bitpix);
            }
        }
    }

    std::fclose(stream);
    *out_buffer = buffer;
    return IDV_STATUS_OK;
}

idv_status_t IDV_CALL get_buffer_info(idv_plugin_handle_t, idv_buffer_handle_t buffer_handle, idv_buffer_info_t* out_info)
{
    auto* buffer = static_cast<idv_buffer_t*>(buffer_handle);
    if (!buffer || !out_info) {
        return IDV_STATUS_INVALID_ARGUMENT;
    }
    std::memset(out_info, 0, sizeof(*out_info));
    out_info->struct_size = sizeof(*out_info);
    out_info->data = buffer->bytes.data();
    out_info->byte_length = buffer->bytes.size();
    out_info->scalar_type = buffer->scalar_type;
    out_info->axis_count = buffer->axis_count;
    for (uint32_t i = 0; i < buffer->axis_count; ++i) {
        out_info->axis_lengths[i] = buffer->axis_lengths[i];
    }
    return IDV_STATUS_OK;
}

idv_status_t IDV_CALL release_buffer(idv_plugin_handle_t, idv_buffer_handle_t buffer)
{
    delete static_cast<idv_buffer_t*>(buffer);
    return IDV_STATUS_OK;
}

idv_status_t IDV_CALL unsupported_read_region_f32(idv_plugin_handle_t, idv_fits_file_handle_t, const idv_region_request_t*, float**, int64_t*)
{
    return IDV_STATUS_UNSUPPORTED;
}

idv_status_t IDV_CALL unsupported_read_table_column(idv_plugin_handle_t, idv_fits_file_handle_t, uint32_t, idv_buffer_handle_t*)
{
    return IDV_STATUS_UNSUPPORTED;
}

idv_status_t IDV_CALL unsupported_write_mask_subcube(idv_plugin_handle_t, idv_fits_file_handle_t, const idv_region_request_t*, const int16_t*, int64_t)
{
    return IDV_STATUS_UNSUPPORTED;
}

idv_status_t IDV_CALL free_image_f32(idv_plugin_handle_t, float* voxels)
{
    delete[] voxels;
    return IDV_STATUS_OK;
}

idv_fits_io_v1_t kFitsIoApi = {
    sizeof(idv_fits_io_v1_t),
    open_file,
    close_file,
    get_dataset_info,
    read_header,
    unsupported_read_region_f32,
    read_region_typed,
    unsupported_read_table_column,
    unsupported_write_mask_subcube,
    free_image_f32,
    get_buffer_info,
    release_buffer
};

const idv_capability_descriptor_t kCapabilities[] = {
    {
        sizeof(idv_capability_descriptor_t),
        IDV_CAPABILITY_FITS_IO,
        {1, 0, 0, 0},
        sizeof(idv_fits_io_v1_t),
        &kFitsIoApi
    }
};

idv_status_t IDV_CALL initialise(const idv_host_services_t* services, idv_plugin_handle_t* out_plugin)
{
    if (!out_plugin) {
        return IDV_STATUS_INVALID_ARGUMENT;
    }
    auto* plugin = new (std::nothrow) idv_plugin_t();
    if (!plugin) {
        return IDV_STATUS_OUT_OF_MEMORY;
    }
    plugin->services = services;
    *out_plugin = plugin;
    return IDV_STATUS_OK;
}

idv_status_t IDV_CALL shutdown(idv_plugin_handle_t plugin)
{
    delete static_cast<idv_plugin_t*>(plugin);
    return IDV_STATUS_OK;
}

const idv_plugin_manifest_t kManifest = {
    sizeof(idv_plugin_manifest_t),
    {IDV_CONTRACT_MAJOR_CURRENT, IDV_CONTRACT_MINOR_CURRENT, IDV_CONTRACT_PATCH_CURRENT, 0},
    {0, 1, 0, 0},
    "idavie.refactoring.fits-lite",
    "iDaVIE FITS lite I/O plug-in",
    "iDaVIE",
    "MIT",
    1,
    kCapabilities,
    initialise,
    shutdown
};

} // namespace

extern "C" IDV_API const idv_plugin_manifest_t* IDV_CALL idv_plugin_entry(void)
{
    return &kManifest;
}
