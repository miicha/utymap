#include "index/GeoStore.hpp"
#include "index/PersistentElementStore.hpp"
#include "mapcss/MapCssParser.hpp"

#include <boost/filesystem/operations.hpp>
#include <boost/test/unit_test.hpp>
#include <chrono>
#include <fstream>
#include <thread>

#include "config.hpp"
#include "test_utils/DependencyProvider.hpp"

using namespace utymap;
using namespace utymap::index;
using namespace utymap::mapcss;
using namespace utymap::tests;

namespace {
const std::string DataDirectory = "data";
const std::string TestZoomDirectory = DataDirectory + "/16";

StyleSheet createStylesheet(const std::string &path) {
  std::ifstream file(TEST_MAPCSS_DEFAULT);
  std::string dir = path.substr(0, path.find_last_of("\\/") + 1);
  MapCssParser parser(dir);
  return parser.parse(file);
}

/// Decorates persistent store with additional test logic.
class PersistentElementStoreEx : public ElementStore {
public:

  PersistentElementStoreEx(const std::string &dataPath,
                           const StringTable &stringTable,
                           CancellationToken &token) :
    ElementStore(stringTable),
    store_(dataPath, stringTable), token_(token), counter_(0), isErased_(false) {}

  void search(const std::string&, const std::string&, const std::string&, const BoundingBox&,
              const LodRange&, entities::ElementVisitor&, const CancellationToken&) override {
    throw std::domain_error("Unexpected function call.");
  }

  void search(const QuadKey &quadKey,
              entities::ElementVisitor &visitor,
              const CancellationToken &cancelToken) override {
    store_.search(quadKey, visitor, cancelToken);
  }

  bool hasData(const QuadKey &quadKey) const override {
    return store_.hasData(quadKey);
  }

  void erase(const QuadKey &quadKey) override {
    store_.erase(quadKey);
    isErased_ = true;
  }

  void erase(const BoundingBox &bbox, const LodRange &range) override {
    throw std::domain_error("Unexpected function call.");
  }

  void save(const entities::Element &element, const QuadKey &quadKey) override {
    ++counter_;
    store_.save(element, quadKey);
    // NOTE should not be more than expected amount of imported elements
    if (counter_ > 200) {
      token_.cancel();
    }
  }

  void waitForErase() const {
    while (!isErased_) {}
  }

private:
  PersistentElementStore store_;
  CancellationToken &token_;
  int counter_;
  volatile bool isErased_;
};

struct Index_GeoStoreFixture {
  Index_GeoStoreFixture() :
    dependencyProvider(),
    store_(*dependencyProvider.getStringTable()),
    stylesheet(createStylesheet(TEST_MAPCSS_DEFAULT)) {
    boost::filesystem::create_directories(TestZoomDirectory);
  }

  ~Index_GeoStoreFixture() {
    boost::filesystem::remove_all(DataDirectory);
  }

  DependencyProvider dependencyProvider;
  GeoStore store_;
  StyleSheet stylesheet;
};
}

BOOST_FIXTURE_TEST_SUITE(Index_GeoStore, Index_GeoStoreFixture)

BOOST_AUTO_TEST_CASE(GivenImportQuadKeyToPersistentStore_WhenAddOperationIsCancelled_ThenAllDataErased) {
  // ARRANGE
  QuadKey quadKey(16, 35205, 21489);
  const std::string storeKey = "file_storage";
  CancellationToken cancelToken;
  auto store = new PersistentElementStoreEx(DataDirectory, *dependencyProvider.getStringTable(), cancelToken);
  store_.registerStore(storeKey, std::unique_ptr<PersistentElementStoreEx>(store));

  // ACT
  std::thread t([&]() {
    store_.add(storeKey, TEST_XML_FILE, quadKey, *dependencyProvider.getStyleProvider(stylesheet), cancelToken);
  });
  t.join();

  // ASSERT
  store->waitForErase();
  BOOST_ASSERT(!store_.hasData(quadKey));
  BOOST_ASSERT(!boost::filesystem::exists(TestZoomDirectory + "/1202102332220103.bmp"));
  BOOST_ASSERT(!boost::filesystem::exists(TestZoomDirectory + "/1202102332220103.dat"));
  BOOST_ASSERT(!boost::filesystem::exists(TestZoomDirectory + "/1202102332220103.idf"));
}

BOOST_AUTO_TEST_SUITE_END()
