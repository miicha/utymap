#include "builders/MeshCache.hpp"
#include "entities/Node.hpp"
#include "entities/Way.hpp"
#include "entities/Area.hpp"
#include "entities/Relation.hpp"

#include <functional>
#include <boost/filesystem/operations.hpp>
#include <boost/test/unit_test.hpp>

#include "test_utils/DependencyProvider.hpp"
#include "test_utils/ElementUtils.hpp"

using namespace utymap;
using namespace utymap::builders;
using namespace utymap::entities;
using namespace utymap::math;
using namespace utymap::tests;

namespace {
    const QuadKey quadKey = QuadKey(1, 0, 0);
    const std::string stylesheet = "node|z1[any], way|z1[any], area|z1[any], relation|z1[any] { clip: false; }";

    struct Builders_MeshCacheFixture
    {
        void meshCallback(const Mesh& mesh) {
            lastName = mesh.name;
        }

        void elementCallback(const Element& element) {
            lastId_ = element.id;
        }

        void resetData() {
            lastId_ = 0;
            lastName = "";
        }

        std::string getCacheDir() const {
            return std::string("cache/") + dependencyProvider.getStyleProvider()->getTag() + "/1";
        }

        Builders_MeshCacheFixture() :
            cache_(""),
            origContext(quadKey,
                *dependencyProvider.getStyleProvider(stylesheet),
                *dependencyProvider.getStringTable(),
                *dependencyProvider.getElevationProvider(),
                std::bind(&Builders_MeshCacheFixture::meshCallback, this, std::placeholders::_1),
                std::bind(&Builders_MeshCacheFixture::elementCallback, this, std::placeholders::_1)),
            wrapContext(cache_.wrap(origContext))
        {
            resetData();
            boost::filesystem::create_directories(getCacheDir());
        }

        ~Builders_MeshCacheFixture()
        {
            boost::filesystem::remove(getCacheDir() + "/0.mesh");
        }

        void assertStoreAndFetch(const Element& element) {
            // Assert that original callback is called
            wrapContext.elementCallback(element);
            BOOST_CHECK_EQUAL(lastId_, element.id);
            
            // Release context and reset actual values
            cache_.unwrap(wrapContext, token);
            resetData();

            // Assert that element is read back
            cache_.fetch(origContext, token);
            BOOST_CHECK_EQUAL(lastId_, element.id);
        }

        MeshCache cache_;
        DependencyProvider dependencyProvider;
        BuilderContext origContext;
        BuilderContext wrapContext;
        CancellationToken token;

        std::uint64_t lastId_;
        std::string lastName;
    };
}

BOOST_FIXTURE_TEST_SUITE(Builders_MeshCache, Builders_MeshCacheFixture)

BOOST_AUTO_TEST_CASE(GivenNode_WhenStoreAndFetch_ThenItIsStoredAndReadBack)
{
    Node node = ElementUtils::createElement<Node>(*dependencyProvider.getStringTable(), 1, { { "any", "true" } });

    assertStoreAndFetch(node);
}

BOOST_AUTO_TEST_CASE(GivenWay_WhenStoreAndFetch_ThenItIsStoredAndReadBack)
{
    Way way = ElementUtils::createElement<Way>(*dependencyProvider.getStringTable(), 7, { { "any", "true" } }, { { 1, -1 }, { 5, -5 } });

    assertStoreAndFetch(way);
}

BOOST_AUTO_TEST_CASE(GivenArea_WhenStoreAndFetch_ThenItIsStoredAndReadBack)
{
    Area area = ElementUtils::createElement<Area>(*dependencyProvider.getStringTable(), 7, { { "any", "true" } }, { { 1, -1 }, { 5, -5 }, { 10, -10 } });

    assertStoreAndFetch(area);
}

BOOST_AUTO_TEST_CASE(GivenRelation_WhenStoreAndFetch_ThenItIsStoredAndReadBack)
{
    Node node = ElementUtils::createElement<Node>(*dependencyProvider.getStringTable(), 1, { { "n", "1" } });
    node.coordinate = { 0.5, -0.5 };
    Way way = ElementUtils::createElement<Way>(*dependencyProvider.getStringTable(), 2, { { "w", "2" } }, { { 1, -1 }, { 2, -2 } });
    Area area = ElementUtils::createElement<Area>(*dependencyProvider.getStringTable(), 3, { { "a", "3" } }, { { 3, -3 }, { 4, -4 }, { 5, -5 } });
    Relation relation = ElementUtils::createElement<Relation>(*dependencyProvider.getStringTable(), 4, { { "any", "true" } });
    relation.elements.push_back(std::make_shared<Node>(node));
    relation.elements.push_back(std::make_shared<Way>(way));
    relation.elements.push_back(std::make_shared<Area>(area));

    assertStoreAndFetch(relation);
}

BOOST_AUTO_TEST_SUITE_END()