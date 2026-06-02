#include "idv_plugin_abi.h"

#include <dlfcn.h>

#include <cstdio>
#include <cstdlib>
#include <cstring>

namespace {

struct LoadedPlugin {
    void* library = nullptr;
    const idv_plugin_manifest_t* manifest = nullptr;
    idv_plugin_handle_t handle = nullptr;
};

void IDV_CALL log_message(idv_log_level_t level, const char* category, const char* message)
{
    std::fprintf(stderr, "[kernel log %d] %s: %s\n", static_cast<int>(level), category ? category : "", message ? message : "");
}

void* IDV_CALL host_alloc(size_t bytes, size_t)
{
    return std::malloc(bytes);
}

void IDV_CALL host_free(void* ptr)
{
    std::free(ptr);
}

bool IDV_CALL is_cancelled(idv_cancellation_token_t)
{
    return false;
}

void require_status(idv_status_t actual, idv_status_t expected, const char* label)
{
    if (actual != expected) {
        std::fprintf(stderr, "FAIL: %s returned %d, expected %d\n", label, static_cast<int>(actual), static_cast<int>(expected));
        std::exit(1);
    }
    std::printf("ok: %s\n", label);
}

void require_true(bool condition, const char* label)
{
    if (!condition) {
        std::fprintf(stderr, "FAIL: %s\n", label);
        std::exit(1);
    }
    std::printf("ok: %s\n", label);
}

template <typename T>
T load_symbol(void* library, const char* name)
{
    dlerror();
    void* symbol = dlsym(library, name);
    const char* error = dlerror();
    if (error || !symbol) {
        std::fprintf(stderr, "FAIL: dlsym(%s): %s\n", name, error ? error : "missing symbol");
        std::exit(1);
    }
    return reinterpret_cast<T>(symbol);
}

const idv_capability_descriptor_t* find_capability(const idv_plugin_manifest_t* manifest, idv_capability_id_t capability)
{
    for (uint32_t i = 0; i < manifest->capability_count; ++i) {
        if (manifest->capabilities[i].id == capability) {
            return &manifest->capabilities[i];
        }
    }
    return nullptr;
}

LoadedPlugin load_plugin(const char* path)
{
    LoadedPlugin plugin;
    plugin.library = dlopen(path, RTLD_NOW | RTLD_LOCAL);
    if (!plugin.library) {
        std::fprintf(stderr, "FAIL: dlopen(%s): %s\n", path, dlerror());
        std::exit(1);
    }

    auto entry = load_symbol<idv_plugin_entry_fn>(plugin.library, "idv_plugin_entry");
    plugin.manifest = entry();
    require_true(plugin.manifest != nullptr, "manifest not null");
    require_true(plugin.manifest->struct_size == sizeof(idv_plugin_manifest_t), "manifest struct size");
    require_true(plugin.manifest->contract_version.major == IDV_CONTRACT_MAJOR_CURRENT, "manifest contract major version");
    require_true(plugin.manifest->initialise != nullptr, "manifest initialise pointer");
    require_true(plugin.manifest->shutdown != nullptr, "manifest shutdown pointer");
    std::printf("loaded: %s (%s)\n", plugin.manifest->plugin_id, plugin.manifest->display_name);

    idv_host_services_t services = {};
    services.struct_size = sizeof(services);
    services.host_version = {0, 1, 0, 0};
    services.contract_version = {IDV_CONTRACT_MAJOR_CURRENT, IDV_CONTRACT_MINOR_CURRENT, IDV_CONTRACT_PATCH_CURRENT, 0};
    services.log = log_message;
    services.alloc = host_alloc;
    services.free = host_free;
    services.is_cancelled = is_cancelled;
    require_status(plugin.manifest->initialise(&services, &plugin.handle), IDV_STATUS_OK, "initialise plugin");
    require_true(plugin.handle != nullptr, "plugin handle not null");
    return plugin;
}

void unload_plugin(LoadedPlugin& plugin)
{
    if (plugin.manifest && plugin.manifest->shutdown && plugin.handle) {
        require_status(plugin.manifest->shutdown(plugin.handle), IDV_STATUS_OK, "shutdown plugin");
        plugin.handle = nullptr;
    }
    if (plugin.library) {
        dlclose(plugin.library);
        plugin.library = nullptr;
    }
}

} // namespace

