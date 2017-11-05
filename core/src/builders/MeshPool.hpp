#ifndef BUILDERS_MESHPOOL_HPP_DEFINED
#define BUILDERS_MESHPOOL_HPP_DEFINED

#include "math/Mesh.hpp"

#include <map>
#include <mutex>

namespace utymap {
namespace builders {

/// Provides the way to use pool of meshes instead of
/// building them each time which might be expensive.
class MeshPool final {
  const std::size_t ThresholdSize = 1024 * 10;
 public:

  MeshPool() {}

  /// Disable copying to prevent accidental copy
  MeshPool(const MeshPool &) = delete;
  MeshPool &operator=(const MeshPool &) = delete;

  /// Gets small size mesh.
  utymap::math::Mesh getSmall(const std::string& name) {
    return getMesh(name, true);
  }

  /// Gets large size mesh.
  utymap::math::Mesh getLarge(const std::string& name) {
    return getMesh(name, false);
  }

  /// Returns mesh to pool.
  void release(utymap::math::Mesh&& mesh) {
    mesh.clear();
    std::lock_guard<std::mutex> lock(lock_);
    pool_.emplace(mesh.vertices.capacity(), std::move(mesh));
  }

 private:
  utymap::math::Mesh getMesh(const std::string &name, bool isLower) {
    std::lock_guard<std::mutex> lock(lock_);
    auto it = isLower
      ? pool_.lower_bound(0)
      : pool_.upper_bound(ThresholdSize);
    if (it == pool_.end())
      return utymap::math::Mesh(name);

    auto mesh = std::move(it->second);
    mesh.name = name;
    pool_.erase(it);
    return mesh;
  }

  std::map<std::size_t, utymap::math::Mesh> pool_;
  std::mutex lock_;
};

}
}

#endif // BUILDERS_MESHPOOL_HPP_DEFINED
