#include "entities/Node.hpp"
#include "index/BitmapIndex.hpp"

#include <boost/test/unit_test.hpp>
#include "test_utils/DependencyProvider.hpp"
#include "test_utils/ElementUtils.hpp"

using namespace utymap;
using namespace utymap::entities;
using namespace utymap::index;
using namespace utymap::tests;

namespace {
  /// Defines the simplest in memory string index implementation.
  class TestBitmapIndex : public BitmapIndex {
   public:
    TestBitmapIndex(const StringTable &stringTable,
                    ElementVisitor &visitor) :
        BitmapIndex(stringTable),
        addedElements(),
        visitor_(visitor),
        registry_() {}

    void notify(const utymap::QuadKey& quadKey,
                const std::uint32_t order,
                ElementVisitor &visitor) override {
      addedElements.at(order)->accept(visitor_);
    }

    Bitmap& getBitmap(const utymap::QuadKey& quadKey) {
      auto bitmap = registry_.find(quadKey);
      if (bitmap==registry_.end()) {
        bitmap = registry_.emplace(quadKey, Bitmap()).first;
      }
      return bitmap->second;
    }

    std::vector<std::shared_ptr<Element>> addedElements;
   private:
    ElementVisitor &visitor_;
    std::map<QuadKey, BitmapIndex::Bitmap, QuadKey::Comparator> registry_;
  };

  struct Index_BitmapIndexFixture : ElementVisitor {
    Index_BitmapIndexFixture() :
      dependencyProvider(),
      index(*dependencyProvider.getStringTable(), *this),
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

