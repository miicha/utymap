#ifndef INDEX_ELEMENTGEOMETRYVISITOR_HPP_DEFINED
#define INDEX_ELEMENTGEOMETRYVISITOR_HPP_DEFINED

#include "BoundingBox.hpp"
#include "QuadKey.hpp"
#include "entities/Node.hpp"
#include "entities/Way.hpp"
#include "entities/Area.hpp"
#include "entities/Relation.hpp"
#include "entities/ElementVisitor.hpp"

namespace utymap {
namespace index {

/// Creates bounding box of given element.
class ElementGeometryVisitor : public utymap::entities::ElementVisitor {
 public:
  utymap::BoundingBox boundingBox;

  void visitNode(const utymap::entities::Node &node) override {
    boundingBox.expand(node.coordinate);
  }

  void visitWay(const utymap::entities::Way &way) override {
    boundingBox.expand(way.coordinates.cbegin(), way.coordinates.cend());
  }

  void visitArea(const utymap::entities::Area &area) override {
    boundingBox.expand(area.coordinates.cbegin(), area.coordinates.cend());
  }

  void visitRelation(const utymap::entities::Relation &relation) override {
    for (const auto &element: relation.elements) {
      element->accept(*this);
    }
  }

  /// Checks whether element geometry intersects given bbox.
  static bool intersects(const utymap::entities::Element &element,
                         const utymap::BoundingBox &bbox) {
    ElementGeometryVisitor visitor;
    element.accept(visitor);
    return bbox.intersects(visitor.boundingBox);
  }
};

}
}

#endif //INDEX_ELEMENTGEOMETRYVISITOR_HPP_DEFINED
