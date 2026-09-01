cmake_minimum_required(VERSION 3.20)
project(MurimApplication LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

add_executable(Murim WIN32
    murim_app.cpp
    ../../engine/murim_engine.cpp
)

target_include_directories(Murim PRIVATE ../../engine)

target_compile_definitions(Murim PRIVATE UNICODE _UNICODE WIN32_LEAN_AND_MEAN NOMINMAX)
target_compile_options(Murim PRIVATE /W4 /GS /sdl /utf-8)
target_link_libraries(Murim PRIVATE user32 gdi32 shell32)
if(MSVC)
    target_link_options(Murim PRIVATE /DYNAMICBASE /NXCOMPAT)
endif()

set_target_properties(Murim PROPERTIES OUTPUT_NAME "Murim")