    void addThreeElements() {
      addToIndex({ { "addr:country", "Deutschland" } });
      addToIndex({ { "addr:street", "Eichendorffstr." } });
      addToIndex({ { "addr:city", "Berlin" } });
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
    TestBitmapIndex index;
    BoundingBox bbox;
    LodRange lodRange;

    std::vector<std::shared_ptr<Element>> visitedElements;
  };
}

BOOST_FIXTURE_TEST_SUITE(Index_BitmapIndex, Index_BitmapIndexFixture)

BOOST_AUTO_TEST_CASE(GivenThreeElements_WhenEmptyQuery_ThenNoResults) {
  BitmapIndex::Query query = { "", "", "", bbox, lodRange };
  addThreeElements();

  index.search(query, *this);

  BOOST_CHECK_EQUAL(this->visitedElements.size(), 0);
}

BOOST_AUTO_TEST_CASE(GivenThreeElements_WhenQueryWithOneAND_ThenOneResult) {
  BitmapIndex::Query query = { "", "street", "", bbox, lodRange };
  addThreeElements();

  index.search(query, *this);

  BOOST_CHECK_EQUAL(this->visitedElements.size(), 1);
  BOOST_CHECK_EQUAL(getString(this->visitedElements[0]->tags[0].key), "addr:street");
}

BOOST_AUTO_TEST_CASE(GivenThreeElements_WhenQueryWithTwoAND_ThenOneResult) {
  BitmapIndex::Query query = { "", "addr Eichendorffstr", "", bbox, lodRange };
  addThreeElements();

  index.search(query, *this);

  BOOST_CHECK_EQUAL(this->visitedElements.size(), 1);
  BOOST_CHECK_EQUAL(getString(this->visitedElements[0]->tags[0].key), "addr:street");
}

BOOST_AUTO_TEST_CASE(GivenThreeElements_WhenQueryWithOneAND_ThenThreeResults) {
  BitmapIndex::Query query = { "", "addr", "", bbox, lodRange };
  addThreeElements();

  index.search(query, *this);

  BOOST_CHECK_EQUAL(this->visitedElements.size(), 3);
}

BOOST_AUTO_TEST_CASE(GivenThreeElements_WhenQueryWithNot_ThenNoResults) {
  BitmapIndex::Query query = { "country", "", "", bbox, lodRange };
  addThreeElements();

  index.search(query, *this);

  BOOST_CHECK_EQUAL(this->visitedElements.size(), 0);
}

BOOST_AUTO_TEST_CASE(GivenThreeElements_WhenQueryWithNotAnd_ThenOneResult) {
  BitmapIndex::Query query = { "street", "addr", "", bbox, lodRange };
  addThreeElements();

  index.search(query, *this);

  BOOST_CHECK_EQUAL(getString(this->visitedElements[0]->tags[0].key), "addr:country");
  BOOST_CHECK_EQUAL(getString(this->visitedElements[1]->tags[0].key), "addr:city");
}

BOOST_AUTO_TEST_CASE(GivenThreeElements_WhenQueryWithNotAnd_ThenNoResults) {
  BitmapIndex::Query query = { "Deutschland", "country", "", bbox, lodRange };
  addThreeElements();

  index.search(query, *this);

  BOOST_CHECK_EQUAL(this->visitedElements.size(), 0);
}

BOOST_AUTO_TEST_CASE(GivenThreeElements_WhenQueryWithOr_ThenHasOneResult) {
  BitmapIndex::Query query = { "", "", "country", bbox, lodRange };
  addThreeElements();

  index.search(query, *this);

  BOOST_CHECK_EQUAL(this->visitedElements.size(), 1);
  BOOST_CHECK_EQUAL(getString(this->visitedElements[0]->tags[0].key), "addr:country");
}

BOOST_AUTO_TEST_CASE(GivenThreeElements_WhenQueryWithTwoOr_ThenHasTwoResults) {
  BitmapIndex::Query query = { "", "", "country Berlin", bbox, lodRange };
  addThreeElements();

  index.search(query, *this);

  BOOST_CHECK_EQUAL(this->visitedElements.size(), 2);
  BOOST_CHECK_EQUAL(getString(this->visitedElements[0]->tags[0].key), "addr:country");
  BOOST_CHECK_EQUAL(getString(this->visitedElements[1]->tags[0].key), "addr:city");
}

BOOST_AUTO_TEST_CASE(GivenThreeElements_WhenQueryWithTwoOrPlusNot_ThenHasOneResult) {
  BitmapIndex::Query query = { "Berlin" , "", "Deutschland city", bbox, lodRange };
  addThreeElements();

  index.search(query, *this);

  BOOST_CHECK_EQUAL(this->visitedElements.size(), 1);
  BOOST_CHECK_EQUAL(getString(this->visitedElements[0]->tags[0].key), "addr:country");
}

BOOST_AUTO_TEST_CASE(GivenThreeElements_WhenQueryWithThreeOrPlusNotPlusAnd_ThenHasOneResult) {
  BitmapIndex::Query query = { "Berlin", "street", "Deutschland city Eichendorffstr", bbox, lodRange };
  addThreeElements();

  index.search(query, *this);

  BOOST_CHECK_EQUAL(this->visitedElements.size(), 1);
  BOOST_CHECK_EQUAL(getString(this->visitedElements[0]->tags[0].key), "addr:street");
}

BOOST_AUTO_TEST_CASE(GivenThreeElements_WhenQueryWithConflictingRules_ThenHasNoResult) {
  BitmapIndex::Query query = { "Deutschland Eichendorffstr",
                               "street addr",
                               "Deutschland Berlin Eichendorffstr",
                               bbox, lodRange };
  addThreeElements();

  index.search(query, *this);

  BOOST_CHECK_EQUAL(this->visitedElements.size(), 0);
}

BOOST_AUTO_TEST_CASE(GivenElement_WhenQueryWithPartiallyValidAndRule_ThenHasNoResult) {
  BitmapIndex::Query query = { "", "Kremlin Senate", "", bbox, lodRange };
  addToIndex({ { "name:en", "Kremlin Clock" } });

  index.search(query, *this);

  BOOST_CHECK_EQUAL(this->visitedElements.size(), 0);
}

BOOST_AUTO_TEST_SUITE_END()
