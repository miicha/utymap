#ifndef INDEX_ELEMENTVISITORFILTER_HPP_DEFINED
#define INDEX_ELEMENTVISITORFILTER_HPP_DEFINED

#include "BoundingBox.hpp"
#include "QuadKey.hpp"
#include "entities/Node.hpp"
#include "entities/Way.hpp"
#include "entities/Area.hpp"
#include "entities/Relation.hpp"
#include "entities/ElementVisitor.hpp"

#include <functional>

namespace utymap {
namespace index {

/// Provides the way to visit elements with filtering.
class ElementVisitorFilter : public utymap::entities::ElementVisitor {
 public:

  using Filter = std::function<bool(const utymap::entities::Element&)>;

  ElementVisitorFilter(utymap::entities::ElementVisitor &visitor,
                       const Filter &predicate) :
      visitor_(visitor), predicate_(predicate) { }

  void visitNode(const utymap::entities::Node &node) override {
    visit(node);
  }

  void visitWay(const utymap::entities::Way &way) override {
    visit(way);
  }

  void visitArea(const utymap::entities::Area &area) override {
    visit(area);
  }

  void visitRelation(const utymap::entities::Relation &relation) override {
    visit(relation);
  }
 private:
  void visit(const utymap::entities::Element &element) {
    if (predicate_(element)) {
      element.accept(visitor_);
    }
  }

  utymap::entities::ElementVisitor &visitor_;
  Filter predicate_;
};

}
}

#endif //INDEX_ELEMENTVISITORFILTER_HPP_DEFINED
