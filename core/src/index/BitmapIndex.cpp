#include "index/BitmapIndex.hpp"
#include "utils/CoreUtils.hpp"

#include<boost/tokenizer.hpp>

using namespace utymap::entities;
using namespace utymap::index;

namespace {
  using Bitset = BitmapIndex::Bitset;
  using Bitmap = BitmapIndex::Bitmap;
  /// Defines symbols considered as token delimiters
  const boost::char_separator<char> separator(" _:;!@#$%^&*(){}[],.?`\\/\"\'");

  /// Applies logical operation
  void applyOperation(const BitmapIndex::Ids &terms,
                      const Bitmap &bitmap,
                      const std::function<void(Bitset)> &op,
                      const std::function<bool()> &noOp) {
    for (const auto term: terms) {
      auto array = bitmap.find(term);
      // no term defined for this quad key
      if (array == bitmap.end()) {
        if (!noOp()) return;
      }
      else
        op(array->second);
    }
  }
}

void BitmapIndex::add(const Element &element, const utymap::QuadKey &quadKey, const std::uint32_t order) {
  auto& bitmap = getBitmap(quadKey);
  for (const auto &token : tokenize(element)) {
    bitmap[token].set(order);
  }
}

void BitmapIndex::search(const BitmapIndex::Query &query, ElementVisitor &visitor) {
  Ids andTerms, orTerms, notTerms;
  tokenize(query.andTerms, andTerms);
  tokenize(query.orTerms, orTerms);
  tokenize(query.notTerms, notTerms);

  for (int lod = query.range.start; lod <= query.range.end; ++lod) {
    utymap::utils::GeoUtils::visitTileRange(query.boundingBox, lod,
      [&](const QuadKey &quadKey, const BoundingBox&) {
        if (!hasData(quadKey)) return;

        auto& bitmap = getBitmap(quadKey);
        Bitset bitset;

        applyOperation(orTerms, bitmap, [&](const Bitset &b) {
          bitset = b.logicalor(bitset);
        }, [](){ return true; });

        applyOperation(andTerms, bitmap, [&](const Bitset &b) {
          if (bitset.sizeInBits() == 0) {
            bitset = b;
            return;
          }
          bitset = b.logicaland(bitset);
        }, [&]() {
          bitset.reset();
          return false;
        });

        applyOperation(notTerms, bitmap, [&](const Bitset &b) {
          bitset = b.logicalxor(bitset).logicaland(bitset);
        }, [](){ return true; });

        for (auto i = bitset.begin(); i != bitset.end(); ++i) {
          notify(quadKey, static_cast<std::uint32_t >(*i), visitor);
        }
      });
  }
}

std::vector<std::uint32_t> BitmapIndex::tokenize(const Element &element) {
  Ids tokens;
  tokens.reserve(element.tags.size() * 2 + 4);
  for (const auto &tag : element.tags) {
    tokenize(stringTable_.getString(tag.key), tokens);
    tokenize(stringTable_.getString(tag.value), tokens);
  }
  return tokens;
}

void BitmapIndex::tokenize(const std::string &source,
                           Ids &destination) {
  boost::tokenizer<boost::char_separator<char>> tokens(source, separator);
  for (const auto& token : tokens) {
    destination.push_back(stringTable_.getId(token));
  }
}

BitmapIndex::BitmapIndex(const StringTable &stringTable) :
    stringTable_(stringTable) {
}
