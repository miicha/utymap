#ifndef SEARCH_HPP_DEFINED
#define SEARCH_HPP_DEFINED

#include "entities/Node.hpp"
#include "entities/Way.hpp"
#include "entities/Area.hpp"
#include "entities/Relation.hpp"
#include "math/Mesh.hpp"

/// Exposes search API.
class Search {
public:
  explicit Search(Context& context) :
    context_(context) {}

  /// Gets data represented by elements matching given text query.
  /// Note, that styles and real elevation height are not included.
  void getDataByText(int tag,                                  // request tag
                     const char *notTerms,                     // NOT terms
                     const char *andTerms,                     // AND terms
                     const char *orTerms,                      // OR terms
                     double minLatitude,                       // min latitude
                     double minLongitude,                      // min longitude
                     double maxLatitude,                       // max latitude
                     double maxLongitude,                      // max longitude
                     int startLod,                             // start lod
                     int endLod,                               // end lod
                     OnElementLoaded *elementCallback,         // element callback
                     OnError *errorCallback,                   // error callback
                     utymap::CancellationToken *cancellationToken) {
    utymap::BoundingBox bbox(utymap::GeoCoordinate(minLatitude, minLongitude),
                             utymap::GeoCoordinate(maxLatitude, maxLongitude));
    utymap::LodRange lodRange(startLod, endLod);
    ExportElementVisitor elementVisitor(tag, context_.stringTable, elementCallback);
    ::safeExecute([&]() {
      context_.geoStore.search(notTerms, andTerms, orTerms, bbox, lodRange, elementVisitor, *cancellationToken);
    }, errorCallback);
  }

  /// Gets data represented by elements and meshes for given quad key.
  void getDataByQuadKey(int tag,                                 // request tag
                        const char *styleFile,                   // style file
                        int tileX, int tileY, int levelOfDetail, // quad key info
                        int eleDataType,                         // elevation data type
                        OnMeshBuilt *meshCallback,               // mesh callback
                        OnElementLoaded *elementCallback,        // element callback
                        OnError *errorCallback,                  // error callback
                        utymap::CancellationToken *cancellationToken) {
    utymap::QuadKey quadKey(levelOfDetail, tileX, tileY);
    auto eleProviderType = static_cast<ElevationDataType>(eleDataType);
    ::safeExecute([&]() {
      auto &styleProvider = context_.getStyleProvider(styleFile);
      auto &eleProvider = context_.getElevationProvider(quadKey, eleProviderType);
      ExportElementVisitor elementVisitor(tag, quadKey, context_.stringTable, styleProvider, eleProvider, elementCallback);
      context_.quadKeyBuilder.build(
        quadKey, styleProvider, eleProvider,
        [&meshCallback, tag](const utymap::math::Mesh &mesh) {
        // NOTE do not notify if mesh is empty.
        if (!mesh.vertices.empty()) {
          meshCallback(tag, mesh.name.data(),
            mesh.vertices.data(), static_cast<int>(mesh.vertices.size()),
            mesh.triangles.data(), static_cast<int>(mesh.triangles.size()),
            mesh.colors.data(), static_cast<int>(mesh.colors.size()),
            mesh.uvs.data(), static_cast<int>(mesh.uvs.size()),
            mesh.uvMap.data(), static_cast<int>(mesh.uvMap.size()));
        }
      }, [&elementVisitor](const utymap::entities::Element &element) {
        element.accept(elementVisitor);
      }, *cancellationToken);
    }, errorCallback);
  }

  /// Gets elevation for given geocoordinate using specific elevation provider.
  double getElevationByQuadKey(int tileX, int tileY, int levelOfDetail, // quadkey info
                               int eleDataType,                         // elevation data type
                               double latitude, double longitude) const {
    utymap::QuadKey quadKey(levelOfDetail, tileX, tileY);
    auto eleProviderType = static_cast<ElevationDataType>(eleDataType);
    utymap::GeoCoordinate coordinate(latitude, longitude);
    return context_.getElevationProvider(quadKey, eleProviderType).getElevation(quadKey, coordinate);
  }

private:
  Context &context_;

  /// Exports elements to external code using element callback.
  struct ExportElementVisitor : public utymap::entities::ElementVisitor {
    using Tags = std::vector<utymap::formats::Tag>;
    using Coordinates = std::vector<utymap::GeoCoordinate>;

