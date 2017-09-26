#include "StringIndex.hpp"
#include "utils/CoreUtils.hpp"
#include "utils/GeoUtils.hpp"

#include<boost/tokenizer.hpp>

using namespace utymap::entities;
using namespace utymap::index;

namespace {
  /// Defines symbols considered as token delimeters
  const boost::char_separator<char> separator(":;!@#$%^&*(){}[],.?`\\/\"\'");
  /// Applies logical AND
  void applyLogicalAnd(const StringIndex::Ids &terms,
                       const StringIndex::Bitmap &bitmap,
                       StringIndex::Bitset &bitset) {
    for (const auto term: terms) {
      auto array = bitmap.find(term);
      // no term defined for this quad key
      if (array == bitmap.end()) return;

      if (bitset.sizeInBits() == 0) {
        bitset = array->second;
        continue;
      }
      bitset = bitset.logicaland(array->second);
    }
  }
}

void StringIndex::add(const Element &element, const QuadKey &quadKey, std::uint32_t order) {
  auto& bitmap = getBitmap(quadKey);
  for (const auto &token : tokenize(element)) {
    bitmap[token].set(order);
  }
}

void StringIndex::search(const StringIndex::Query &query, ElementVisitor &visitor) {
  Ids andTerms, orTerms, notTerms;
  tokenize(query.andTerms, andTerms);
  tokenize(query.orTerms, orTerms);
  tokenize(query.notTerms, notTerms);

  for (int lod = query.range.start; lod <= query.range.end; ++lod) {
    utymap::utils::GeoUtils::visitTileRange(query.boundingBox, lod,
      [&](const QuadKey &quadKey, const BoundingBox&) {
        // process elements for given quad key
        auto& bitmap = getBitmap(quadKey);
        // apply query
        Bitset bitset;
        applyLogicalAnd(andTerms, bitmap, bitset);
        // return matched elements to caller
        for (auto i = bitset.begin(); i != bitset.end(); ++i) {
          getElement(static_cast<std::uint32_t >(*i)).accept(visitor);
        }
      });
  }
}

std::vector<std::uint32_t> StringIndex::tokenize(const Element &element) {
  Ids tokens;
  tokens.reserve(element.tags.size() * 2 + 4);
  for (const auto &tag : element.tags) {
    tokenize(stringTable_.getString(tag.key), tokens);
    tokenize(stringTable_.getString(tag.value), tokens);
  }
  return tokens;
}

void StringIndex::tokenize(const std::vector<std::string> &source,
                           Ids &destination) {
  for (const auto &term : source) {
    tokenize(term, destination);
  }
}

void StringIndex::tokenize(const std::string &source,
                           Ids &destination) {
  boost::tokenizer<boost::char_separator<char>> tokens(source, separator);
  for (const auto& token : tokens) {
    destination.push_back(stringTable_.getId(token));
  }
}

StringIndex::StringIndex(const StringTable &stringTable) :
    stringTable_(stringTable) {
}
