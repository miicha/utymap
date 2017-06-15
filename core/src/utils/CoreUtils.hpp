#ifndef UTILS_COREUTILS_HPP_DEFINED
#define UTILS_COREUTILS_HPP_DEFINED

#include <chrono>
#include <string>
#include <sstream>
#include <memory>
#include <vector>

#include <boost/lexical_cast.hpp>

namespace utymap {
namespace utils {

template<typename T>
std::string toString(T t) {
  return boost::lexical_cast<std::string>(t);
}

inline std::string removeExtension(const std::string &filename) {
  auto lastdot = filename.find_last_of('.');
  return lastdot==std::string::npos
         ? filename
         : filename.substr(0, lastdot);
}

inline std::vector<std::string> splitBy(const char c, const std::string &str) {
  std::stringstream ss(str);
  std::vector<std::string> result;

  while (ss.good()) {
    std::string substr;
    getline(ss, substr, c);
    result.push_back(substr);
  }
  return result;
}

template<typename T>
T lexicalCast(const std::string &value) {
  return boost::lexical_cast<T>(value);
}

inline bool endsWith(std::string const &value, std::string const &ending) {
  if (ending.size() > value.size())
    return false;
  return std::equal(ending.rbegin(), ending.rend(), value.rbegin());
}

inline double parseDouble(const std::string &value, double defaultValue = 0) {
  try {
    return boost::lexical_cast<double>(value);
  }
  catch (const boost::bad_lexical_cast &) {
    return defaultValue;
  }
}

template<typename TimeT = std::chrono::milliseconds>
struct measure {
  template<typename F, typename ...Args>
  static typename TimeT::rep execution(F &&func, Args &&... args) {
    auto start = std::chrono::system_clock::now();
    std::forward<decltype(func)>(func)(std::forward<Args>(args)...);
    auto duration = std::chrono::duration_cast<TimeT>
        (std::chrono::system_clock::now() - start);
    return duration.count();
  }
};

/// Custom implementation of missing function in C++11
template<typename T, typename... Args>
std::unique_ptr<T> make_unique(Args &&... args) {
  return std::unique_ptr<T>(new T(std::forward<Args>(args)...));
}

}
}

#endif // UTILS_COREUTILS_HPP_DEFINED
