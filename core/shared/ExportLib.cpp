#if _MSC_VER
#define EXPORT_API __declspec(dllexport)
#elif _GCC
#define EXPORT_API __attribute__((visibility("default")))
#else
#define EXPORT_API
#endif

#include "Application.hpp"

static Application *applicationPtr = nullptr;

// Specifies export functions.
// NOTE: see documentation comments in actual method implementation.
extern "C"
{

/************* Lifecycle API *****************/
void EXPORT_API disconnect() {
  delete applicationPtr;
}

void EXPORT_API connect(const char *indexPath, OnError *errorCallback) {
  try {
    applicationPtr = new Application(indexPath);
  } catch (std::exception &ex) {
    errorCallback(ex.what());
  }
}

/************* Configuration API *****************/
void EXPORT_API registerStylesheet(const char *path, OnNewDirectory *directoryCallback) {
  applicationPtr->getConfiguration().registerStylesheet(path, directoryCallback);
}

void EXPORT_API registerInMemoryStore(const char *key) {
  applicationPtr->getConfiguration().registerInMemoryStore(key);
}

void EXPORT_API registerPersistentStore(const char *key, const char *dataPath, OnNewDirectory *directoryCallback) {
  applicationPtr->getConfiguration().registerPersistentStore(key, dataPath, directoryCallback);
}

void EXPORT_API enableMeshCache(int enabled) {
  applicationPtr->getConfiguration().enableMeshCache(enabled);
}

/************* Storage API *****************/
void EXPORT_API addDataInRange(const char *key, const char *styleFile, const char *path, int startLod, int endLod,
                               OnError *errorCallback, utymap::CancellationToken *cancellationToken) {
  applicationPtr->getStorage().addToStore(key, styleFile, path, startLod, endLod, errorCallback, cancellationToken);
}

void EXPORT_API addDataInBoundingBox(const char *key, const char *styleFile, const char *path,
                                     double minLat, double minLon, double maxLat,  double maxLon, int startLod, int endLod,
                                     OnError *errorCallback, utymap::CancellationToken *cancellationToken) {
  applicationPtr->getStorage().addToStore(key, styleFile, path, minLat, minLon, maxLat, maxLon, startLod, endLod, errorCallback, cancellationToken);
}

void EXPORT_API addDataInQuadKey(const char *key, const char *styleFile, const char *path,
                                 int tileX, int tileY, int levelOfDetail,
                                 OnError *errorCallback, utymap::CancellationToken *cancellationToken) {
  applicationPtr->getStorage().addToStore(key, styleFile, path, tileX, tileY, levelOfDetail, errorCallback, cancellationToken);
}

void EXPORT_API addDataInElement(const char *key, const char *styleFile, std::uint64_t id, const double *vertices, int vertexLength,
                                 const char **tags, int tagLength, int startLod, int endLod,
                                 OnError *errorCallback, utymap::CancellationToken *cancellationToken) {
  applicationPtr->getStorage().addToStore(key, styleFile, id, vertices, vertexLength, tags, tagLength, startLod, endLod, errorCallback, cancellationToken);
}

bool EXPORT_API hasData(int tileX, int tileY, int levelOfDetail) {
  return applicationPtr->getStorage().hasData(tileX, tileY, levelOfDetail);
}

/************* Search API *****************/
void EXPORT_API getDataByText(int tag, const char *notTerms, const char *andTerms, const char *orTerms,
                              double minLatitude, double minLongitude, double maxLatitude, double maxLongitude, int startLod, int endLod,
                              OnElementLoaded *elementCallback, OnError *errorCallback, utymap::CancellationToken *cancellationToken) {
  applicationPtr->getSearch().getDataByText(tag, notTerms, andTerms, orTerms, minLatitude, minLongitude,maxLatitude, maxLongitude,
    startLod, endLod, elementCallback, errorCallback, cancellationToken);
}

void EXPORT_API getDataByQuadKey(int tag, const char *styleFile, int tileX, int tileY, int levelOfDetail, int eleDataType,
                                 OnMeshBuilt *meshCallback, OnElementLoaded *elementCallback, OnError *errorCallback,
                                 utymap::CancellationToken *cancellationToken) {
  applicationPtr->getSearch().getDataByQuadKey(tag, styleFile, tileX, tileY, levelOfDetail, 
    eleDataType, meshCallback, elementCallback, errorCallback, cancellationToken);
}

double EXPORT_API getElevationByQuadKey(int tileX, int tileY, int levelOfDetail, int eleDataType, double latitude, double longitude) {
  return applicationPtr->getSearch().getElevationByQuadKey(tileX, tileY, levelOfDetail, eleDataType, latitude, longitude);
}

}
