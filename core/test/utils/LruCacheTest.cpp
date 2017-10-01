#include "utils/LruCache.hpp"

#include <boost/test/unit_test.hpp>

using namespace utymap::utils;

namespace {
  struct CachedValue {
    std::string data;

    CachedValue(const std::string &value) :
      data(value) {}

    CachedValue(const CachedValue &&other) :
      data(std::move(other.data)) {}

    CachedValue(const CachedValue &) = delete;
    CachedValue &operator=(const CachedValue &) = delete;

    ~CachedValue() {
      data.clear();
    }
  };
}

BOOST_AUTO_TEST_SUITE(Utils_LruCache)

BOOST_AUTO_TEST_CASE(GivenLruCacheWithValue_WhenClear_IsEmpty) {
  LruCache<int, CachedValue> cache;
  cache.put(0, CachedValue("my string 0"));

  cache.clear();

  BOOST_CHECK_EQUAL(cache.size(), 0);
}

BOOST_AUTO_TEST_CASE(GivenLruCacheWithValue_WhenClear_ValueIsUsed) {
  LruCache<int, CachedValue> cache;
  cache.put(0, CachedValue("my string 0"));
  cache.put(1, CachedValue("my string 1"));

  auto value = cache.get(0);
  cache.clear();

  BOOST_CHECK_EQUAL(value->data, "my string 0");
}

BOOST_AUTO_TEST_SUITE_END()
