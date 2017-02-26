#ifndef APPLICATION_HPP_DEFINED
#define APPLICATION_HPP_DEFINED

#include "BoundingBox.hpp"
#include "CancellationToken.hpp"
#include "QuadKey.hpp"
#include "LodRange.hpp"
#include "builders/BuilderContext.hpp"
#include "builders/QuadKeyBuilder.hpp"
#include "builders/buildings/BuildingBuilder.hpp"
#include "builders/misc/BarrierBuilder.hpp"
#include "builders/misc/LampBuilder.hpp"
#include "builders/misc/LaneBuilder.hpp"
#include "builders/poi/TreeBuilder.hpp"
#include "builders/terrain/TerraBuilder.hpp"
#include "heightmap/FlatElevationProvider.hpp"
#include "heightmap/GridElevationProvider.hpp"
#include "heightmap/SrtmElevationProvider.hpp"
#include "index/GeoStore.hpp"
#include "index/InMemoryElementStore.hpp"
#include "index/PersistentElementStore.hpp"
#include "mapcss/MapCssParser.hpp"
#include "mapcss/StyleSheet.hpp"
#include "utils/CoreUtils.hpp"

#include "Callbacks.hpp"
#include "ExportElementVisitor.hpp"

#include <exception>
#include <fstream>
#include <memory>
#include <vector>

/// Exposes API for external usage.
class Application
{
public:
    enum class ElevationDataType
    {
        Flat = 0,
        Srtm,
        Grid
    };

    /// Composes object graph.
    Application(const char* dataPath, 
                OnError* errorCallback) :
        stringTable_(dataPath), geoStore_(stringTable_),
        flatEleProvider_(), srtmEleProvider_(dataPath), gridEleProvider_(dataPath),
        quadKeyBuilder_(geoStore_, stringTable_)
    {
        registerDefaultBuilders();
    }

    /// Registers stylesheet.
    void registerStylesheet(const char* path)
    {
        getStyleProvider(path);
    }

    /// Registers new in-memory store.
    void registerInMemoryStore(const char* key)
    {
        geoStore_.registerStore(key, utymap::utils::make_unique<utymap::index::InMemoryElementStore>(stringTable_));
    }

    /// Registers new persistent store.
    void registerPersistentStore(const char* key, const char* dataPath)
    {
        geoStore_.registerStore(key, utymap::utils::make_unique<utymap::index::PersistentElementStore>(dataPath, stringTable_));
    }

    /// Adds data to store.
    void addToStore(const char* key, 
                    const char* styleFile, 
                    const char* path, 
                    const utymap::QuadKey& quadKey, 
                    OnError* errorCallback)
    {
        safeExecute([&]() {
            geoStore_.add(key, path, quadKey, getStyleProvider(styleFile));
        }, errorCallback);
    }

    /// Adds data to store.
    void addToStore(const char* key, 
                    const char* styleFile, 
                    const char* path, 
                    const utymap::BoundingBox& bbox, 
                    const utymap::LodRange& range, 
                    OnError* errorCallback)
    {
        safeExecute([&]() {
            geoStore_.add(key, path, bbox, range, getStyleProvider(styleFile));
        }, errorCallback);
    }

    /// Adds data to store.
    void addToStore(const char* key, 
                    const char* styleFile, 
                    const char* path, 
                    const utymap::LodRange& range, 
                    OnError* errorCallback)
    {
        safeExecute([&]() {
            geoStore_.add(key, path, range, getStyleProvider(styleFile));
        }, errorCallback);
    }

    /// Adds element to store.
    void addToStore(const char* key,
                    const char* styleFile, 
                    const utymap::entities::Element& element, 
                    const utymap::LodRange& range, 
                    OnError* errorCallback)
    {
        safeExecute([&]() {
            geoStore_.add(key, element, range, getStyleProvider(styleFile));
        }, errorCallback);
    }

    bool hasData(const utymap::QuadKey& quadKey)
    {
        return geoStore_.hasData(quadKey);
    }

