#ifndef APPLICATION_HPP_DEFINED
#define APPLICATION_HPP_DEFINED

#include "Common.hpp"
#include "Configuration.hpp"
#include "Search.hpp"
#include "Storage.hpp"

#include "heightmap/FlatElevationProvider.hpp"
#include "heightmap/GridElevationProvider.hpp"
#include "heightmap/SrtmElevationProvider.hpp"
#include "mapcss/MapCssParser.hpp"

/// Composes object graph and exposes functionality as API.
class Application {
 public:

  explicit Application(const char *indexPath) :
    context_(indexPath,
             std::bind(&Application::getStyleProvider, this, std::placeholders::_1),
             std::bind(&Application::getElevationProvider, this, std::placeholders::_1, std::placeholders::_2)),
    flatEleProvider_(), srtmEleProvider_(indexPath), gridEleProvider_(indexPath),
    configuration_(context_), search_(context_), storage_(context_)
  {}

  /// Gets configuration API.
  Configuration& getConfiguration() {
    return configuration_;
  }

  /// Gets search API.
  Search& getSearch() {
    return search_;
  }

  /// Gets storage API.
  Storage& getStorage() {
    return storage_;
  }

 private:
  /// Gets registered style provider by its style path.
  const utymap::mapcss::StyleProvider &getStyleProvider(const std::string &stylePath) {
    auto pair = styleProviders_.find(stylePath);
    if (pair != styleProviders_.end())
      return *pair->second;

    std::ifstream styleFile(stylePath);
    if (!styleFile.good())
      throw std::invalid_argument(std::string("Cannot read mapcss file:") + stylePath);

    // NOTE not safe, but don't want to use boost filesystem only for this task.
    std::string dir = stylePath.substr(0, stylePath.find_last_of("\\/") + 1);
    utymap::mapcss::MapCssParser parser(dir);
    utymap::mapcss::StyleSheet stylesheet = parser.parse(styleFile);

    styleProviders_.emplace(stylePath,
      utymap::utils::make_unique<const utymap::mapcss::StyleProvider>(stylesheet, context_.stringTable));

    return *styleProviders_[stylePath];
  }

  /// Gets registered elevation provider by its type and requested quad key.
  const utymap::heightmap::ElevationProvider &getElevationProvider(const utymap::QuadKey &quadKey,
                                                                   const ElevationDataType &eleDataType) const {
    switch (eleDataType) {
      case ElevationDataType::Grid: return gridEleProvider_;
      case ElevationDataType::Srtm: return srtmEleProvider_;
      default: return flatEleProvider_;
    }
  }

  Context context_;

  utymap::heightmap::FlatElevationProvider flatEleProvider_;
  utymap::heightmap::SrtmElevationProvider srtmEleProvider_;
  utymap::heightmap::GridElevationProvider gridEleProvider_;
  std::unordered_map<std::string, std::unique_ptr<const utymap::mapcss::StyleProvider>> styleProviders_;

  Configuration configuration_;
  Search search_;
  Storage storage_;
};

#endif // APPLICATION_HPP_DEFINED
