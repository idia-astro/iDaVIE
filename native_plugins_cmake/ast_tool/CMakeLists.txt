find_library(CFITSIO_LIBRARY libcfitsio)
find_path(CFITSIO_INCLUDE_DIR NAMES cfitsio/fitsio.h)
include_directories(${CFITSIO_INCLUDE_DIR})
include_directories(./ast/include)
link_directories(./ast/lib)

add_library(ast_tool SHARED ast_tool.cpp ast_tool.h)

set(CMAKE_VERBOSE_MAKEFILE ON)

target_link_options(ast_tool PUBLIC "-static-libgcc" "-static-libstdc++")
target_link_libraries(ast_tool ${CFITSIO_LIBRARY} ast ast_err ast_pal ast_grf_5.6 ast_grf_3.2 ast_grf_2.0 ast_grf3d m)
