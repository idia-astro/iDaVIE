add_library(fits_reader SHARED fits_reader.cpp fits_reader.h)
target_link_libraries(fits_reader cfitsio)