#ifndef MATH_POLYCLIP_HPP_DEFINED
#define MATH_POLYCLIP_HPP_DEFINED

#include "clipper/clipper.hpp"

namespace utymap {
namespace math {

using cInt = ClipperLib::cInt;
using IntPoint = ClipperLib::IntPoint;
using IntPath = ClipperLib::Path;
using IntPaths = ClipperLib::Paths;
using IntRect = ClipperLib::IntRect;

using PolyTree = ClipperLib::PolyTree;
using PolyNode = ClipperLib::PolyNode;

using Clipper = ClipperLib::ClipperEx;
using ClipperOffset = ClipperLib::ClipperOffset;

inline void addClip(Clipper &clipper, const IntPath &pg, bool isClosed = true) {
  clipper.AddPath(pg, ClipperLib::ptClip, isClosed);
}

inline void addSubject(Clipper &clipper, const IntPath &path, bool isClosed = true) {
  clipper.AddPath(path, ClipperLib::ptSubject, isClosed);
}

inline void addSubjects(Clipper &clipper, const IntPaths &paths, bool isClosed = true) {
  clipper.AddPaths(paths, ClipperLib::ptSubject, isClosed);
}

inline void addRoundOpenRound(ClipperOffset &offset, const IntPaths &paths) {
  offset.AddPaths(paths, ClipperLib::jtRound, ClipperLib::etOpenRound);
}

inline void addMiter(ClipperOffset &offset, const IntPath &path) {
  offset.AddPath(path, ClipperLib::JoinType::jtMiter, ClipperLib::EndType::etClosedPolygon);
}

inline void executeIntersection(Clipper &clipper, IntPaths &solution) {
  clipper.Execute(ClipperLib::ctIntersection, solution);
}

inline void executeIntersection(Clipper &clipper, PolyTree &solution) {
  clipper.Execute(ClipperLib::ctIntersection, solution);
}

inline void executeUnion(Clipper &clipper, IntPaths &solution) {
  clipper.Execute(ClipperLib::ctUnion, solution, ClipperLib::pftNonZero, ClipperLib::pftNonZero);
}

inline void executeDifference(Clipper &clipper, IntPaths &solution) {
  clipper.Execute(ClipperLib::ctDifference, solution, ClipperLib::pftNonZero, ClipperLib::pftNonZero);
}

inline void simplifyPolygons(IntPaths &polys) {
  ClipperLib::SimplifyPolygons(polys);
}

inline void cleanPolygons(IntPaths& polys) {
  ClipperLib::CleanPolygons(polys);
}

inline double getArea(const IntPath &poly) {
  return ClipperLib::Area(poly);
}

inline bool getOrientation(const IntPath &poly) {
  return ClipperLib::Orientation(poly);
}

}
}
#endif // MATH_POLYCLIP_HPP_DEFINED
