#include "index/MeshStream.hpp"

#include <cstdint>

using namespace utymap::index;
using namespace utymap::math;

namespace {
const double Precision = 1E7;

template<typename T>
void initData(std::istream &stream, std::vector<T> &data) {
  std::uint32_t size = 0;
  stream.read(reinterpret_cast<char *>(&size), sizeof(size));
  data.resize(size);
}

template<typename T>
void initData(std::ostream &stream, const std::vector<T> &data) {
  auto size = static_cast<std::uint32_t>(data.size());
  stream.write(reinterpret_cast<const char *>(&size), sizeof(size));
}

template<typename T>
T read(std::istream &stream) {
  T data;
  stream.read(reinterpret_cast<char *>(&data), sizeof(data));
  return data;
}

/// Restores double stored as signed 4-bytes integer.
template<>
double read<double>(std::istream &stream) {
  std::int32_t data;
  stream.read(reinterpret_cast<char *>(&data), sizeof(data));
  return data / Precision;
}

template<typename T>
void write(std::ostream &stream, const T &data) {
  stream.write(reinterpret_cast<const char *>(&data), sizeof(data));
}

/// Stores double as signed 4-bytes integer.
/// 
template<>
void write<double>(std::ostream &stream, const double &data) {
  auto simplified = static_cast<std::int32_t>(data * Precision);
  stream.write(reinterpret_cast<const char *>(&simplified), sizeof(simplified));
}

template<typename T>
std::ostream &operator<<(std::ostream &stream, const std::vector<T> &data) {
  initData(stream, data);
  for (const auto &item : data)
    write<T>(stream, item);
  return stream;
}

template<typename T>
std::istream &operator>>(std::istream &stream, std::vector<T> &data) {
  initData(stream, data);
  for (size_t i = 0; i < data.size(); ++i)
    data[i] = read<T>(stream);
  return stream;
}

void readVertices(std::istream &stream, std::vector<double> &data) {
  initData(stream, data);
  for (size_t i = 0; i < data.size(); ++i)
    // NOTE store elevation as float, not as packed double
    data[i] = (i + 1) % 3 == 0 ? read<float>(stream) : read<double>(stream);
}

void writeVertices(std::ostream &stream, const std::vector<double> &data) {
  initData(stream, data);
  for (size_t i = 0; i < data.size(); ++i) {
    if ((i + 1) % 3 == 0)
      write<float>(stream, static_cast<float>(data[i]));
    else
      write<double>(stream, data[i]);
  }
}

std::ostream &operator<<(std::ostream &stream, const Mesh &mesh) {
  stream << mesh.name.c_str() << '\0';
  writeVertices(stream, mesh.vertices);
  return stream << mesh.triangles << mesh.colors << mesh.uvs << mesh.uvMap;
}

std::istream &operator>>(std::istream &stream, Mesh &mesh) {
  std::getline(stream, mesh.name, '\0');
  readVertices(stream, mesh.vertices);
  return stream >> mesh.triangles >> mesh.colors >> mesh.uvs >> mesh.uvMap;
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
