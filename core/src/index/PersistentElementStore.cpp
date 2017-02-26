#include "BoundingBox.hpp"
#include "entities/Node.hpp"
#include "entities/Way.hpp"
#include "entities/Area.hpp"
#include "entities/Relation.hpp"
#include "index/PersistentElementStore.hpp"
#include "utils/CoreUtils.hpp"
#include "utils/LruCache.hpp"

#include <fstream>
#include <sstream>
#include <mutex>

using namespace utymap;
using namespace utymap::index;
using namespace utymap::entities;
using namespace utymap::mapcss;
using namespace utymap::utils;

namespace {
    ///                                      Index file format
    ///   DESCRIPTION    |                       DETAILS                                                     |
    ///------------------------------------------------------------------------------------------------------|
    ///     Element      |  List of entries, each is represented by element id (8b) and file offset (4b)     |
    ///------------------------------------------------------------------------------------------------------|
    const std::string IndexFileExtension = ".idf";

    ///                                      Data file format
    ///------------------------------------------------------------------------------------------------------|
    ///   DESCRIPTION    |                       DETAILS                                                     |
    ///------------------------------------------------------------------------------------------------------|
    ///  (1b) Flags      |  00 00 00 AA, where:                                                              |
    ///                  |    AA - Element type (00 - Node, 01 - Way, 10 - Area, 11 - Relation)              |
    ///------------------------------------------------------------------------------------------------------|
    ///  (2b) Tags Size  |  Size of tag list where each tag represented by key-value pair (4b + 4b)          |
    ///------------------------------------------------------------------------------------------------------|
    ///      Tags        |               Tags data with size (2b)                                            |
    ///------------------------------------------------------------------------------------------------------|
    ///    Geometry      |             Geometry for Node, Way or Area                                        |
    ///       or         |                                                                                   |
    ///    Element List  |             Element list in the same format + id                                  |
    ///                  |                                                                                   |
    ///------------------------------------------------------------------------------------------------------|
    const std::string DataFileExtension = ".dat";

    /// Writes element to file stream.
    class ElementWriter final : public ElementVisitor
    {
    public:
        explicit ElementWriter(std::fstream& dataFile) : dataFile_(dataFile)
        {
        }

        void visitNode(const Node& node) override
        {
            writeFlags(0);
            writeTags(node.tags);
            writeCoordinate(node.coordinate);
        }

        void visitWay(const Way& way) override
        {
            writeFlags(1);
            writeTags(way.tags);
            std::uint16_t size = static_cast<std::uint16_t>(way.coordinates.size());
            dataFile_.write(reinterpret_cast<const char*>(&size), sizeof(size));
            for (const auto& coord : way.coordinates) {
                writeCoordinate(coord);
            }
        }

        void visitArea(const Area& area) override
        {
            writeFlags(2);
            writeTags(area.tags);
            // NOTE do not write the last one
            std::uint16_t size = static_cast<std::uint16_t>(area.coordinates.size());
            dataFile_.write(reinterpret_cast<const char*>(&size), sizeof(size));
            for (const auto& coord : area.coordinates) {
                writeCoordinate(coord);
            }
        }

        void visitRelation(const Relation& relation) override
        {
            writeFlags(3);
            writeTags(relation.tags);
            std::uint16_t elementSize = static_cast<std::uint16_t>(relation.elements.size());
            dataFile_.write(reinterpret_cast<const char*>(&elementSize), sizeof(elementSize));
            for (const auto& element : relation.elements) {
                dataFile_.write(reinterpret_cast<const char*>(&element->id), sizeof(element->id));
                element->accept(*this);
            }
        }

    private:

        void writeFlags(const std::uint8_t flags) const
        {
            dataFile_.write(reinterpret_cast<const char*>(&flags), sizeof(flags));
        }

        void writeTags(const std::vector<Tag>& tags) const
        {
            std::uint16_t size = static_cast<std::uint16_t>(tags.size());
            dataFile_.write(reinterpret_cast<const char*>(&size), sizeof(size));
            for (const auto& tag : tags) {
                dataFile_.write(reinterpret_cast<const char*>(&tag.key), sizeof(tag.key));
                dataFile_.write(reinterpret_cast<const char*>(&tag.value), sizeof(tag.value));
            }
        }

