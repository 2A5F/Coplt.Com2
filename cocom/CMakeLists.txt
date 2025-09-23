project(cocom)

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
)

add_library(${PROJECT_NAME} INTERFACE ${SOURCE_FILES} src/cc.cc)

add_library(${PROJECT_NAME}_shared SHARED ${SOURCE_FILES} src/cc.cc)
set_target_properties(${PROJECT_NAME}_shared PROPERTIES OUTPUT_NAME ${PROJECT_NAME})

target_include_directories(${PROJECT_NAME} INTERFACE includes)
