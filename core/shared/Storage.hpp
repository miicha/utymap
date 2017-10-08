#ifndef STORAGE_HPP_DEFINED
#define STORAGE_HPP_DEFINED

/// Wraps Storage API.
class Storage {
public:
  explicit Storage(Context& context) :
    context_(context) {}

  /// Adds data to store for specific quadkey only.
  void addToStore(const char *key,           // store key
                  const char *styleFile,     // style file
                  const char *path,          // path to data
                  int tileX,                 // tile x
                  int tileY,                 // tile y
                  int levelOfDetail,         // level of detail
                  OnError *errorCallback,    // error callback
                  utymap::CancellationToken *cancelToken) {
    utymap::QuadKey quadKey(levelOfDetail, tileX, tileY);
    ::safeExecute([&]() {
      context_.geoStore.add(key, path, quadKey, context_.getStyleProvider(styleFile), *cancelToken);
    }, errorCallback);
  }

  /// Adds data to store only for specific level of details range and bounding box.
  void addToStore(const char *key,           // store key
                  const char *styleFile,     // style file
                  const char *path,          // path to data
                  double minLat,             // minimal latitude
                  double minLon,             // minimal longitude
                  double maxLat,             // maximal latitude
                  double maxLon,             // maximal longitude
                  int startLod,              // start zoom level
                  int endLod,                // end zoom level
                  OnError *errorCallback,    // error callback
                  utymap::CancellationToken *cancelToken) {
    utymap::BoundingBox bbox(utymap::GeoCoordinate(minLat, minLon),
                             utymap::GeoCoordinate(maxLat, maxLon));
    utymap::LodRange lodRange(startLod, endLod);
    ::safeExecute([&]() {
      context_.geoStore.add(key, path, bbox, lodRange, context_.getStyleProvider(styleFile), *cancelToken);
    }, errorCallback);
  }

  /// Adds data to store for specific level of details range.
  void addToStore(const char *key,           // store key
                  const char *styleFile,     // style file
                  const char *path,          // path to data
                  int startLod,              // start zoom level
                  int endLod,                // end zoom level
                  OnError *errorCallback,    // error callback
                  utymap::CancellationToken *cancelToken) {
    utymap::LodRange lodRange(startLod, endLod);
    ::safeExecute([&]() {
      context_.geoStore.add(key, path, lodRange, context_.getStyleProvider(styleFile), *cancelToken);
    }, errorCallback);
  }

  /// Adds element to store.
  /// NOTE: relation is not yet supported.
  void addToStore(const char *key,           // store key
                  const char *styleFile,     // style file
                  std::uint64_t id,          // element id
                  const double *vertices,    // vertex array
                  int vertexLength,          // vertex array length,
                  const char **tags,          // tag array
                  int tagLength,             // tag array length
                  int startLod,              // start zoom level
                  int endLod,                // end zoom level
                  OnError *errorCallback,    // error callback
                  utymap::CancellationToken *cancellationToken) {
    utymap::LodRange lod(startLod, endLod);
    std::vector<utymap::entities::Tag> elementTags;
    elementTags.reserve(static_cast<std::size_t>(tagLength / 2));
    for (std::size_t i = 0; i < tagLength; i += 2) {
      auto keyId = getStringId(tags[i]);
      auto valueId = getStringId(tags[i + 1]);
      elementTags.push_back(utymap::entities::Tag(keyId, valueId));
    }

    // Node
    if (vertexLength / 2 == 1) {
      utymap::entities::Node node;
      node.id = id;
      node.tags = elementTags;
      node.coordinate = utymap::GeoCoordinate(vertices[0], vertices[1]);
      addToStore(key, styleFile, node, lod, errorCallback, cancellationToken);
      return;
    }

    std::vector<utymap::GeoCoordinate> coordinates;
    coordinates.reserve(static_cast<std::size_t>(vertexLength / 2));
    for (int i = 0; i < vertexLength; i += 2) {
      coordinates.push_back(utymap::GeoCoordinate(vertices[i], vertices[i + 1]));
    }

    // Way or Area
    if (coordinates[0] == coordinates[coordinates.size() - 1]) {
      utymap::entities::Area area;
      area.id = id;
      area.coordinates = coordinates;
      area.tags = elementTags;
      addToStore(key, styleFile, area, lod, errorCallback, cancellationToken);
    }
    else {
      utymap::entities::Way way;
      way.id = id;
      way.coordinates = coordinates;
      way.tags = elementTags;
      addToStore(key, styleFile, way, lod, errorCallback, cancellationToken);
    }
  }

  /// Checks whether data is present in any registered stores for given quad key.
  bool hasData(int tileX, int tileY, int levelOfDetail) const {
    return context_.geoStore.hasData(utymap::QuadKey(levelOfDetail, tileX, tileY));
  }

private:
  /// Adds element to store.
  void addToStore(const char *key,
                  const char *styleFile,
                  const utymap::entities::Element &element,
                  const utymap::LodRange &range,
                  OnError *errorCallback,
                  utymap::CancellationToken *cancelToken) {
    safeExecute([&]() {
      context_.geoStore.add(key, element, range, context_.getStyleProvider(styleFile), *cancelToken);
    }, errorCallback);
  }

  /// Gets id for the string.
  std::uint32_t getStringId(const char *str) const {
    return context_.stringTable.getId(str);
  }

  Context &context_;
};

#endif // STORAGE_HPP_DEFINED
