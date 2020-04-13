
add_library(DebugLogPlugin SHARED DebugCpp.cpp DebugCpp.h)


set_target_properties(DebugLogPlugin PROPERTIES
        RUNTIME_OUTPUT_DIRECTORY_DEBUG ..
        RUNTIME_OUTPUT_DIRECTORY_RELEASE ..
        )