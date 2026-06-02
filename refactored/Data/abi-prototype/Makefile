CXX ?= g++
CXXFLAGS ?= -std=c++17 -Wall -Wextra -pedantic -O2 -g
CPPFLAGS ?= -Iinclude
LDFLAGS ?=
CFITSIO_CFLAGS ?=
CFITSIO_LIBS ?= -lcfitsio

BUILD_DIR := build

.PHONY: all cfitsio-plugin test test-cfitsio clean

all: $(BUILD_DIR)/libidv_fits_lite_plugin.so $(BUILD_DIR)/libidv_stats_plugin.so $(BUILD_DIR)/plugin_harness

$(BUILD_DIR):
	mkdir -p $(BUILD_DIR)

$(BUILD_DIR)/libidv_fits_lite_plugin.so: src/fits_lite_plugin.cpp include/idv_plugin_abi.h | $(BUILD_DIR)
	$(CXX) $(CPPFLAGS) $(CXXFLAGS) -fPIC -shared $< -o $@

$(BUILD_DIR)/libidv_stats_plugin.so: src/stats_plugin.cpp include/idv_plugin_abi.h | $(BUILD_DIR)
	$(CXX) $(CPPFLAGS) $(CXXFLAGS) -fPIC -shared $< -o $@

$(BUILD_DIR)/libidv_fits_cfitsio_plugin.so: src/fits_cfitsio_plugin.cpp src/cfitsio_adapter.cpp src/cfitsio_adapter.h include/idv_plugin_abi.h | $(BUILD_DIR)
	$(CXX) $(CPPFLAGS) $(CFITSIO_CFLAGS) $(CXXFLAGS) -fPIC -shared src/fits_cfitsio_plugin.cpp src/cfitsio_adapter.cpp $(CFITSIO_LIBS) -o $@

$(BUILD_DIR)/plugin_harness: tests/plugin_harness.cpp include/idv_plugin_abi.h | $(BUILD_DIR)
	$(CXX) $(CPPFLAGS) $(CXXFLAGS) $< -ldl -o $@

cfitsio-plugin: $(BUILD_DIR)/libidv_fits_cfitsio_plugin.so

test: all
	$(BUILD_DIR)/plugin_harness ../../../Data/SampleData/test_volume.fits $(BUILD_DIR)/libidv_fits_lite_plugin.so $(BUILD_DIR)/libidv_stats_plugin.so

test-cfitsio: $(BUILD_DIR)/libidv_fits_cfitsio_plugin.so $(BUILD_DIR)/libidv_stats_plugin.so $(BUILD_DIR)/plugin_harness
	$(BUILD_DIR)/plugin_harness ../../../Data/SampleData/test_volume.fits $(BUILD_DIR)/libidv_fits_cfitsio_plugin.so $(BUILD_DIR)/libidv_stats_plugin.so

clean:
	rm -rf $(BUILD_DIR)
