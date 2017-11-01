#ifndef INDEX_ELEMENTGEOMETRYCLIPPER_HPP_DEFINED
#define INDEX_ELEMENTGEOMETRYCLIPPER_HPP_DEFINED

#include "BoundingBox.hpp"
#include "QuadKey.hpp"
#include "entities/Element.hpp"
#include "entities/ElementVisitor.hpp"
#include "math/PolyClip.hpp"

#include <functional>

namespace utymap {
namespace index {

/// Modifies geometry of element by bounding box clipping.
class ElementGeometryClipper final : private utymap::entities::ElementVisitor {
 public:
  /// Defines callback
  typedef std::function<void(const utymap::entities::Element &element, const utymap::QuadKey &quadKey)> Callback;
  /// Defines polygon points location relative to current quadkey.
  enum class PointLocation { AllInside, AllOutside, Mixed };

  ElementGeometryClipper(const utymap::QuadKey &quadKey,
                         const utymap::BoundingBox &quadKeyBbox,
                         Callback callback);

  void clipAndCall(const utymap::entities::Element &element);

 private:

  void visitNode(const utymap::entities::Node &node) override;

  void visitWay(const utymap::entities::Way &way) override;

  void visitArea(const utymap::entities::Area &area) override;

  void visitRelation(const utymap::entities::Relation &relation) override;

  Callback callback_;
  QuadKey quadKey_;
  BoundingBox quadKeyBbox_;
  utymap::math::Clipper clipper_;
};

}
}

#endif //INDEX_ELEMENTGEOMETRYCLIPPER_HPP_DEFINED
