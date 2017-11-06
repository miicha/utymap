#include "builders/terrain/ExteriorGenerator.hpp"

using namespace utymap;
using namespace utymap::builders;
using namespace utymap::entities;
using namespace utymap::mapcss;
using namespace utymap::math;

namespace {
const std::string TerrainMeshName = "terrain_exterior";
}

class ExteriorGenerator::ExteriorGeneratorImpl {
  // TODO
};

ExteriorGenerator::ExteriorGenerator(const BuilderContext &context,
                                     const Style &style,
                                     const IntPath &tileRect) :
    TerraGenerator(context, style, tileRect, context.meshPool.getSmall(TerrainMeshName)),
    p_impl(utymap::utils::make_unique<ExteriorGeneratorImpl>()) {
}

void ExteriorGenerator::onNewRegion(const std::string &type,
                                    const Element &element,
                                    const Style &style,
                                    const std::shared_ptr<Region> &region) {
}

void ExteriorGenerator::generateFrom(const std::vector<Layer> &layers) {
  // TODO
  context_.meshPool.release(std::move(mesh_));
}

ExteriorGenerator::~ExteriorGenerator() {
}

void ExteriorGenerator::addGeometry(int level, Polygon &polygon, const RegionContext &regionContext) {
}
