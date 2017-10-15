#include "index/BitmapStream.hpp"

using namespace utymap::index;

void BitmapStream::read(std::istream &in, BitmapIndex::Bitmap &bitmap) {
  std::uint32_t key;
  BitmapIndex::Bitset bitset;
  in.seekg(0, std::ios::beg);
  do
  {
    in.read(reinterpret_cast<char *>(&key), sizeof(key));
    bitset.read(in);
    bitmap.emplace(key, bitset);
  } while (in);
}

void BitmapStream::write(std::ostream &out, const BitmapIndex::Bitmap &bitmap) {
  for (const auto kv : bitmap) {
    out.write(reinterpret_cast<const char *>(&kv.first), sizeof(kv.first));
    kv.second.write(out);
  }
  out.flush();
}
