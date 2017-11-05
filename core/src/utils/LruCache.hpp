#ifndef UTILS_LRUCACHE_HPP_DEFINED
#define UTILS_LRUCACHE_HPP_DEFINED

#include <stdexcept>
#include <list>
#include <map>
#include <memory>

namespace utymap {
namespace utils {

/// Implements Least Recently Used cache.
template<typename Key, typename Value, typename Comparator = std::less<Key>>
class LruCache final {
  typedef typename std::pair<Key, std::shared_ptr<Value>> KeyValuePair;
  typedef typename std::list<KeyValuePair>::iterator ListIterator;
 public:

  LruCache(size_t maxSize = 8) :
      maxSize_(maxSize) {}

  /// Puts value to cache.
  void put(const Key &key, Value &&value) {
    itemsList_.push_front(KeyValuePair(key, std::make_shared<Value>(std::move(value))));

    auto it = itemsMap_.find(key);
    if (it!=itemsMap_.end()) {
      itemsList_.erase(it->second);
      itemsMap_.erase(it);
    }
    itemsMap_[key] = itemsList_.begin();

    if (itemsMap_.size() > maxSize_) {
      auto last = itemsList_.end();
      --last;
      itemsMap_.erase(last->first);
      itemsList_.pop_back();
    }
  }

  /// Returns value from cache without promoting it higher.
  std::shared_ptr<Value> peek(const Key &key) {
    auto it = itemsMap_.find(key);
    if (it == itemsMap_.end()) throwException();

    return it->second->second;
  }

  /// Returns value from cache promoting it higher.
  std::shared_ptr<Value> get(const Key &key) {
    auto it = itemsMap_.find(key);
    if (it == itemsMap_.end()) throwException();

    itemsList_.splice(itemsList_.begin(), itemsList_, it->second);
    return it->second->second;
  }

  /// Promotes key in cache to higher level.
  void promote(const Key &key) {
    auto it = itemsMap_.find(key);
    if (it == itemsMap_.end()) throwException();

    itemsList_.splice(itemsList_.begin(), itemsList_, it->second);
  }

  /// Checks whether value is in cache.
  bool exists(const Key &key) const {
    return itemsMap_.find(key)!=itemsMap_.end();
  }

  /// Returns current cache size.
  size_t size() const {
    return itemsMap_.size();
  }

  /// Clears cache.
  void clear() {
    itemsList_.clear();
    itemsMap_.clear();
  }

 private:
   static void throwException() {
     throw std::range_error("There is no such key in cache.");
   }

  std::list<KeyValuePair> itemsList_;
  std::map<Key, ListIterator, Comparator> itemsMap_;
  size_t maxSize_;
};

}
}
#endif // UTILS_LRUCACHE_HPP_DEFINED
