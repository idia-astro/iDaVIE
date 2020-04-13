
file(GLOB DEBUG_SRC_FILES
        "../DebugCpp/*.cpp"
        "./DebugCpp/*.h"
        )

add_library(data_analysis_tool SHARED data_analysis_tool.cpp data_analysis_tool.h ${DEBUG_SRC_FILES})

find_package(OpenMP)
if(OpenMP_CXX_FOUND)
    target_link_libraries(data_analysis_tool PUBLIC OpenMP::OpenMP_CXX)
endif()



set_target_properties(data_analysis_tool PROPERTIES
        RUNTIME_OUTPUT_DIRECTORY_DEBUG ..
        RUNTIME_OUTPUT_DIRECTORY_RELEASE ..
        )