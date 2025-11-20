#[[*
 * iDaVIE (immersive Data Visualisation Interactive Explorer)
 * Copyright (C) 2024 Inter-University Institute for Data Intensive Astronomy
 *
 * This file is part of the iDaVIE project.
 *
 * iDaVIE is free software: you can redistribute it and/or modify it under the terms 
 * of the GNU Lesser General Public License (LGPL) as published by the Free Software 
 * Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * iDaVIE is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR 
 * PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with 
 * iDaVIE in the LICENSE file. If not, see <https://www.gnu.org/licenses/>.
 *
 * Additional information and disclaimers regarding liability and third-party 
 * components can be found in the DISCLAIMER and NOTICE files included with this project.
 *
 *]]
cmake_minimum_required(VERSION 3.15)

project(native_plugins)
set(CMAKE_CXX_STANDARD 17)

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
find_library(AST_LIB_PATH libast)
get_filename_component(AST_LIB_DIR ${AST_LIB_PATH} DIRECTORY)
include_directories(${AST_INCLUDE_DIR})
link_directories(${AST_LIB_DIR})


add_library(idavie_native SHARED ast_tool.cpp ast_tool.h fits_reader.cpp fits_reader.h data_analysis_tool.cpp data_analysis_tool.h cdl_zscale.cc)


set_target_properties(idavie_native PROPERTIES CXX_STANDARD 17)

find_package(OpenMP REQUIRED)
set (CMAKE_C_FLAGS "${CMAKE_C_FLAGS} ${OpenMP_C_FLAGS}")
set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} ${OpenMP_CXX_FLAGS}")
set (CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} ${OpenMP_EXE_LINKER_FLAGS}")

target_link_libraries(idavie_native cfitsio cminpack::cminpack libast libast_err libast_pal libast_grf_5.6 libast_grf_3.2 libast_grf_2.0 libast_grf3d OpenMP::OpenMP_CXX)


SET(CMAKE_FIND_LIBRARY_PREFIXES "")
SET(CMAKE_FIND_LIBRARY_SUFFIXES ".dll")
find_library(CFITSIO_RUNTIME "cfitsio")
find_library(ZLIB_RUNTIME "zlib1")
find_library(CMINPACK_RUNTIME "cminpack")

install(TARGETS idavie_native
        RUNTIME
            DESTINATION ${PROJECT_SOURCE_DIR}/../Assets/Plugins
        )
install(FILES ${CFITSIO_RUNTIME} ${ZLIB_RUNTIME} ${CMINPACK_RUNTIME} DESTINATION ${PROJECT_SOURCE_DIR}/../Assets/Plugins)
