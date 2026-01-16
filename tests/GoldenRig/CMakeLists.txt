cmake_minimum_required(VERSION 3.16)
project(GoldenRig C)

# Set Cyclone DDS paths
set(CYCLONE_ROOT "D:/Work/FastCycloneDdsCsharpBindings")
set(CYCLONE_CORE_INCLUDE "${CYCLONE_ROOT}/cyclonedds/src/core/ddsc/include")
set(CYCLONE_CORE_SRC_INCLUDE "${CYCLONE_ROOT}/cyclonedds/src/core/ddsc/src")
set(CYCLONE_DDSI_INCLUDE "${CYCLONE_ROOT}/cyclonedds/src/core/ddsi/include")
set(CYCLONE_CDR_INCLUDE "${CYCLONE_ROOT}/cyclonedds/src/core/cdr/include")
set(CYCLONE_RT_INCLUDE "${CYCLONE_ROOT}/cyclonedds/src/ddsrt/include")
set(CYCLONE_LIB_DIR "${CYCLONE_ROOT}/cyclone-bin/Release")
set(LOCAL_INCLUDE "${CMAKE_CURRENT_SOURCE_DIR}/include")

include_directories(
    ${CYCLONE_CORE_INCLUDE}
    ${CYCLONE_CORE_SRC_INCLUDE}
    ${CYCLONE_DDSI_INCLUDE}
    ${CYCLONE_CDR_INCLUDE}
    ${CYCLONE_RT_INCLUDE}
    ${LOCAL_INCLUDE}
)
link_directories(${CYCLONE_LIB_DIR})

add_executable(golden_gen golden_data_generator.c Golden.c)

# Link against ddsc.lib
target_link_libraries(golden_gen ddsc)
