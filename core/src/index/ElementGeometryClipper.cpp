#include "BoundingBox.hpp"
#include "QuadKey.hpp"
#include "entities/Element.hpp"
#include "entities/Node.hpp"
#include "entities/Way.hpp"
#include "entities/Area.hpp"
#include "entities/Relation.hpp"
#include "index/ElementStore.hpp"
#include "index/ElementGeometryClipper.hpp"

using namespace utymap;
using namespace utymap::entities;
using namespace utymap::mapcss;
using namespace utymap::math;

namespace {
/// Max precision for Lat/Lon
const double Scale = 1E7;

using PointLocation = utymap::index::ElementGeometryClipper::PointLocation;

template<typename T, typename std::enable_if<std::is_same<T, Way>::value, std::size_t>::type = 0>
bool areConnected(const BoundingBox &, const BoundingBox &, bool allOutside) {
  return !allOutside;
}

template<typename T, typename std::enable_if<std::is_same<T, Area>::value, std::size_t>::type = 0>
bool areConnected(const BoundingBox &quadKeyBbox, const BoundingBox &elementBbox, bool allOutside) {
  return quadKeyBbox.intersects(elementBbox);
}

template<typename T>
PointLocation checkElement(const BoundingBox &quadKeyBbox, const T &element, IntPath &elementShape) {
  elementShape.reserve(element.coordinates.size());
  bool allInside = true;
  bool allOutside = true;
  BoundingBox elementBbox;
  for (const GeoCoordinate &coord : element.coordinates) {
    bool contains = quadKeyBbox.contains(coord);
    allInside &= contains;
    allOutside &= !contains;
    elementBbox.expand(coord);

    auto x = static_cast<cInt>(coord.longitude*Scale);
    auto y = static_cast<cInt>(coord.latitude*Scale);
    elementShape.push_back(IntPoint(x, y));
  }

  return allInside ? PointLocation::AllInside :
         (areConnected<T>(quadKeyBbox, elementBbox, allOutside) ? PointLocation::Mixed : PointLocation::AllOutside);
}

template<typename T>
void setCoordinates(T &t, const IntPath &path) {
  t.coordinates.reserve(path.size());
  for (const auto &c : path) {
    t.coordinates.push_back(GeoCoordinate(c.Y/Scale, c.X/Scale));
  }
}

IntPath createPathFromBoundingBox(const BoundingBox &quadKeyBbox) {
  double xMin = quadKeyBbox.minPoint.longitude, yMin = quadKeyBbox.minPoint.latitude,
      xMax = quadKeyBbox.maxPoint.longitude, yMax = quadKeyBbox.maxPoint.latitude;
  IntPath rect;
  rect.push_back(IntPoint(static_cast<cInt>(xMin*Scale),
                                      static_cast<cInt>(yMin*Scale)));

  rect.push_back(IntPoint(static_cast<cInt>(xMax*Scale),
                                      static_cast<cInt>(yMin*Scale)));

  rect.push_back(IntPoint(static_cast<cInt>(xMax*Scale),
                                      static_cast<cInt>(yMax*Scale)));

  rect.push_back(IntPoint(static_cast<cInt>(xMin*Scale),
                                      static_cast<cInt>(yMax*Scale)));
  return std::move(rect);
}

template<typename T>
std::shared_ptr<Element> clipElement(Clipper &clipper,
                                     const BoundingBox &bbox,
                                     const T &element,
                                     bool isClosed) {
  IntPath elementShape;
  PointLocation pointLocation = checkElement(bbox, element, elementShape);
  // 1. all geometry inside current quadkey: no need to truncate.
  if (pointLocation==PointLocation::AllInside) {
    return std::make_shared<T>(element);
  }

  // 2. all geometry outside : way should be skipped
  if (pointLocation==PointLocation::AllOutside) {
    return nullptr;
  }

  PolyTree solution;
  addSubject(clipper, elementShape, isClosed);
  executeIntersection(clipper, solution);
  clipper.removeSubject();

  std::size_t count = static_cast<std::size_t>(solution.Total());

  // 3. way intersects border only once: store a copy with clipped geometry
  if (count==1) {
    auto clippedElement = std::make_shared<T>();
    clippedElement->id = element.id;
    clippedElement->tags = element.tags;
    setCoordinates(*clippedElement, solution.GetFirst()->Contour);
    
    return clippedElement;
  }
  // 4. in this case, result should be stored as relation (collection of ways)
  if (count > 1) {
    auto relation = std::make_shared<Relation>();
    relation->id = element.id;
    relation->elements.reserve(count);
    PolyNode *polyNode = solution.GetFirst();
    while (polyNode) {
      auto clippedElement = std::make_shared<T>();
      clippedElement->id = 0;
      clippedElement->tags = element.tags;
      setCoordinates(*clippedElement, polyNode->Contour);
      relation->elements.push_back(clippedElement);
      polyNode = polyNode->GetNext();
    }
    return relation;
  }

  // no intersection
  return nullptr;
}

std::shared_ptr<Element> clipWay(Clipper &clipper, const BoundingBox &bbox, const Way &way) {
  return clipElement(clipper, bbox, way, false);
}

std::shared_ptr<Element> clipArea(Clipper &clipper, const BoundingBox &bbox, const Area &area) {
  return clipElement(clipper, bbox, area, true);
}

std::shared_ptr<Element> clipRelation(Clipper &clipper,
                                      const BoundingBox &bbox,
                                      const Relation &relation);

/// Visits relation and collects clipped elements
struct RelationVisitor : public ElementVisitor {
  RelationVisitor(Clipper &clipper, const BoundingBox &quadKeyBbox) :
      relation(nullptr), clipper_(clipper), bbox_(quadKeyBbox) {
  }

