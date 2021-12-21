cmake_minimum_required(VERSION 3.15)

project(native_plugins)
set(CMAKE_CXX_STANDARD 14)

IF (WIN32)
    find_package(unofficial-cfitsio CONFIG REQUIRED)
ELSE ()
    find_package(CFITSIO QUIET)
    if (CFITSIO_FOUND)
        message(STATUS "Found cfitsio using find_package")
    else ()
        find_package(PkgConfig REQUIRED)
        pkg_search_module(CFITSIO REQUIRED cfitsio)
        if (CFITSIO_FOUND)
            message(STATUS "Found cfitsio using pkg-config")
        else ()
            message(FATAL_ERROR "Could not find cfitsio")
        endif ()
    endif()
ENDIF ()

include_directories(${CFITSIO_INCLUDE_DIR})

find_package(cminpack CONFIG REQUIRED)

find_path(AST_INCLUDE_DIR ast.h)
find_library(AST_LIB_PATH ast)
get_filename_component(AST_LIB_DIR ${AST_LIB_PATH} DIRECTORY)
include_directories(${AST_INCLUDE_DIR})
link_directories(${AST_LIB_DIR})

add_subdirectory(data_analysis_tool)
add_subdirectory(fits_reader)
add_subdirectory(ast_tool)

SET(CMAKE_FIND_LIBRARY_PREFIXES "")
SET(CMAKE_FIND_LIBRARY_SUFFIXES ".dll")
find_library(CFITSIO_RUNTIME "cfitsio")
find_library(ZLIB_RUNTIME "zlib1")
find_library(CMINPACK_RUNTIME "cminpack")

install(TARGETS fits_reader data_analysis_tool ast_tool
        RUNTIME
            DESTINATION ${PROJECT_SOURCE_DIR}/../Assets/Plugins
        )
install(FILES ${CFITSIO_RUNTIME} ${ZLIB_RUNTIME} ${CMINPACK_RUNTIME} DESTINATION ${PROJECT_SOURCE_DIR}/../Assets/Plugins)
