#include <mapcss/StyleConsts.hpp>
#include "builders/generators/LSystemGenerator.hpp"
#include "builders/poi/TreeBuilder.hpp"
#include "entities/Node.hpp"
#include "entities/Way.hpp"
#include "entities/Relation.hpp"
#include "utils/GeometryUtils.hpp"
#include "utils/MeshUtils.hpp"

using namespace utymap::builders;
using namespace utymap::entities;
using namespace utymap::mapcss;
using namespace utymap::math;
using namespace utymap::utils;

namespace {
const std::string NodeMeshNamePrefix = "tree:";
const std::string WayMeshNamePrefix = "trees:";

const std::string TreeStepKey = "tree-step";
}

void TreeBuilder::visitNode(const utymap::entities::Node &node) {
  auto mesh = context_.meshPool.getSmall(utymap::utils::getMeshName(NodeMeshNamePrefix, node));
  Style style = context_.styleProvider.forElement(node, context_.quadKey.levelOfDetail);

  double elevation = context_.eleProvider.getElevation(context_.quadKey, node.coordinate);

  LSystemGenerator::generate(context_, style, mesh, node.coordinate, elevation);

  context_.meshCallback(mesh);
  context_.meshPool.release(std::move(mesh));
}

void TreeBuilder::visitWay(const utymap::entities::Way &way) {
  auto treeMesh = context_.meshPool.getSmall("");
  auto newMesh = context_.meshPool.getLarge(utymap::utils::getMeshName(WayMeshNamePrefix, way));

  Style style = context_.styleProvider.forElement(way, context_.quadKey.levelOfDetail);
  const auto center = context_.boundingBox.center();

  LSystemGenerator::generate(context_, style, treeMesh, center, 0);

  double treeStepInMeters = style.getValue(TreeStepKey);

  for (std::size_t i = 0; i < way.coordinates.size() - 1; ++i) {
    const auto &p0 = way.coordinates[i];
    const auto &p1 = way.coordinates[i + 1];
    utymap::utils::copyMeshAlong(context_.quadKey,
                                 center,
                                 p0,
                                 p1,
                                 treeMesh,
                                 newMesh,
                                 treeStepInMeters,
                                 context_.eleProvider);
  }

  context_.meshCallback(newMesh);
  context_.meshPool.release(std::move(treeMesh));
  context_.meshPool.release(std::move(newMesh));
}

void TreeBuilder::visitRelation(const utymap::entities::Relation &relation) {
  for (const auto &element : relation.elements) {
    element->accept(*this);
  }
}
