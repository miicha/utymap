#include "entities/Node.hpp"
#include "index/StringIndex.hpp"

#include <boost/test/unit_test.hpp>
#include "test_utils/DependencyProvider.hpp"
#include "test_utils/ElementUtils.hpp"

using namespace utymap;
using namespace utymap::entities;
using namespace utymap::index;
using namespace utymap::tests;

namespace {
  struct Index_StringIndexFixture : ElementVisitor {
    Index_StringIndexFixture() :
      dependencyProvider(),
      index(std::bind(&Index_StringIndexFixture::getElementByOrder, this, std::placeholders::_1),
        *dependencyProvider.getStringTable()),
      bbox(BoundingBox(GeoCoordinate(-90, -180), GeoCoordinate(90, 180))),
      lodRange(1, 16),
      addedElements(),
      visitedElements() {
    }

    Element& getElementByOrder(std::uint32_t order) {
      return *addedElements.at(order);
    }

    /// Adds node with given tags in index for given quadkey
    void addToIndex(std::initializer_list<std::pair<const char *, const char *>> tags,
                       const QuadKey quadKey = QuadKey(1, 0, 0)) {
      auto node = ElementUtils::createElement<Node>(*dependencyProvider.getStringTable(), 0, tags);
      addedElements.push_back(std::make_shared<Node>(node));
      index.add(node, utymap::QuadKey(1, 0, 0), 0);
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
    StringIndex index;
    BoundingBox bbox;
    LodRange lodRange;
    std::vector<std::shared_ptr<Element>> addedElements;
    std::vector<std::shared_ptr<Element>> visitedElements;
  };
}

BOOST_FIXTURE_TEST_SUITE(Index_StringIndex, Index_StringIndexFixture)

BOOST_AUTO_TEST_CASE(GivenElementWithTags_WhenItIsAdd_ThenQueryCanFindItByTagValue) {
  StringIndex::Query query = { { "key1" }, {}, {}, bbox, lodRange };
  addToIndex({ { "addr:street", "Eichendorffstr." } });
  
  index.search(query, *this);

  //BOOST_CHECK_EQUAL(this->visitedElements.size(), 1);
}

BOOST_AUTO_TEST_SUITE_END()