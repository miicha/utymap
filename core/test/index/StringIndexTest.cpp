#include "entities/Node.hpp"
#include "index/StringIndex.hpp"

#include <boost/test/unit_test.hpp>
#include "test_utils/DependencyProvider.hpp"
#include "test_utils/ElementUtils.hpp"

#include <map>

using namespace utymap;
using namespace utymap::entities;
using namespace utymap::index;
using namespace utymap::tests;

namespace {
  /// Defines the simplest in memory string index implementation.
  class TestStringIndex : public StringIndex {
   public:
    TestStringIndex(const StringTable &stringTable)
        : StringIndex(stringTable) {}

    utymap::entities::Element& getElement(std::uint32_t order) {
      return *addedElements.at(order);
    }

    Bitmap& getBitmap(const utymap::QuadKey& quadKey) {
      auto bitmap = registry_.find(quadKey);
      if (bitmap==registry_.end()) {
        bitmap = registry_.emplace(quadKey, Bitmap()).first;
      }
      return bitmap->second;
    }

    std::vector<std::shared_ptr<Element>> addedElements;
    std::map<QuadKey, StringIndex::Bitmap, QuadKey::Comparator> registry_;
  };

  struct Index_StringIndexFixture : ElementVisitor {
    Index_StringIndexFixture() :
      dependencyProvider(),
      index(*dependencyProvider.getStringTable()),
      bbox(BoundingBox(GeoCoordinate(-90, -180), GeoCoordinate(90, 180))),
      lodRange(1, 1),
      visitedElements() {
    }

    /// Adds node with given tags in index for given quadkey
    void addToIndex(std::initializer_list<std::pair<const char *, const char *>> tags,
                       const QuadKey quadKey = QuadKey(1, 0, 0)) {
      auto node = ElementUtils::createElement<Node>(*dependencyProvider.getStringTable(), 0, tags);
      index.add(node,
                utymap::QuadKey(1, 0, 0),
                static_cast<std::uint32_t>(index.addedElements.size()));
      index.addedElements.push_back(std::make_shared<Node>(node));
    }

    std::string getString(std::uint32_t id) {
      return dependencyProvider.getStringTable()->getString(id);
    }

    /// Adds visited node in special collection.
    void visitNode(const Node &node) override {
      visitedElements.push_back(std::make_shared<Node>(node));
    }

    // Ignore the rest as we always use nodes
    void visitWay(const Way &) override { }
    void visitArea(const Area &) override { }
    void visitRelation(const Relation &) override { }

    DependencyProvider dependencyProvider;
    TestStringIndex index;
    BoundingBox bbox;
    LodRange lodRange;

    std::vector<std::shared_ptr<Element>> visitedElements;
  };
}

BOOST_FIXTURE_TEST_SUITE(Index_StringIndex, Index_StringIndexFixture)

BOOST_AUTO_TEST_CASE(GivenThreeElements_WhenQueryWithOneAND_ThenOnlyOneIsReturned) {
  StringIndex::Query query = { { "street" }, {}, {}, bbox, lodRange };
  addToIndex({ { "addr:country", "Deutschland" } });
  addToIndex({ { "addr:street", "Eichendorffstr." } });
  addToIndex({ { "addr:city", "Berlin" } });
  
  index.search(query, *this);

  BOOST_CHECK_EQUAL(this->visitedElements.size(), 1);
  BOOST_CHECK_EQUAL(getString(this->visitedElements[0]->tags[0].key), "addr:street");
}

BOOST_AUTO_TEST_SUITE_END()