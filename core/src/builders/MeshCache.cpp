#include "builders/MeshCache.hpp"
#include "index/ElementStream.hpp"
#include "index/MeshStream.hpp"

#include <fstream>
#include <mutex>

using namespace utymap;
using namespace utymap::builders;
using namespace utymap::entities;
using namespace utymap::index;
using namespace utymap::math;
using namespace utymap::utils;

namespace {
const char ElementType = 0;
const char MeshType = 1;
}

class MeshCache::MeshCacheImpl {
  using MeshCallback = BuilderContext::MeshCallback;
  using ElementCallback = BuilderContext::ElementCallback;

 public:
  explicit MeshCacheImpl(const std::string &dataPath, const std::string &extension) :
      dataPath_(dataPath),
      extension_('.' + extension) {}

  BuilderContext wrap(const BuilderContext &context) {
    auto filePath = getFilePath(context);

    std::lock_guard<std::mutex> lock(lock_);
    return isCacheHit(context.quadKey, filePath) ? context : wrap(context, filePath);
  }

  bool fetch(const BuilderContext &context) {
    auto filePath = getFilePath(context);

    {
      std::lock_guard<std::mutex> lock(lock_);
      if (!isCacheHit(context.quadKey, filePath))
        return false;
    }

    readCache(filePath, context);

    return true;
  }

  void unwrap(const BuilderContext &context) {
    std::lock_guard<std::mutex> lock(lock_);

    auto entry = cachingQuads_.find(context.quadKey);

    if (entry==cachingQuads_.end()) return;

    if (entry->second->good())
      entry->second->close();

    // NOTE no guarantee that all data was processed and saved.
    // So it is better to delete the whole file
    if (context.cancelToken.isCancelled())
      std::remove(getFilePath(context).c_str());

    cachingQuads_.erase(entry);
  }

 private:

  /// Checks whether the data associated with given context is already cached on disk.
  bool isCacheHit(const QuadKey &quadKey, const std::string &filePath) const {
    // NOTE if quadkey is preset in collection, then caching is in progress.
    // in this case, we let app to behaviour as there is no cache at all
    if (cachingQuads_.find(quadKey) != cachingQuads_.end()) return false;
    // NOTE if file is on disk, it should not be empty.
    std::ifstream file(filePath, std::ios::binary | std::ios::ate);
    return file.good() && file.tellg() > 0;
  }

  /// Gets path to cache file on disk.
  std::string getFilePath(const BuilderContext &context) const {
    std::stringstream ss;
    ss << dataPath_ << "cache/" << context.styleProvider.getTag()
       << "/" << context.quadKey.levelOfDetail << "/"
       << GeoUtils::quadKeyToString(context.quadKey) << extension_;
    return ss.str();
  }

  BuilderContext wrap(const BuilderContext &context, const std::string &filePath) {
    auto file = std::make_shared<std::fstream>();
    file->open(filePath, std::ios::out | std::ios::binary | std::ios::app | std::ios::ate);

    cachingQuads_.insert({context.quadKey, file});

    return BuilderContext(
        context.quadKey,
        context.styleProvider,
        context.stringTable,
        context.eleProvider,
        wrap(*file, context.meshCallback),
        wrap(*file, context.elementCallback),
        context.cancelToken);
  }

  static MeshCallback wrap(std::ostream &stream, const MeshCallback &callback) {
    return [&stream, &callback](const Mesh &mesh) {
      stream << MeshType;
      MeshStream::write(stream, mesh);
      callback(mesh);
    };
  }

  static ElementCallback wrap(std::ostream &stream, const ElementCallback &callback) {
    return [&stream, &callback](const Element &element) {
      stream << ElementType;
      stream.write(reinterpret_cast<const char *>(&element.id), sizeof(element.id));
      ElementStream::write(stream, element);
      callback(element);
    };
  }

  static void readCache(std::string &filePath, const BuilderContext &context) {
    std::fstream file;
    file.open(filePath, std::ios::in | std::ios::binary | std::ios::app);
    file.seekg(0, std::ios::beg);

    while (!context.cancelToken.isCancelled()) {
      char type;
      if (!(file >> type)) break;

      if (type==MeshType)
        context.meshCallback(MeshStream::read(file));
      else if (type==ElementType) {
        std::uint64_t id;
        file.read(reinterpret_cast<char *>(&id), sizeof(id));
        context.elementCallback(*ElementStream::read(file, id));
      } else
        throw std::invalid_argument("Cannot read cache.");
    }
  }

  const std::string dataPath_;
  const std::string extension_;
  std::mutex lock_;
  std::map<QuadKey, std::shared_ptr<std::fstream>, QuadKey::Comparator> cachingQuads_;
};

MeshCache::MeshCache(const std::string &directory, const std::string &extension) :
    pimpl_(utymap::utils::make_unique<MeshCacheImpl>(directory, extension)),
    isEnabled_(true) {}

BuilderContext MeshCache::wrap(const BuilderContext &context) const {
  return isEnabled_ ? pimpl_->wrap(context) : context;
}

bool MeshCache::fetch(const BuilderContext &context) const {
  return isEnabled_ && pimpl_->fetch(context);
}

void MeshCache::unwrap(const BuilderContext &context) const {
  pimpl_->unwrap(context);
}

MeshCache::~MeshCache() {}
