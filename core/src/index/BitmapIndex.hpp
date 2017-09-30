#ifndef INDEX_BITMAPINDEX_HPP_DEFINED
#define INDEX_BITMAPINDEX_HPP_DEFINED

#include "BoundingBox.hpp"
#include "LodRange.hpp"
#include "StringTable.hpp"
#include "QuadKey.hpp"
#include "entities/Element.hpp"

#include <ewah/ewah.h>
#include <unordered_map>

namespace utymap {
namespace index {

/// Provides the way to index strings in order to perform fast exact search.
class BitmapIndex {
 public:
  using Bitset = EWAHBoolArray<std::uint32_t>;
  using Bitmap = std::unordered_map<std::uint32_t, Bitset>;
  using Ids = std::vector<std::uint32_t>;

  /// Defines query to indexed string data.
  struct Query {
    /// Logical "not": result should not include any of these terms.
    std::vector<std::string> notTerms;
    /// Logical "and": result has to include all of these terms.
    std::vector<std::string> andTerms;
    /// Logical "or": result might include any of these terms.
    std::vector<std::string> orTerms;
    /// Bounding box constraint.
    utymap::BoundingBox boundingBox;
    /// LOD range constraint.
    utymap::LodRange range;
  };

  explicit BitmapIndex(const utymap::index::StringTable &stringTable);

  virtual ~BitmapIndex() = default;

  /// Adds element into index
  void add(const utymap::entities::Element &element,
           const utymap::QuadKey &quadKey,
           const std::uint32_t order);

  /// Performs search for relevant data match query.
  void search(const Query &query,
              utymap::entities::ElementVisitor &visitor);

 protected:
  /// Notifies that element with given store order id
  /// should be visited with visitor.
  virtual void notify(const utymap::QuadKey& quadKey,
                      const std::uint32_t order,
                      utymap::entities::ElementVisitor &visitor) = 0;

  /// Get bitmap for given quad key.
  virtual Bitmap& getBitmap(const utymap::QuadKey& quadKey) = 0;

 private:
  /// Gets tokens from element.
  std::vector<std::uint32_t> tokenize(const utymap::entities::Element &element);
  /// Stores tokens received from source into destination.
  void tokenize(const std::vector<std::string> &source, Ids &destination);
  /// Stores tokens received from source into destination.
  void tokenize(const std::string &str, Ids &destination);

  const StringTable& stringTable_;
};

}
}

#endif // INDEX_BITMAPINDEX_HPP_DEFINED
