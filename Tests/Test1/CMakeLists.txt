project(Test1)

set(CMAKE_CXX_STANDARD 23)

add_executable(${PROJECT_NAME} src/main.cc)

#list(APPEND CMAKE_PREFIX_PATH "${CMAKE_SOURCE_DIR}/install")
#message(STATUS "CMAKE_PREFIX_PATH = ${CMAKE_PREFIX_PATH}")
#find_package(cocom CONFIG REQUIRED)

target_link_libraries(${PROJECT_NAME} PRIVATE Coplt::Com)
