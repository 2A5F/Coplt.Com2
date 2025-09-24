project(cocom VERSION 0.1.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 23)

set(TARGET_NAME Com)
set(SOURCE_FILES
        includes/CoCom.h
        includes/CoCom.Macros.h
        includes/CoCom.Guid.h
        includes/CoCom.Types.h
        includes/CoCom.Concepts.h
        includes/CoCom.Interface.h
        includes/CoCom.NullPointerError.h
        includes/CoCom.Rc.h
        includes/CoCom.Object.h
)

add_library(${TARGET_NAME} INTERFACE ${SOURCE_FILES})

#shared for debug
add_library(${TARGET_NAME}_shared SHARED ${SOURCE_FILES} src/cc.cc)
set_target_properties(${TARGET_NAME}_shared PROPERTIES OUTPUT_NAME ${PROJECT_NAME})

target_include_directories(${TARGET_NAME} INTERFACE
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/includes>
        $<INSTALL_INTERFACE:include/cocom>
)

install(DIRECTORY includes/ DESTINATION include/cocom)

add_library(Coplt::Com ALIAS ${TARGET_NAME})

install(TARGETS ${TARGET_NAME}
        EXPORT Coplt${TARGET_NAME}Targets)

install(EXPORT Coplt${TARGET_NAME}Targets
        FILE Coplt${TARGET_NAME}Targets.cmake
        NAMESPACE Coplt::
        DESTINATION lib/cmake/${PROJECT_NAME})

include(CMakePackageConfigHelpers)

write_basic_package_version_file(
        "${CMAKE_CURRENT_BINARY_DIR}/Coplt${TARGET_NAME}ConfigVersion.cmake"
        VERSION ${PROJECT_VERSION}
        COMPATIBILITY SameMajorVersion
)
configure_package_config_file(
        "${CMAKE_CURRENT_SOURCE_DIR}/cmake/Config.cmake.in"
        "${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_NAME}Config.cmake"
        INSTALL_DESTINATION lib/cmake/${PROJECT_NAME}
)
install(FILES
        "${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_NAME}Config.cmake"
        "${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_NAME}ConfigVersion.cmake"
        DESTINATION lib/cmake/${PROJECT_NAME}
)
