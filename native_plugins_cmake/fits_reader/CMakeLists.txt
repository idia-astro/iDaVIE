file(GLOB DEBUG_SRC_FILES
        "../DebugCpp/*.cpp"
        "./DebugCpp/*.h"
        )

find_library(CFITSIO_LIBRARY cfitsio)
find_path(CFITSIO_INCLUDE_DIR NAMES cfitsio/fitsio.h)
include_directories(${CFITSIO_INCLUDE_DIR})
add_library(fits_reader SHARED fits_reader.cpp fits_reader.h ${CFITSIO_INCLUDE_DIR} ${DEBUG_SRC_FILES})
target_link_libraries(fits_reader ${CFITSIO_LIBRARY})

set_target_properties(fits_reader PROPERTIES
        RUNTIME_OUTPUT_DIRECTORY_DEBUG ..
        RUNTIME_OUTPUT_DIRECTORY_RELEASE ..
        )