  void visitNode(const Node &node) override {
    if (bbox_.contains(node.coordinate)) {
      ensureRelation();
      relation->elements.push_back(std::make_shared<Node>(node));
    }
  }

  void visitWay(const Way &way) override {
    addElement(clipWay(clipper_, bbox_, way));
  }

  void visitArea(const Area &area) override {
    addElement(clipArea(clipper_, bbox_, area));
  }

  void visitRelation(const Relation &relation) override {
    addElement(clipRelation(clipper_, bbox_, relation));
  }

  std::shared_ptr<Relation> relation;

 private:

  void ensureRelation() {
    if (relation==nullptr)
      relation = std::make_shared<Relation>();
  }

  void addElement(std::shared_ptr<Element> element) {
    if (element==nullptr) return;

    ensureRelation();

    relation->elements.push_back(element);
  }

  Clipper &clipper_;
  const BoundingBox &bbox_;
};

std::shared_ptr<Element> clipRelation(Clipper &clipper,
                                      const BoundingBox &bbox,
                                      const Relation &relation) {
  RelationVisitor visitor(clipper, bbox);

  for (const auto &element : relation.elements)
    element->accept(visitor);

  if (visitor.relation==nullptr)
    return nullptr;

  std::shared_ptr<Element> element = visitor.relation->elements.size()==1
    ? visitor.relation->elements.at(0)
    : visitor.relation;

  element->id = relation.id;
  element->tags = relation.tags;

  return element;
}
}

namespace utymap {
namespace index {

ElementGeometryClipper::ElementGeometryClipper(const utymap::QuadKey &quadKey,
                                               const utymap::BoundingBox &quadKeyBbox,
                                               Callback callback) :
 callback_(callback), quadKey_(quadKey), quadKeyBbox_(quadKeyBbox), clipper_() {
  addClip(clipper_, createPathFromBoundingBox(quadKeyBbox_));
}

void ElementGeometryClipper::clipAndCall(const Element &element) {
  element.accept(*this);
  clipper_.removeSubject();
}

void ElementGeometryClipper::visitNode(const Node &node) {
  if (quadKeyBbox_.contains(node.coordinate))
    callback_(node, quadKey_);
}

void ElementGeometryClipper::visitWay(const Way &way) {
  auto element = clipWay(clipper_, quadKeyBbox_, way);
  if (element!=nullptr)
    callback_(*element, quadKey_);
}

void ElementGeometryClipper::visitArea(const Area &area) {
  auto element = clipArea(clipper_, quadKeyBbox_, area);
  if (element!=nullptr)
    callback_(*element, quadKey_);
}

void ElementGeometryClipper::visitRelation(const Relation &relation) {
  auto element = clipRelation(clipper_, quadKeyBbox_, relation);
  if (element!=nullptr)
    callback_(*element, quadKey_);
}

}
}
