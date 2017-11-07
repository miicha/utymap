#include "builders/MeshPool.hpp"

#include <boost/test/unit_test.hpp>

using namespace utymap::builders;
using namespace utymap::math;

namespace {
  const std::size_t SmallSize = 4096 + 2;
  const std::size_t BigSize = 10240 * 2 + 1024;

  void addMesh(MeshPool &pool, std::size_t capacity) {

    Mesh mesh("");
    mesh.vertices.resize(capacity);
    mesh.triangles.resize(capacity);
    mesh.uvs.resize(capacity);

    pool.release(std::move(mesh));
  }
}

BOOST_AUTO_TEST_SUITE(Builders_MeshPool)

BOOST_AUTO_TEST_CASE(GivenEmptyPool_WhenGetSmall_ThenMeshReturned) {
  MeshPool pool;

  auto mesh = pool.getSmall("my_name");

  BOOST_CHECK_EQUAL(mesh.name, "my_name");
}

BOOST_AUTO_TEST_CASE(GivenEmptyPool_WhenGetLarge_ThenMeshReturned) {
  MeshPool pool;

  auto mesh = pool.getLarge("my_name");

  BOOST_CHECK_EQUAL(mesh.name, "my_name");
}

BOOST_AUTO_TEST_CASE(GivenPoolWithTwoObjects_WhenGetSmall_ThenSmallestReturned) {
  MeshPool pool;
  addMesh(pool, SmallSize);
  addMesh(pool, BigSize);

  auto mesh = pool.getSmall("my_name");

  BOOST_CHECK_EQUAL(mesh.vertices.capacity(), SmallSize);
}

BOOST_AUTO_TEST_CASE(GivenPoolWithTwoObjects_WhenGetLarge_ThenLargestReturned) {
  MeshPool pool;
  addMesh(pool, SmallSize);
  addMesh(pool, BigSize);

  auto mesh = pool.getLarge("my_name");

  BOOST_CHECK_EQUAL(mesh.vertices.capacity(), BigSize);
}

BOOST_AUTO_TEST_CASE(GivenEmptyPoolAfterRelease_WhenGetLarge_ThenMeshReturned) {
  MeshPool pool;
  Mesh mesh("");
  mesh.vertices.resize(BigSize);
  pool.release(std::move(mesh));

  auto result = pool.getLarge("my_name");

  BOOST_CHECK_EQUAL(result.vertices.capacity(), BigSize);
}

BOOST_AUTO_TEST_CASE(GivenEmptyPoolAfterRelease_WhenGetSmall_ThenMeshReturned) {
  MeshPool pool;
  Mesh mesh("");
  mesh.vertices.resize(SmallSize);
  pool.release(std::move(mesh));

  auto result = pool.getSmall("my_name");

  BOOST_CHECK_EQUAL(result.vertices.capacity(), SmallSize);
}

BOOST_AUTO_TEST_SUITE_END()
