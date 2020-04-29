file(GLOB VOPARSER_SRC_FILES
        "VOTableParser/*.cpp"
        )
include_directories(VOTableParser)

find_path(XALANC_INCLUDE xalanc)
find_path(XERCESC_INCLUDE xercesc)
include_directories(${XALANC_INCLUDE})
include_directories(${XERCESC_INCLUDE})
add_library(votable_reader SHARED votable_reader.cpp votable_reader.h ${VOPARSER_SRC_FILES})
target_link_options(votable_reader PUBLIC "-static-libgcc" "-static-libstdc++")
set_target_properties(votable_reader
    PROPERTIES
        RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/$<CONFIG>/
)
find_library(XALAN_LIB libxalan-c)
find_library(XERCES_LIB libxerces-c)

target_link_libraries(votable_reader ${XALAN_LIB})
target_link_libraries(votable_reader ${XERCES_LIB})

