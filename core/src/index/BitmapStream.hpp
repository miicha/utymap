#ifndef INDEX_BITMAPSTREAM_HPP_DEFINED
#define INDEX_BITMAPSTREAM_HPP_DEFINED

#include <iostream>
#include "StringIndex.hpp"

namespace utymap {
namespace index {

/// Provides the way to store in stream and restore from it back.
class BitmapStream final {
  public:
  /// Reads bitmap from stream.
  static void read(std::istream &in,
                    StringIndex::Bitmap &bitmap);

  /// Writes bitmap to stream.
  static void write(std::ostream &out,
                    const StringIndex::Bitmap &bitmap);
};

}
}

#endif // INDEX_BITMAPSTREAM_HPP_DEFINED
