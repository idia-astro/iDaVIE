#include "cfitsio_adapter.h"

#include <fitsio.h>

#include <cstring>
#include <new>
#include <string>
#include <vector>

struct idv_plugin_t {
    const idv_host_services_t* services = nullptr;
};

namespace {

struct Buffer {
    std::vector<unsigned char> bytes;
    idv_scalar_type_t scalar_type = IDV_SCALAR_UINT8;
    uint32_t axis_count = 1;
    int64_t axis_lengths[IDV_MAX_AXES] = {0};
};

using File = idv::CfitsioFile;

thread_local std::string g_last_error;
thread_local int g_last_cfitsio_status = 0;

idv_status_t remember(const idv::FitsAdapterError& error)
{
    g_last_error = error.message;
    g_last_cfitsio_status = error.cfitsio_status;
    return error.status;
}

void fill_buffer_info(const Buffer& buffer, idv_buffer_info_t* out_info)
{
    std::memset(out_info, 0, sizeof(*out_info));
    out_info->struct_size = sizeof(*out_info);
    out_info->data = const_cast<unsigned char*>(buffer.bytes.data());
    out_info->byte_length = static_cast<int64_t>(buffer.bytes.size());
    out_info->scalar_type = buffer.scalar_type;
    out_info->axis_count = buffer.axis_count;
    for (uint32_t axis = 0; axis < buffer.axis_count; ++axis) {
        out_info->axis_lengths[axis] = buffer.axis_lengths[axis];
    }
}

idv_status_t IDV_CALL open_file(
    idv_plugin_handle_t,
    const char* path_utf8,
    int32_t path_byte_length,
    const idv_open_options_t* options,
    idv_fits_file_handle_t* out_file)
{
    if (!path_utf8 || path_byte_length < 0 || !out_file) {
        return IDV_STATUS_INVALID_ARGUMENT;
    }

    const uint32_t selected_part = options ? options->selected_part : 0;
    const int mode = options && options->mode == IDV_OPEN_READ_WRITE ? READWRITE : READONLY;
    std::string path(path_utf8, static_cast<size_t>(path_byte_length));

    auto* file = new (std::nothrow) File();
    if (!file) {
        return IDV_STATUS_OUT_OF_MEMORY;
    }
    auto result = file->open(path.c_str(), mode, selected_part);
    if (result.status != IDV_STATUS_OK) {
        delete file;
        *out_file = nullptr;
        return remember(result);
    }

    *out_file = file;
    return IDV_STATUS_OK;
}

idv_status_t IDV_CALL close_file(idv_plugin_handle_t, idv_fits_file_handle_t file)
{
    if (!file) {
        return IDV_STATUS_INVALID_ARGUMENT;
    }
    delete static_cast<File*>(file);
    return IDV_STATUS_OK;
}

idv_status_t IDV_CALL get_dataset_info(idv_plugin_handle_t, idv_fits_file_handle_t file_handle, idv_dataset_info_t* out_info)
{
    if (!file_handle || !out_info) {
        return IDV_STATUS_INVALID_ARGUMENT;
    }
    idv::FitsImageInfo info;
    auto result = static_cast<File*>(file_handle)->image_info(&info);
    if (result.status != IDV_STATUS_OK) {
        return remember(result);
    }

    std::memset(out_info, 0, sizeof(*out_info));
    out_info->struct_size = sizeof(*out_info);
    out_info->kind = info.kind;
    out_info->scalar_type = info.scalar_type;
    out_info->axis_count = info.axis_count;
    out_info->part_count = info.part_count;
    for (uint32_t axis = 0; axis < info.axis_count; ++axis) {
        out_info->axis_lengths[axis] = info.axis_lengths[axis];
    }
    return IDV_STATUS_OK;
}

idv_status_t IDV_CALL read_header(idv_plugin_handle_t, idv_fits_file_handle_t file_handle, idv_buffer_handle_t* out_header_cards)
{
    if (!file_handle || !out_header_cards) {
        return IDV_STATUS_INVALID_ARGUMENT;
    }

    auto* buffer = new (std::nothrow) Buffer();
    if (!buffer) {
        return IDV_STATUS_OUT_OF_MEMORY;
    }
    auto result = static_cast<File*>(file_handle)->header_cards(&buffer->bytes);
    if (result.status != IDV_STATUS_OK) {
        delete buffer;
        *out_header_cards = nullptr;
        return remember(result);
    }

    buffer->scalar_type = IDV_SCALAR_UINT8;
    buffer->axis_count = 1;
    buffer->axis_lengths[0] = static_cast<int64_t>(buffer->bytes.size());
    *out_header_cards = buffer;
    return IDV_STATUS_OK;
}

idv_status_t IDV_CALL read_region_f32(
    idv_plugin_handle_t,
    idv_fits_file_handle_t file_handle,
    const idv_region_request_t* request,
    float** out_voxels,
    int64_t* out_voxel_count)
{
    if (!file_handle || !request || !out_voxels || !out_voxel_count) {
        return IDV_STATUS_INVALID_ARGUMENT;
    }

    std::vector<float> voxels;
    auto result = static_cast<File*>(file_handle)->read_region_f32(*request, &voxels);
    if (result.status != IDV_STATUS_OK) {
        *out_voxels = nullptr;
        *out_voxel_count = 0;
        return remember(result);
    }

    auto* output = new (std::nothrow) float[voxels.size()];
    if (!output) {
        *out_voxels = nullptr;
        *out_voxel_count = 0;
        return IDV_STATUS_OUT_OF_MEMORY;
    }
    std::memcpy(output, voxels.data(), voxels.size() * sizeof(float));
    *out_voxels = output;
    *out_voxel_count = static_cast<int64_t>(voxels.size());
    return IDV_STATUS_OK;
}

idv_status_t IDV_CALL read_region_typed(
    idv_plugin_handle_t,
    idv_fits_file_handle_t file_handle,
    const idv_region_request_t* request,
    idv_buffer_handle_t* out_buffer)
{
    if (!file_handle || !request || !out_buffer) {
        return IDV_STATUS_INVALID_ARGUMENT;
    }

    auto* buffer = new (std::nothrow) Buffer();
    if (!buffer) {
        return IDV_STATUS_OUT_OF_MEMORY;
    }

    auto result = static_cast<File*>(file_handle)->read_region_typed(*request, request->output_type, &buffer->bytes);
    if (result.status != IDV_STATUS_OK) {
        delete buffer;
        *out_buffer = nullptr;
        return remember(result);
    }

    buffer->scalar_type = request->output_type;
    buffer->axis_count = request->axis_count;
    for (uint32_t axis = 0; axis < request->axis_count; ++axis) {
        buffer->axis_lengths[axis] = request->end[axis] - request->start[axis] + 1;
    }
    *out_buffer = buffer;
    return IDV_STATUS_OK;
}

idv_status_t IDV_CALL read_table_column(idv_plugin_handle_t, idv_fits_file_handle_t, uint32_t, idv_buffer_handle_t*)
{
    return IDV_STATUS_UNSUPPORTED;
}

idv_status_t IDV_CALL write_mask_subcube(
    idv_plugin_handle_t,
    idv_fits_file_handle_t file_handle,
    const idv_region_request_t* region,
    const int16_t* mask_data,
    int64_t mask_voxel_count)
{
    if (!file_handle || !region || !mask_data || mask_voxel_count < 0) {
        return IDV_STATUS_INVALID_ARGUMENT;
    }

    auto result = static_cast<File*>(file_handle)->write_mask_subcube(*region, mask_data, mask_voxel_count);
    if (result.status != IDV_STATUS_OK) {
        return remember(result);
    }
    return IDV_STATUS_OK;
}

idv_status_t IDV_CALL free_image_f32(idv_plugin_handle_t, float* voxels)
{
    delete[] voxels;
    return IDV_STATUS_OK;
}

idv_status_t IDV_CALL get_buffer_info(idv_plugin_handle_t, idv_buffer_handle_t buffer_handle, idv_buffer_info_t* out_info)
{
    if (!buffer_handle || !out_info) {
        return IDV_STATUS_INVALID_ARGUMENT;
    }
    fill_buffer_info(*static_cast<Buffer*>(buffer_handle), out_info);
    return IDV_STATUS_OK;
}

idv_status_t IDV_CALL release_buffer(idv_plugin_handle_t, idv_buffer_handle_t buffer)
{
    delete static_cast<Buffer*>(buffer);
    return IDV_STATUS_OK;
}

idv_fits_io_v1_t kFitsIoApi = {
    sizeof(idv_fits_io_v1_t),
    open_file,
    close_file,
    get_dataset_info,
    read_header,
    read_region_f32,
    read_region_typed,
    read_table_column,
    write_mask_subcube,
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
    "idavie.fits.cfitsio",
    "iDaVIE CFITSIO FITS I/O plug-in",
    "iDaVIE",
    "LGPL-3.0-or-later",
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
