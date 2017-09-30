#include "index/BitmapStream.hpp"

#include <boost/filesystem/operations.hpp>
#include <boost/test/unit_test.hpp>

#include <fstream>

using namespace utymap::index;

namespace {
  const std::string bitmapFilePath = "bitmap.bmp";

  struct IndexIndex_BitmapStreamFixture {

    IndexIndex_BitmapStreamFixture() : file() {
      using std::ios;
      file.open(bitmapFilePath, ios::in | ios::out | ios::binary | ios::app | ios::ate);
    }

    ~IndexIndex_BitmapStreamFixture() {
      file.close();
      boost::filesystem::remove(bitmapFilePath);
    }

    std::fstream file;
  };
}

BOOST_FIXTURE_TEST_SUITE(Index_BitmapStream, IndexIndex_BitmapStreamFixture)

BOOST_AUTO_TEST_CASE(GivenBitmapWithOneRow_WhenSaved_ThenCanBeReadBack) {
  StringIndex::Bitmap bitmap;
  StringIndex::Bitmap result;
  bitmap[0].set(3);
  bitmap[0].set(7);

  BitmapStream::write(file, bitmap);
  BitmapStream::read(file, result);

  BOOST_CHECK_EQUAL(result.size(), 1);
  auto resultArray = result[0].toArray();
  std::vector<std::uint32_t> expectedArray = { 3, 7 };
  BOOST_CHECK_EQUAL_COLLECTIONS(resultArray.begin(), resultArray.end(), expectedArray.begin(), expectedArray.end());
}

BOOST_AUTO_TEST_CASE(GivenBitmapWithTwoRows_WhenSaved_ThenCanBeReadBack) {
  StringIndex::Bitmap bitmap;
  StringIndex::Bitmap result;
  bitmap[0].set(3);
  bitmap[0].set(7);
  bitmap[3].set(1);
  bitmap[3].set(4);
  bitmap[3].set(17);

  BitmapStream::write(file, bitmap);
  BitmapStream::read(file, result);

  BOOST_CHECK_EQUAL(result.size(), 2);
  auto resultArray1 = result[0].toArray();
  auto resultArray2 = result[3].toArray();
  std::vector<std::uint32_t> expectedArray1 = { 3, 7 };
  std::vector<std::uint32_t> expectedArray2 = { 1, 4, 17 };
  BOOST_CHECK_EQUAL_COLLECTIONS(resultArray1.begin(), resultArray1.end(), expectedArray1.begin(), expectedArray1.end());
  BOOST_CHECK_EQUAL_COLLECTIONS(resultArray2.begin(), resultArray2.end(), expectedArray2.begin(), expectedArray2.end());
}

BOOST_AUTO_TEST_SUITE_END()