    /// Loads given quadKey.
    void loadQuadKey(const char* styleFile,
                     const utymap::QuadKey& quadKey,
                     const ElevationDataType& eleDataType,
                     OnMeshBuilt* meshCallback,
                     OnElementLoaded* elementCallback,
                     OnError* errorCallback,
                     utymap::CancellationToken* cancellationToken)
    {
        safeExecute([&]() {
            auto& styleProvider = getStyleProvider(styleFile);
            auto& eleProvider = getElevationProvider(quadKey, eleDataType);
            ExportElementVisitor elementVisitor(quadKey, stringTable_, styleProvider, eleProvider, elementCallback);
            quadKeyBuilder_.build(quadKey, styleProvider, eleProvider,
                [&meshCallback](const utymap::math::Mesh& mesh) {
                // NOTE do not notify if mesh is empty.
                if (!mesh.vertices.empty()) {
                    meshCallback(mesh.name.data(),
                        mesh.vertices.data(), static_cast<int>(mesh.vertices.size()),
                        mesh.triangles.data(), static_cast<int>(mesh.triangles.size()),
                        mesh.colors.data(), static_cast<int>(mesh.colors.size()),
                        mesh.uvs.data(), static_cast<int>(mesh.uvs.size()),
                        mesh.uvMap.data(), static_cast<int>(mesh.uvMap.size()));
                }
            }, [&elementVisitor](const utymap::entities::Element& element) {
                element.accept(elementVisitor);
            }, *cancellationToken);
        }, errorCallback);
    }

    /// Gets id for the string.
    std::uint32_t getStringId(const char* str) const
    {
        return stringTable_.getId(str);
    }

    /// Gets elevation for given geocoordinate using specific elevation provider.
    double getElevation(const utymap::QuadKey& quadKey, 
                        const ElevationDataType& elevationDataType,
                        const utymap::GeoCoordinate& coordinate) const
    {
        return getElevationProvider(quadKey, elevationDataType).getElevation(quadKey, coordinate);
    }

private:

    static void safeExecute(const std::function<void()>& action,  OnError* errorCallback)
    {
        try {
            action();
        }
        catch (std::exception& ex) {
            errorCallback(ex.what());
        }
    }

    const utymap::heightmap::ElevationProvider& getElevationProvider(const utymap::QuadKey& quadKey,
                                                                     const ElevationDataType& eleDataType) const
    {
        switch (eleDataType) {
            case ElevationDataType::Grid:
                return gridEleProvider_;
            case ElevationDataType::Srtm:
                return srtmEleProvider_;
            default:
                return flatEleProvider_;
        }
    }

    const utymap::mapcss::StyleProvider& getStyleProvider(const std::string& stylePath)
    {
        auto pair = styleProviders_.find(stylePath);
        if (pair != styleProviders_.end())
            return *pair->second;

        std::ifstream styleFile(stylePath);
        if (!styleFile.good())
            throw std::invalid_argument(std::string("Cannot read mapcss file:") + stylePath);

        // NOTE not safe, but don't want to use boost filesystem only for this task.
        std::string dir = stylePath.substr(0, stylePath.find_last_of("\\/") + 1);
        utymap::mapcss::MapCssParser parser(dir);
        utymap::mapcss::StyleSheet stylesheet = parser.parse(styleFile);
      
        styleProviders_.emplace(
            stylePath, 
            utymap::utils::make_unique<const utymap::mapcss::StyleProvider>(stylesheet, stringTable_));

        return *styleProviders_[stylePath];
    }

    void registerDefaultBuilders()
    {
        quadKeyBuilder_.registerElementBuilder("terrain", [&](const utymap::builders::BuilderContext& context) {
            return utymap::utils::make_unique<utymap::builders::TerraBuilder>(context);
        });

        quadKeyBuilder_.registerElementBuilder("building", [&](const utymap::builders::BuilderContext& context) {
            return utymap::utils::make_unique<utymap::builders::BuildingBuilder>(context);
        });

        quadKeyBuilder_.registerElementBuilder("tree", [&](const utymap::builders::BuilderContext& context) {
            return utymap::utils::make_unique<utymap::builders::TreeBuilder>(context);
        });

        quadKeyBuilder_.registerElementBuilder("barrier", [&](const utymap::builders::BuilderContext& context) {
            return utymap::utils::make_unique<utymap::builders::BarrierBuilder>(context);
        });

        quadKeyBuilder_.registerElementBuilder("lamp", [&](const utymap::builders::BuilderContext& context) {
            return utymap::utils::make_unique<utymap::builders::LampBuilder>(context);
        });

        quadKeyBuilder_.registerElementBuilder("lane", [&](const utymap::builders::BuilderContext& context) {
            return utymap::utils::make_unique<utymap::builders::LaneBuilder>(context);
        });
    }

    utymap::index::StringTable stringTable_;
    utymap::index::GeoStore geoStore_;

    utymap::heightmap::FlatElevationProvider flatEleProvider_;
    utymap::heightmap::SrtmElevationProvider srtmEleProvider_;
    utymap::heightmap::GridElevationProvider gridEleProvider_;

    utymap::builders::QuadKeyBuilder quadKeyBuilder_;
    std::unordered_map<std::string, std::unique_ptr<const utymap::mapcss::StyleProvider>> styleProviders_;
};

#endif // APPLICATION_HPP_DEFINED
