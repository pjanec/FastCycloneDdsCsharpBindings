cmake_minimum_required(VERSION 3.16)
project(CycloneDDS.Roundtrip.Native C)

# ============================================================================
# Configuration
# ============================================================================

if(NOT DEFINED CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Release)
endif()

message(STATUS "Build Type: ${CMAKE_BUILD_TYPE}")

# Find Cyclone installation
if(NOT DEFINED CYCLONE_INSTALL_DIR)
    get_filename_component(REPO_ROOT "${CMAKE_CURRENT_SOURCE_DIR}/../../.." ABSOLUTE)
    set(CYCLONE_INSTALL_DIR "${REPO_ROOT}/cyclone-compiled")
endif()

message(STATUS "Cyclone Install: ${CYCLONE_INSTALL_DIR}")

set(IDLC_EXE "${CYCLONE_INSTALL_DIR}/bin/idlc.exe")

if(NOT EXISTS "${IDLC_EXE}")
    message(FATAL_ERROR "idlc not found at: ${IDLC_EXE}. Please build CycloneDDS first.")
endif()

# ============================================================================
# CycloneDDS Dependencies
# ============================================================================

include_directories("${CYCLONE_INSTALL_DIR}/include")
link_directories("${CYCLONE_INSTALL_DIR}/lib")

# ============================================================================
# IDL Code Generation
# ============================================================================

file(GLOB IDL_FILES "${CMAKE_CURRENT_SOURCE_DIR}/../idl/*.idl")

set(GENERATED_SOURCES "")
set(GENERATED_HEADERS "")

foreach(IDL_FILE ${IDL_FILES})
    get_filename_component(IDL_NAME ${IDL_FILE} NAME_WE)
    
    set(GEN_C "${CMAKE_CURRENT_BINARY_DIR}/generated/${IDL_NAME}.c")
    set(GEN_H "${CMAKE_CURRENT_BINARY_DIR}/generated/${IDL_NAME}.h")
    
    add_custom_command(
        OUTPUT ${GEN_C} ${GEN_H}
        COMMAND ${CMAKE_COMMAND} -E make_directory "${CMAKE_CURRENT_BINARY_DIR}/generated"
        COMMAND ${IDLC_EXE} -l c -o "${CMAKE_CURRENT_BINARY_DIR}/generated" ${IDL_FILE}
        DEPENDS ${IDL_FILE}
        COMMENT "Generating C code for ${IDL_NAME}.idl"
        VERBATIM
    )
    
    list(APPEND GENERATED_SOURCES ${GEN_C})
    list(APPEND GENERATED_HEADERS ${GEN_H})
endforeach()

message(STATUS "Generated sources: ${GENERATED_SOURCES}")

# ============================================================================
# Source Files
# ============================================================================

file(GLOB NATIVE_SOURCES 
    "src/*.c"
)

file(GLOB HANDLER_SOURCES
    "src/handlers/*.c"
)

set(ALL_SOURCES
    ${NATIVE_SOURCES}
    ${HANDLER_SOURCES}
    ${GENERATED_SOURCES}
)

# ============================================================================
# Build Native DLL
# ============================================================================

add_library(CycloneDDS.Roundtrip.Native SHARED ${ALL_SOURCES})

target_include_directories(CycloneDDS.Roundtrip.Native PRIVATE
    "${CMAKE_CURRENT_SOURCE_DIR}/src"
    "${CMAKE_CURRENT_BINARY_DIR}/generated"
)

target_link_libraries(CycloneDDS.Roundtrip.Native PRIVATE ddsc)

# ============================================================================
# Platform-Specific Settings
# ============================================================================

if(WIN32)
    # Export all symbols on Windows
    set_target_properties(CycloneDDS.Roundtrip.Native PROPERTIES
        WINDOWS_EXPORT_ALL_SYMBOLS ON
    )
    
    # Add DLL export macro
    target_compile_definitions(CycloneDDS.Roundtrip.Native PRIVATE
        BUILDING_DLL=1
    )
endif()

# C11 standard
set_property(TARGET CycloneDDS.Roundtrip.Native PROPERTY C_STANDARD 11)

# ============================================================================
# Installation
# ============================================================================

install(TARGETS CycloneDDS.Roundtrip.Native
    RUNTIME DESTINATION bin
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
)

# ============================================================================
# Debug Info
# ============================================================================

message(STATUS "========================================")
message(STATUS "Roundtrip Native Library Configuration")
message(STATUS "========================================")
message(STATUS "Build Type:       ${CMAKE_BUILD_TYPE}")
message(STATUS "Cyclone Dir:      ${CYCLONE_INSTALL_DIR}")
message(STATUS "IDLC Path:        ${IDLC_EXE}")
message(STATUS "Generated Dir:    ${CMAKE_CURRENT_BINARY_DIR}/generated")
message(STATUS "Source Files:     ${NATIVE_SOURCES}")
message(STATUS "Handler Files:    ${HANDLER_SOURCES}")
message(STATUS "========================================")
