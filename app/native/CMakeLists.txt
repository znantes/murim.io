cmake_minimum_required(VERSION 3.20)
project(MurimApplication LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

add_executable(Murim WIN32
    main.cpp
    security.cpp
    ../../engine/murim_engine.cpp
    ../../engine/murim_core.c
)

target_include_directories(Murim PRIVATE ../../engine)

if (WIN32)
    target_compile_definitions(Murim PRIVATE UNICODE _UNICODE WIN32_LEAN_AND_MEAN NOMINMAX)
    target_link_libraries(Murim PRIVATE user32 gdi32 shell32)
    if (MSVC)
        # Keep warnings visible, but do not turn existing macro-compatibility warnings into build failures.
        target_compile_options(Murim PRIVATE /W4 /GS /guard:cf /sdl)
        target_link_options(Murim PRIVATE /DYNAMICBASE /NXCOMPAT /GUARD:CF)
    endif()
endif()

set_target_properties(Murim PROPERTIES OUTPUT_NAME "Murim")
