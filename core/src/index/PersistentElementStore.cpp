#include "index/BitmapStream.hpp"
#include "index/ElementStream.hpp"
#include "index/PersistentElementStore.hpp"
#include "index/StringIndex.hpp"
#include "utils/LruCache.hpp"

#include <fstream>
#include <mutex>
#include <tuple>

using namespace utymap;
using namespace utymap::index;
using namespace utymap::entities;
using namespace utymap::mapcss;
using namespace utymap::utils;

namespace {
const std::string IndexFileExtension = ".idf";
const std::string DataFileExtension = ".dat";
const std::string bitmapFileExtension = ".bmp";

struct BitmapData {
  const std::string path;
  StringIndex::Bitmap data;

  BitmapData(const std::string &bitmapPath) :
    path(bitmapPath) {}

  BitmapData(BitmapData &&other) :
    path(std::move(other.path)),
    data(std::move(other.data)) {}
};

/// Stores file handlers related to data of specific quad key.
struct QuadKeyData {
  std::unique_ptr<std::fstream> dataFile;
  std::unique_ptr<std::fstream> indexFile;

  QuadKeyData(const std::string &dataPath,
              const std::string &indexPath,
              const std::string &bitmapPath) :
      dataFile(utymap::utils::make_unique<std::fstream>()),
      indexFile(utymap::utils::make_unique<std::fstream>()),
      bitmapData_(utymap::utils::make_unique<BitmapData>(bitmapPath)) {
    using std::ios;
    dataFile->open(dataPath, ios::in | ios::out | ios::binary | ios::app | ios::ate);
    indexFile->open(indexPath, ios::in | ios::out | ios::binary | ios::app | ios::ate);
  }

  BitmapData& getBitmap() const {
    if (bitmapData_->data.empty()) {
      // TODO not thread safe!
      std::fstream bitmapFile;
      bitmapFile.open(bitmapData_->path, std::ios::in | std::ios::binary);
      BitmapStream::read(bitmapFile, bitmapData_->data);
    }
    return *bitmapData_;
  }

  QuadKeyData(const QuadKeyData &) = delete;
  QuadKeyData &operator=(const QuadKeyData &) = delete;

  QuadKeyData(QuadKeyData &&other) :
      dataFile(std::move(other.dataFile)),
      indexFile(std::move(other.indexFile)),
      bitmapData_(std::move(other.bitmapData_)) {}

  ~QuadKeyData() {
    if (dataFile!=nullptr && dataFile->good()) dataFile->close();
    if (indexFile!=nullptr && indexFile->good()) indexFile->close();
  }
private:
  std::unique_ptr<BitmapData> bitmapData_;
};
}

using Cache = utymap::utils::LruCache<QuadKey, QuadKeyData, QuadKey::Comparator>;

// TODO improve thread safety!
class PersistentElementStore::PersistentElementStoreImpl: StringIndex {
 public:
  PersistentElementStoreImpl(const std::string &dataPath,
                             const StringTable &stringTable):
    StringIndex(stringTable),
    dataPath_(dataPath),
    lock_(),
    cache_(12) {}

  void store(const Element &element, const QuadKey &quadKey) {
    const auto &quadKeyData = getQuadKeyData(quadKey);
    auto offset = static_cast<std::uint32_t>(quadKeyData.dataFile->tellg());
    auto order = quadKeyData.indexFile->tellg() / sizeof(element.id);

    // write element index
    quadKeyData.indexFile->seekg(0, std::ios::end);
    quadKeyData.indexFile->write(reinterpret_cast<const char *>(&element.id), sizeof(element.id));
    quadKeyData.indexFile->write(reinterpret_cast<const char *>(&offset), sizeof(offset));

    // write element data
    quadKeyData.dataFile->seekg(0, std::ios::end);
    ElementStream::write(*quadKeyData.dataFile, element);

    // write element search data
    add(element, quadKey, static_cast<std::uint32_t>(order));
    // TODO we always clean/write the whole file here.
    std::fstream bitmapFile(quadKeyData.getBitmap().path, std::ios::out | std::ios::binary | std::ios::trunc);
    BitmapStream::write(bitmapFile, quadKeyData.getBitmap().data);
  }

  void search(const StringIndex::Query query,
              ElementVisitor &visitor,
              const utymap::CancellationToken &cancelToken) {
    StringIndex::search(query, visitor);
  }