int main(int argc, char** argv)
{
    if (argc != 4) {
        std::fprintf(stderr, "usage: %s <fits-file> <fits-plugin.so> <stats-plugin.so>\n", argv[0]);
        return 2;
    }

    const char* fits_file = argv[1];
    LoadedPlugin fits_plugin = load_plugin(argv[2]);
    LoadedPlugin stats_plugin = load_plugin(argv[3]);

    const auto* fits_descriptor = find_capability(fits_plugin.manifest, IDV_CAPABILITY_FITS_IO);
    require_true(fits_descriptor != nullptr, "resolve FITS I/O capability");
    require_true(fits_descriptor->function_table_size == sizeof(idv_fits_io_v1_t), "FITS I/O table size");
    auto* fits = static_cast<const idv_fits_io_v1_t*>(fits_descriptor->function_table);
    require_true(fits->struct_size == sizeof(idv_fits_io_v1_t), "FITS I/O API struct size");

    const auto* stats_descriptor = find_capability(stats_plugin.manifest, IDV_CAPABILITY_STATISTICS);
    require_true(stats_descriptor != nullptr, "resolve statistics capability");
    require_true(stats_descriptor->function_table_size == sizeof(idv_statistics_v1_t), "statistics table size");
    auto* stats = static_cast<const idv_statistics_v1_t*>(stats_descriptor->function_table);
    require_true(stats->struct_size == sizeof(idv_statistics_v1_t), "statistics API struct size");

    idv_open_options_t options = {};
    options.struct_size = sizeof(options);
    options.mode = IDV_OPEN_READ_ONLY;

    idv_fits_file_handle_t file = nullptr;
    require_status(
        fits->open_file(fits_plugin.handle, fits_file, static_cast<int32_t>(std::strlen(fits_file)), &options, &file),
        IDV_STATUS_OK,
        "open FITS file");

    idv_dataset_info_t dataset_info = {};
    require_status(fits->get_dataset_info(fits_plugin.handle, file, &dataset_info), IDV_STATUS_OK, "get dataset info");
    require_true(dataset_info.kind == IDV_DATASET_VOLUME, "dataset is volume");
    require_true(dataset_info.axis_count == 3, "dataset has 3 axes");
    require_true(dataset_info.axis_lengths[0] == 4, "axis 1 length is 4");
    require_true(dataset_info.axis_lengths[1] == 4, "axis 2 length is 4");
    require_true(dataset_info.axis_lengths[2] == 4, "axis 3 length is 4");

    idv_buffer_handle_t header = nullptr;
    require_status(fits->read_header(fits_plugin.handle, file, &header), IDV_STATUS_OK, "read FITS header");
    idv_buffer_info_t header_info = {};
    require_status(fits->get_buffer_info(fits_plugin.handle, header, &header_info), IDV_STATUS_OK, "get header buffer info");
    require_true(header_info.byte_length > 0, "header buffer is non-empty");
    require_status(fits->release_buffer(fits_plugin.handle, header), IDV_STATUS_OK, "release header buffer");

    idv_region_request_t region = {};
    region.struct_size = sizeof(region);
    region.part_index = 0;
    region.axis_count = 3;
    region.output_type = IDV_SCALAR_FLOAT64;
    for (uint32_t i = 0; i < 3; ++i) {
        region.start[i] = 0;
        region.end[i] = dataset_info.axis_lengths[i] - 1;
    }

    if (std::strcmp(fits_plugin.manifest->plugin_id, "idavie.fits.cfitsio") == 0) {
        int16_t mask_value = 1;
        require_status(
            fits->write_mask_subcube(fits_plugin.handle, file, &region, nullptr, 64),
            IDV_STATUS_INVALID_ARGUMENT,
            "reject FITS mask write with null data");
        require_status(
            fits->write_mask_subcube(fits_plugin.handle, file, &region, &mask_value, 1),
            IDV_STATUS_INVALID_ARGUMENT,
            "reject FITS mask write with mismatched voxel count");
    }

    idv_buffer_handle_t region_buffer = nullptr;
    require_status(fits->read_region_typed(fits_plugin.handle, file, &region, &region_buffer), IDV_STATUS_OK, "read full FITS region");
    idv_buffer_info_t region_info = {};
    require_status(fits->get_buffer_info(fits_plugin.handle, region_buffer, &region_info), IDV_STATUS_OK, "get region buffer info");
    require_true(region_info.scalar_type == IDV_SCALAR_FLOAT64, "region buffer is float64");
    require_true(region_info.axis_count == 3, "region buffer has 3 axes");
    require_true(region_info.byte_length == 64 * static_cast<int64_t>(sizeof(double)), "region buffer has 64 float64 values");

    idv_array_view_t view = {};
    view.struct_size = sizeof(view);
    view.data = region_info.data;
    view.byte_length = region_info.byte_length;
    view.scalar_type = region_info.scalar_type;
    view.axis_count = region_info.axis_count;
    for (uint32_t i = 0; i < view.axis_count; ++i) {
        view.axis_lengths[i] = region_info.axis_lengths[i];
    }

    idv_basic_stats_t basic_stats = {};
    require_status(stats->compute_basic_stats(stats_plugin.handle, &view, &basic_stats), IDV_STATUS_OK, "compute basic stats");
    require_true(basic_stats.finite_count == 64, "statistics finite count is 64");
    require_true(basic_stats.max_value >= basic_stats.min_value, "statistics min/max ordering");
    std::printf("stats: min=%g max=%g mean=%g stddev=%g\n",
                basic_stats.min_value,
                basic_stats.max_value,
                basic_stats.mean_value,
                basic_stats.stddev_value);

    require_status(fits->release_buffer(fits_plugin.handle, region_buffer), IDV_STATUS_OK, "release region buffer");
    require_status(fits->close_file(fits_plugin.handle, file), IDV_STATUS_OK, "close FITS file");

    unload_plugin(stats_plugin);
    unload_plugin(fits_plugin);
    std::printf("all conformance checks passed\n");
    return 0;
}
