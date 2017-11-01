#ifndef BUILDERS_BUILDINGS_ROOFS_MANSARDROOFBUILDER_HPP_DEFINED
#define BUILDERS_BUILDINGS_ROOFS_MANSARDROOFBUILDER_HPP_DEFINED

#include "builders/buildings/roofs/FlatRoofBuilder.hpp"
#include "builders/MeshBuilder.hpp"
#include "math/PolyClip.hpp"

#include <algorithm>
#include <limits>

namespace utymap {
namespace builders {

/// Builds mansard roof.
class MansardRoofBuilder final : public FlatRoofBuilder {
  const double Scale = 1E7;

 public:
  MansardRoofBuilder(const utymap::builders::BuilderContext &builderContext,
                     utymap::builders::MeshContext &meshContext)
      : FlatRoofBuilder(builderContext, meshContext) {
  }

  void build(utymap::math::Polygon &polygon) override {
    utymap::math::ClipperOffset offset;
    utymap::math::IntPath path;
    path.reserve(polygon.points.size()/2);

    auto lastPointIndex = polygon.points.size() - 2;
    double min = std::numeric_limits<double>::max();
    for (std::size_t i = 0; i < polygon.points.size(); i += 2) {
      auto nextIndex = i==lastPointIndex ? 0 : i + 2;

      utymap::math::Vector2 v1(polygon.points[i], polygon.points[i + 1]);
      utymap::math::Vector2 v2(polygon.points[nextIndex], polygon.points[nextIndex + 1]);

      min = std::min(min, utymap::math::Vector2::distance(v1, v2));

      path.push_back(utymap::math::IntPoint(static_cast<utymap::math::cInt>(v1.x*Scale),
                                            static_cast<utymap::math::cInt>(v1.y*Scale)));
    }

    utymap::math::addMiter(offset, path);

    utymap::math::IntPaths solution;
    // NOTE: use minimal side value as reference for offsetting.
    offset.Execute(solution, -(min/10)*Scale);

    // NOTE: this is unexpected result for algorithm below, fallback to flat roof.
    if (solution.size()!=1 || solution[0].size()!=path.size()) {
      return FlatRoofBuilder::build(polygon);
    }

    build(polygon, solution[0], findFirstIndex(solution[0][0], polygon));
  }

 private:

  void build(const utymap::math::Polygon &polygon,
             utymap::math::IntPath &offsetShape,
             std::size_t index) {
    if (!utymap::math::getOrientation(offsetShape))
      std::reverse(offsetShape.begin(), offsetShape.end());

    // build top
    utymap::math::Polygon topShape(offsetShape.size(), 0);
    std::vector<utymap::math::Vector2> topShapeVertices;
    topShapeVertices.reserve(offsetShape.size());
    for (const auto &p : offsetShape) {
      topShapeVertices.push_back(utymap::math::Vector2(p.X/Scale, p.Y/Scale));
    }
    topShape.addContour(topShapeVertices);

    meshContext_.geometryOptions.elevation = height_;
    meshContext_.geometryOptions.heightOffset = minHeight_;
    meshContext_.geometryOptions.flipSide = false;

    builderContext_.meshBuilder.addPolygon(meshContext_.mesh,
                                           topShape,
                                           meshContext_.geometryOptions,
                                           meshContext_.appearanceOptions);

    double scale = utymap::utils::GeoUtils::getScaled(builderContext_.boundingBox,
                                                      meshContext_.appearanceOptions.textureScale,
                                                      height_);

    // build sides
    meshContext_.geometryOptions.elevation = std::numeric_limits<double>::lowest();
    meshContext_.geometryOptions.heightOffset = 0;

    double topHeight = minHeight_ + height_;
    auto size = polygon.points.size();
    for (std::size_t i = 0; i < size; i += 2) {
      auto topIndex = i;
      auto bottomIndex = (index + i)%size;
      auto nextTopIndex = (i + 2)%size;
      auto nextBottomIndex = (index + i + 2)%size;

      auto v0 = utymap::math::Vector3(polygon.points[bottomIndex], minHeight_, polygon.points[bottomIndex + 1]);
      auto v1 = utymap::math::Vector3(polygon.points[nextBottomIndex], minHeight_, polygon.points[nextBottomIndex + 1]);
      auto v2 = utymap::math::Vector3(topShape.points[nextTopIndex], topHeight, topShape.points[nextTopIndex + 1]);
      auto v3 = utymap::math::Vector3(topShape.points[topIndex], topHeight, topShape.points[topIndex + 1]);

      builderContext_.meshBuilder.addTriangle(meshContext_.mesh, v2, v0, v3,
                                              utymap::math::Vector2(scale, scale),
                                              utymap::math::Vector2(0, 0),
                                              utymap::math::Vector2(0, scale),
                                              meshContext_.geometryOptions, meshContext_.appearanceOptions);

      builderContext_.meshBuilder.addTriangle(meshContext_.mesh, v0, v2, v1,
                                              utymap::math::Vector2(0, 0),
                                              utymap::math::Vector2(scale, scale),
                                              utymap::math::Vector2(scale, 0),
                                              meshContext_.geometryOptions, meshContext_.appearanceOptions);
    }
  }

  std::size_t findFirstIndex(const utymap::math::IntPoint &p,
                             const utymap::math::Polygon &polygon) const {
    utymap::math::Vector2 point(p.X/Scale, p.Y/Scale);

    std::size_t index = 0, size = polygon.points.size()/2;
    double minDistance = std::numeric_limits<double>::max();

    for (std::size_t i = 0; i < size; i += 2) {
      double distance = utymap::math::Vector2::distance(point,
                                                        utymap::math::Vector2(polygon.points[i],
                                                                              polygon.points[i + 1]));
      if (distance < minDistance) {
        minDistance = distance;
        index = i;
      }
    }

    return index;
  }
};

}
}

#endif // BUILDERS_BUILDINGS_ROOFS_MANSARDROOFBUILDER_HPP_DEFINED
