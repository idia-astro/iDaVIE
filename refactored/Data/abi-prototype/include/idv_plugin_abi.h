#ifndef IDV_PLUGIN_ABI_H
#define IDV_PLUGIN_ABI_H

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

#if defined(_WIN32)
#define IDV_CALL __cdecl
#define IDV_EXPORT __declspec(dllexport)
#define IDV_IMPORT __declspec(dllimport)
#else
#define IDV_CALL
#define IDV_EXPORT __attribute__((visibility("default")))
#define IDV_IMPORT
#endif

#ifdef IDV_BUILDING_PLUGIN
#define IDV_API IDV_EXPORT
#else
#define IDV_API IDV_IMPORT
#endif

#define IDV_CONTRACT_MAJOR_CURRENT 2
#define IDV_CONTRACT_MINOR_CURRENT 0
#define IDV_CONTRACT_PATCH_CURRENT 0
#define IDV_MAX_AXES 8

typedef enum idv_status_e : int32_t {
    IDV_STATUS_OK = 0,
    IDV_STATUS_CANCELLED = 1,
    IDV_STATUS_INVALID_ARGUMENT = -1,
    IDV_STATUS_NOT_FOUND = -2,
    IDV_STATUS_OUT_OF_MEMORY = -3,
    IDV_STATUS_IO_ERROR = -4,
    IDV_STATUS_UNSUPPORTED = -5,
    IDV_STATUS_PRECONDITION_FAILED = -6,
    IDV_STATUS_VERSION_MISMATCH = -7,
    IDV_STATUS_CAPABILITY_CONFLICT = -8,
    IDV_STATUS_THREAD_AFFINITY_ERROR = -9,
    IDV_STATUS_FORMAT_ERROR = -10,
    IDV_STATUS_PLUGIN_INTERNAL_ERROR = -100
} idv_status_t;

typedef enum idv_capability_id_e : uint32_t {
    IDV_CAPABILITY_NONE = 0,
    IDV_CAPABILITY_FITS_IO = 0x46495453u,
    IDV_CAPABILITY_WCS_TRANSFORM = 0x57435320u,
    IDV_CAPABILITY_DATA_ANALYSIS = 0x44415441u,
    IDV_CAPABILITY_FITS_EXTENSIONS = 0x46495458u,
    IDV_CAPABILITY_TIFF_EXTENSIONS = 0x54494658u,
    IDV_CAPABILITY_VOTABLE_EXTENSIONS = 0x564F5458u,
    IDV_CAPABILITY_STATISTICS = 0x53544154u,
    IDV_CAPABILITY_DOWNSAMPLING = 0x44534D50u,
    IDV_CAPABILITY_SOURCE_FINDING = 0x53524346u
} idv_capability_id_t;

typedef enum idv_log_level_e : int32_t {
    IDV_LOG_LEVEL_TRACE = 0,
    IDV_LOG_LEVEL_DEBUG = 1,
    IDV_LOG_LEVEL_INFO = 2,
    IDV_LOG_LEVEL_WARN = 3,
    IDV_LOG_LEVEL_ERROR = 4,
    IDV_LOG_LEVEL_FATAL = 5
} idv_log_level_t;

typedef struct idv_version_s {
    uint16_t major;
    uint16_t minor;
    uint16_t patch;
    uint16_t _reserved;
} idv_version_t;

typedef void* idv_plugin_handle_t;
typedef void* idv_fits_file_handle_t;
typedef void* idv_buffer_handle_t;
typedef void* idv_cancellation_token_t;

typedef enum idv_scalar_type_e : int32_t {
    IDV_SCALAR_FLOAT32 = 1,
    IDV_SCALAR_FLOAT64 = 2,
    IDV_SCALAR_INT16 = 3,
    IDV_SCALAR_INT32 = 4,
    IDV_SCALAR_UINT8 = 5,
    IDV_SCALAR_INT64 = 6,
    IDV_SCALAR_INT8 = 7
} idv_scalar_type_t;

typedef enum idv_dataset_kind_e : int32_t {
    IDV_DATASET_IMAGE = 1,
    IDV_DATASET_VOLUME = 2,
    IDV_DATASET_TABLE = 3,
    IDV_DATASET_MIXED = 4
} idv_dataset_kind_t;

typedef enum idv_open_mode_e : int32_t {
    IDV_OPEN_READ_ONLY = 0,
    IDV_OPEN_READ_WRITE = 1
} idv_open_mode_t;

typedef struct idv_host_services_s {
    uint32_t struct_size;
    idv_version_t host_version;
    idv_version_t contract_version;
    void (IDV_CALL *log)(idv_log_level_t level, const char* category, const char* message_utf8);
    void* (IDV_CALL *alloc)(size_t bytes, size_t alignment);
    void (IDV_CALL *free)(void* ptr);
    bool (IDV_CALL *is_cancelled)(idv_cancellation_token_t token);
} idv_host_services_t;

typedef struct idv_capability_descriptor_s {
    uint32_t struct_size;
    idv_capability_id_t id;
    idv_version_t capability_version;
    uint32_t function_table_size;
    const void* function_table;
} idv_capability_descriptor_t;

typedef struct idv_plugin_manifest_s {
    uint32_t struct_size;
    idv_version_t contract_version;
    idv_version_t plugin_version;
    const char* plugin_id;
    const char* display_name;
    const char* vendor;
    const char* license_spdx;
    uint32_t capability_count;
    const idv_capability_descriptor_t* capabilities;
    idv_status_t (IDV_CALL *initialise)(const idv_host_services_t* host, idv_plugin_handle_t* out_plugin);
    idv_status_t (IDV_CALL *shutdown)(idv_plugin_handle_t plugin);
} idv_plugin_manifest_t;

