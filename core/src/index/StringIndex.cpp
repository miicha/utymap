#include "StringIndex.hpp"
#include "utils/CoreUtils.hpp"

#include<boost/tokenizer.hpp>

using namespace utymap::entities;
using namespace utymap::index;

namespace {
  using Bitset = StringIndex::Bitset;
  using Bitmap = StringIndex::Bitmap;
  /// Defines symbols considered as token delimiters
  const boost::char_separator<char> separator(":;!@#$%^&*(){}[],.?`\\/\"\'");

  /// Applies logical operation
  void applyOperation(const StringIndex::Ids &terms,
                      const Bitmap &bitmap,
                      Bitset &bitset,
                      const std::function<void(Bitset)> &op) {
    for (const auto term: terms) {
      auto array = bitmap.find(term);
      // no term defined for this quad key
      if (array == bitmap.end())
        return;
      op(array->second);
    }
  }
}

void StringIndex::add(const Element &element, const utymap::QuadKey &quadKey, const std::uint32_t order) {
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
        auto& bitmap = getBitmap(quadKey);
        Bitset bitset;

        applyOperation(orTerms, bitmap, bitset, [&](const Bitset &b) {
          bitset = b.logicalor(bitset);
        });

        applyOperation(andTerms, bitmap, bitset, [&](const Bitset &b) {
          if (bitset.sizeInBits() == 0) {
            bitset = b;
            return;
          };
          bitset = b.logicaland(bitset);
        });

        applyOperation(notTerms, bitmap, bitset, [&](const Bitset &b) {
          bitset = b.logicalxor(bitset).logicaland(bitset);
        });

        for (auto i = bitset.begin(); i != bitset.end(); ++i) {
          notify(quadKey, static_cast<std::uint32_t >(*i), visitor);
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