    /// Creates visitor which supports styles and elevation.
    ExportElementVisitor(int tag,
      const utymap::QuadKey &quadKey,
      utymap::index::StringTable &stringTable,
      const utymap::mapcss::StyleProvider &styleProvider,
      const utymap::heightmap::ElevationProvider &eleProvider,
      OnElementLoaded *elementCallback) :
      tag_(tag), quadKey_(quadKey), stringTable_(stringTable), styleProvider_(&styleProvider),
      eleProvider_(&eleProvider), elementCallback_(elementCallback) {}

    /// Creates visitor which does not return style and real height.
    ExportElementVisitor(int tag,
      utymap::index::StringTable &stringTable,
      OnElementLoaded *elementCallback) :
      tag_(tag), quadKey_(), stringTable_(stringTable), styleProvider_(nullptr),
      eleProvider_(nullptr), elementCallback_(elementCallback) {}

    void visitNode(const utymap::entities::Node &node) override {
      visitElement(node, Coordinates{ node.coordinate });
    }

    void visitWay(const utymap::entities::Way &way) override {
      visitElement(way, way.coordinates);
    }

    void visitArea(const utymap::entities::Area &area) override {
      visitElement(area, area.coordinates);
    }

    void visitRelation(const utymap::entities::Relation &relation) override {
      // TODO return geometry
      visitElement(relation, Coordinates{ { 0, 0 } });
    }
  private:
    void visitElement(const utymap::entities::Element &element,
      const Coordinates &coordinates) {
      auto ctags = getTags(element);
      auto cstyles = getStyles(element);

      fillVertices(coordinates);

      elementCallback_(tag_, element.id,
        ctags.data(), static_cast<int>(ctags.size()),
        vertices_.data(), static_cast<int>(vertices_.size()),
        cstyles.data(), static_cast<int>(cstyles.size()));

      // NOTE clear vectors after raw array data is consumed by external code
      vertices_.clear();
      tagStrings_.clear();
      styleStrings_.clear();
    }

    /// Gets tags.
    std::vector<const char *> getTags(const utymap::entities::Element &element) {
      std::vector<const char *> ctags;
      tagStrings_.reserve(element.tags.size() * 2);
      ctags.reserve(element.tags.size() * 2);
      for (std::size_t i = 0; i < element.tags.size(); ++i) {
        const utymap::entities::Tag &tag = element.tags[i];
        auto key = stringTable_.getString(tag.key);
        auto value = stringTable_.getString(tag.value);
        tagStrings_.push_back(*key);
        tagStrings_.push_back(*value);
        ctags.push_back(tagStrings_[tagStrings_.size() - 2].c_str());
        ctags.push_back(tagStrings_[tagStrings_.size() - 1].c_str());
      }

      return std::move(ctags);
    }

    /// Gets and converts style to their string representation.
    std::vector<const char *> getStyles(const utymap::entities::Element &element) {
      std::vector<const char *> cstyles;
      if (styleProvider_ == nullptr)
        return cstyles;

      utymap::mapcss::Style style = styleProvider_->forElement(element, quadKey_.levelOfDetail);
      auto declarations = style.declarations();
      styleStrings_.reserve(declarations.size() * 2);
      cstyles.reserve(declarations.size());
      for (const auto &declaration : declarations) {
        auto decKey = stringTable_.getString(declaration->key());
        styleStrings_.push_back(*decKey);
        styleStrings_.push_back(declaration->value());
        cstyles.push_back(styleStrings_[styleStrings_.size() - 2].c_str());
        cstyles.push_back(styleStrings_[styleStrings_.size() - 1].c_str());
      }

      return std::move(cstyles);
    }

    /// Converts geometry.
    void fillVertices(const Coordinates &coordinates) {
      vertices_.reserve(coordinates.size() * 3);
      for (std::size_t i = 0; i < coordinates.size(); ++i) {
        const utymap::GeoCoordinate coordinate = coordinates[i];
        vertices_.push_back(coordinate.longitude);
        vertices_.push_back(coordinate.latitude);
        vertices_.push_back(eleProvider_ == nullptr ? 0 : eleProvider_->getElevation(quadKey_, coordinate));
      }
    }

    const int tag_;
    const utymap::QuadKey quadKey_;
    utymap::index::StringTable &stringTable_;

    const utymap::mapcss::StyleProvider *styleProvider_;
    const utymap::heightmap::ElevationProvider *eleProvider_;
    OnElementLoaded *elementCallback_;

    std::vector<double> vertices_;
    std::vector<std::string> tagStrings_;   // holds temporary tag strings
    std::vector<std::string> styleStrings_; // holds temporary style strings
  };
};

#endif // SEARCH_HPP_DEFINED
