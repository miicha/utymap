#include "entities/Node.hpp"
#include "entities/Way.hpp"
#include "entities/Area.hpp"
#include "entities/Relation.hpp"
#include "index/ElementGeometryVisitor.hpp"
#include "index/ElementVisitorFilter.hpp"
#include "index/InMemoryElementStore.hpp"
#include "index/BitmapIndex.hpp"

using namespace utymap;
using namespace utymap::index;
using namespace utymap::entities;
using namespace utymap::mapcss;

namespace {
using Elements = std::vector<std::shared_ptr<Element>>;
using ElementMap = std::map<QuadKey, Elements, QuadKey::Comparator>;
using Bitmaps = std::map<QuadKey, BitmapIndex::Bitmap, QuadKey::Comparator>;

class ElementMapVisitor : public ElementVisitor {
 public:
  ElementMapVisitor(Elements &elements) :
      elements_(elements) {
  }

  void visitNode(const utymap::entities::Node &node) override {
    elements_.push_back(std::make_shared<Node>(node));
  }

  void visitWay(const utymap::entities::Way &way) override {
    elements_.push_back(std::make_shared<Way>(way));
  }

  void visitArea(const utymap::entities::Area &area) override {
    elements_.push_back(std::make_shared<Area>(area));
  }

  void visitRelation(const utymap::entities::Relation &relation) override {
    elements_.push_back(std::make_shared<Relation>(relation));
  }
 private:
  Elements &elements_;
};

class InMemoryStringIndex : public BitmapIndex {
 public:
  InMemoryStringIndex(const StringTable &stringTable,
                      const ElementMap &elementsMap) :
      BitmapIndex(stringTable),
      elementsMap_(elementsMap),
      bitmaps_() {}

  void erase(const utymap::QuadKey &quadKey) override {
    bitmaps_.erase(quadKey);
  }

 protected:
  void notify(const utymap::QuadKey& quadKey,
              const std::uint32_t order,
              ElementVisitor &visitor) override {
    auto elements = elementsMap_.find(quadKey);
    if (elements==elementsMap_.end())
      throw std::domain_error("Cannot find element in memory while searching text!");

    elements->second.at(order)->accept(visitor);
  }

  Bitmap &getBitmap(const utymap::QuadKey &quadKey) override {
    return bitmaps_[quadKey];
  }

  bool hasData(const utymap::QuadKey &quadKey) const override {
    return elementsMap_.find(quadKey) != elementsMap_.end();
  }

 private:
  const ElementMap &elementsMap_;
  Bitmaps bitmaps_;
};
}

class InMemoryElementStore::InMemoryElementStoreImpl {
 public:
  explicit InMemoryElementStoreImpl(const StringTable &stringTable) :
      stringTable_(stringTable),
      elementsMap_(),
      stringIndex_(stringTable, elementsMap_) {}

  void search(const BitmapIndex::Query query,
              ElementVisitor &visitor,
              const utymap::CancellationToken &cancelToken) {
    ElementVisitorFilter filter(visitor, [&](const Element &element) {
      return ElementGeometryVisitor::intersects(element, query.boundingBox);
    });
    stringIndex_.search(query, filter);
  }

  void search(const utymap::QuadKey &quadKey,
              ElementVisitor &visitor,
              const utymap::CancellationToken &cancelToken) {
    auto it = begin(quadKey);
    // No elements for this quad key
    if (it == end())
      return;

    for (const auto &element : it->second) {
      if (cancelToken.isCancelled()) break;
      element->accept(visitor);
    }
  }

  bool hasData(const utymap::QuadKey &quadKey) const {
    return elementsMap_.find(quadKey)!=elementsMap_.end();
  }

  void store(const utymap::entities::Element &element, const QuadKey &quadKey) {
    auto &elements = elementsMap_[quadKey];

    stringIndex_.add(element, quadKey, static_cast<std::uint32_t>(elements.size()));
    ElementMapVisitor visitor(elements);
    element.accept(visitor);
  }

  void erase(const utymap::QuadKey &quadKey) {
    elementsMap_.erase(quadKey);
    stringIndex_.erase(quadKey);
  }

  void erase(const utymap::BoundingBox &bbox, const utymap::LodRange &range) {
    throw std::domain_error("Deletion by bounding box and lod range is not implemented.");
  }

 private:
  ElementMap::const_iterator begin(const utymap::QuadKey &quadKey) const {
    return elementsMap_.find(quadKey);
  }

  ElementMap::const_iterator end() const {
    return elementsMap_.cend();
  }

  const StringTable &stringTable_;
  ElementMap elementsMap_;
  InMemoryStringIndex stringIndex_;
};

InMemoryElementStore::InMemoryElementStore(const StringTable &stringTable) :
    ElementStore(stringTable), pimpl_(utymap::utils::make_unique<InMemoryElementStoreImpl>(stringTable)) {
}

InMemoryElementStore::~InMemoryElementStore() {
}

void InMemoryElementStore::save(const Element &element, const QuadKey &quadKey) {
  pimpl_->store(element, quadKey);
}

bool InMemoryElementStore::hasData(const utymap::QuadKey &quadKey) const {
  return pimpl_->hasData(quadKey);
}

void InMemoryElementStore::search(const std::string &notTerms,
                                  const std::string &andTerms,
                                  const std::string &orTerms,
                                  const utymap::BoundingBox &bbox,
                                  const utymap::LodRange &range,
                                  ElementVisitor &visitor,
                                  const utymap::CancellationToken &cancelToken) {
  BitmapIndex::Query query = { notTerms, andTerms, orTerms, bbox, range };
  pimpl_->search(query, visitor, cancelToken);
}

void InMemoryElementStore::search(const utymap::QuadKey &quadKey,
                                  ElementVisitor &visitor,
                                  const utymap::CancellationToken &cancelToken) {
  pimpl_->search(quadKey, visitor, cancelToken);
}

void InMemoryElementStore::erase(const utymap::QuadKey &quadKey) {
  pimpl_->erase(quadKey);
}

void InMemoryElementStore::erase(const utymap::BoundingBox &bbox,
                                 const utymap::LodRange &range) {
  pimpl_->erase(bbox, range);
}
