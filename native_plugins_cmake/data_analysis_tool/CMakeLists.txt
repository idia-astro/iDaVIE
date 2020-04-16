

add_library(data_analysis_tool SHARED data_analysis_tool.cpp data_analysis_tool.h)
target_link_options(data_analysis_tool PUBLIC "-static-libgcc" "-static-libstdc++")

find_package(OpenMP)
if(OpenMP_CXX_FOUND)
    target_link_libraries(data_analysis_tool PUBLIC OpenMP::OpenMP_CXX)
endif()



set_target_properties(data_analysis_tool PROPERTIES
        RUNTIME_OUTPUT_DIRECTORY_DEBUG ..
        RUNTIME_OUTPUT_DIRECTORY_RELEASE ..
        )