        void writeCoordinate(const GeoCoordinate& coord) const
        {
            dataFile_.write(reinterpret_cast<const char*>(&coord.latitude), sizeof(coord.latitude));
            dataFile_.write(reinterpret_cast<const char*>(&coord.longitude), sizeof(coord.longitude));
        }

        std::fstream& dataFile_;
    };

    /// Reads element from file stream.
    class ElementReader final
    {
    public:
        explicit ElementReader(std::fstream& dataFile) : dataFile_(dataFile)
        {
        }

        std::shared_ptr<Element> readElement(std::uint64_t id, std::uint32_t offset)
        {
            dataFile_.seekg(offset, std::ios::beg);
            auto element = readElement();
            element->id = id;
            return element;
        }

    private:

        std::shared_ptr<Element> readElement()
        {
            std::uint8_t flags;
            dataFile_.read(reinterpret_cast<char*>(&flags), sizeof(flags));
            std::uint8_t elementType = flags & 0x3;

            switch (elementType) {
            case 0:
                return readNode();
            case 1:
                return readWay();
            case 2:
                return readArea();
            default:
                return readRelation();
            }
        }

        std::shared_ptr<Node> readNode() const
        {
            auto node = std::make_shared<Node>();
            node->tags = readTags();
            node->coordinate = readCoordinate();
            return node;
        }

        std::shared_ptr<Way> readWay() const
        {
            auto way = std::make_shared<Way>();
            way->tags = readTags();
            way->coordinates = readCoordinates();
            return way;
        }

        std::shared_ptr<Area> readArea() const
        {
            auto area = std::make_shared<Area>();
            area->tags = readTags();
            area->coordinates = readCoordinates();
            return area;
        }

        std::shared_ptr<Relation> readRelation()
        {
            auto relation = std::make_shared<Relation>();
            relation->tags = readTags();
            std::uint16_t elementSize;
            dataFile_.read(reinterpret_cast<char*>(&elementSize), sizeof(elementSize));

            for (std::uint16_t i = 0; i < elementSize; ++i) {
                std::uint64_t id;
                dataFile_.read(reinterpret_cast<char*>(&id), sizeof(id));
                auto element = readElement();
                element->id = id;
                relation->elements.push_back(element);
            }
            return relation;
        }

        GeoCoordinate readCoordinate() const
        {
            GeoCoordinate coord;
            dataFile_.read(reinterpret_cast<char*>(&coord.latitude), sizeof(coord.latitude));
            dataFile_.read(reinterpret_cast<char*>(&coord.longitude), sizeof(coord.longitude));
            return coord;
        }

        std::vector<GeoCoordinate> readCoordinates() const
        {
            std::uint16_t coordSize;
            dataFile_.read(reinterpret_cast<char*>(&coordSize), sizeof(coordSize));

            std::vector<GeoCoordinate> coordinates;
            coordinates.reserve(coordSize);
            for (std::size_t i = 0; i < coordSize; ++i) {
                coordinates.push_back(readCoordinate());
            }

            return std::move(coordinates);
        }

        std::vector<Tag> readTags() const
        {
            std::uint16_t tagSize;
            dataFile_.read(reinterpret_cast<char*>(&tagSize), sizeof(tagSize));

            std::vector<Tag> tags;
            for (std::size_t i = 0; i < tagSize; ++i) {
                Tag tag;
                dataFile_.read(reinterpret_cast<char*>(&tag.key), sizeof(tag.key));
                dataFile_.read(reinterpret_cast<char*>(&tag.value), sizeof(tag.value));
                tags.push_back(tag);
            }

            return std::move(tags);
        }

        std::fstream& dataFile_;
    };
}

// TODO improve thread safety!
class PersistentElementStore::PersistentElementStoreImpl final
{
    struct QuadKeyData
    {
        std::unique_ptr<std::fstream> dataFile;
        std::unique_ptr<std::fstream> indexFile;

        QuadKeyData(const std::string& dataPath, const std::string& indexPath) :
            dataFile(utymap::utils::make_unique<std::fstream>()),
            indexFile(utymap::utils::make_unique<std::fstream>())
        {
            using std::ios;
            dataFile->open(dataPath, ios::in | ios::out | ios::binary | ios::app | ios::ate);
            indexFile->open(indexPath, ios::in | ios::out | ios::binary | ios::app | ios::ate);
        }

