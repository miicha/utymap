#include "index/MeshStream.hpp"

#include <cstdint>

using namespace utymap::index;
using namespace utymap::math;

namespace {
template<typename T>
std::ostream &operator<<(std::ostream &stream, const std::vector<T> &data) {
  auto size = static_cast<std::uint32_t>(data.size());
  stream.write(reinterpret_cast<const char *>(&size), sizeof(size));
  for (const auto &item : data)
    stream.write(reinterpret_cast<const char *>(&item), sizeof(item));
  return stream;
}

template<typename T>
std::istream &operator>>(std::istream &stream, std::vector<T> &data) {
  std::uint32_t size = 0;
  stream.read(reinterpret_cast<char *>(&size), sizeof(size));
  data.resize(size);
  for (size_t i = 0; i < size; ++i)
    stream.read(reinterpret_cast<char *>(&data[i]), sizeof(data[i]));
  return stream;
}

std::ostream &operator<<(std::ostream &stream, const Mesh &mesh) {
  return stream << mesh.name.c_str() << '\0' << mesh.vertices << mesh.triangles
                << mesh.colors << mesh.uvs << mesh.uvMap;
}

std::istream &operator>>(std::istream &stream, Mesh &mesh) {
  std::getline(stream, mesh.name, '\0');
  return stream >> mesh.vertices >> mesh.triangles
                >> mesh.colors >> mesh.uvs >> mesh.uvMap;
}
}

Mesh MeshStream::read(std::istream &stream) {
  Mesh mesh("");
  stream >> mesh;
  return std::move(mesh);
}

void MeshStream::write(std::ostream &stream, const Mesh &mesh) {
  stream << mesh;
}
