#include "builders/BuilderContext.hpp"
#include "builders/ExternalBuilder.hpp"
#include "builders/QuadKeyBuilder.hpp"
#include "utils/CoreUtils.hpp"

using namespace utymap;
using namespace utymap::builders;
using namespace utymap::entities;
using namespace utymap::heightmap;
using namespace utymap::index;
using namespace utymap::mapcss;
using namespace utymap::math;

namespace {
    const std::string BuilderKeyName = "builders";
}

class QuadKeyBuilder::QuadKeyBuilderImpl
{
private:

    typedef std::unordered_map<std::string, ElementBuilderFactory> BuilderFactoryMap;

class AggregateElementVisitor : public ElementVisitor
{
public:
    AggregateElementVisitor(const QuadKey& quadKey,
                           const StyleProvider& styleProvider,
                           StringTable& stringTable,
                           const ElevationProvider& eleProvider,
                           const MeshCallback& meshFunc,
                           const ElementCallback& elementFunc,
                           BuilderFactoryMap& builderFactoryMap,
                           std::uint32_t builderKeyId) :
        context_(quadKey, styleProvider, stringTable, eleProvider, meshFunc, elementFunc),
        builderFactoryMap_(builderFactoryMap),
        builderKeyId_(builderKeyId)
    {
    }

    void visitNode(const Node& node) override { visitElement(node); }

    void visitWay(const Way& way) override { visitElement(way); }

    void visitArea(const Area& area) override { visitElement(area); }

    void visitRelation(const Relation& relation) override { visitElement(relation); }

    void complete()
    {
        for (const auto& builder : builders_)
            builder.second->complete();
    }

private:
    /// Calls appropriate visitor for given element
    void visitElement(const Element& element)
    {
        Style style = context_.styleProvider.forElement(element, context_.quadKey.levelOfDetail);

        // we don't know how to build it. Skip.
        if (!style.has(builderKeyId_))
            return;

        std::stringstream ss(style.get(builderKeyId_).value());
        std::string name;
        while (ss.good()) {
            getline(ss, name, ',');
            element.accept(getBuilder(name));
            name.clear();
        }
    }

    ElementBuilder& getBuilder(const std::string& name)
    {
        auto builderPair = builders_.find(name);
        if (builderPair != builders_.end()) {
            return *builderPair->second;
        }

        auto factory = builderFactoryMap_.find(name);
        if (factory == builderFactoryMap_.end()) {
            // use external builder by default
            builders_.emplace(name, utymap::utils::make_unique<ExternalBuilder>(context_));
        } else {
            builders_.emplace(name, factory->second(context_));
        }

        return *builders_[name];
    }

    const BuilderContext context_;
    BuilderFactoryMap& builderFactoryMap_;
    std::uint32_t builderKeyId_;
    std::unordered_map<std::string, std::unique_ptr<ElementBuilder>> builders_;
};

public:
    QuadKeyBuilderImpl(GeoStore& geoStore, StringTable& stringTable) :
        geoStore_(geoStore),
        stringTable_(stringTable),
        builderKeyId_(stringTable.getId(BuilderKeyName)),
        builderFactory_()
    {
    }

    void registerElementVisitor(const std::string& name, ElementBuilderFactory factory)
    {
        builderFactory_[name] = factory;
    }

    void build(const QuadKey& quadKey,
               const StyleProvider& styleProvider,
               const ElevationProvider& eleProvider,
               const MeshCallback& meshFunc,
               const ElementCallback& elementFunc,
               const utymap::CancellationToken& cancelToken)
    {
        AggregateElementVisitor elementVisitor(quadKey, styleProvider, stringTable_,
            eleProvider, meshFunc, elementFunc, builderFactory_, builderKeyId_);

        geoStore_.search(quadKey, styleProvider, elementVisitor, cancelToken);
        elementVisitor.complete();
    }

private:
    GeoStore& geoStore_;
    StringTable& stringTable_;
    std::uint32_t builderKeyId_;
    BuilderFactoryMap builderFactory_;
};

void QuadKeyBuilder::registerElementBuilder(const std::string& name, ElementBuilderFactory factory)
{
    pimpl_->registerElementVisitor(name, factory);
}

void QuadKeyBuilder::build(const QuadKey& quadKey, const StyleProvider& styleProvider, const ElevationProvider& eleProvider, 
    MeshCallback meshFunc, ElementCallback elementFunc, const utymap::CancellationToken& cancelToken)
{
    pimpl_->build(quadKey, styleProvider, eleProvider, meshFunc, elementFunc, cancelToken);
}

QuadKeyBuilder::QuadKeyBuilder(GeoStore& geoStore, StringTable& stringTable) :
    pimpl_(utymap::utils::make_unique<QuadKeyBuilderImpl>(geoStore, stringTable))
{
}

QuadKeyBuilder::~QuadKeyBuilder()
{
}
