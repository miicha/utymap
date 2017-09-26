#include "StringIndex.hpp"
#include "utils/CoreUtils.hpp"
#include "utils/GeoUtils.hpp"

#include<boost/tokenizer.hpp>

using namespace utymap::entities;
using namespace utymap::index;

namespace {
  /// Defines symbols considered as token delimeters
  const boost::char_separator<char> separator(":;!@#$%^&*(){}[],.?`\\/\"\'");
  /// Query with string ids instead of actual content.
  struct TokenizedQuery {
    std::vector<std::uint32_t> andTerms;
    std::vector<std::uint32_t> orTerms;
    std::vector<std::uint32_t> notTerms;
  };
}

void StringIndex::add(const Element &element, const QuadKey &quadKey, std::uint32_t order) {
  auto& bitmap = getBitmap(quadKey);
  for (const auto &token : tokenize(element)) {
    bitmap[token].set(order);
  }
}

void StringIndex::search(const StringIndex::Query &query, ElementVisitor &visitor) {
  TokenizedQuery tokenizedQuery;
  tokenize(query.andTerms, tokenizedQuery.andTerms);
  tokenize(query.orTerms, tokenizedQuery.orTerms);
  tokenize(query.notTerms, tokenizedQuery.notTerms);

  for (int lod = query.range.start; lod <= query.range.end; ++lod) {
    utymap::utils::GeoUtils::visitTileRange(query.boundingBox, lod,
      [&](const QuadKey &quadKey, const BoundingBox&) {
        auto& bitmap = getBitmap(quadKey);

        //for (auto i = bitmap.begin(); i != bitmap.end(); ++i) {
         // getElement(i->first).accept(visitor);
        //}
      });
  }
}

std::vector<std::uint32_t> StringIndex::tokenize(const Element &element) {
  std::vector<std::uint32_t> tokens;
  tokens.reserve(element.tags.size() * 2 + 4);
  for (const auto &tag : element.tags) {
    tokenize(stringTable_.getString(tag.key), tokens);
    tokenize(stringTable_.getString(tag.value), tokens);
  }
  return tokens;
}

void StringIndex::tokenize(const std::vector<std::string> &source,
                           std::vector<std::uint32_t> &destination) {
  for (const auto &term : source) {
    tokenize(term, destination);
  }
}

void StringIndex::tokenize(const std::string &source,
                           std::vector<std::uint32_t> &destination) {
  boost::tokenizer<boost::char_separator<char>> tokens(source, separator);
  for (const auto& token : tokens) {
    destination.push_back(stringTable_.getId(token));
  }
}

StringIndex::StringIndex(const StringTable &stringTable) :
    stringTable_(stringTable) {
}
