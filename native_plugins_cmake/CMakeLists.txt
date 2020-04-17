cmake_minimum_required(VERSION 3.15)

project(native_plugins)
set(CMAKE_CXX_STANDARD 14)

add_subdirectory(data_analysis_tool)
add_subdirectory(fits_reader)
add_subdirectory(votable_reader)

