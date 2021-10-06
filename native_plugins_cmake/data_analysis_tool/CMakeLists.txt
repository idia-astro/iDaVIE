add_library(data_analysis_tool SHARED data_analysis_tool.cpp data_analysis_tool.h cdl_zscale.cc)
set_target_properties(data_analysis_tool PROPERTIES CXX_STANDARD 17)

find_package(OpenMP REQUIRED)
set (CMAKE_C_FLAGS "${CMAKE_C_FLAGS} ${OpenMP_C_FLAGS}")
set (CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} ${OpenMP_CXX_FLAGS}")
set (CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} ${OpenMP_EXE_LINKER_FLAGS}")
target_link_libraries(data_analysis_tool PUBLIC OpenMP::OpenMP_CXX)
