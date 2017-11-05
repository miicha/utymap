#include "builders/generators/CylinderGenerator.hpp"
#include "builders/generators/IcoSphereGenerator.hpp"
#include "builders/generators/LSystemGenerator.hpp"
#include <lsys/Turtle3d.hpp>
#include "utils/GeometryUtils.hpp"

using namespace utymap::builders;
using namespace utymap::lsys;
using namespace utymap::mapcss;
using namespace utymap::math;
using namespace utymap::utils;

namespace {

const std::string Prefix = "lsys-";
const std::string SizeKey = Prefix + StyleConsts::SizeKey();
const std::string GradientsKey = Prefix + "colors";
const std::string TextureIndicesKey = Prefix + "texture-indices";
const std::string TextureTypesKey = Prefix + "texture-types";
const std::string TextureScalesKey = Prefix + "texture-scales";

/// Parses appearances from comma separated representation.
std::vector<MeshBuilder::AppearanceOptions> createAppearances(const BuilderContext &builderContext,
                                                              const Style &style) {
  auto colorStrings = utymap::utils::splitBy(',', style.getString(GradientsKey));
  auto indices = utymap::utils::splitBy(',', style.getString(TextureIndicesKey));
  auto types = utymap::utils::splitBy(',', style.getString(TextureTypesKey));
  auto scales = utymap::utils::splitBy(',', style.getString(TextureScalesKey));

  std::vector<MeshBuilder::AppearanceOptions> appearances;
  appearances.reserve(colorStrings.size());
  for (std::size_t i = 0; i < colorStrings.size(); ++i) {
    auto textureIndex = utymap::utils::lexicalCast<std::uint16_t>(indices.at(i));
    appearances.push_back(MeshBuilder::AppearanceOptions(
        builderContext.styleProvider.getGradient(colorStrings.at(i)),
        0,
        textureIndex,
        builderContext.styleProvider.getTexture(textureIndex, types.at(i)).random(0),
        utymap::utils::lexicalCast<double>(scales.at(i))));
  }

  return appearances;
}

/// Gets appearance by index safely.
const MeshBuilder::AppearanceOptions &getAppearanceByIndex(std::size_t index,
                                                           const std::vector<MeshBuilder::AppearanceOptions> &appearances) {
  return appearances.at(index%appearances.size());
}

class DickTurtle : public Turtle3d {
  static std::unordered_map<std::string, void (DickTurtle::*)()> WordMap;
 public:
  DickTurtle(const BuilderContext &builderContext,
             const Style &style,
             Mesh &mesh,
             const utymap::GeoCoordinate &position,
             double minHeight) :
      builderContext_(builderContext),
      appearances_(createAppearances(builderContext, style)),
      cylinderContext_(utymap::utils::make_unique<MeshContext>(mesh, style, getAppearanceByIndex(0, appearances_))),
      icoSphereContext_(utymap::utils::make_unique<MeshContext>(mesh, style, getAppearanceByIndex(1, appearances_))),
      cylinderGenerator_(builderContext, *cylinderContext_),
      icoSphereGenerator_(builderContext, *icoSphereContext_),
      translationFunc_(std::bind(&DickTurtle::translate, this, std::placeholders::_1)),
      position_(position),
      minHeight_(minHeight) {
    cylinderGenerator_
        .setMaxSegmentHeight(0)
        .setRadialSegments(7);

    icoSphereGenerator_
        .setRecursionLevel(1);

    double size = style.getValue(SizeKey);

    state_.length = size;
    state_.width = size;

    cylinderGenerator_.setTranslation(translationFunc_);
    icoSphereGenerator_.setTranslation(translationFunc_);
  }

  void say(const std::string &word) override {
    (this->*WordMap.at(word))();
  }

  void moveForward() override {
    addCylinder();
  }

  void switchStyle() override {
    Turtle3d::switchStyle();
    updateStyles();
  }

 private:
  void addSphere() {
    icoSphereGenerator_
        .setCenter(state_.position)
        .setSize(getSize())
        .generate();

    builderContext_.meshBuilder.writeTextureMappingInfo(icoSphereContext_->mesh,
                                                        icoSphereContext_->appearanceOptions);
    jumpForward();
  }

  void addCylinder() {
    cylinderGenerator_
        .setCenter(state_.position)
        .setDirection(state_.direction, state_.right)
        .setSize(getSize())
        .generate();

    builderContext_.meshBuilder.writeTextureMappingInfo(cylinderContext_->mesh,
                                                        cylinderContext_->appearanceOptions);
    jumpForward();
  }

  void addCone() {
    cylinderGenerator_
        .setCenter(state_.position)
        .setDirection(state_.direction, state_.right)
        .setSize(getSize(), Vector3(0, 0, 0))
        .generate();

    builderContext_.meshBuilder.writeTextureMappingInfo(cylinderContext_->mesh,
                                                        cylinderContext_->appearanceOptions);
    jumpForward();
  }

  void updateStyles() {
    cylinderContext_ = utymap::utils::make_unique<MeshContext>(cylinderContext_->mesh, cylinderContext_->style,
                                                               getAppearanceByIndex(state_.texture, appearances_));
    icoSphereContext_ = utymap::utils::make_unique<MeshContext>(icoSphereContext_->mesh, icoSphereContext_->style,
                                                                getAppearanceByIndex(state_.texture + 1, appearances_));

    cylinderGenerator_.setContext(*cylinderContext_);
    icoSphereGenerator_.setContext(*icoSphereContext_);
  }

  Vector3 translate(const Vector3 &v) const {
    auto coordinate = GeoUtils::worldToGeo(position_, v.x, v.z);
    return Vector3(coordinate.longitude, v.y + minHeight_, coordinate.latitude);
  }

  Vector3 getSize() const {
    return Vector3(state_.width, state_.length, state_.width);
  }

  const BuilderContext &builderContext_;

  std::vector<MeshBuilder::AppearanceOptions> appearances_;

  std::unique_ptr<MeshContext> cylinderContext_;
  std::unique_ptr<MeshContext> icoSphereContext_;

  CylinderGenerator cylinderGenerator_;
  IcoSphereGenerator icoSphereGenerator_;

  AbstractGenerator::TranslateFunc translationFunc_;

  utymap::GeoCoordinate position_;
  double minHeight_;
};

std::unordered_map<std::string, void (DickTurtle::*)()> DickTurtle::WordMap =
    {
        {"cone", &DickTurtle::addCone},
        {"sphere", &DickTurtle::addSphere},
        {"cylinder", &DickTurtle::addCylinder},
    };

}

void LSystemGenerator::generate(const BuilderContext &builderContext,
                                const Style &style,
                                Mesh &mesh,
                                const utymap::GeoCoordinate &position,
                                double elevation) {

  const auto &lsystem = builderContext.styleProvider
      .getLsystem(style.getString(StyleConsts::LSystemKey()));

  generate(builderContext, style, lsystem, mesh, position, elevation);
}

void LSystemGenerator::generate(const BuilderContext &builderContext,
                                const Style &style,
                                const LSystem &lsystem,
                                Mesh &mesh,
                                const utymap::GeoCoordinate &position,
                                double elevation) {
  DickTurtle(builderContext, style, mesh, position, elevation)
      .run(lsystem);
}
