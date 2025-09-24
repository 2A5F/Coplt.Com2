project(cocom VERSION 0.1.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 23)

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

add_library(${PROJECT_NAME} INTERFACE ${SOURCE_FILES})

#shared for debug
add_library(${PROJECT_NAME}_shared SHARED ${SOURCE_FILES} src/cc.cc)
set_target_properties(${PROJECT_NAME}_shared PROPERTIES OUTPUT_NAME ${PROJECT_NAME})

target_include_directories(${PROJECT_NAME} INTERFACE
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/includes>
        $<INSTALL_INTERFACE:include>
)

install(DIRECTORY includes/ DESTINATION include)

add_library(Coplt::Com ALIAS ${PROJECT_NAME})

install(TARGETS ${PROJECT_NAME}
        EXPORT ${PROJECT_NAME}Targets)

install(EXPORT ${PROJECT_NAME}Targets
        FILE ${PROJECT_NAME}Targets.cmake
        NAMESPACE Coplt::
        DESTINATION lib/cmake/${PROJECT_NAME})

include(CMakePackageConfigHelpers)

write_basic_package_version_file(
        "${CMAKE_CURRENT_BINARY_DIR}/${PROJECT_NAME}ConfigVersion.cmake"
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
