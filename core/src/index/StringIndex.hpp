#ifndef INDEX_STRINGINDEX_HPP_DEFINED
#define INDEX_STRINGINDEX_HPP_DEFINED

#include "BoundingBox.hpp"
#include "LodRange.hpp"
#include "StringTable.hpp"
#include "QuadKey.hpp"
#include "entities/Element.hpp"

namespace utymap {
namespace index {

/// Provides the way to index strings in order to perform fast exact search
class StringIndex final {
 public:

  /// Defines query to indexed data.
  struct Query {
    /// Logical "and" terms
    std::vector<std::string> andTerms;
    /// Logical "or" terms
    std::vector<std::string> orTerms;
    /// Logical "not" terms
    std::vector<std::string> notTerms;
    /// Bounding box constraint
    utymap::BoundingBox &bbox;
    /// LOD range constraint
    utymap::LodRange &range;
  };

  StringIndex(const std::function<utymap::entities::Element&(std::uint32_t&)> &elementAccessor,
              const utymap::index::StringTable &stringTable);

  ~StringIndex();

  /// Adds element into index
  void add(const utymap::entities::Element &element,
           const utymap::QuadKey &quadKey,
           const std::uint32_t order);

  /// Performs search for relevant data match query
  void search(const Query &query,
              utymap::entities::ElementVisitor &visitor);

 private:
  class StringIndexImpl;
  std::unique_ptr<StringIndexImpl> pimpl_;
};

}
}

#endif // INDEX_STRINGINDEX_HPP_DEFINED
