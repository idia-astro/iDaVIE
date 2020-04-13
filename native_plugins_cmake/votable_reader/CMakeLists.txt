file(GLOB VOPARSER_SRC_FILES
        "VOTableParser/*.cpp"
        )
include_directories(VOTableParser)
find_path(XALANC_INCLUDE xalanc)
find_path(XERCESC_INCLUDE xercesc)
include_directories(${XALANC_INCLUDE})
include_directories(${XERCESC_INCLUDE})
add_library(votable_reader SHARED votable_reader.cpp votable_reader.h ${VOPARSER_SRC_FILES})
find_package(XalanC CONFIG REQUIRED)
target_link_libraries(votable_reader PRIVATE xalan_xalan-c XalanC::XalanC)


find_package(XercesC CONFIG REQUIRED)
target_link_libraries(votable_reader PRIVATE xerces_xerces-c XercesC::XercesC)

set_target_properties(votable_reader PROPERTIES
        RUNTIME_OUTPUT_DIRECTORY_DEBUG ..
        RUNTIME_OUTPUT_DIRECTORY_RELEASE ..
        )
