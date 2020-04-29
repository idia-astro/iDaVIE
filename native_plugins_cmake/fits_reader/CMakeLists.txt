find_library(CFITSIO_LIBRARY libcfitsio)
find_path(CFITSIO_INCLUDE_DIR NAMES cfitsio/fitsio.h)
include_directories(${CFITSIO_INCLUDE_DIR})
add_library(fits_reader SHARED fits_reader.cpp fits_reader.h)
set_target_properties(fits_reader
    PROPERTIES
        RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/$<CONFIG>/
)
target_link_options(fits_reader PUBLIC "-static-libgcc" "-static-libstdc++")
target_link_libraries(fits_reader ${CFITSIO_LIBRARY})
