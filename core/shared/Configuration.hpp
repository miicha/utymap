#ifndef CONFIGURATION_HPP_DEFINED
#define CONFIGURATION_HPP_DEFINED

#include "Common.hpp"

#include "builders/CacheBuilder.hpp"
#include "builders/MeshCache.hpp"
#include "builders/misc/LampBuilder.hpp"
#include "builders/misc/BarrierBuilder.hpp"
#include "builders/poi/TreeBuilder.hpp"
#include "builders/buildings/BuildingBuilder.hpp"
#include "builders/terrain/TerraBuilder.hpp"
#include "mapcss/StyleProvider.hpp"
#include "index/InMemoryElementStore.hpp"
#include "index/PersistentElementStore.hpp"

/// Exposes configuration API.
class Configuration {
  const int MinLevelOfDetail = 1;
  const int MaxLevelOfDetail = 16;
public:
  explicit Configuration(Context& context) : context_(context) {
    registerDefaultBuilders();
  }

  /// Registers stylesheet.
  void registerStylesheet(const char *path, OnNewDirectory *directoryCallback) const {
    auto &styleProvider = context_.getStyleProvider(path);
    std::string root = context_.indexPath + "/cache/" + styleProvider.getTag() + '/';
    createDataDirs(root, directoryCallback);
  }

  /// Registers new in-memory store.
  void registerInMemoryStore(const char *key) const {
    context_.geoStore.registerStore(key,
      utymap::utils::make_unique<utymap::index::InMemoryElementStore>(context_.stringTable));
  }

  /// Registers new persistent store.
  void registerPersistentStore(const char *key, const char *dataPath, OnNewDirectory *directoryCallback) const {
    context_.geoStore.registerStore(key,
      utymap::utils::make_unique<utymap::index::PersistentElementStore>(dataPath, context_.stringTable));
    createDataDirs(dataPath, directoryCallback);
  }

  /// Enables or disables mesh caching.
  void enableMeshCache(int enabled) {
    for (const auto &entry : meshCaches_) {
      if (enabled) entry.second->enable();
      else entry.second->disable();
    }
  }

private:
  /// Creates map data directories in given root directory.
  void createDataDirs(const std::string &root, OnNewDirectory *directoryCallback) const {
    for (int i = MinLevelOfDetail; i <= MaxLevelOfDetail; ++i) {
      auto lodDir = root + "/" + utymap::utils::toString(i);
      directoryCallback(lodDir.c_str());
    }
  }

  /// Registers default builders.
  void registerDefaultBuilders() {
    registerBuilder<utymap::builders::TerraBuilder>("terrain", true);
    registerBuilder<utymap::builders::BuildingBuilder>("building");
    registerBuilder<utymap::builders::TreeBuilder>("tree");
    registerBuilder<utymap::builders::BarrierBuilder>("barrier");
    registerBuilder<utymap::builders::LampBuilder>("lamp");
  }

  template<typename Builder>
  void registerBuilder(const std::string &name, bool useCache = false) {
    if (useCache)
      meshCaches_.emplace(name, utymap::utils::make_unique<utymap::builders::MeshCache>(context_.indexPath, name));

    context_.quadKeyBuilder.registerElementBuilder(name,
      useCache ? createCacheFactory<Builder>(name) : createFactory<Builder>());
  }

  template<typename Builder>
  utymap::builders::QuadKeyBuilder::ElementBuilderFactory createFactory() const {
    return [](const utymap::builders::BuilderContext &context) {
      return utymap::utils::make_unique<Builder>(context);
    };
  }

  template<typename Builder>
  utymap::builders::QuadKeyBuilder::ElementBuilderFactory createCacheFactory(const std::string &name) const {
    auto &meshCache = *meshCaches_.find(name)->second;
    return [&, name](const utymap::builders::BuilderContext &context) {
      return utymap::utils::make_unique<utymap::builders::CacheBuilder<Builder>>(meshCache, context);
    };
  }

  Context &context_;
  std::unordered_map<std::string, std::unique_ptr<utymap::builders::MeshCache>> meshCaches_;
};

#endif // CONFIGURATION_HPP_DEFINED