  void search(const QuadKey &quadKey,
              ElementVisitor &visitor,
              const utymap::CancellationToken &cancelToken) {
    const auto &quadKeyData = getQuadKeyData(quadKey);
    auto count = static_cast<std::uint32_t>(quadKeyData.indexFile->tellg() /
        (sizeof(std::uint64_t) + sizeof(std::uint32_t)));

    quadKeyData.indexFile->seekg(0, std::ios::beg);
    for (std::uint32_t i = 0; i < count; ++i) {
      if (cancelToken.isCancelled()) break;

      auto entry = readIndexEntry(quadKeyData);
      quadKeyData.dataFile->seekg(std::get<1>(entry), std::ios::beg);

      ElementStream::read(*quadKeyData.dataFile, std::get<0>(entry ))->accept(visitor);
    }
  }

  bool hasData(const QuadKey &quadKey) const {
    std::ifstream file(getFilePath(quadKey, DataFileExtension));
    return file.good();
  }

  void flush() {
    cache_.clear();
  }

 protected:
  void notify(const utymap::QuadKey& quadKey,
              const std::uint32_t order,
              ElementVisitor &visitor) override {
    const auto &quadKeyData = getQuadKeyData(quadKey);
    auto offset = order * (sizeof(std::uint64_t) + sizeof(std::uint32_t));

    quadKeyData.indexFile->seekg(offset, std::ios::beg);
    auto entry = readIndexEntry(quadKeyData);
    quadKeyData.dataFile->seekg(std::get<1>(entry), std::ios::beg);

    ElementStream::read(*quadKeyData.dataFile, std::get<0>(entry))->accept(visitor);
  }

  Bitmap& getBitmap(const utymap::QuadKey& quadKey) override {
    return getQuadKeyData(quadKey).getBitmap().data;
  }

 private:
  /// Gets quad key data.
  const QuadKeyData& getQuadKeyData(const QuadKey& quadKey) {
    // TODO this is not 100% thread safe as we return reference to data
    // object it points can be destroyed right after.
    std::lock_guard<std::mutex> lock(lock_);

    if (cache_.exists(quadKey))
      return cache_.get(quadKey);

    cache_.put(quadKey, QuadKeyData(getFilePath(quadKey, DataFileExtension),
                                    getFilePath(quadKey, IndexFileExtension),
                                    getFilePath(quadKey, bitmapFileExtension)));

    return cache_.get(quadKey);
  }

  /// Gets full file path for given quad key
  std::string getFilePath(const QuadKey &quadKey, const std::string &extension) const {
    std::stringstream ss;
    ss << dataPath_ << "data/" << quadKey.levelOfDetail << "/" <<
      GeoUtils::quadKeyToString(quadKey) << extension;
    return ss.str();
  }

  /// Reads element info from index.
  std::tuple<std::uint64_t, std::uint32_t> readIndexEntry(const QuadKeyData &quadKeyData) {
    std::uint64_t id;
    std::uint32_t offset;
    quadKeyData.indexFile->read(reinterpret_cast<char *>(&id), sizeof(id));
    quadKeyData.indexFile->read(reinterpret_cast<char *>(&offset), sizeof(offset));
    return std::make_tuple(id, offset);
  }

  const std::string dataPath_;
  std::mutex lock_;
  utymap::utils::LruCache<QuadKey, QuadKeyData, QuadKey::Comparator> cache_;
};

PersistentElementStore::PersistentElementStore(const std::string &dataPath,
                                               const StringTable &stringTable) :
  ElementStore(stringTable),
  pimpl_(utymap::utils::make_unique<PersistentElementStoreImpl>(dataPath, stringTable)) {}

PersistentElementStore::~PersistentElementStore() {
}

void PersistentElementStore::storeImpl(const Element &element,
                                       const QuadKey &quadKey) {
  pimpl_->store(element, quadKey);
}

void PersistentElementStore::search(const std::string &notTerms,
                                    const std::string &andTerms,
                                    const std::string &orTerms,
                                    const utymap::BoundingBox &bbox,
                                    const utymap::LodRange &range,
                                    utymap::entities::ElementVisitor &visitor,
                                    const utymap::CancellationToken &cancelToken) {
  StringIndex::Query query = { { notTerms }, { andTerms }, { orTerms }, bbox, range };
  pimpl_->search(query, visitor, cancelToken);
}

void PersistentElementStore::search(const QuadKey &quadKey,
                                    ElementVisitor &visitor,
                                    const utymap::CancellationToken &cancelToken) {
  pimpl_->search(quadKey, visitor, cancelToken);
}

bool PersistentElementStore::hasData(const QuadKey &quadKey) const {
  return pimpl_->hasData(quadKey);
}

void PersistentElementStore::flush() {
  pimpl_->flush();
}

