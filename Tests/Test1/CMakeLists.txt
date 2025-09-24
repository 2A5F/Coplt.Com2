project(Test1)

set(CMAKE_CXX_STANDARD 23)

add_executable(${PROJECT_NAME} src/main.cc)
target_link_libraries(${PROJECT_NAME} PRIVATE Coplt::Com)
