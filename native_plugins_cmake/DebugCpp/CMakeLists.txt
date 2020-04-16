
add_library(DebugLogPlugin SHARED DebugCpp.cpp DebugCpp.h)

target_link_options(DebugLogPlugin PUBLIC "-static-libgcc" "-static-libstdc++")

set_target_properties(DebugLogPlugin PROPERTIES
        RUNTIME_OUTPUT_DIRECTORY_DEBUG ..
        RUNTIME_OUTPUT_DIRECTORY_RELEASE ..
        )