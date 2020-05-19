cmake_minimum_required(VERSION 3.15)

project(native_plugins)
set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -static")

add_subdirectory(data_analysis_tool)
add_subdirectory(fits_reader)
add_subdirectory(votable_reader)

install(TARGETS fits_reader data_analysis_tool votable_reader
        RUNTIME
            DESTINATION ${PROJECT_SOURCE_DIR}/../Assets/Plugins
        )
