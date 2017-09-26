#ifndef INDEX_STRINGINDEX_HPP_DEFINED
#define INDEX_STRINGINDEX_HPP_DEFINED

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
class StringIndex {
 public:
  using Bitset = EWAHBoolArray<std::uint32_t>;
  using Bitmap = std::unordered_map<std::uint32_t, Bitset>;
  using Ids = std::vector<std::uint32_t>;

  /// Defines query to indexed string data.
  struct Query {
    /// Logical "and" terms.
    std::vector<std::string> andTerms;
    /// Logical "or" terms.
    std::vector<std::string> orTerms;
    /// Logical "not" terms.
    std::vector<std::string> notTerms;
    /// Bounding box constraint.
    utymap::BoundingBox boundingBox;
    /// LOD range constraint.
    utymap::LodRange range;
  };

  explicit StringIndex(const utymap::index::StringTable &stringTable);

  /// Adds element into index
  void add(const utymap::entities::Element &element,
           const utymap::QuadKey &quadKey,
           std::uint32_t order);

  /// Performs search for relevant data match query.
  void search(const Query &query,
              utymap::entities::ElementVisitor &visitor);

 protected:
  /// Gets element by element store order id.
  virtual utymap::entities::Element& getElement(std::uint32_t order) = 0;
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

#endif // INDEX_STRINGINDEX_HPP_DEFINED
