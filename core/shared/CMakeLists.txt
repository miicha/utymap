include_directories(${MAIN_SOURCE})

set(LIBRARY_NAME UtyMap.Shared)

add_library(${LIBRARY_NAME} SHARED
   Common.hpp
   Application.hpp
   Configuration.hpp
   Search.hpp
   Storage.hpp

   ExportLib.cpp
)

set_target_properties(${LIBRARY_NAME} PROPERTIES LINKER_LANGUAGE CXX)
target_link_libraries(${LIBRARY_NAME} UtyMap)
