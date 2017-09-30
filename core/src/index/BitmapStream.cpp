#include "index/BitmapStream.hpp"

using namespace utymap::index;

void BitmapStream::read(std::istream &in, BitmapIndex::Bitmap &bitmap) {
  std::uint32_t key;
  BitmapIndex::Bitset bitset;
  in.seekg(0, std::ios::beg);
  while (true) {
    if (!(in >> key)) break;
    bitset.read(in);
    bitmap.emplace(key, bitset);
  }
}

void BitmapStream::write(std::ostream &out, const BitmapIndex::Bitmap &bitmap) {
  for (const auto kv : bitmap) {
    out << kv.first;
    kv.second.write(out);
  }
  out.flush();
}
