cmake_minimum_required(VERSION 3.15)

#set(VCPKG_TARGET_TRIPLET x64-windows)
#set(VCPKG_APPLOCAL_DEPS false)

#if(DEFINED ENV{VCPKG_ROOT} AND NOT DEFINED CMAKE_TOOLCHAIN_FILE)
#    set(CMAKE_TOOLCHAIN_FILE "$ENV{VCPKG_ROOT}/scripts/buildsystems/vcpkg.cmake"
#            CACHE STRING "")
#endif()

project(native_plugins)
set(CMAKE_CXX_STANDARD 14)

add_subdirectory(DebugCpp)
add_subdirectory(data_analysis_tool)
add_subdirectory(fits_reader)
add_subdirectory(votable_reader)


