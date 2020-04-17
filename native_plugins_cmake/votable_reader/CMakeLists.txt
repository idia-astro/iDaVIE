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

find_library(XALAN_LIB libxalan-c)
find_library(XERCES_LIB libxerces-c)

target_link_libraries(votable_reader ${XALAN_LIB})
target_link_libraries(votable_reader ${XERCES_LIB})

#find_library(CURL_LIB libcurl)
#find_library(WINPTHREAD_LIB libwinpthreads)
#find_library(STDCPP_LIB libstdc++)

#file(COPY ${CURL_LIB} DESTINATION ${${CMAKE_PROJECT_NAME}_BINARY_DIR})
#file(COPY ${WINPTHREAD_LIB} DESTINATION ${${CMAKE_PROJECT_NAME}_BINARY_DIR})
#file(COPY ${XALAN_LIB} DESTINATION ${${CMAKE_PROJECT_NAME}_BINARY_DIR})
#file(COPY ${XERCES_LIB} DESTINATION ${${CMAKE_PROJECT_NAME}_BINARY_DIR})
#file(COPY ${STDCPP_LIB} DESTINATION ${${CMAKE_PROJECT_NAME}_BINARY_DIR})
