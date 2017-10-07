#ifndef CONFIGURATION_HPP_DEFINED
#define CONFIGURATION_HPP_DEFINED

#include "Common.hpp"

#include "builders/MeshCache.hpp"
#include "mapcss/StyleProvider.hpp"
#include "index/InMemoryElementStore.hpp"
#include "index/PersistentElementStore.hpp"

/// Exposes configuration API.
class Configuration {
  const int MinLevelOfDetail = 1;
  const int MaxLevelOfDetail = 16;
  const std::string CachePathPrefix = "cache/";
public:

  explicit Configuration(Context& context)
    : context_(context) {}

  /// Registers stylesheet.
  void registerStylesheet(const char *path, OnNewDirectory *directoryCallback) const {
    auto &styleProvider = context_.getStyleProvider(path);
    std::string root = context_.indexPath + CachePathPrefix + styleProvider.getTag() + '/';
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
      auto lodDir = root + utymap::utils::toString(i);
      directoryCallback(lodDir.c_str());
    }
  }

  Context &context_;
  std::unordered_map<std::string, std::unique_ptr<utymap::builders::MeshCache>> meshCaches_;
};

#endif // CONFIGURATION_HPP_DEFINED
