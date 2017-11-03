#ifndef BUILDERS_GENERATORS_TREEGENERATOR_HPP_DEFINED
#define BUILDERS_GENERATORS_TREEGENERATOR_HPP_DEFINED

#include "builders/BuilderContext.hpp"
#include "builders/MeshContext.hpp"
#include "lsys/LSystem.hpp"

namespace utymap {
namespace builders {

/// Defines generator which generates a tree like structures using lsystem.
class LSystemGenerator final {
 public:

  static void generate(const utymap::builders::BuilderContext &builderContext,
                       const utymap::mapcss::Style &style,
                       const utymap::lsys::LSystem &lsystem,
                       utymap::math::Mesh &mesh,
                       const utymap::GeoCoordinate &position,
                       double elevation);

  static void generate(const utymap::builders::BuilderContext &builderContext,
                       const utymap::mapcss::Style &style,
                       utymap::math::Mesh &mesh,
                       const utymap::GeoCoordinate &position,
                       double elevation);
};

}
}

#endif // BUILDERS_GENERATORS_TREEGENERATOR_HPP_DEFINED
