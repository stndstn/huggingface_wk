# TODO 1: Set the minimum required version of CMake to be 3.10
cmake_minimum_required(VERSION 3.10)

# TODO 2: Create a project named here
project(DlibWrapper)

# Tell cmake we will need dlib.  This command will pull in dlib and compile it
# into your project.  Note that you don't need to compile or install dlib.  All
# cmake needs is the dlib source code folder and it will take care of everything.
add_subdirectory(../../dlib dlib_build)

# lists the source files that will be compiled into the dynamic link library.
set(SOURCES DlibWrapper.cpp) 

add_library(DlibWrapper SHARED ${SOURCES})

# Finally, you need to tell CMake that this program, assignment_learning_ex,
# depends on dlib.  You do that with this statement: 
target_link_libraries(DlibWrapper dlib::dlib )

if(${CMAKE_VERSION} VERSION_GREATER_EQUAL "3.13.0") 
# And strip symbols to make your binary smaller if you like.  Certainly not
# required though.
    target_link_options(DlibWrapper PRIVATE $<$<CONFIG:RELEASE>:-s>)
endif()

add_compile_options(-fPIC)
