#include "idv_plugin_abi.h"

#include <cmath>
#include <cstring>
#include <limits>
#include <new>

struct idv_plugin_t {
    const idv_host_services_t* services;
};

namespace {

const char* kLastError = "";

double value_at(const idv_array_view_t* input, int64_t index)
{
    switch (input->scalar_type) {
    case IDV_SCALAR_FLOAT32:
        return static_cast<const float*>(input->data)[index];
    case IDV_SCALAR_FLOAT64:
        return static_cast<const double*>(input->data)[index];
    case IDV_SCALAR_INT16:
        return static_cast<const int16_t*>(input->data)[index];
    case IDV_SCALAR_INT32:
        return static_cast<const int32_t*>(input->data)[index];
    case IDV_SCALAR_INT64:
        return static_cast<double>(static_cast<const int64_t*>(input->data)[index]);
    case IDV_SCALAR_UINT8:
        return static_cast<const uint8_t*>(input->data)[index];
    default:
        return std::numeric_limits<double>::quiet_NaN();
    }
}

int64_t element_count(const idv_array_view_t* input)
{
    int64_t count = 1;
    for (uint32_t i = 0; i < input->axis_count; ++i) {
        count *= input->axis_lengths[i];
    }
    return count;
}

idv_status_t IDV_CALL compute_basic_stats(idv_plugin_handle_t, const idv_array_view_t* input, idv_basic_stats_t* out_stats)
{
    if (!input || !input->data || !out_stats || input->axis_count == 0 || input->axis_count > 8) {
        kLastError = "Invalid statistics input";
        return IDV_STATUS_INVALID_ARGUMENT;
    }

    const int64_t count = element_count(input);
    if (count == 0) {
        kLastError = "Statistics input is empty";
        return IDV_STATUS_INVALID_ARGUMENT;
    }

    double min_value = std::numeric_limits<double>::infinity();
    double max_value = -std::numeric_limits<double>::infinity();
    double sum = 0.0;
    double sum_sq = 0.0;
    int64_t finite_count = 0;
    int64_t nan_count = 0;

    for (int64_t i = 0; i < count; ++i) {
        const double value = value_at(input, i);
        if (!std::isfinite(value)) {
            ++nan_count;
            continue;
        }
        min_value = std::min(min_value, value);
        max_value = std::max(max_value, value);
        sum += value;
        sum_sq += value * value;
        ++finite_count;
    }

    if (finite_count == 0) {
        kLastError = "Statistics input has no finite values";
        return IDV_STATUS_FORMAT_ERROR;
    }

    const double mean = sum / static_cast<double>(finite_count);
    const double variance = std::max(0.0, (sum_sq / static_cast<double>(finite_count)) - mean * mean);

    std::memset(out_stats, 0, sizeof(*out_stats));
    out_stats->struct_size = sizeof(*out_stats);
    out_stats->min_value = min_value;
    out_stats->max_value = max_value;
    out_stats->mean_value = mean;
    out_stats->stddev_value = std::sqrt(variance);
    out_stats->finite_count = finite_count;
    out_stats->nan_count = nan_count;
    kLastError = "";
    return IDV_STATUS_OK;
}

idv_status_t IDV_CALL unsupported_buffer_info(idv_plugin_handle_t, idv_buffer_handle_t, idv_buffer_info_t*)
{
    return IDV_STATUS_UNSUPPORTED;
}

idv_status_t IDV_CALL unsupported_release_buffer(idv_plugin_handle_t, idv_buffer_handle_t)
{
    return IDV_STATUS_UNSUPPORTED;
}

idv_statistics_v1_t kStatsApi = {
    sizeof(idv_statistics_v1_t),
    compute_basic_stats,
    nullptr,
    nullptr,
    nullptr,
    nullptr,
    unsupported_buffer_info,
    unsupported_release_buffer
};

const idv_capability_descriptor_t kCapabilities[] = {
    {
        sizeof(idv_capability_descriptor_t),
        IDV_CAPABILITY_STATISTICS,
        {1, 0, 0, 0},
        sizeof(idv_statistics_v1_t),
        &kStatsApi
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
    "idavie.refactoring.stats",
    "iDaVIE statistics operation plug-in",
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
