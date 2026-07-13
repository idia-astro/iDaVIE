cmake_minimum_required(VERSION 3.16)
project(idavie_plugin_tests CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Path to the built shared library and its headers.
set(PLUGIN_DIR ${CMAKE_SOURCE_DIR}/../native_plugins_cmake)
set(PLUGIN_BUILD_DIR ${PLUGIN_DIR}/build)

if (WIN32)
    # vcpkg installs cfitsio with a CMake config package; fall back to manual
    # find if running against a non-vcpkg toolchain.
    find_package(CFITSIO QUIET)
    if (NOT CFITSIO_FOUND)
        find_path(CFITSIO_INCLUDE_DIRS fitsio.h)
        find_library(CFITSIO_LIBRARIES NAMES cfitsio)
        if (NOT CFITSIO_INCLUDE_DIRS OR NOT CFITSIO_LIBRARIES)
            message(FATAL_ERROR "Could not find cfitsio. On Windows pass -DCMAKE_TOOLCHAIN_FILE=<vcpkg>/scripts/buildsystems/vcpkg.cmake")
        endif ()
    endif ()
else ()
    find_package(PkgConfig REQUIRED)
    pkg_check_modules(CFITSIO REQUIRED cfitsio)
endif ()

add_executable(test_fits_reader test_fits_reader.cpp)
target_include_directories(test_fits_reader PRIVATE
    ${PLUGIN_DIR}
    ${CFITSIO_INCLUDE_DIRS})

# The plugin build directory layout differs by generator: multi-config
# generators (MSVC) place the import library in <build>/<Config>/, single-config
# generators (Make/Ninja on Linux) place it directly in <build>/.
if (WIN32)
    target_link_directories(test_fits_reader PRIVATE
        ${PLUGIN_BUILD_DIR}/$<CONFIG>)
else ()
    target_link_directories(test_fits_reader PRIVATE
        ${PLUGIN_BUILD_DIR})
endif ()

target_link_libraries(test_fits_reader PRIVATE
    idavie_native
    ${CFITSIO_LIBRARIES})

if (NOT WIN32)
    # Starlink AST's debian package leaves the graphics back-end (astGLine,
    # astGMark, ...) undefined — host apps are expected to supply their own
    # plotting layer. The plugin never calls those symbols, so tell the linker
    # not to fail on transitively-undefined references that come in via .so
    # dependencies. This is a GNU-ld-only flag and is not needed on Windows,
    # where the AST library port supplies graphics stubs.
    target_link_options(test_fits_reader PRIVATE -Wl,--unresolved-symbols=ignore-in-shared-libs)

    # Make sure the harness can find the .so at run time without `make install`.
    set_target_properties(test_fits_reader PROPERTIES
        BUILD_RPATH "${PLUGIN_BUILD_DIR}")
else ()
    # Windows has no RPATH equivalent: copy the plugin DLL next to the .exe
    # so the loader finds it at run time.
    add_custom_command(TARGET test_fits_reader POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
            ${PLUGIN_BUILD_DIR}/$<CONFIG>/idavie_native.dll
            $<TARGET_FILE_DIR:test_fits_reader>)
endif ()
