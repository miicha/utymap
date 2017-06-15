#ifndef BUILDERS_CACHEBUILDER_HPP_DEFINED
#define BUILDERS_CACHEBUILDER_HPP_DEFINED

#include "builders/BuilderContext.hpp"
#include "builders/ElementBuilder.hpp"
#include "builders/EmptyBuilder.hpp"
#include "builders/MeshCache.hpp"
#include "entities/Node.hpp"
#include "entities/Way.hpp"
#include "entities/Area.hpp"
#include "entities/Relation.hpp"

namespace utymap {
namespace builders {

/// Provides the way to cache element builder output.
template<typename T>
class CacheBuilder final : public ElementBuilder {
 public:
  CacheBuilder(MeshCache &meshCache, const BuilderContext &context) :
      ElementBuilder(context),
      meshCache_(meshCache),
      cacheContext_(meshCache_.wrap(context)) {}

  void prepare() override {
    if (meshCache_.fetch(context_))
      builder_ = utymap::utils::make_unique<EmptyBuilder>(context_);
    else
      builder_ = utymap::utils::make_unique<T>(cacheContext_);
  }

  void visitNode(const utymap::entities::Node &node) override {
    builder_->visitNode(node);
  }

  void visitWay(const utymap::entities::Way &way) override {
    builder_->visitWay(way);
  }

  void visitArea(const utymap::entities::Area &area) override {
    builder_->visitArea(area);
  }

  void visitRelation(const utymap::entities::Relation &relation) override {
    builder_->visitRelation(relation);
  }

  void complete() override {
    builder_->complete();
    meshCache_.unwrap(cacheContext_);
  }

 private:
  MeshCache &meshCache_;
  BuilderContext cacheContext_;
  std::unique_ptr<ElementBuilder> builder_;
};

}
}

#endif // BUILDERS_CACHEBUILDER_HPP_DEFINED