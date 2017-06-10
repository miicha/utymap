#include "builders/MeshCache.hpp"
#include "entities/Node.hpp"

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

    wrapContext.elementCallback(node);
    BOOST_CHECK_EQUAL(lastId_, 1);
    cache_.unwrap(wrapContext, token);
    resetData();

    cache_.fetch(origContext, token);
    BOOST_CHECK_EQUAL(lastId_, 1);
}

BOOST_AUTO_TEST_SUITE_END()