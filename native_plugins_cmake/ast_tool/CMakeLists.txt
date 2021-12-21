add_library(ast_tool SHARED ast_tool.cpp ast_tool.h)
target_link_libraries(ast_tool cfitsio cminpack::cminpack ast ast_err ast_pal ast_grf_5.6 ast_grf_3.2 ast_grf_2.0 ast_grf3d)