        QuadKeyData(const QuadKeyData&) = delete;
        QuadKeyData& operator=(const QuadKeyData&) = delete;

        QuadKeyData(QuadKeyData&& other) :
            dataFile(std::move(other.dataFile)),
            indexFile(std::move(other.indexFile))
        {
        }

        ~QuadKeyData()
        {
            if (dataFile != nullptr && dataFile->good()) dataFile->close();
            if (indexFile != nullptr && indexFile->good()) indexFile->close();
        }
    };

public:
    explicit PersistentElementStoreImpl(const std::string& dataPath) :
        dataPath_(dataPath),
        dataCache_(8)
    {
    }

    void store(const Element& element, const QuadKey& quadKey)
    {
        const auto& quadKeyData = getQuadKeyData(quadKey);

        // write element data
        std::uint32_t offset = static_cast<std::uint32_t>(quadKeyData.dataFile->tellg());

        ElementWriter visitor(*quadKeyData.dataFile);
        element.accept(visitor);

        // write element index
        quadKeyData.indexFile->seekg(0, std::ios::end);
        quadKeyData.indexFile->write(reinterpret_cast<const char*>(&element.id), sizeof(element.id));
        quadKeyData.indexFile->write(reinterpret_cast<const char*>(&offset), sizeof(offset));
    }

    void search(const QuadKey& quadKey, ElementVisitor& visitor, const utymap::CancellationToken& cancelToken)
    {
        const auto& quadKeyData = getQuadKeyData(quadKey);

        std::uint32_t count = static_cast<std::uint32_t>(quadKeyData.indexFile->tellg() /
                (sizeof(std::uint64_t) + sizeof(std::uint32_t)));

        ElementReader reader(*quadKeyData.dataFile);

        quadKeyData.indexFile->seekg(0, std::ios::beg);
        for (std::uint32_t i = 0; i < count; ++i) {
            if (cancelToken.isCancelled())
                break;

            std::uint64_t id;
            std::uint32_t offset;
            quadKeyData.indexFile->read(reinterpret_cast<char*>(&id), sizeof(id));
            quadKeyData.indexFile->read(reinterpret_cast<char*>(&offset), sizeof(offset));

            reader.readElement(id, offset)->accept(visitor);
        }
    }

    bool hasData(const QuadKey& quadKey) const
    {
        std::ifstream file(getFilePath(quadKey, DataFileExtension));
        return file.good();
    }

private:
    /// Gets full file path for given quadkey
    std::string getFilePath(const QuadKey& quadKey, const std::string& extension) const
    {
        std::stringstream ss;
        ss << dataPath_ << quadKey.levelOfDetail << "/" << GeoUtils::quadKeyToString(quadKey) << extension;
        return ss.str();
    }

    /// Ensures that open/close function is called on files.
    const QuadKeyData& getQuadKeyData(const QuadKey& quadKey)
    {
        // TODO this is not 100% thread safe as we return reference to data
        // object it points can be destroyed right after.
        std::lock_guard<std::mutex> lock(lock_);
        if (dataCache_.exists(quadKey))
            return dataCache_.get(quadKey);

        dataCache_.put(quadKey, QuadKeyData(getFilePath(quadKey, DataFileExtension), getFilePath(quadKey, IndexFileExtension)));
        
        return dataCache_.get(quadKey);
    }

    mutable std::mutex lock_;
    const std::string dataPath_;
    utymap::utils::LruCache<QuadKey, QuadKeyData, QuadKey::Comparator> dataCache_;
};

PersistentElementStore::PersistentElementStore(const std::string& dataPath, const StringTable& stringTable) :
    ElementStore(stringTable), pimpl_(utymap::utils::make_unique<PersistentElementStoreImpl>(dataPath))
{
}

PersistentElementStore::~PersistentElementStore()
{
}

void PersistentElementStore::storeImpl(const Element& element, const QuadKey& quadKey)
{
    pimpl_->store(element, quadKey);
}

void PersistentElementStore::search(const QuadKey& quadKey, ElementVisitor& visitor, const utymap::CancellationToken& cancelToken)
{
    pimpl_->search(quadKey, visitor, cancelToken);
}

bool PersistentElementStore::hasData(const QuadKey& quadKey) const
{
    return pimpl_->hasData(quadKey);
}
