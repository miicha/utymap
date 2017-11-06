#ifndef COMMON_H_DEFINED
#define COMMON_H_DEFINED

#include "builders/QuadKeyBuilder.hpp"
#include "heightmap/ElevationProvider.hpp"
#include "index/GeoStore.hpp"

#include <cstdint>
#include <exception>
#include <functional>

/// Callback which is called when directory should be created.
/// NOTE with C++11, directory cannot be created with header only libs.
typedef void OnNewDirectory(const char *path);

/// Callback which is called when mesh is built.
typedef void OnMeshBuilt(int tag,                                // a request tag
                         const char *name,                       // name
                         const double *vertices, int vertexSize, // vertices (x, y, elevation)
                         const int *triangles, int triSize,      // triangle indices
                         const int *colors, int colorSize,       // rgba colors
                         const double *uvs, int uvSize,          // absolute texture uvs
                         const int *uvMap, int uvMapSize);       // map with info about used atlas and texture region

/// Callback which is called when element is loaded.
typedef void OnElementLoaded(int tag,                                // a request tag
                             std::uint64_t id,                       // element id
                             const char **tags, int tagsSize,        // tags
                             const double *vertices, int vertexSize, // vertices (x, y, elevation)
                             const char **style, int styleSize);     // mapcss styles (key, value)

/// Callback which is called when error is occured.
typedef void OnError(const char *errorMessage);

/// Specifies mapping from integer to elevation data type.
enum class ElevationDataType { Flat = 0, Srtm, Grid };

/// Provides shared context properties.
struct Context {
  using StyleProviderGetter = std::function<const utymap::mapcss::StyleProvider&(const char*)>;
  using ElevationProviderGetter = std::function<const utymap::heightmap::ElevationProvider&(const utymap::QuadKey&, const ElevationDataType&)>;

  Context(const std::string& indexPath,
          const StyleProviderGetter& styleProviderGetter,
          const ElevationProviderGetter& elevationProviderGetter) :
    indexPath(indexPath),
    stringTable(indexPath),
    geoStore(stringTable),
    quadKeyBuilder(geoStore, stringTable),
    getStyleProvider(styleProviderGetter),
    getElevationProvider(elevationProviderGetter) { }

  const std::string indexPath;
  utymap::index::StringTable stringTable;
  utymap::index::GeoStore geoStore;
  utymap::builders::QuadKeyBuilder quadKeyBuilder;
  StyleProviderGetter getStyleProvider;
  ElevationProviderGetter getElevationProvider;
};

/// Executes function and catches exception if it occurs.
inline void safeExecute(const std::function<void()> &action, OnError *errorCallback) {
  try {
    action();
  }
  catch (std::exception &ex) {
    errorCallback(ex.what());
  }
}

#endif // COMMON_H_DEFINED