typedef struct idv_open_options_s {
    uint32_t struct_size;
    idv_open_mode_t mode;
    uint32_t selected_part;
    uint32_t flags;
} idv_open_options_t;

typedef struct idv_dataset_info_s {
    uint32_t struct_size;
    idv_dataset_kind_t kind;
    idv_scalar_type_t scalar_type;
    uint32_t axis_count;
    int64_t axis_lengths[IDV_MAX_AXES];
    uint32_t part_count;
    uint32_t capability_flags;
} idv_dataset_info_t;

typedef struct idv_region_request_s {
    uint32_t struct_size;
    uint32_t part_index;
    uint32_t axis_count;
    int64_t start[IDV_MAX_AXES];
    int64_t end[IDV_MAX_AXES];
    idv_scalar_type_t output_type;
} idv_region_request_t;

typedef struct idv_buffer_info_s {
    uint32_t struct_size;
    void* data;
    int64_t byte_length;
    idv_scalar_type_t scalar_type;
    uint32_t axis_count;
    int64_t axis_lengths[IDV_MAX_AXES];
} idv_buffer_info_t;

typedef struct idv_array_view_s {
    uint32_t struct_size;
    const void* data;
    int64_t byte_length;
    idv_scalar_type_t scalar_type;
    uint32_t axis_count;
    int64_t axis_lengths[IDV_MAX_AXES];
    int64_t strides_bytes[IDV_MAX_AXES];
} idv_array_view_t;

typedef struct idv_basic_stats_s {
    uint32_t struct_size;
    double min_value;
    double max_value;
    double mean_value;
    double stddev_value;
    int64_t finite_count;
    int64_t nan_count;
} idv_basic_stats_t;

typedef struct idv_fits_io_v1_s {
    uint32_t struct_size;
    idv_status_t (IDV_CALL *open_file)(idv_plugin_handle_t plugin, const char* path_utf8, int32_t path_byte_length, const idv_open_options_t* options, idv_fits_file_handle_t* out_file);
    idv_status_t (IDV_CALL *close_file)(idv_plugin_handle_t plugin, idv_fits_file_handle_t file);
    idv_status_t (IDV_CALL *get_dataset_info)(idv_plugin_handle_t plugin, idv_fits_file_handle_t file, idv_dataset_info_t* out_info);
    idv_status_t (IDV_CALL *read_header)(idv_plugin_handle_t plugin, idv_fits_file_handle_t file, idv_buffer_handle_t* out_header_cards);
    idv_status_t (IDV_CALL *read_region_f32)(idv_plugin_handle_t plugin, idv_fits_file_handle_t file, const idv_region_request_t* request, float** out_voxels, int64_t* out_voxel_count);
    idv_status_t (IDV_CALL *read_region_typed)(idv_plugin_handle_t plugin, idv_fits_file_handle_t file, const idv_region_request_t* request, idv_buffer_handle_t* out_buffer);
    idv_status_t (IDV_CALL *read_table_column)(idv_plugin_handle_t plugin, idv_fits_file_handle_t file, uint32_t column_index, idv_buffer_handle_t* out_column_data);
    idv_status_t (IDV_CALL *write_mask_subcube)(idv_plugin_handle_t plugin, idv_fits_file_handle_t file, const idv_region_request_t* region, const int16_t* mask_data, int64_t mask_voxel_count);
    idv_status_t (IDV_CALL *free_image_f32)(idv_plugin_handle_t plugin, float* voxels);
    idv_status_t (IDV_CALL *get_buffer_info)(idv_plugin_handle_t plugin, idv_buffer_handle_t buffer, idv_buffer_info_t* out_info);
    idv_status_t (IDV_CALL *release_buffer)(idv_plugin_handle_t plugin, idv_buffer_handle_t buffer);
} idv_fits_io_v1_t;

typedef struct idv_statistics_v1_s {
    uint32_t struct_size;
    idv_status_t (IDV_CALL *compute_basic_stats)(idv_plugin_handle_t plugin, const idv_array_view_t* input, idv_basic_stats_t* out_stats);
    idv_status_t (IDV_CALL *compute_histogram)(idv_plugin_handle_t plugin, const idv_array_view_t* input, const void* request, idv_buffer_handle_t* out_histogram);
    idv_status_t (IDV_CALL *compute_percentiles)(idv_plugin_handle_t plugin, const idv_array_view_t* input, const double* percentiles, uint32_t percentile_count, double* out_values);
    idv_status_t (IDV_CALL *extract_profile)(idv_plugin_handle_t plugin, const idv_array_view_t* input, const void* request, idv_buffer_handle_t* out_profile);
    idv_status_t (IDV_CALL *compute_zscale)(idv_plugin_handle_t plugin, const idv_array_view_t* input, void* out_zscale);
    idv_status_t (IDV_CALL *get_buffer_info)(idv_plugin_handle_t plugin, idv_buffer_handle_t buffer, idv_buffer_info_t* out_info);
    idv_status_t (IDV_CALL *release_buffer)(idv_plugin_handle_t plugin, idv_buffer_handle_t buffer);
} idv_statistics_v1_t;

typedef const idv_plugin_manifest_t* (IDV_CALL *idv_plugin_entry_fn)(void);

IDV_API const idv_plugin_manifest_t* IDV_CALL idv_plugin_entry(void);

#ifdef __cplusplus
}
#endif

#endif
