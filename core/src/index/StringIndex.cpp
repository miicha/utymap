#include "StringIndex.hpp"
#include "utils/CoreUtils.hpp"

#include <ewah/ewah.h>

#include<boost/tokenizer.hpp>
#include <map>
#include <unordered_map>

using namespace utymap::entities;
using namespace utymap::index;

namespace {
  /// Defines symbols considered as token delimeters
  const boost::char_separator<char> separator(":;!@#$%^&*(){}[],.?`\\/\"\'");

  struct TokenizedQuery {
    std::vector<std::uint32_t> andTerms;
    std::vector<std::uint32_t> orTerms;
    std::vector<std::uint32_t> notTerms;
  };
}

/// Specifies accessor used to get back element by its order
using ElementAccessor = std::function<Element&(std::uint32_t&)>;
/// Specifies bitmap where row is string id (termId), column is bit vector
using Bitmap = std::unordered_map<std::uint32_t, EWAHBoolArray<>>;
/// Specifies registry to separate bitmaps by their quad keys
using BitmapRegistry = std::map<utymap::QuadKey, Bitmap, utymap::QuadKey::Comparator>;

class StringIndex::StringIndexImpl {
public:
  StringIndexImpl(const ElementAccessor &elementAccessor, const StringTable &stringTable) :
      elementAccessor_(elementAccessor),
      stringTable_(stringTable) {
  }

  void add(const Element &element, const QuadKey &quadKey, const std::uint32_t &order) {
    auto bitmap = registry_.find(quadKey);
    if (bitmap==registry_.end()) {
      bitmap = registry_.emplace(quadKey, Bitmap()).first;
    }

    for (const auto &token : tokenize(element)) {
      bitmap->second[token].set(order);
    }
  }

  void search(const StringIndex::Query &query, ElementVisitor &visitor) {
    // TODO
  }

 private:
   /// Gets tokenized query from query.
   TokenizedQuery tokenize(const StringIndex::Query &query) {
     TokenizedQuery tokenizedQuery;
     // TODO
     return tokenizedQuery;
   }

  /// Gets tokens from element.
  std::vector<std::uint32_t> tokenize(const Element &element) {
    std::vector<std::uint32_t> tokens;
    tokens.reserve(element.tags.size() * 2 + 4);
    
    for (const auto &tag : element.tags) {
      tokenize(stringTable_.getString(tag.key), tokens);
      tokenize(stringTable_.getString(tag.value), tokens);
    }

    return tokens;
  }

  /// Stores tokens received from str into result.
  void tokenize(const std::string &str, std::vector<std::uint32_t> &result) {
    boost::tokenizer<boost::char_separator<char>> tokens(str, separator);
    for (const auto& token : tokens) {
      result.push_back(stringTable_.getId(token));
    }
  }

  const ElementAccessor& elementAccessor_;
  const StringTable& stringTable_;
  BitmapRegistry registry_;
};

StringIndex::StringIndex(const ElementAccessor &elementAccessor,
                         const StringTable &stringTable) :
    pimpl_(utymap::utils::make_unique<StringIndex::StringIndexImpl>(elementAccessor, stringTable)) {
}

StringIndex::~StringIndex() {}

void StringIndex::add(const Element &element,
                      const utymap::QuadKey &quadKey,
                      const std::uint32_t order) {
  pimpl_->add(element, quadKey, order);
}

void StringIndex::search(const StringIndex::Query &query,
                         ElementVisitor &visitor) {
  pimpl_->search(query, visitor);
}
